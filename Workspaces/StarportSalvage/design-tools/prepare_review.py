"""Record requirement coverage for draft workspaces without granting approval."""
from pathlib import Path
import json
import hashlib

ROOT=Path(__file__).resolve().parents[1]
SOURCE=ROOT.parents[1]/'docs/game-example'
definitions={
 'StarportHomePanel':('星港基地',['Button','Image','Text'],
  '队长名称、信用点、能源、携行重量、远征入口、仓库入口、任务状态、设置与重置入口。',
  '无任务、执行中、待领取；点击进入子页，任务状态由接入游戏维护。'),
 'ExpeditionSelectPanel':('远征选择',['Button','ScrollView','Image','Text'],
  '三项任务动态卡片、风险、费用、预计时长、锁定说明、奖励、推荐补给、携行摘要与出发操作。',
  '正常、业务选中、锁定、资源不足、执行中、待领取、加载、错误、空列表。'),
 'SupplyInventoryPanel':('仓库整备',['Button','InputField','ScrollView','Slider','Image','Text'],
  '分类、名称搜索、物品动态列表、物品详情、补给数量选择、携行列表、重量、返回与清空。',
  '选中、零库存、材料不可携带、数量固定、正常数量滑块、空列表、搜索空态、超重、任务锁定。'),
 'GameSettingsPanel':('游戏设置',['Button','InputField','Toggle','Slider','Image','Text'],
  '队长名称输入、音效开关、音量与百分比、低中高画质按钮、恢复默认、取消与应用。',
  '表单草稿、应用、取消、名称为空或超长、音效开关、不同音量与画质。'),
 'ActionConfirmPanel':('操作确认',['Button','Image','Text'],
  '单一弹窗结构：标题、内容、费用、携行摘要、关闭、取消与确认；游戏层提供输入阻断遮罩。',
  '出发确认、清空携行、重置演示；初始预览展示出发模式，其他模式文案见UI需求。'),
 'ExpeditionResultPanel':('远征结算',['Button','ScrollView','Image','Text'],
  '任务结果、任务名称、已扣费用、补给消耗、奖励动态列表、暂不领取与领取返回。',
  '待领取、领取处理中、已领取、无补给消耗；点击领取只处理一次。')}

for key,(title,components,content,states) in definitions.items():
    root=ROOT/'Panels'/key
    brief=f'''# {title} / {key}

状态：需求已整理，整体预览 v01 待用户确认；没有设计批准或就绪记录。

## 需求依据

- [游戏需求](../../../../../docs/game-example/game-requirements.md)
- [UI 需求](../../../../../docs/game-example/ui-requirements.md)
- 需求文档用于内容与交互约束，实际执行流程遵循用户指定的 ui-panel-design-to-html Skill。

## 必需内容

{content}

## 必需状态

{states}

当前整体图仅展示主要状态；其他状态列为后续元素组图与实现验收项目，尚未完成。

## 组件与交付

- 仅使用：{', '.join(components)}。Container 作为结构节点，不新增无关控件。
- 画布 1920×1080；主体 {'720×520，位置(600,280)' if key=='ActionConfirmPanel' else '1680×920，位置(120,80)'}。
- 预览：[v01](../design/layout/Img_{key}_Preview_v01.png)。
- 运行文本保持 TMP；正式素材 POT；同形色差共用中性素材；动态项使用原生纵向 ScrollRect。
- 独立准备目录保留在 Unity 外，暂存目标 Assets/HtmlToUGUIStage/{key}/。
- 现有 HTML 为初始化器生成的未审核占位草稿，不是本预览的实现。

## 样本口径

基地/远征/仓库/确认图使用已整备状态：信用点2400、能源100、急救包携带3、电池携带2，总重7.0kg。仓库数量未扣。结算图使用货运站待领取结果：信用点+300、矿石+8、合金板+2、急救包-1。初始空携行与其他状态另行验收。
'''
    (root/'inputs/brief.md').write_text(brief,encoding='utf-8')
    manifest=root/'design/element-groups.json'
    data=json.loads(manifest.read_text(encoding='utf-8'))
    data['requiredComponents']=components
    manifest.write_text(json.dumps(data,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')

sources={name:hashlib.sha256((SOURCE/name).read_bytes()).hexdigest() for name in ['game-requirements.md','ui-requirements.md']}
(ROOT/'design-review/source-requirements.json').write_text(json.dumps(dict(source='docs/game-example',sha256=sources),ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print('Recorded six requirement briefs and actual component subsets; approvals remain absent.')
