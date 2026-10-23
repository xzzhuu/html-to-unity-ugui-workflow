# 可复用元素组图 v01

整体布局确认原文：“确认，进行下一步”。已核对六张整体预览与上一阶段登记的SHA-256一致，逐面板保存layout-approved.png及真实approval.json。

阶段更新：用户已回复“确定”。元素批准、22张正式PNG及六套HTML已完成；原审核说明留作版本追踪，当前状态见readiness.json，Unity尚未导入。

## 独立组图

| 组 | 审核图 | 重点 |
| --- | --- | --- |
| 底板与卡片 | [Img_Surface](element-groups-v01/Img_Surface_Sheet.png) | 九宫格、着色、多尺寸拼接 |
| 按钮 | [Btn_Action](element-groups-v01/Btn_Action_Sheet.png) | 普通、悬停、按下、焦点、禁用、主次和危险操作 |
| 输入框 | [Inp_Form](element-groups-v01/Inp_Form_Sheet.png) | 占位、输入光标、错误、不可交互 |
| 开关 | [Tgl_Sound](element-groups-v01/Tgl_Sound_Sheet.png) | 开、关、不可交互，轨道与旋钮分层 |
| 滑块 | [Sld_Value](element-groups-v01/Sld_Value_Sheet.png) | 音量、整数数量、重量只读、固定数量 |
| 列表 | [Scr_List](element-groups-v01/Scr_List_Sheet.png) | 任务卡、物品行、携行行、奖励行尺寸与空态 |
| 物品图标 | [Img_Item](element-groups-v01/Img_Item_Sheet.png) | 八种独立轮廓；名称、数量与稀有度为TMP |
| 操作与资源图标 | [Img_Utility](element-groups-v01/Img_Utility_Sheet.png) | 返回、关闭、搜索、确认、风险、锁、能源、重量、信用点 |
| 文字 | [Txt_Typography](element-groups-v01/Txt_Typography_Sheet.png) | 字号、错误、资源缺口、执行中、空与失败状态 |

总览：[九组元素](Img_ElementGroups_Overview_v01.png)。每组原图1920×1080；总览统一缩放至640×360，各画布完整保留。

## 素材复用与导出计划

完整计划见[sprite-export-plan-v01.json](sprite-export-plan-v01.json)。这是计划元数据，不是已经生成的文件。

- Img_RoundedSurface：底板、卡片、列表共用；512×512，九宫格24px。
- Img_Control_Background：按钮与输入框共用；512×128，九宫格20px。
- Img_Circle：开关旋钮与滑块手柄共用；64×64。
- Sld_Value_Bar：同一512×32中性图分别作为轨道与填充，九宫格16px。
- Tgl_Sound_Track：128×64；开关使用轨道与旋钮分层组合。
- 八种物品图形各256×256；九种操作/资源图形各128×128。
- 以上22个计划文件全部保留Alpha、中性白色与全纹理有效区。颜色由Image.color或原生Color Tint提供；不制作颜色副本。
- 图标底座复用底板；图形单独导出。图标外的文字、单位和数量保留TMP。

组图审核阶段，各准备包design/element-groups.json已登记真实组、组件类型、用途及引用位置，assets/sprites暂为空。确认后按图导出并补录真实文件尺寸、哈希与素材归属；不能将当前清单作为ready交付。

## 本次检查

- 查看九组总览，并原尺寸检查操作图标与滑块；核对数量Slider示例范围1～4、当前2的相对位置。
- 同形结构已合并：按钮与输入框、轨道与填充、开关旋钮与滑块手柄分别共享中性PNG。
- 六个工作区普通结构校验均通过；这只证明当前草稿和组图引用的结构一致。
- 尚未验证实际Alpha边缘、九宫格拉伸、PNG导入、浏览器交互、Unity Toggle/Slider装配及中文TMP。它们在导出与实现后分别检查。
- 原生Toggle本身只管理开关状态；旋钮切换图形的组合方式需使用现有原生组件/项目绑定装配并验收，不由本阶段假定已经完成。

## 请确认的范围

确认九组v01的造型、配色示意、尺寸/九宫格、状态区分和复用方案。可以整体确认，也可指定某一组修改。确认不等同HTML/Unity验收。
