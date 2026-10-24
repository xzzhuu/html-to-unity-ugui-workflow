"""Build six portable XHTML panels and semantic manifests from approved design."""
from pathlib import Path
import json,sys,shutil,hashlib
import xml.etree.ElementTree as ET
from render_previews import ROOT,C
sys.path.insert(0,str(ROOT.parents[1]/'Skills/ui-panel-design-to-html/scripts'))
from pipeline_contract import project_elements,serialize_xhtml
from validate_directory import definition_digest

PLAN=json.loads((ROOT/'design-review/sprite-export-plan-v01.json').read_text(encoding='utf-8'))
ASSETS={x['name']:x for x in PLAN['assets']}
def write(path,data):path.write_text(json.dumps(data,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
def sha(path):return hashlib.sha256(path.read_bytes()).hexdigest()
from game_workflow import read, render_tokens, font_size
STYLE=read(ROOT/'contracts/style.json')
BASECSS=render_tokens((ROOT/'design-tools/templates/base.css').read_text(encoding='utf-8'), STYLE)

class Panel:
 def __init__(self,name,title,number,modal=False):
  self.name,self.title=name,title;self.dir=ROOT/'Panels'/name;self.used=set();self.specs={};self.template_specs={}
  self.doc=ET.Element('html',{'xmlns':'http://www.w3.org/1999/xhtml','lang':'zh-CN'})
  head=ET.SubElement(self.doc,'head');ET.SubElement(head,'meta',{'charset':'utf-8'})
  ET.SubElement(head,'meta',{'name':'viewport','content':'width=device-width,initial-scale=1'})
  ET.SubElement(head,'title').text='星港拾荒者 · '+title
  ET.SubElement(head,'link',{'rel':'stylesheet','href':name+'.css'})
  body=ET.SubElement(self.doc,'body')
  self.root=ET.SubElement(body,'div',{'id':name,'data-root':'true','data-ugui':'Container','data-panel-name':name,
    'data-asset-prefix':f'Assets/Art/UI/Sprite/{name}/','data-output-folder':f'Assets/Art/UIPrefab/{name}',
    'data-project-name':ROOT.name,'data-project-directory':'../../..',
    'data-staging-root':f'Assets/HtmlToUGUIStage/{ROOT.name}/{name}',
    'class':'node','style':'left:0px;top:0px;width:1920px;height:1080px;background-color:#00000000;','data-purpose':'完整1920×1080面板画布，透明根背景复用游戏背景'})
  self.positions={id(self.root):(0,0)}
  if modal:
   n=self.node('Btn_ModalBlocker','Button',0,0,1920,1080,bg='#050C14B8',extra={'data-interactable':'false','disabled':'disabled'})
   n.set('data-purpose','全屏半透明遮罩，原生Button图形拦截底层指针；自身不执行操作')
   self.specs[n.get('id')]['purpose']=n.get('data-purpose')
  self.surface('PanelBody',600 if modal else 120,280 if modal else 80,720 if modal else 1680,520 if modal else 920,'base',r=STYLE['geometry']['panelRadius'])
  self.label('Feedback','',420,970,1000,28,20,'amber',bind='Feedback.Message')
  ET.SubElement(body,'script',{'src':name+'.preview.js'})

 def node(self,name,kind,x,y,w,h,*,text=None,color=None,bg=None,size=24,bold=False,bind=None,parent=None,tag=None,extra=None,klass=''):
  parent=parent if parent is not None else self.root
  px,py=self.positions.get(id(parent),(0,0))
  size=font_size(size,STYLE)
  tags={'Text':'span','Button':'button','InputField':'input','Toggle':'input','Slider':'input'}
  attrs={'id':self.name+'__'+name,'data-ui-name':name,'data-ugui':kind,'class':'node '+klass,
   'style':f'left:{x-px}px;top:{y-py}px;width:{w}px;height:{h}px;font-size:{size}px;color:{C.get(color,color) if color else C["text"]};font-weight:{700 if bold else 400};',
   'data-purpose':(('显示'+text) if kind=='Text' and text else name.split('_',1)[-1]+'，用于'+self.title)}
  if bind:attrs['data-binding']=bind
  if bg:attrs['style']+='background-color:'+C.get(bg,bg)+';'
  if kind=='Text':attrs['class']+=' text';attrs['data-wrap']='true';attrs['data-overflow']='ellipsis'
  if kind in ['Button','InputField','Toggle','Slider']:
   attrs.update({'data-normal-color':'#FFFFFF','data-highlighted-color':'#FFFFFF','data-pressed-color':'#DCDCDC','data-selected-color':'#FFFFFF','data-disabled-color':'#707070'})
  if extra:attrs.update(extra)
  n=ET.SubElement(parent,tag or tags.get(kind,'div'),attrs)
  if text:n.text=text
  self.positions[id(n)]=(x,y)
  self.specs[n.get('id')]={'pictureRegion':[x,y,w,h],'purpose':attrs['data-purpose'],'dataSource':bind,'states':['normal','disabled'] if kind in ['Button','InputField','Slider','Toggle'] else ['normal']}
  return n

 def sprite(self,n,file,border=None):
  self.used.add(file);a=ASSETS[file];border=border if border is not None else a['border']
  n.set('data-sprite',f'Assets/Art/UI/Sprite/{self.name}/shared/{file}')
  if any(border):n.set('data-nine-slice','true');n.set('data-border',','.join(map(str,border)))

 def label(self,name,text,x,y,w,h=40,size=24,color='text',bold=False,bind=None,parent=None):
  n=self.node('Txt_'+name,'Text',x,y,w,h,text=text,size=size,color=color,bold=bold,bind=bind,parent=parent)
  if name in ['InventoryName','MissionCardName']:
   n.set('data-wrap','false');n.set('style',n.get('style')+'white-space:nowrap;text-overflow:ellipsis;')
  return n
 def surface(self,name,x,y,w,h,color='card',r=12,parent=None):
  n=self.node('Group_'+name,'Image',x,y,w,h,bg=color,parent=parent);self.sprite(n,'Img_RoundedSurface.png')
  n.set('style',n.get('style')+f'border-radius:{r}px;');return n
 def button(self,name,text,x,y,w,h=60,primary=False,danger=False,bind=None,parent=None):
  n=self.node('Btn_'+name,'Button',x,y,w,h,text=text,size=24,color='ink' if primary else ('red' if danger else 'text'),
    bg='teal' if primary else ('#50343C' if danger else 'card'),bold=True,bind=bind or 'Action.'+name,parent=parent,klass='control')
  self.sprite(n,'Img_Control_Background.png');n.set('style',n.get('style')+f"border-radius:{STYLE['geometry']['buttonRadius']}px;text-align:center;");return n
 def icon(self,name,file,x,y,size=56,color='teal',parent=None,bind=None,seat=True):
  if any(k in file for k in ['Ore','Alloy','Chip','Credits']):color='amber'
  if seat:self.surface(name+'Seat',x,y,size,size,'deep',10,parent)
  n=self.node('Img_'+name,'Image',x,y,size,size,bg=color,parent=parent,bind=bind,klass='icon')
  self.sprite(n,file);n.set('style',n.get('style')+f'mask-image:url(assets/shared/{file});-webkit-mask-image:url(assets/shared/{file});');return n
 def line(self,name,x,y,w):return self.node('Img_'+name,'Image',x,y,w,2,bg='line')
 def header(self,subtitle,resources=True):
  self.label('Title',self.title,160,112,700,60,40,bold=True)
  self.label('Subtitle',subtitle,160,170,1100,35,20,'muted',bind='Page.Subtitle')
  if resources:
   for x,key,label,val in [(980,'Credits','信用点','2,400'),(1220,'Energy','能源','100 / 100'),(1480,'LoadoutWeight','携行重量','7.0 / 20.0 kg')]:
    self.label('Resource'+key+'Label',label,x,115,240,32,20,'muted')
    self.label('Resource'+key,val,x,149,270,50,28,'teal' if x==1480 else 'text',True,'Player.'+key)
  self.line('HeaderDivider',160,218,1600)
 def input(self,name,x,y,w,value='',placeholder='',bind=None):
  n=self.node('Input_'+name,'InputField',x,y,w,64,bg='card',bind=bind or 'Input.'+name,klass='input',extra={'type':'text','value':value,'placeholder':placeholder,'data-placeholder':placeholder})
  self.sprite(n,'Img_Control_Background.png');n.set('style',n.get('style')+f"border-radius:{STYLE['geometry']['inputRadius']}px;");return n
 def slider(self,name,x,y,w,value,maxval=100,minval=0,bind=None):
  n=self.node('Slider_'+name,'Slider',x,y,w,40,bind=bind or 'Slider.'+name,klass='slider',extra={
   'type':'range','min':str(minval),'max':str(maxval),'step':'1','value':str(value),'data-min':str(minval),'data-max':str(maxval),'data-value':str(value),'data-whole-numbers':'true'})
  self.sprite(n,'Sld_Value_Bar.png');n.set('style',n.get('style')+f'--progress:{(value-minval)/(maxval-minval)*100}%;');return n
 def scroll(self,name,x,y,w,h,bind):
  n=self.node('Scroll_'+name,'ScrollView',x,y,w,h,bind=bind,klass='scroll',extra={'data-content-height':str(h)})
  n.set('style',n.get('style')+f'content-height:{h}px;');return n
 def template(self,scroll,key,name,w,h,step):
  t=ET.SubElement(scroll,'template',{'data-prefab':key})
  root=self.node('Item_'+name,'Button' if key in ['MissionEntry','InventoryEntry'] else 'Container',0,0,w,h,parent=t,bind='Mission.Select' if key=='MissionEntry' else ('Item.Select' if key=='InventoryEntry' else None),bg='deep' if key in ['MissionEntry','InventoryEntry'] else None,klass='control' if key in ['MissionEntry','InventoryEntry'] else '')
  if root.get('data-ugui')=='Button':self.sprite(root,'Img_RoundedSurface.png');root.set('style',root.get('style')+'border-radius:10px;')
  self.template_specs[key]=dict(itemSize=[w,h],stepPx=step,dataSource={'MissionEntry':'missions','InventoryEntry':'inventory filtered by category and search','LoadoutEntry':'planned loadout','RewardEntry':'active expedition rewards'}[key],states=['normal','selected','empty','locked'])
  return root

 def group_hierarchy(self):
  """Preserve canvas rectangles while assigning functional ownership."""
  if self.name=='GameSettingsPanel':
   for name,rect in [('CaptainNameRow',(340,294,1240,106)),('SoundRow',(340,414,1240,88)),('VolumeRow',(340,526,1240,96)),('QualityRow',(340,646,1240,96))]:
    self.node('Group_'+name,'Container',*rect)
  if self.name!='ActionConfirmPanel':
   self.node('Group_Header','Container',160,112,1600,108)
   self.node('Group_Footer','Container',160,890,1600,108)
  direct=list(self.root)
  groups=[n for n in direct if n.get('data-ui-name','').startswith('Group_') and n.get('data-ui-name')!='Group_IconCatalog']
  def rect(n):return self.specs[n.get('id')]['pictureRegion']
  def area(n):
   r=rect(n);return r[2]*r[3]
  for n in direct:
   name=n.get('data-ui-name','')
   if name in ['Group_IconCatalog','Btn_ModalBlocker']:continue
   x,y,w,h=rect(n)
   candidates=[]
   for g in groups:
    if g is n or area(g)<=w*h:continue
    gx,gy,gw,gh=rect(g)
    if gx<=x and gy<=y and x+w<=gx+gw and y+h<=gy+gh:candidates.append(g)
   seat=next((g for g in groups if g.get('data-ui-name')==name.replace('Img_','Group_',1)+'Seat'),None) if name.startswith('Img_') else None
   parent=seat if seat is not None else (min(candidates,key=area) if candidates else self.root)
   if parent is self.root:continue
   self.root.remove(n);parent.append(n)
   px,py=self.positions[id(parent)]
   n.set('style',';'.join(f'left:{x-px}px' if s.startswith('left:') else f'top:{y-py}px' if s.startswith('top:') else s for s in n.get('style').split(';')))
  for n in self.root.iter():
   if n.get('data-binding')=='Item.DetailRarity':
    parent=next(p for p in self.root.iter() if n in list(p))
    n.set('data-parent-canvas-y',str(self.positions[id(parent)][1]))

 def finish(self):
  # Every reference is local to its panel workspace, including the shared runtime copy.
  html=self.dir/'html'
  dynamic={
   'StarportHomePanel':['Img_Item'+k+'.png' for k in ['Medkit','Battery','Ration','Beacon']],
   'ExpeditionSelectPanel':['Img_Item'+k+'.png' for k in ['Ore','Alloy','Circuit','Chip']],
   'SupplyInventoryPanel':['Img_Item'+k+'.png' for k in ['Medkit','Battery','Ration','Beacon','Ore','Alloy','Circuit','Chip']]+['Img_Circle.png'],
   'GameSettingsPanel':['Img_Circle.png'],
   'ExpeditionResultPanel':['Img_Item'+k+'.png' for k in ['Ore','Alloy','Circuit','Chip']]}.get(self.name,[])
  # Explicit, fully transparent native Images preserve dynamic Sprite references.
  # They are resource declarations, not visible decoration or browser-only assets.
  extras=set(dynamic)-self.used
  if extras:
   catalog=self.node('Group_IconCatalog','Container',0,0,32,32)
   for file in sorted(extras):
    n=self.node(file.removesuffix('.png'),'Image',0,0,32,32,bg='#FFFFFF00',parent=catalog,bind='Source.'+file.removesuffix('.png'))
    self.sprite(n,file);n.set('data-purpose','注册动态Sprite引用，透明不绘制；供接入游戏的数据绑定使用')
    self.specs[n.get('id')]['purpose']=n.get('data-purpose')
  self.group_hierarchy()
  html.joinpath(self.name+'.html').write_text('<!DOCTYPE html>\n'+serialize_xhtml(self.doc)+'\n',encoding='utf-8')
  html.joinpath(self.name+'.css').write_text(BASECSS,encoding='utf-8')
  shutil.copyfile(ROOT/'design-tools/preview_runtime.js',html/(self.name+'.preview.js'))
  elements,templates=project_elements(self.root)
  for e in elements:
   spec=self.specs.get(e['htmlId'],{})
   e.update(purpose=spec.get('purpose',e.get('purpose','原生控件辅助节点')+'，所属'+str(e['parentId'] or self.name)),
    dataSource=spec.get('dataSource'),pictureRegion=spec.get('pictureRegion'),states=spec.get('states',['normal']))
  for t in templates:t.update(self.template_specs[t['name']])
  write(html/'elements.json',dict(schemaVersion=1,panelName=self.name,status='draft-unapproved',templates=templates,elements=elements))
  group_path=self.dir/'design/element-groups.json';groups=json.loads(group_path.read_text(encoding='utf-8'))
  # Replace proposed design paths with exact implementation hierarchy paths.
  def owners(group):
   kind=group['componentType'];key=group['groupId'];out=[]
   for e in elements:
    if e['generated']:continue
    node=next(n for n in self.root.iter() if n.get('id')==e['htmlId'])
    file=node.get('data-sprite','').rsplit('/',1)[-1]
    include=(kind=='Text' and e['sourceType']=='Text') or (kind in ['Button','InputField','Toggle','Slider','ScrollView'] and e['sourceType']==kind)
    if key=='Img_Surface_Sheet':include=file=='Img_RoundedSurface.png'
    if key=='Img_Item_Sheet':include=file.startswith('Img_Item')
    if key=='Img_Utility_Sheet':include=file in ['Img_'+k+'.png' for k in ['NavigateBack','Close','Search','Confirm','Risk','Lock','Energy','Weight','Credits']]
    if include:out.append(e['hierarchyPath'])
   return out
  groups['groups']=[dict(g,usedBy=owners(g)) for g in groups['groups'] if owners(g)]
  sprites=[];group_sprites=[]
  for name in sorted(self.used):
   a=ASSETS[name];dest=html/'assets/shared'/name;dest.parent.mkdir(parents=True,exist_ok=True)
   shutil.copyfile(ROOT/'shared-assets'/name,dest)
   originals=self.dir/'inputs/art-source';originals.mkdir(exist_ok=True)
   shutil.copyfile(ROOT/'shared-art-source'/name.replace('.png','.svg'),originals/name.replace('.png','.svg'))
   referenced=any(n.get('data-sprite','').endswith('/'+name) for n in self.root.iter())
   sprites.append(dict(source='assets/shared/'+name,path=f'Assets/Art/UI/Sprite/{self.name}/shared/'+name,role='Sprite',border=a['border'],pixelSize=a['pixelSize'],hasAlpha=True,purpose=a['purpose'],usage='source' if referenced else 'preview-state'))
   group_sprites.append(dict(source='html/assets/shared/'+name,componentType=a['componentType'],pixelSize=a['pixelSize'],originalPixelSize=a['pixelSize'],hasAlpha=True,border=a['border'],tintable=True,purpose=a['purpose']))
  # Remove only this generator's known, now-unused drafts; no user files/directories.
  for name in ASSETS:
   obsolete=html/'assets/shared'/name
   if name not in self.used and obsolete.is_file():obsolete.unlink()
  groups['sprites']=group_sprites
  for group in groups['groups']:
   group['assets']=['html/assets/shared/'+a['name'] for a in PLAN['assets'] if group['groupId'] in a['groups'] and a['name'] in self.used]
  write(group_path,groups);write(html/'sprites.json',dict(schemaVersion=1,panelName=self.name,sprites=sprites))
  approvals=[]
  for group in groups['groups']:
   sheet_record=next(r for r in json.loads((ROOT/'design-review/element-groups-index-v01.json').read_text(encoding='utf-8'))['groups'] if r['groupId']==group['groupId'])
   if sha(self.dir/group['sheet'])!=sheet_record['sha256']:raise ValueError('Approved element sheet changed: '+group['groupId'])
   approvals.append(dict(groupId=group['groupId'],userApproval='确定',imageSha256=sha(self.dir/group['sheet']),
     definitionSha256=definition_digest(group,{s['source']:s for s in group_sprites}),filesSha256={s:sha(self.dir/s) for s in group['assets']},
     scope='用户确认九组元素v01；据同一矢量造型导出PNG，切片中心保留修正见素材计划。'))
  write(self.dir/'design/review/element-groups-approval.json',dict(status='approved',groups=approvals))
  print(self.name,len(elements),'nodes',len(templates),'templates',len(sprites),'sprites')

def home():
 p=Panel('StarportHomePanel','星港基地','01');p.header('指挥终端 / 林岚队长，欢迎返回星港')
 p.surface('Main',160,250,960,600,'deep',16);p.surface('OnlineBadge',200,286,276,36,'deep',8)
 p.label('Online','星港在线 · 系统就绪',214,289,340,32,20,'teal')
 p.label('GameName','星港拾荒者',200,364,880,112,76,bold=True)
 p.label('GameEnglish','STARPORT / SALVAGE DIVISION',205,466,840,40,24,'teal',True)
 p.label('Welcome','整备补给，回收失落资源。',205,538,820,55,30)
 p.label('Introduction','选择目的地，规划携行，让每一次远征都有收获。',205,592,840,45,24,'muted')
 p.button('OpenExpeditions','选择远征 →',205,714,360,76,True);p.button('OpenInventory','仓库整备',589,714,300,76)
 p.surface('ExpeditionStatus',1152,250,608,600,'card',16);p.label('ControlTitle','远征控制',1188,286,340,45,28,bold=True)
 p.label('TaskState','待命',1580,289,140,36,20,'teal',bind='Task.State')
 p.label('TaskHeadline','尚未派遣远征队',1188,364,536,60,32,bold=True,bind='Task.Headline')
 p.label('TaskDescription','从低风险货运站开始第一趟回收。',1188,424,536,58,24,'muted',bind='Task.Description')
 p.line('StatusDivider',1188,490,536);p.label('LoadoutTitle','已准备补给',1188,524,480,36,22,'muted')
 for i,(kind,name) in enumerate([('Medkit','急救包'),('Battery','能源电池')]):
  y=574+i*78;p.icon('Summary'+kind,'Img_Item'+kind+'.png',1188,y);p.label('SummaryName'+kind,name,1260,y+6,320,42,26)
  p.label('SummaryQuantity'+kind,'× 0',1636,y+6,92,42,26,'teal',bind='Loadout.'+kind)
 p.label('LoadoutSummary','携行0.0kg / 上限20.0kg',1188,752,536,40,22,'muted',bind='Loadout.Summary')
 p.button('ViewResult','查看结算',1188,754,320,60,True)
 p.label('DemoNotice','演示模式 / 远征使用固定结果与3秒模拟',160,925,1080,35,20,'muted')
 p.button('OpenSettings','设置',1300,900,180);p.button('ResetDemo','重置演示',1504,900,256,danger=True);p.finish()

def missions():
 p=Panel('ExpeditionSelectPanel','远征选择','02');p.header('任务目录 / 选择目标，查看消耗与预计回收')
 p.label('ListTitle','可用远征',160,246,360,40,24,bold=True);p.label('MissionCount','03项',560,250,140,36,20,'muted',bind='Missions.Count')
 sc=p.scroll('Missions',160,300,540,550,'Missions.List');t=p.template(sc,'MissionEntry','MissionEntry',540,152,174)
 p.node('Img_MissionSelection','Image',0,20,5,112,bg='teal',parent=t,bind='Mission.SelectedMarker')
 p.label('MissionIndex','01',28,23,48,36,22,'teal',True,bind='Mission.Index',parent=t)
 p.label('MissionCardName','废弃货运站',84,22,440,44,28,bold=True,bind='Mission.Name',parent=t)
 p.label('MissionCardRisk','低风险 · 预计10分钟',84,74,430,36,22,'muted',bind='Mission.Risk',parent=t)
 p.label('MissionCardState','已选中',84,110,430,32,20,'teal',bind='Mission.State',parent=t)
 p.surface('Details',732,250,1028,600,'deep',16);p.label('RiskBadge','低风险',786,285,380,36,20,'teal',bind='Mission.Risk')
 p.label('MissionName','废弃货运站',772,340,920,66,44,bold=True,bind='Mission.Name')
 p.label('MissionDescription','回收遗留货箱与基础建材，适合新队长的首次远征。',772,411,944,56,24,'muted',bind='Mission.Description')
 for x,key,title,value in [(772,'CreditsCost','信用点费用','100'),(1090,'EnergyCost','能源费用','20'),(1408,'Duration','预计时长','10分钟')]:
  p.label(key+'Label',title,x,480,300,32,20,'muted');p.label(key,value,x,516,300,54,34,bold=True,bind='Mission.'+key)
 p.label('RewardTitle','预计奖励',772,588,540,40,24,bold=True)
 for i,(kind,title,qty) in enumerate([('Credits','信用点','+300'),('ItemOre','钛矿石','×8'),('ItemAlloy','合金板','×2')]):
  x=772+i*318;p.icon('Reward'+str(i),'Img_'+kind+'.png',x,638,bind='Mission.RewardIcon'+str(i))
  p.label('RewardName'+str(i),title,x+72,637,230,36,22,bind='Mission.RewardName'+str(i))
  p.label('RewardQuantity'+str(i),qty,x+72,672,230,36,24,'teal',True,bind='Mission.RewardQuantity'+str(i))
 p.label('Recommended','推荐补给：急救包（推荐，非强制）',772,754,944,36,22,'amber',bind='Mission.Recommended')
 p.label('MissionLoadout','',772,794,944,40,22,'muted',bind='Mission.LoadoutSummary')
 p.button('ReturnHome','返回基地',160,900,220);p.button('EditLoadout','调整补给',1210,900,244);p.button('StartExpedition','确认出发',1478,900,282,primary=True)
 p.label('ResourceDeficit','资源充足，可以派遣',418,917,760,46,22,'teal',bind='Mission.Deficit')
 p.label('ListState','',160,370,540,144,24,'muted',bind='Missions.Status');p.button('RetryMissions','重试',160,550,220);p.finish()

def inventory():
 p=Panel('SupplyInventoryPanel','仓库整备','03');p.header('补给管理 / 库存用于规划，出发整备不扣除仓库物品')
 for x,key,title in [(160,'All','全部'),(276,'Material','材料'),(392,'Supply','补给')]:p.button('Category'+key,title,x,246,104,56,key=='All')
 search=p.input('ItemSearch',520,246,290,placeholder='搜索物品名称',bind='Filter.Search')
 search.set('style',search.get('style')+'height:56px;font-size:22px;background-color:#192E3C;')
 p.specs[search.get('id')]['pictureRegion']=[520,246,290,56]
 p.button('ClearSearch','清空',822,246,88,56)
 p.label('InventoryTitle','仓库物品',160,330,360,40,24,bold=True);p.label('ItemCount','8类物品',666,334,140,32,20,'muted',bind='Inventory.Count')
 sc=p.scroll('Inventory',160,380,620,478,'Inventory.List');t=p.template(sc,'InventoryEntry','InventoryEntry',620,84,96)
 p.node('Img_ItemSelection','Image',0,12,4,60,bg='teal',parent=t,bind='Item.SelectedMarker')
 p.icon('InventoryItem','Img_ItemMedkit.png',16,14,parent=t,bind='Item.Icon')
 p.label('InventoryName','急救包',90,10,410,36,24,bold=True,bind='Item.Name',parent=t)
 p.label('InventoryCategory','补给 · 普通',90,47,410,32,20,'muted',bind='Item.Category',parent=t)
 p.label('InventoryQuantity','×4',512,25,100,36,24,'teal',bind='Item.Quantity',parent=t)
 p.label('InventoryState','',164,430,580,160,24,'muted',bind='Inventory.Status')
 p.label('ScrollHint','滚动查看其余材料 ↓',164,867,600,35,20,'muted')
 p.surface('ItemDetails',804,330,432,556,'deep',14);p.label('DetailsTitle','物品详情',836,358,368,40,24,bold=True)
 p.icon('DetailIcon','Img_ItemMedkit.png',836,412,88,bind='Item.DetailIcon')
 p.label('DetailName','急救包',948,420,256,50,32,bold=True,bind='Item.DetailName');p.label('DetailRarity','普通补给',948,467,256,36,20,'teal',bind='Item.DetailRarity')
 desc=p.scroll('ItemDescription',836,537,368,76,'Item.DescriptionScroll')
 p.label('DetailDescription','成功远征时，若有携带，\n消耗1个。',836,537,368,76,22,'muted',bind='Item.Description',parent=desc)
 p.label('Stock','仓库4 / 已携行3',836,624,368,36,22,bind='Item.Stock');p.label('ItemWeight','单重1.0kg · 可加入1',836,663,368,36,22,'muted',bind='Item.Weight')
 p.label('QuantityLabel','加入数量',836,720,220,34,22,'muted',bind='Item.QuantityLabel');p.label('QuantityValue','1',1120,720,70,34,22,'teal',bind='Supply.QuantityValue')
 p.slider('AddQuantity',878,752,282,1,4,1,bind='Supply.AddQuantity');p.button('DecreaseQuantity','−',836,750,40,42);p.button('IncreaseQuantity','+',1164,750,40,42)
 p.label('QuantityHint','仅剩1个可加入，数量固定',836,761,368,32,20,'muted',bind='Supply.QuantityHint');p.button('AddSupply','加入携行',836,805,368,primary=True)
 p.surface('Loadout',1260,330,500,556,'deep',14);p.label('LoadoutTitle','携行清单',1292,358,300,40,24,bold=True);p.label('LoadoutCount','2类',1614,357,100,36,20,'teal',bind='Loadout.Count')
 sc=p.scroll('Loadout',1292,420,436,300,'Loadout.List');t=p.template(sc,'LoadoutEntry','LoadoutEntry',436,136,148)
 p.icon('LoadoutIcon','Img_ItemMedkit.png',0,0,parent=t,bind='Supply.Icon');p.label('LoadoutName','急救包',72,0,320,36,24,bold=True,bind='Supply.Name',parent=t)
 p.label('LoadoutAmount','×3 / 3.0kg',72,39,340,32,20,'muted',bind='Supply.Summary',parent=t)
 p.button('Decrease','减少1',72,79,144,48,bind='Action.Decrease',parent=t);p.button('Remove','移除',232,79,144,48,bind='Action.Remove',parent=t)
 p.label('LoadoutState','携行清单为空',1292,470,420,90,24,'muted',bind='Loadout.Status')
 p.label('LoadoutWeight','重量7.0 / 20.0kg',1292,741,436,40,24,'teal',True,'Loadout.Weight')
 p.surface('WeightTrack',1292,788,436,10,'card',5);n=p.node('Img_WeightFill','Image',1292,788,153,10,bg='teal',bind='Loadout.WeightFill');p.sprite(n,'Sld_Value_Bar.png');n.set('style',n.get('style')+'border-radius:5px;')
 p.label('LoadoutNotice','清单是计划分配，库存尚未扣除',1292,824,436,34,20,'muted')
 p.button('Return','返回',160,916,220);p.label('SaveNotice','更改即时保留 · 仅出发后结算消耗',414,933,1040,38,22,'muted');p.button('ClearLoadout','清空携行',1504,916,256,danger=True)
 p.finish()

def settings():
 p=Panel('GameSettingsPanel','游戏设置','04');p.header('偏好设置 / 点击应用后生效；取消将放弃当前修改',False)
 p.label('SettingsNotice','本地演示设置',1460,126,280,38,20,'teal');p.surface('SettingsForm',300,258,1320,540,'deep',16)
 for y,key,title,sub in [(300,'Name','队长名称','1～12个字符'),(414,'Sound','游戏音效','保存开关状态'),(526,'Volume','主音量','调整全局音量'),(646,'Quality','画质档位','本示例保存偏好数值')]:
  p.label(key+'Title',title,340,y,360,44,28,bold=True);p.label(key+'Hint',sub,340,y+44,360,32,20,'muted')
 p.input('CaptainName',740,294,740,'林岚',bind='Settings.CaptainName');p.label('NameCounter','2/12',1394,312,100,32,20,'muted',bind='Settings.NameCounter')
 p.label('NameError','',740,368,800,32,20,'red',bind='Settings.NameError')
 n=p.node('Toggle_Sound','Toggle',740,420,90,42,bind='Settings.SoundEnabled',klass='toggle',extra={'type':'checkbox','checked':'checked','data-is-on':'true'});p.sprite(n,'Tgl_Sound_Track.png')
 p.label('SoundState','开启',852,424,320,40,24,'teal',bind='Settings.SoundState')
 p.slider('MasterVolume',740,537,600,80,bind='Settings.MasterVolume');p.label('MasterVolume','80%',1390,537,180,50,30,'teal',True,'Settings.VolumeLabel')
 for i,(key,text) in enumerate([('Low','低'),('Medium','中'),('High','高')]):p.button('Quality'+key,text,740+i*220,646,196,64,i==2)
 p.label('SettingsExplanation','说明：本阶段为演示设置，尚未接入真实音频与画质服务。',340,742,1220,36,20,'muted')
 p.button('ResetDefaults','恢复默认',300,890,240);p.button('CancelSettings','取消',1090,890,240);p.button('ApplySettings','应用',1354,890,266,primary=True);p.finish()

def confirm():
 p=Panel('ActionConfirmPanel','操作确认','05',True)
 p.label('ConfirmTitle','确认派遣远征队？',640,316,570,58,34,bold=True,bind='Confirm.Title')
 close=p.button('CloseConfirm','×',1240,312,48,48);close.set('style',close.get('style')+'background-color:#00000000;')
 p.label('ConfirmMission','废弃货运站',640,378,450,44,26,bold=True,bind='Confirm.Mission');p.label('ConfirmRisk','低风险',1126,380,140,34,20,'teal',bind='Confirm.Risk')
 p.surface('CostSummary',640,432,640,100,'deep',10)
 for x,key,title,value in [(664,'Credits','信用点费用','100'),(974,'Energy','能源费用','20')]:
  p.label('Confirm'+key+'Label',title,x,448,280,32,20,'muted',bind='Confirm.'+key+'Label');p.label('Confirm'+key,value,x,481,280,44,28,bold=True,bind='Confirm.'+key)
 p.label('ConfirmLoadout','携行：急救包×3、能源电池×2',640,558,640,36,22,bind='Confirm.Loadout')
 p.label('ConfirmWeight','总重7.0 / 20.0kg · 演示耗时3秒',640,597,640,36,22,'muted',bind='Confirm.Weight')
 p.label('ConfirmWarning','确认后立即扣除费用；取消不会产生消耗。',640,646,640,50,22,'amber',bind='Confirm.Warning')
 p.button('CancelConfirm','取消',640,710,240);p.button('Confirm','确认出发',904,710,376,primary=True);p.finish()

def result():
 p=Panel('ExpeditionResultPanel','远征结算','06');p.header('回收报告 / 固定结果演示 · 待领取',False)
 p.label('ResultState','任务已完成',1512,129,240,36,20,'teal',bind='Result.State');p.surface('ResultSummary',160,250,580,600,'deep',16)
 p.icon('ResultSuccess','Img_Confirm.png',200,286,80);p.label('ResultTitle','远征成功',200,400,500,76,48,bold=True,bind='Result.Title')
 p.label('ResultMission','废弃货运站',200,474,500,54,32,bold=True,bind='Result.Mission');p.label('ResultDescription','远征队已安全返回星港。',200,532,500,50,24,'muted',bind='Result.Description')
 p.line('ResultDivider',200,598,500);p.label('CostTitle','出发费用（已扣除）',200,626,500,36,22,'muted');p.label('PaidCosts','信用点100 / 能源20',200,670,500,46,26,bind='Result.PaidCosts')
 p.label('ConsumptionTitle','本次补给消耗',200,736,500,36,22,'muted');p.label('Consumption','急救包 −1',200,780,500,46,26,'amber',bind='Result.Consumption')
 p.surface('Rewards',772,250,988,600,'deep',16);p.label('RewardsTitle','回收奖励',812,286,560,44,28,bold=True);p.label('RewardNotice','领取后一次到账',1440,292,280,32,20,'muted')
 sc=p.scroll('Rewards',812,368,908,380,'Rewards.List');t=p.template(sc,'RewardEntry','RewardEntry',908,108,132)
 p.surface('RewardBackground',0,0,908,108,'card',12,parent=t);p.icon('RewardIcon','Img_Credits.png',24,24,60,parent=t,bind='Reward.Icon')
 p.label('RewardName','信用点',108,34,600,44,28,bold=True,bind='Reward.Name',parent=t);p.label('RewardQuantity','+300',762,30,140,48,32,'teal',True,'Reward.Quantity',parent=t)
 p.label('RewardCloseNotice','关闭后仍可从基地再次查看并领取。',812,792,908,38,22,'muted')
 p.button('Postpone','暂不领取',160,900,220);p.label('ClaimNotice','费用不重复扣除 · 奖励仅可领取一次',416,917,940,40,22,'muted');p.button('ClaimRewards','领取并返回基地',1380,900,380,primary=True);p.finish()

if __name__=='__main__':
 for build in [home,missions,inventory,settings,confirm,result]:build()
