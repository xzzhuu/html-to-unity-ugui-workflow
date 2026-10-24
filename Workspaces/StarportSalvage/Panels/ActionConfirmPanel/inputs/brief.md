# 操作确认 / ActionConfirmPanel

状态：整体预览与元素组图v01已获真实确认；HTML已完成并通过浏览器验收，原生 UI 已完成标准验收；业务接线与设备仍未测。

## 需求依据

- [游戏需求](../../../../../docs/game-example/game-requirements.md)
- [UI 需求](../../../../../docs/game-example/ui-requirements.md)
- 需求文档用于内容与交互约束，实际执行流程遵循用户指定的 ui-panel-design-to-html Skill。

## 必需内容

单一弹窗结构：标题、内容、费用、携行摘要、关闭、取消与确认；游戏层提供输入阻断遮罩。

## 必需状态

出发确认、清空携行、重置演示；初始预览展示出发模式，其他模式文案见UI需求。

整体图展示主要状态；边界状态已在HTML交互演示与浏览器验收中覆盖，见design/review/html-acceptance.md。

## 组件与交付

- 仅使用：Button, Image, Text。Container 作为结构节点，不新增无关控件。
- 画布 1920×1080；主体 720×520，位置(600,280)。
- 预览：[v01](../design/layout/Img_ActionConfirmPanel_Preview_v01.png)。
- 运行文本保持 TMP；正式素材 POT；同形色差共用中性素材；动态项使用原生纵向 ScrollRect。
- 独立准备目录保留在 Unity 外，暂存目标 Assets/HtmlToUGUIStage/StarportSalvage/ActionConfirmPanel/。
- 现有HTML是已验收实现；审核标注由浏览器层提供，源码可供转换。

## 样本口径

基地/远征/仓库/确认图使用已整备状态：信用点2400、能源100、急救包携带3、电池携带2，总重7.0kg。仓库数量未扣。结算图使用货运站待领取结果：信用点+300、矿石+8、合金板+2、急救包-1。初始空携行与其他状态另行验收。
