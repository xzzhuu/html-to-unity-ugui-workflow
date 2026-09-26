"""Shared contract predicts source objects and converter-generated helpers."""
import re
import xml.etree.ElementTree as ET

def serialize_xhtml(document):
    """Keep XML validity while avoiding HTML's non-void self-closing trap."""
    ET.register_namespace('','http://www.w3.org/1999/xhtml')
    text=ET.tostring(document,encoding='unicode',short_empty_elements=False)
    return re.sub(r'<(img|input|br|hr|meta|link)(\b[^>]*)></\1>',r'<\1\2 />',text,flags=re.I)
DIRECTORIES=('inputs/references','design/layout','design/review','html/assets/icons','html/assets/sprites','html/assets/textures')
PANEL_NAME=r'[A-Z][A-Za-z0-9]*Panel'
UI_NAME=r'(?:Group|Txt|Img|RawImg|Btn|Input|Scroll|Toggle|Slider|Item)_[A-Z][A-Za-z0-9]*(?:_[0-9]{2,})?'
NATIVE={'Container':'RectTransform','Text':'TextMeshProUGUI','Image':'Image','RawImage':'RawImage','Button':'Button','InputField':'TMP_InputField','ScrollView':'ScrollRect','Toggle':'Toggle','Slider':'Slider'}
HELPERS={
 'InputField':[('Group_TextViewport','RectTransform','裁剪输入文本视口'),('Group_TextViewport/Txt_Placeholder','TextMeshProUGUI','显示输入提示'),('Group_TextViewport/Txt_Value','TextMeshProUGUI','显示当前输入文本')],
 'ScrollView':[('Group_Content','RectTransform','承载滚动内容和动态条目')],
 'Toggle':[('Img_Checkmark','Image','显示勾选状态')],
 'Slider':[('Img_Fill','Image','显示填充进度'),('Group_HandleArea','RectTransform','限制滑块移动范围'),('Group_HandleArea/Img_Handle','Image','显示拖动滑块')]}

def require(condition,message):
    if not condition: raise ValueError(message)

def local_tag(node):
    return node.tag.rsplit('}',1)[-1]

def project_elements(root):
    records,templates=[],[]
    name=root.get('data-panel-name') or root.get('id')
    require(re.fullmatch(PANEL_NAME,name or '') is not None,'Root needs a semantic *Panel name')
    def add(path,component,node=None,template=None,purpose=None):
        record=dict(elementId=path,unityName=path.rsplit('/',1)[-1],parentId=path.rsplit('/',1)[0] if '/' in path else None,
                    hierarchyPath=path,uguiComponent=component,sourceType=node.get('data-ugui') if node is not None else None,
                    htmlId=node.get('id') if node is not None else None,generated=node is None,template=template,
                    bindingKey=node.get('data-binding') if node is not None else None)
        if purpose: record['purpose']=purpose
        records.append(record)
    def walk(node,parent,template=None,is_root=False):
        if local_tag(node)=='template':
            key=node.get('data-prefab','')
            require(re.fullmatch('[A-Za-z0-9_-]+',key) is not None,'Invalid template key')
            require(template is None and len(node)==1,'Templates need one root and cannot nest')
            item=node[0]
            require((item.get('data-ui-name') or '').startswith('Item_'),'Template root needs Item_* name')
            templates.append(dict(name=key,itemRootId=parent+'/'+item.get('data-ui-name'),parentId=parent,bindingScope=key))
            walk(item,parent,key)
            return
        kind=node.get('data-ugui')
        require(kind in NATIVE,'Every source component needs supported data-ugui')
        label=name if is_root else node.get('data-ui-name','')
        require(is_root or re.fullmatch(UI_NAME,label) is not None,'Invalid/missing semantic UI name: '+label)
        path=label if is_root else parent+'/'+label
        add(path,NATIVE[kind],node,template)
        helpers=list(HELPERS.get(kind,[]))
        if kind=='Button' and ''.join([node.text or '']+[child.tail or '' for child in node]).strip():
            helpers.append(('Txt_Label','TextMeshProUGUI','显示按钮操作文字'))
        for relative,component,purpose in helpers: add(path+'/'+relative,component,template=template,purpose=purpose)
        require(not (len(node) and (kind in ('Text','RawImage','InputField','Slider') or node.get('data-ignore-children')=='true')),'Converter ignores source children: '+path)
        for child in node: walk(child,path+'/Group_Content' if kind=='ScrollView' else path,template)
    walk(root,None,is_root=True)
    paths=[item['elementId'].casefold() for item in records]
    require(len(paths)==len(set(paths)),'Duplicate object path including generated helpers')
    keys=[item['name'].casefold() for item in templates]
    items=[item['itemRootId'].rsplit('/',1)[-1].casefold() for item in templates]
    require(len(keys)==len(set(keys)) and len(items)==len(set(items)),'Template key or Item prefab filename collision')
    bindings,ids=set(),set()
    for item in records:
        if not item['generated']:
            identifier=item['htmlId']
            require(identifier and identifier not in ids,'Source ID missing/duplicate')
            ids.add(identifier)
        if item['bindingKey']:
            key=(item['template'],item['bindingKey'].casefold())
            require(key not in bindings,'Duplicate binding within one scope')
            bindings.add(key)
    return records,templates
