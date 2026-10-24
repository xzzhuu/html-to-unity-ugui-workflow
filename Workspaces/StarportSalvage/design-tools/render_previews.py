"""Draw review-only vector-style UI previews; no runtime assets or approvals."""
from pathlib import Path
import hashlib
import json
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[1]
import os, sys
sys.path.insert(0, str(ROOT.parents[1]/'Skills/ui-panel-design-to-html/scripts'))
from game_workflow import read, font_size
STYLE = read(ROOT/'contracts/style.json')
FONT = os.environ.get('HTML_UI_PREVIEW_FONT', STYLE['previewFonts']['regular'])
BOLD = os.environ.get('HTML_UI_PREVIEW_FONT_BOLD', STYLE['previewFonts']['bold'])
C = dict(STYLE['colors'], canvas=STYLE['canvas']['background'])
PANELS = [(p['panelName'],p['title'],p['number']) for p in STYLE['panels']]

def f(size=24, bold=False):
    path = BOLD if bold else FONT
    if not Path(path).is_file():
        raise ValueError('Preview font missing; configure contracts/style.json or HTML_UI_PREVIEW_FONT(_BOLD): '+path)
    return ImageFont.truetype(path, font_size(size, STYLE))

class Board:
    def __init__(self, key, title, number, modal=False):
        self.key, self.title = key, title
        self.image = Image.new('RGB', (1920,1080), C['canvas'])
        self.d = ImageDraw.Draw(self.image)
        self.text(120,24,'STARPORT SALVAGE  /  UI DESIGN REVIEW',20,'ink',True)
        self.text(1310,24,f'{number}  /  {title}  /  v01 待确认',20,'ink')
        self.box(600 if modal else 120,280 if modal else 80,
                 720 if modal else 1680,520 if modal else 920,'base',STYLE['geometry']['panelRadius'])
        dim = '主体 720 × 520  /  宽 37.5% · 高 48.1%' if modal else '主体 1680 × 920  /  宽 87.5% · 高 85.2%'
        self.text(120,1025,'画布 1920 × 1080   /   '+dim,20,'ink')
        self.text(1440,1025,'静态设计预览 · 非运行界面',20,'ink')

    def box(self,x,y,w,h,color='card',r=12,outline=None):
        self.d.rounded_rectangle((x,y,x+w,y+h),radius=r,fill=C.get(color,color),
                                outline=C.get(outline,outline) if outline else None,width=2)

    def text(self,x,y,t,size=24,color='text',bold=False):
        self.d.text((x,y),str(t),font=f(size,bold),fill=C.get(color,color),stroke_width=0)

    def line(self,x,y,x2,y2,color='line',width=2):
        self.d.line((x,y,x2,y2),fill=C.get(color,color),width=width)

    def button(self,x,y,w,label,primary=False,danger=False,h=60):
        self.box(x,y,w,h,'teal' if primary else ('#50343C' if danger else 'card'),STYLE['geometry']['buttonRadius'])
        tw=self.d.textlength(label,font=f(24,True))
        self.text(x+(w-tw)/2,y+(h-34)/2,label,24,'ink' if primary else ('red' if danger else 'text'),True)

    def pill(self,x,y,label,color='teal',w=None):
        w=w or int(self.d.textlength(label,font=f(20)))+28
        self.box(x,y,w,36,'deep',8)
        self.text(x+14,y+3,label,20,color)

    def header(self,subtitle,resources=True):
        self.text(160,112,self.title,40,'text',True)
        self.text(160,170,subtitle,20,'muted')
        if resources:
            for x,label,value in [(980,'信用点','2,400'),(1220,'能源','100 / 100'),(1480,'携行重量','7.0 / 20.0 kg')]:
                self.text(x,115,label,20,'muted')
                self.text(x,149,value,28,'teal' if x==1480 else 'text',True)
        self.line(160,218,1760,218)

    def icon(self,x,y,kind,size=56):
        """Simple semantic pictograms kept inside the review image only."""
        self.box(x,y,size,size,'deep',10)
        s=size/56
        def p(a,b):return (x+a*s,y+b*s)
        def l(points,col='teal',width=3):self.d.line([p(a,b) for a,b in points],fill=C[col],width=max(2,int(width*s)))
        col='amber' if kind in ('ore','alloy','chip','credit') else 'teal'
        if kind=='med':
            self.d.rectangle((*p(23,13),*p(33,43)),fill=C[col]); self.d.rectangle((*p(13,23),*p(43,33)),fill=C[col])
        elif kind=='battery':
            self.d.rounded_rectangle((*p(15,16),*p(41,43)),radius=max(2,int(3*s)),outline=C[col],width=max(2,int(3*s)))
            l([(23,12),(33,12)],width=3);l([(29,21),(24,29),(32,29),(27,37)],width=2)
        elif kind=='ore':
            l([(13,32),(21,15),(36,13),(44,30),(33,43),(17,42),(13,32)],col)
            l([(21,15),(26,30),(44,30)],col,2);l([(26,30),(17,42)],col,2)
        elif kind=='alloy':
            for yy in (17,27,37):l([(13,yy),(35,yy-5),(44,yy+1),(22,yy+6),(13,yy)],col,2)
        elif kind in ('circuit','chip'):
            self.d.rectangle((*p(18,18),*p(38,38)),outline=C[col],width=max(2,int(3*s)))
            for v in (22,28,34):l([(v,12),(v,18)],col,2);l([(v,38),(v,44)],col,2)
        elif kind=='ration':
            self.d.rounded_rectangle((*p(16,13),*p(40,43)),radius=max(2,int(3*s)),outline=C[col],width=max(2,int(3*s)))
            l([(21,24),(35,24)],col,2);l([(21,32),(31,32)],col,2)
        elif kind=='beacon':
            l([(20,43),(28,18),(36,43)],col);l([(15,21),(12,14),(17,8)],col,2);l([(41,21),(44,14),(39,8)],col,2)
        elif kind=='credit':
            self.d.ellipse((*p(13,13),*p(43,43)),outline=C[col],width=max(2,int(3*s)));l([(33,20),(23,20),(20,28),(23,36),(33,36)],col,2)
        elif kind=='check':l([(14,29),(24,39),(43,18)],col,4)
        else:l([(17,36),(28,14),(39,36),(17,36)],col,2)

    def save(self):
        dest=ROOT/'Panels'/self.key/'design/layout'/f'Img_{self.key}_Preview_v01.png'
        dest.parent.mkdir(parents=True,exist_ok=True)
        self.image.save(dest)
        return dest

def home(b):
    b.header('指挥终端   /   林岚队长，欢迎返回星港')
    b.box(160,250,960,600,'deep',16)
    b.pill(200,286,'星港在线  ·  系统就绪')
    b.text(200,364,'星港拾荒者',76,'text',True)
    b.text(205,466,'STARPORT  /  SALVAGE DIVISION',24,'teal',True)
    b.text(205,538,'整备补给，回收失落资源。',30)
    b.text(205,592,'选择目的地，规划携行，让每一次远征都有收获。',24,'muted')
    b.button(205,714,360,'选择远征   →',True,h=76)
    b.button(589,714,300,'仓库整备',h=76)
    b.box(1152,250,608,600,'card',16)
    b.text(1188,286,'远征控制',28,'text',True);b.pill(1570,286,'待命',w=116)
    b.text(1188,364,'尚未派遣远征队',32,'text',True)
    b.text(1188,424,'从低风险货运站开始第一趟回收。',24,'muted')
    b.line(1188,490,1724,490)
    b.text(1188,524,'已准备补给',22,'muted')
    b.icon(1188,574,'med');b.text(1260,580,'急救包',26);b.text(1636,580,'× 3',26,'teal')
    b.icon(1188,652,'battery');b.text(1260,658,'能源电池',26);b.text(1636,658,'× 2',26,'teal')
    b.text(1188,752,'携行 7.0 kg  /  上限 20.0 kg',22,'muted')
    b.text(160,925,'演示模式  /  远征使用固定结果与 3 秒模拟',20,'muted')
    b.button(1300,900,180,'设置');b.button(1504,900,256,'重置演示',danger=True)

def missions(b):
    b.header('任务目录   /   选择目标，查看消耗与预计回收')
    b.text(160,246,'可用远征',24,'text',True);b.text(560,250,'03 项',20,'muted')
    rows=[('01','废弃货运站','低风险  ·  预计 10 分钟','已选中','teal'),('02','失联信号塔','中风险  ·  预计 20 分钟','已解锁','amber'),('03','旧反应堆核心区','高风险  ·  预计 30 分钟','未解锁','muted')]
    for i,(num,name,detail,state,col) in enumerate(rows):
        y=300+i*174;b.box(160,y,540,152,'card' if i==0 else 'deep',12)
        if i==0:b.box(160,y+20,5,112,'teal',2)
        b.text(188,y+23,num,22,col,True);b.text(244,y+22,name,28,'text',True)
        b.text(244,y+74,detail,22,'muted');b.text(244,y+110,state,20,col)
    b.box(732,250,1028,600,'deep',16)
    b.pill(772,282,'低风险');b.text(772,340,'废弃货运站',44,'text',True)
    b.text(772,411,'回收遗留货箱与基础建材，适合新队长的首次远征。',24,'muted')
    for x,label,value in [(772,'信用点费用','100'),(1090,'能源费用','20'),(1408,'预计时长','10 分钟')]:
        b.text(x,480,label,20,'muted');b.text(x,516,value,34,'text',True)
    b.text(772,588,'预计奖励',24,'text',True)
    for x,kind,label,qty in [(772,'credit','信用点','+300'),(1090,'ore','钛矿石','×8'),(1408,'alloy','合金板','×2')]:
        b.icon(x,638,kind);b.text(x+72,637,label,22);b.text(x+72,672,qty,24,'teal',True)
    b.text(772,754,'推荐补给：急救包（推荐，非强制）',22,'amber')
    b.text(772,794,'已携行：急救包 ×3、能源电池 ×2   /   演示耗时 3 秒',22,'muted')
    b.button(160,900,220,'返回基地');b.button(1210,900,244,'调整补给');b.button(1478,900,282,'确认出发',True)
    b.text(418,917,'资源充足，可以派遣',22,'teal')

def inventory(b):
    b.header('补给管理   /   库存用于规划，出发整备不扣除仓库物品')
    for x,w,label,active in [(160,104,'全部',True),(276,104,'材料',False),(392,104,'补给',False)]:
        b.button(x,246,w,label,active,h=56)
    b.box(520,246,290,56,'deep',8);b.text(538,259,'搜索物品名称',22,'muted')
    b.button(822,246,88,'清空',h=56)
    b.text(160,330,'仓库物品',24,'text',True);b.text(666,334,'8 类物品',20,'muted')
    items=[('med','急救包','补给 · 普通','4',True),('battery','能源电池','补给 · 优良','2',False),('ration','压缩口粮','补给 · 普通','12',False),('beacon','应急信标','补给 · 稀有','0',False),('ore','钛矿石','材料 · 普通','20',False)]
    for i,(kind,name,desc,qty,sel) in enumerate(items):
        y=380+i*96;b.box(160,y,620,84,'card' if sel else 'deep',10)
        if sel:b.box(160,y+12,4,60,'teal',2)
        b.icon(176,y+14,kind);b.text(250,y+10,name,24,'text' if qty!='0' else 'muted',True)
        b.text(250,y+47,desc,20,'muted');b.text(672,y+25,'× '+qty,24,'teal' if sel else 'muted')
    b.text(164,867,'滚动查看其余材料  ↓',20,'muted')
    b.box(804,330,432,556,'deep',14)
    b.text(836,358,'物品详情',24,'text',True);b.icon(836,412,'med',88)
    b.text(948,420,'急救包',32,'text',True);b.pill(948,467,'普通补给')
    b.text(836,537,'成功远征时，若有携带，',22,'muted');b.text(836,572,'消耗 1 个。',22,'muted')
    b.text(836,624,'仓库 4   /   已携行 3',22);b.text(836,663,'单重 1.0 kg · 可加入 1',22,'muted')
    b.text(836,720,'加入数量',22,'muted');b.pill(1088,716,'1',w=80)
    b.text(836,761,'仅剩 1 个可加入，数量固定',20,'muted')
    b.button(836,805,368,'加入携行',True)
    b.box(1260,330,500,556,'deep',14);b.text(1292,358,'携行清单',24,'text',True);b.pill(1600,354,'2 类',w=112)
    for i,(kind,name,qty,weight) in enumerate([('med','急救包','3','3.0'),('battery','能源电池','2','4.0')]):
        y=420+i*148;b.icon(1292,y,kind);b.text(1364,y,name,24,'text',True)
        b.text(1364,y+39,f'× {qty}    /    {weight} kg',20,'muted')
        b.button(1364,y+79,144,'减少 1',h=48);b.button(1524,y+79,144,'移除',h=48)
    b.text(1292,741,'重量 7.0 / 20.0 kg',24,'teal',True)
    b.box(1292,788,436,10,'card',5);b.box(1292,788,153,10,'teal',5)
    b.text(1292,824,'清单是计划分配，库存尚未扣除',20,'muted')
    b.button(160,916,220,'返回');b.text(414,933,'更改即时保留 · 仅出发后结算消耗',22,'muted')
    b.button(1504,916,256,'清空携行',danger=True)

def settings(b):
    b.header('偏好设置   /   点击应用后生效；取消将放弃当前修改',False)
    b.pill(1450,126,'本地演示设置',w=220)
    b.box(300,258,1320,540,'deep',16)
    for y,title,sub in [(300,'队长名称','1～12 个字符'),(414,'游戏音效','保存开关状态'),(526,'主音量','调整全局音量'),(646,'画质档位','本示例保存偏好数值')]:
        b.text(340,y,title,28,'text',True);b.text(340,y+44,sub,20,'muted')
    b.box(740,294,740,64,'card',8);b.text(764,307,'林岚',28);b.text(1394,315,'2/12',20,'muted')
    b.box(740,420,90,42,'teal',21);b.box(790,425,32,32,'text',16);b.text(852,424,'开启',24,'teal')
    b.box(740,555,600,10,'card',5);b.box(740,555,480,10,'teal',5)
    b.box(1206,546,28,28,'text',14);b.text(1390,537,'80%',30,'teal',True)
    for i,label in enumerate(['低','中','高']):b.button(740+i*220,646,196,label,i==2,h=64)
    b.text(340,742,'说明：本阶段为演示设置，尚未接入真实音频与画质服务。',20,'muted')
    b.button(300,890,240,'恢复默认');b.button(1090,890,240,'取消');b.button(1354,890,266,'应用',True)

def confirm(b):
    b.text(640,316,'确认派遣远征队？',34,'text',True)
    b.text(1248,317,'×',32,'muted')
    b.text(640,378,'废弃货运站',26,'text',True);b.pill(1112,377,'低风险',w=128)
    b.box(640,432,640,100,'deep',10)
    b.text(664,448,'信用点费用',20,'muted');b.text(664,481,'100',28,'text',True)
    b.text(974,448,'能源费用',20,'muted');b.text(974,481,'20',28,'text',True)
    b.text(640,558,'携行：急救包 ×3、能源电池 ×2',22)
    b.text(640,597,'总重 7.0 / 20.0 kg  ·  演示耗时 3 秒',22,'muted')
    b.text(640,646,'确认后立即扣除费用；取消不会产生消耗。',22,'amber')
    b.button(640,710,240,'取消');b.button(904,710,376,'确认出发',True)

def result(b):
    b.header('回收报告   /   固定结果演示 · 待领取',False)
    b.pill(1500,126,'任务已完成',w=188)
    b.box(160,250,580,600,'deep',16)
    b.icon(200,286,'check',80);b.text(200,400,'远征成功',48,'text',True)
    b.text(200,474,'废弃货运站',32,'text',True)
    b.text(200,532,'远征队已安全返回星港。',24,'muted')
    b.line(200,598,700,598)
    b.text(200,626,'出发费用（已扣除）',22,'muted')
    b.text(200,670,'信用点 100   /   能源 20',26)
    b.text(200,736,'本次补给消耗',22,'muted');b.text(200,780,'急救包  −1',26,'amber')
    b.box(772,250,988,600,'deep',16)
    b.text(812,286,'回收奖励',28,'text',True);b.text(1440,292,'领取后一次到账',20,'muted')
    for i,(kind,label,qty) in enumerate([('credit','信用点','+300'),('ore','钛矿石','+8'),('alloy','合金板','+2')]):
        y=368+i*132;b.box(812,y,908,108,'card',12);b.icon(836,y+24,kind,60)
        b.text(920,y+34,label,28,'text',True);b.text(1574,y+30,qty,32,'teal',True)
    b.text(812,792,'关闭后仍可从基地再次查看并领取。',22,'muted')
    b.button(160,900,220,'暂不领取');b.text(416,917,'费用不重复扣除 · 奖励仅可领取一次',22,'muted')
    b.button(1380,900,380,'领取并返回基地',True)

def main():
    RENDER=[home,missions,inventory,settings,confirm,result]
    records=[]
    for (key,title,num),render in zip(PANELS,RENDER):
        board=Board(key,title,num,modal=key=='ActionConfirmPanel')
        render(board);path=board.save()
        records.append(dict(panelName=key,title=title,status='pending-user-review',
                            preview=str(path.relative_to(ROOT)).replace('\\','/'),
                            sha256=hashlib.sha256(path.read_bytes()).hexdigest(),
                            canvas=[1920,1080],body=[600,280,720,520] if key=='ActionConfirmPanel' else [120,80,1680,920]))

    review=ROOT/'design-review';review.mkdir(exist_ok=True)
    overview=Image.new('RGB',(2000,2060),'#0B151E');d=ImageDraw.Draw(overview)
    d.text((40,24),'星港拾荒者  /  六面板整体设计 v01',font=f(32,True),fill=C['text'])
    d.text((40,76),'每格保留完整 1920 × 1080 画布，统一缩放至 50%；灰色为审核画布。',font=f(22),fill=C['muted'])
    for i,record in enumerate(records):
        x=24+(i%2)*984;y=136+(i//2)*636
        im=Image.open(ROOT/record['preview']).resize((960,540),Image.Resampling.LANCZOS)
        overview.paste(im,(x,y));d.rectangle((x,y,x+959,y+539),outline=C['line'],width=1)
        d.text((x+8,y+552),f"{i+1:02d}  {record['title']}  /  {record['panelName']}",font=f(22,True),fill=C['text'])
        body=record['body'];d.text((x+8,y+588),f"主体 {body[2]} × {body[3]}   |   宽 {body[2]/1920:.1%} · 高 {body[3]/1080:.1%}",font=f(20),fill=C['muted'])
    overview.save(review/'Img_StarportSalvage_Overview_v01.png')
    (review/'preview-index.json').write_text(json.dumps(dict(schemaVersion=1,status='pending-user-review',panels=records),ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    print(f'Wrote {len(records)} panel previews and overview under {ROOT}')

if __name__ == "__main__":
    main()
