"""Build the editable, two-page workflow; export PNG separately using draw.io."""
from pathlib import Path
import json
import xml.etree.ElementTree as ET

skill = Path(__file__).resolve().parents[1]/'Skills/ui-panel-design-to-html'
preset = json.loads((Path(__file__).resolve().parent/'workflow-style.json').read_text())
for candidate in sorted((Path.home()/'.drawio-skill/styles').glob('*.json')):
    value = json.loads(candidate.read_text(encoding='utf-8'))
    if value.get('default'):
        preset = value
        break
doc = ET.Element('mxfile', host='app.diagrams.net')

def page(title, nodes, edges):
    diagram = ET.SubElement(doc, 'diagram', name=title, id=str(len(doc)))
    model = ET.SubElement(diagram, 'mxGraphModel', dx='1200', dy='1050', grid='1', gridSize='10', page='1', pageScale='1', pageWidth='1200', pageHeight='1100', background='#ffffff')
    root = ET.SubElement(model, 'root')
    ET.SubElement(root, 'mxCell', id='0')
    ET.SubElement(root, 'mxCell', id='1', parent='0')
    for identifier, label, x, y, role in nodes:
        decision = role == 'decision'
        palette = preset['palette']['warning' if decision else preset['roles'][role]]
        style = (preset['shapes']['decision' if decision else ('external' if role=='external' else 'service')] +
                 ';whiteSpace=wrap;html=1;fillColor='+palette['fillColor']+';strokeColor='+palette['strokeColor']+
                 ';fontFamily='+preset['font']['fontFamily']+';fontSize=14;spacing=10;')
        cell=ET.SubElement(root, 'mxCell', id=identifier, value=label, style=style, vertex='1', parent='1')
        ET.SubElement(cell, 'mxGeometry', x=str(x), y=str(y), width='280', height='85', attrib={'as':'geometry'})
    for count, (source, target, label) in enumerate(edges):
        cell=ET.SubElement(root,'mxCell',id='e'+str(count),value=label,source=source,target=target,parent='1',edge='1',style=preset['edges']['style']+';'+preset['edges']['arrow']+';fontFamily='+preset['font']['fontFamily']+';fontSize=12;')
        geometry=ET.SubElement(cell,'mxGeometry',relative='1',attrib={'as':'geometry'})
        routes={}
        if title.startswith('01'):
            routes[('h','f')]=('exitX=0.5;exitY=0;entryX=1;entryY=0.5;',[(580,660),(580,612.5)])
        else:
            routes[('c','b')]=('exitX=0;exitY=0.5;entryX=0;entryY=0.5;',[(20,342.5),(20,212.5)])
            routes[('g','f')]=('exitX=0.5;exitY=0;entryX=0.5;entryY=0;',[(960,530),(580,530)])
        if (source,target) in routes:
            style,points=routes[(source,target)]
            cell.set('style',cell.get('style')+style)
            array=ET.SubElement(geometry,'Array',attrib={'as':'points'})
            for x,y in points: ET.SubElement(array,'mxPoint',x=str(x),y=str(y))

page('01 新目录初始化', [
('a','统一 Skill 调用<br/>“新文件目录” / $ui-panel-design-to-html',60,40,'external'),
('b','解析目标目录与已有配置<br/>默认当前目录；推导唯一 *Panel 根名',60,170,'service'),
('c','已有配置是否冲突？',60,300,'decision'),
('d','解释冲突并保留现有内容<br/>使用已有配置或另选目标目录',440,300,'error'),
('e','创建固定子目录<br/>inputs / design / html',60,440,'service'),
('f','生成缺失的 XHTML、CSS、JS<br/>组件意义清单、素材清单、导入 JSON',60,570,'service'),
('g','保留已有文件；不复制 Skill 文档<br/>草稿状态 draft-unapproved',60,700,'service'),
('h','结构校验：命名、模板、辅助物体<br/>PNG、引用与输出路径；失败修复',440,700,'service'),
('i','交付 HTML 入口及创建/保留结果<br/>仅初始化请求到此完成',820,700,'database'),
('j','需要正式业务 UI？<br/>是 → 第 02 页审核流程',820,830,'decision'),
('k','目录与可运行草稿交付完成<br/>保留 draft-unapproved 状态',820,960,'database')],
[('a','b',''),('b','c',''),('c','d','是'),('c','e','否'),('e','f',''),('f','g',''),('g','h',''),('h','f','修复缺失'),('h','i','通过'),('i','j',''),('j','k','否')])
page('02 正式 Panel 设计与 Unity 导入',[
('a','需求文档 / 参考图<br/>继承初始化目录与画布配置',60,40,'external'),
('b','制作布局图并列出必要元素<br/>design/layout/',60,170,'service'),
('c','用户审核元素完整性及视觉<br/>修改则回到布局图',60,300,'decision'),
('d','审批证据 → design/review/<br/>确定组件意义、名称、父级及绑定',60,440,'service'),
('e','拆解动态 Item 与独立 Sprite<br/>更新 elements.json / sprites.json',60,570,'service'),
('f','实现 XHTML 与浏览器预览<br/>匹配审核图的布局、元素与状态',440,570,'service'),
('g','结构 + 浏览器 + 视觉验收<br/>有偏差修复实现；设计变更重新审批',820,570,'decision'),
('h','审批图与源码哈希、验收记录一致<br/>正式 HTML ready；源码保持外部',820,700,'database'),
('i','独立 UPM package：外部选择 HTML<br/>Unity 2022.3 + UGUI + TMP',440,700,'external'),
('j','生成 UGUI/TMP 根 Panel 到 Canvas<br/>图片 → Art/UI/Sprite/{PanelName}',440,830,'service'),
('k','保存 Panel 与 Item Prefab<br/>业务数据与交互由目标应用实现',820,830,'database')],
[('a','b',''),('b','c',''),('c','b','修改'),('c','d','通过'),('d','e',''),('e','f',''),('f','g',''),('g','f','实现偏差'),('g','h','通过'),('h','i','请求 Unity 转换'),('i','j',''),('j','k','')])
ET.indent(doc)
(skill/'assets/workflow.drawio').write_bytes(ET.tostring(doc,encoding='utf-8',xml_declaration=True))
print(skill/'assets/workflow.drawio')
