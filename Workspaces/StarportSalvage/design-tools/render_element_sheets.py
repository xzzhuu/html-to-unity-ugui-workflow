"""Make review sheets and export plans only. Do not export runtime PNGs."""
from pathlib import Path
import hashlib
import json
import shutil
from PIL import Image, ImageDraw
from render_previews import Board, C, f, ROOT

REVIEW=ROOT/'design-review'
SHARED=REVIEW/'element-groups-v01'
SHARED.mkdir(parents=True,exist_ok=True)
USER_APPROVAL='确认，进行下一步'

def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()

def write_json(path,data):
    path.parent.mkdir(parents=True,exist_ok=True)
    path.write_text(json.dumps(data,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')

def record_layout_approval():
    index=json.loads((REVIEW/'preview-index.json').read_text(encoding='utf-8'))
    for panel in index['panels']:
        source=ROOT/panel['preview']
        if digest(source)!=panel['sha256']:
            raise ValueError('Layout changed since presentation: '+panel['panelName'])
        dest=ROOT/'Panels'/panel['panelName']/'design/review/layout-approved.png'
        dest.parent.mkdir(parents=True,exist_ok=True)
        shutil.copyfile(source,dest)
        write_json(dest.parent/'approval.json',dict(status='approved',imageSha256=digest(dest),
                   userApproval=USER_APPROVAL,approvedAt='2026-10-22',
                   scope='六面板整体布局与配色 v01；不包含元素组图、未展示状态、HTML或Unity验收',
                   sourcePreview=panel['preview']))
        panel['status']='approved'
    index['status']='approved'
    index['userApproval']=USER_APPROVAL
    write_json(REVIEW/'preview-index.json',index)

class Sheet(Board):
    def __init__(self,key,title,num,subtitle):
        self.key,self.title=key,title
        self.image=Image.new('RGB',(1920,1080),C['canvas'])
        self.d=ImageDraw.Draw(self.image)
        self.text(120,24,'STARPORT SALVAGE  /  COMPONENT REVIEW',20,'ink',True)
        self.text(1340,24,f'{num}  /  元素组图 v01',20,'ink')
        self.box(120,80,1680,920,'base',20)
        self.text(160,112,title,36,'text',True)
        self.text(160,170,subtitle,22,'muted')
        self.line(160,218,1760,218)
        self.text(120,1025,'组图 1920 × 1080  /  以下为审核拼接效果；单张素材按标注POT尺寸另行导出',20,'ink')
        self.text(1490,1025,'待确认 · 非最终素材',20,'ink')

    def note(self,y,a,b):
        self.text(160,y,a,24,'text',True)
        self.text(160,y+40,b,22,'muted')

    def save(self):
        path=SHARED/(self.key+'.png')
        self.image.save(path)
        return path

def surfaces(s):
    s.note(250,'01 / 主底板与内层卡片','Img_RoundedSurface.png · 中性白色Alpha圆角 · 512×512 · L/B/R/T=24/24/24/24')
    for x,w,h,col,label in [(160,660,300,'deep','内容区域 · 660×300'),(852,908,180,'card','内容卡片 · 908×180（下方同形复用）')]:
        s.box(x,350,w,h,col,16);s.text(x+32,382,label,24,'muted')
    s.box(852,560,908,108,'card',12);s.icon(876,584,'credit',60)
    s.text(960,593,'信用点',28,'text',True);s.text(1574,590,'+300',32,'teal',True)
    s.note(710,'图层：中性底板 → Image.color → 图标/文字子节点','同一底板分别着色 #142330 / #192E3C / #203745；不导出三个颜色副本。')
    s.text(160,826,'九宫格 24px 为整张512px纹理的切片边距；显示尺寸由RectTransform决定。',22,'muted')
    s.text(160,874,'主面板/确认弹窗/内容卡片共用结构；不同圆角显示值需在Unity装配中复核。',22,'muted')

def buttons(s):
    s.note(248,'01 / 原生 Selectable 状态','Img_Control_Background.png · 512×128 · L/B/R/T=20/20/20/20 · 与输入框共享中性底板')
    states=[('普通','#4AD7BB'),('悬停','#53E4C9'),('按下','#3BBCA3'),('导航焦点','#4AD7BB'),('禁用','#50666B')]
    for i,(label,color) in enumerate(states):
        x=160+i*324;s.text(x,350,label,22,'muted');s.box(x,396,300,64,color,10)
        s.text(x+100,409,'确认出发',24,'ink',True)
        if i==3:s.text(x,481,'附加焦点标记 ▸',20,'teal')
    s.note(556,'02 / 主次与危险操作共用底板','显示示例 300×60；文字为TMP，不进入背景PNG；颜色通过Image.color与原生Color Tint配置。')
    s.button(160,662,300,'加入携行',True);s.button(508,662,300,'返回基地');s.button(856,662,300,'清空携行',danger=True)
    s.note(778,'03 / 数量与小型操作','减少1 / 移除：144×48；分类：104×56；保留至少48px点击区。业务选中标记属于独立Image。')
    s.button(160,878,144,'减少 1',h=48);s.button(328,878,144,'移除',h=48);s.button(520,874,104,'全部',True,h=56)
    s.text(710,889,'无颜色副本；同一控件不混用SpriteSwap和Color Tint。',22,'muted')

def inputs(s):
    s.note(250,'01 / 输入结构','Img_Control_Background.png · 512×128 · L/B/R/T=20/20/20/20 · 与按钮共享中性底板')
    for i,(label,value,state) in enumerate([('未输入','搜索物品名称','placeholder'),('聚焦输入','林岚','focus'),('非法输入','名称不能为空','error'),('任务锁定','林岚','disabled')]):
        y=350+i*136;s.text(160,y+15,label,24,'muted');s.box(440,y,920,64,'card' if i==1 else 'deep',8)
        s.text(464,y+15,value,24,'red' if i==2 else ('muted' if i in (0,3) else 'text'))
        if state=='focus':s.line(520,y+15,520,y+49,'teal',2)
        s.text(1410,y+17,['搜索占位','光标由TMP生成','错误信息单独TMP','interactable=false'][i],22,'muted')
    s.text(160,906,'层级：底板 → RectMask2D文字视口 → Placeholder / Value；错误提示为独立文本。',22,'muted')

def toggles(s):
    s.note(250,'01 / 音效开关','Tgl_Sound_Track.png · 128×64 · border=24/24/24/24；共享Img_Circle.png · 64×64')
    for i,(label,on,disabled) in enumerate([('开启',True,False),('关闭',False,False),('不可交互',True,True)]):
        x=200+i*520;s.text(x,366,label,28,'text',True)
        s.box(x,440,90,42,'teal' if on and not disabled else 'line',21)
        s.box(x+(50 if on else 5),445,32,32,'text' if not disabled else 'muted',16)
        s.text(x+120,445,'开启' if on else '关闭',24,'teal' if on and not disabled else 'muted')
    s.note(580,'02 / 装配与行为','原生Toggle保存isOn；原有Checkmark辅助节点按需要承接勾选/滑块图形。')
    s.text(160,688,'轨道和圆形旋钮为不同形状，因此拆为2张；不生成开/关两张彩色轨道。',24)
    s.text(160,748,'显示尺寸：轨道90×42、旋钮32×32；关闭音效时保留并允许调整音量值。',22,'muted')
    s.text(160,812,'如需旋钮随isOn改变位置，由接入项目已有UI绑定处理；不添加视觉脚本。',22,'amber')

def sliders(s):
    s.note(250,'01 / 可复用轨道、填充与圆形手柄','Sld_Value_Bar.png 512×32：轨道/填充共用；Img_Circle.png 64×64：与开关共用；Bar border=16')
    for y,label,value,maxvalue,handle in [(380,'音量 · 80%',80,100,True),(530,'加入数量 · 2 / 4',2,4,True),(680,'携行重量 · 7.0 / 20.0kg',7,20,False)]:
        ratio=(value-1)/(maxvalue-1) if y==530 else value/maxvalue
        s.text(160,y,label,26,'text',True)
        s.box(700,y+16,900,10,'card',5);s.box(700,y+16,int(900*ratio),10,'teal',5)
        if handle:s.box(700+int(900*ratio)-14,y+7,28,28,'text',14)
        if y==530:
            s.button(620,y-6,56,'−',h=56)
            s.button(1630,y-6,64,'+',h=56)
    s.note(822,'02 / 仅剩1个时：显示固定值，禁用数量滑块','数量为0时隐藏数量选择并提示“无可加入数量”；重量条无手柄，不接收指针输入。')

def rows(s):
    s.text(160,248,'01 / 任务卡 540×152 · 行步长174',24,'text',True)
    for i,(name,col,tag) in enumerate([('废弃货运站','teal','已选中'),('旧反应堆核心区','muted','未解锁 · 后续版本开放')]):
        y=300+i*180;s.box(160,y,540,152,'card' if i==0 else 'deep',12)
        if i==0:s.box(160,y+20,5,112,'teal',2)
        s.text(188,y+24,name,28,'text',True);s.text(188,y+80,tag,22,col)
    s.text(848,248,'02 / 物品行620×84 · 行步长96',24,'text',True)
    for i,(name,qty,kind) in enumerate([('急救包',4,'med'),('应急信标',0,'beacon')]):
        y=300+i*108;s.box(848,y,620,84,'card' if i==0 else 'deep',10)
        s.icon(864,y+14,kind);s.text(940,y+12,name,24,'text' if qty else 'muted',True)
        s.text(940,y+48,'补给 · 普通' if qty else '补给 · 稀有',20,'muted');s.text(1356,y+26,f'× {qty}',24,'muted')
    s.text(848,552,'03 / 携行条目 · 独立减少与移除',24,'text',True)
    s.icon(848,612,'med');s.text(920,612,'急救包 ×3 / 3.0kg',24);s.button(920,662,144,'减少 1',h=48);s.button(1080,662,144,'移除',h=48)
    s.note(744,'04 / 原生纵向ScrollRect + RectMask2D + Content','列表与条目共用Img_RoundedSurface.png 512×512，border=24；不烘焙整行文字。')
    s.text(160,850,'空态：仓库暂无物品  /  搜索空态：没有找到相关物品 + 清空搜索',22,'muted')
    s.text(160,900,'奖励行908×108 · 行步长132；业务选中使用独立标记，区别于按钮导航焦点。',22,'muted')

def items(s):
    data=[('med','急救包','普通'),('battery','能源电池','优良'),('ration','压缩口粮','普通'),('beacon','应急信标','稀有'),('ore','钛矿石','普通'),('alloy','合金板','优良'),('circuit','电路板','优良'),('chip','数据芯片','稀有')]
    for i,(kind,label,rarity) in enumerate(data):
        x=160+i%4*408;y=268+i//4*286
        s.icon(x,y,kind,128)
        if kind=='chip':
            for dx,dy in [(50,50),(66,50),(50,66),(66,66)]:
                s.d.rectangle((x+dx,y+dy,x+dx+9,y+dy+9),fill=C['amber'])
        elif kind=='circuit':
            s.line(x+51,y+56,x+77,y+56,'teal',3)
        s.text(x+152,y+12,label,26,'text',True);s.text(x+152,y+59,rarity,22,'muted')
        s.text(x,y+158,'源图 256×256 · 显示56/88px',20,'muted')
        s.text(x,y+196,'白色Alpha图形 → Image.color',20,'muted')
    s.note(864,'图层：共用深色图标座 → 独立透明图标 → TMP名称/数量','8种轮廓各一份中性图，稀有度使用TMP与着色表达；无文字烘焙，不从面板截图裁片。')

def utility(s):
    symbols=[('返回','back'),('关闭','close'),('搜索','search'),('确认','check'),('风险','risk'),('锁定','lock'),('能源','battery'),('重量','weight'),('信用点','credit')]
    for i,(label,symbol) in enumerate(symbols):
        x=160+(i%5)*320;y=300+(i//5)*244
        s.box(x,y,88,88,'deep',12)
        col='amber' if label=='风险' else 'teal'
        if symbol in ('credit','check','battery'):s.icon(x,y,symbol,88)
        elif symbol=='back':
            s.line(x+24,y+44,x+65,y+44,col,4);s.line(x+24,y+44,x+40,y+26,col,4);s.line(x+24,y+44,x+40,y+62,col,4)
        elif symbol=='close':
            s.line(x+28,y+28,x+60,y+60,col,4);s.line(x+28,y+60,x+60,y+28,col,4)
        elif symbol=='search':
            s.d.ellipse((x+22,y+19,x+56,y+53),outline=C[col],width=4);s.line(x+52,y+49,x+69,y+66,col,4)
        elif symbol=='risk':
            s.line(x+44,y+19,x+18,y+67,col,3);s.line(x+18,y+67,x+70,y+67,col,3);s.line(x+70,y+67,x+44,y+19,col,3)
            s.line(x+44,y+35,x+44,y+48,col,4);s.d.ellipse((x+42,y+55,x+46,y+59),fill=C[col])
        elif symbol=='lock':
            s.d.arc((x+29,y+17,x+59,y+51),180,360,fill=C[col],width=4)
            s.d.rounded_rectangle((x+23,y+36,x+65,y+69),radius=5,outline=C[col],width=4)
            s.line(x+44,y+48,x+44,y+57,col,4)
        elif symbol=='weight':
            s.d.ellipse((x+36,y+16,x+52,y+32),outline=C[col],width=3)
            s.line(x+26,y+32,x+62,y+32,col,4);s.line(x+26,y+32,x+18,y+68,col,4)
            s.line(x+62,y+32,x+70,y+68,col,4);s.line(x+18,y+68,x+70,y+68,col,4)
        s.text(x,y+112,label,24,'text',True);s.text(x,y+154,'128×128 · Alpha',20,'muted')
    s.note(816,'每种图标独立：Img_NavigateBack / Close / Search / Confirm / Risk / Lock / Energy / Weight / Credits','图形与按钮底板分层；中文标签、kg单位与数值保留TMP，不进入PNG。')
    s.text(160,922,'全部同色图标保留中性白色，使用Image.color着色；常规显示24～32px。',22,'muted')

def typography(s):
    s.text(160,270,'标题 / 40px Bold',40,'text',True);s.text(840,286,'页面名称、主要任务名',24,'muted')
    s.text(160,356,'区块标题 / 28px Bold',28,'text',True);s.text(840,356,'物品详情、回收奖励、资源值',24,'muted')
    s.text(160,434,'正文与操作 / 24px',24);s.text(840,434,'费用、名称、按钮文字',24,'muted')
    s.text(160,506,'辅助信息 / 20px',20,'muted');s.text(840,506,'分类、时长、状态说明',24,'muted')
    s.text(160,596,'还缺 50 信用点、10 能源',24,'amber');s.text(840,596,'风险提示 · 不只依赖颜色',24,'muted')
    s.text(160,674,'名称不能为空',24,'red');s.text(840,674,'错误提示 · 输入框下独立文本',24,'muted')
    s.text(160,752,'远征执行中（演示） · 剩余 3 秒',24,'teal');s.text(840,752,'执行状态 · 禁止再次派遣',24,'muted')
    s.text(160,830,'等待领取 / 读取失败，请重试 / 仓库暂无物品',24)
    s.text(160,918,'所有内容保留TMP；字体复用项目已有中文字体与材质；本组无运行时PNG。',22,'muted')

GROUPS=[
 ('Img_Surface_Sheet','Image','底板与卡片',surfaces),
 ('Btn_Action_Sheet','Button','按钮结构与状态',buttons),
 ('Inp_Form_Sheet','InputField','搜索与名称输入',inputs),
 ('Tgl_Sound_Sheet','Toggle','音效开关',toggles),
 ('Sld_Value_Sheet','Slider','音量、数量与重量',sliders),
 ('Scr_List_Sheet','ScrollView','动态列表与条目',rows),
 ('Img_Item_Sheet','Image','八种物品图标',items),
 ('Img_Utility_Sheet','Image','资源与操作图标',utility),
 ('Txt_Typography_Sheet','Text','文字层级与反馈',typography)]

USES={
 'StarportHomePanel':{'Img_Surface_Sheet':['Group_Main','Group_ExpeditionStatus'],'Btn_Action_Sheet':['Btn_OpenExpeditions','Btn_OpenInventory','Btn_OpenSettings','Btn_ResetDemo'],'Img_Item_Sheet':['Group_LoadoutSummary'],'Img_Utility_Sheet':['Group_ResourceBar'],'Txt_Typography_Sheet':['Txt_Title','Txt_TaskState']},
 'ExpeditionSelectPanel':{'Img_Surface_Sheet':['Group_MissionDetails'],'Btn_Action_Sheet':['Btn_ReturnHome','Btn_EditLoadout','Btn_StartExpedition'],'Scr_List_Sheet':['Scroll_Missions'],'Img_Item_Sheet':['Group_Rewards'],'Img_Utility_Sheet':['Group_MissionDetails'],'Txt_Typography_Sheet':['Txt_MissionName','Txt_ResourceDeficit']},
 'SupplyInventoryPanel':{'Img_Surface_Sheet':['Group_ItemDetails','Group_Loadout'],'Btn_Action_Sheet':['Btn_AddSupply','Btn_ClearLoadout','Btn_Return'],'Inp_Form_Sheet':['Input_ItemSearch'],'Sld_Value_Sheet':['Slider_AddQuantity','Slider_LoadoutWeight'],'Scr_List_Sheet':['Scroll_Inventory','Scroll_Loadout'],'Img_Item_Sheet':['Scroll_Inventory','Group_ItemDetails','Scroll_Loadout'],'Img_Utility_Sheet':['Input_ItemSearch','Group_Weight'],'Txt_Typography_Sheet':['Txt_ItemName','Txt_InventoryState']},
 'GameSettingsPanel':{'Img_Surface_Sheet':['Group_SettingsForm'],'Btn_Action_Sheet':['Btn_ResetDefaults','Btn_Cancel','Btn_Apply','Group_Quality'],'Inp_Form_Sheet':['Input_CaptainName'],'Tgl_Sound_Sheet':['Toggle_Sound'],'Sld_Value_Sheet':['Slider_MasterVolume'],'Txt_Typography_Sheet':['Txt_NameError','Txt_MasterVolume']},
 'ActionConfirmPanel':{'Img_Surface_Sheet':['Group_Dialog','Group_CostSummary'],'Btn_Action_Sheet':['Btn_Cancel','Btn_Confirm','Btn_Close'],'Img_Utility_Sheet':['Btn_Close','Group_CostSummary'],'Txt_Typography_Sheet':['Txt_Title','Txt_Message']},
 'ExpeditionResultPanel':{'Img_Surface_Sheet':['Group_ResultSummary'],'Btn_Action_Sheet':['Btn_Postpone','Btn_ClaimRewards'],'Scr_List_Sheet':['Scroll_Rewards'],'Img_Item_Sheet':['Scroll_Rewards'],'Img_Utility_Sheet':['Img_ResultSuccess','Scroll_Rewards'],'Txt_Typography_Sheet':['Txt_ResultTitle','Txt_Consumption']}}

def main():
    record_layout_approval()
    records=[]
    for i,(key,component,title,render) in enumerate(GROUPS):
        sheet=Sheet(key,title,f'{i+1:02d}','已确认面板的组件拆分  /  中性素材复用 · 必要状态 · 图层与尺寸')
        render(sheet);path=sheet.save()
        records.append(dict(groupId=key,componentType=component,title=title,
                            sheet=str(path.relative_to(ROOT)).replace('\\','/'),sha256=digest(path),status='pending-user-review'))
    for panel,uses in USES.items():
        root=ROOT/'Panels'/panel;manifest=json.loads((root/'design/element-groups.json').read_text(encoding='utf-8'))
        manifest['groups']=[]
        for record in records:
            key=record['groupId']
            if key not in uses:continue
            local=root/'design/element-groups'/(key+'.png');local.parent.mkdir(parents=True,exist_ok=True)
            shutil.copyfile(ROOT/record['sheet'],local)
            manifest['groups'].append(dict(groupId=key,componentType=record['componentType'],purpose=record['title'],
                 usedBy=[panel+'/'+name for name in uses[key]],sheet='design/element-groups/'+key+'.png',assets=[]))
        # Metadata/assets are populated after the user approves candidate sheets.
        # This draft intentionally does not claim that empty assets are production-ready.
        write_json(root/'design/element-groups.json',manifest)
    write_json(REVIEW/'element-groups-index-v01.json',dict(schemaVersion=1,status='pending-user-review',groups=records,
               exportStatus='not-exported',notes='组图内示意图形并非最终素材；确认后按同一造型与尺寸导出，补录清单元数据。'))
    assets=[]
    def plan(name,size,border,component,groups,purpose):
        assets.append(dict(name=name,componentType=component,pixelSize=size,originalPixelSize=size,
                           hasAlpha=True,border=border,tintable=True,fullTexture=True,
                           proposedSource='html/assets/shared/'+name,groups=groups,purpose=purpose,
                           status='planned-not-exported'))
    plan('Img_RoundedSurface.png',[512,512],[24]*4,'Image',['Img_Surface_Sheet','Scr_List_Sheet'],'底板、卡片、列表与条目共用中性圆角')
    plan('Img_Control_Background.png',[512,128],[20]*4,'Image',['Btn_Action_Sheet','Inp_Form_Sheet'],'按钮和输入框共用圆角结构')
    plan('Tgl_Sound_Track.png',[128,64],[24]*4,'Toggle',['Tgl_Sound_Sheet'],'开关轨道，开关状态用tint与旋钮位置组合')
    plan('Img_Circle.png',[64,64],[0]*4,'Image',['Tgl_Sound_Sheet','Sld_Value_Sheet'],'开关旋钮与滑块手柄共用实心圆')
    plan('Sld_Value_Bar.png',[512,32],[16]*4,'Slider',['Sld_Value_Sheet'],'滑块轨道和填充共用胶囊形')
    for name in ['Medkit','Battery','Ration','Beacon','Ore','Alloy','Circuit','Chip']:
        plan('Img_Item'+name+'.png',[256,256],[0]*4,'Image',['Img_Item_Sheet'],'独立物品轮廓，与底座及文字分层')
    for name in ['NavigateBack','Close','Search','Confirm','Risk','Lock','Energy','Weight','Credits']:
        plan('Img_'+name+'.png',[128,128],[0]*4,'Image',['Img_Utility_Sheet'],'独立操作或资源图形，不包含文字单位')
    write_json(REVIEW/'sprite-export-plan-v01.json',dict(schemaVersion=1,status='pending-user-review',assets=assets,
               notes='仅尺寸/复用/边距计划，不是已导出文件清单。组图批准后精确导出并补录各准备包sprites元数据。'))
    overview=Image.new('RGB',(2000,1640),'#0B151E');d=ImageDraw.Draw(overview)
    d.text((32,24),'星港拾荒者  /  九组可复用元素 v01',font=f(32,True),fill=C['text'])
    d.text((32,76),'每组原图1920×1080 · 总览统一缩至1/3 · 正式PNG尚未导出',font=f(22),fill=C['muted'])
    for i,record in enumerate(records):
        x=16+(i%3)*660;y=136+(i//3)*500
        im=Image.open(ROOT/record['sheet']).resize((640,360),Image.Resampling.LANCZOS)
        overview.paste(im,(x,y));d.rectangle((x,y,x+639,y+359),outline=C['line'])
        d.text((x+6,y+376),f'{i+1:02d}  '+record['title'],font=f(25,True),fill=C['text'])
        d.text((x+6,y+420),record['groupId'],font=f(20),fill=C['muted'])
    overview.save(REVIEW/'Img_ElementGroups_Overview_v01.png')
    print('Recorded 6 real layout approvals; created 9 review sheets. No element approvals or runtime exports.')

if __name__=='__main__':main()
