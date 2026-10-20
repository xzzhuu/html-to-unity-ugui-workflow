# 工作区与执行说明

## 调用与模式

保留全局 Skill 的完整目录，脚本只依赖 Python 标准库；不要求 Unity 或 npm 才能初始化。工具路径从已加载 Skill 所在目录解析，不假设某个用户的包安装位置。

```text
python scripts/init_directory.py --root D:/UI/Settings --panel-name SettingsPanel --mode full
python scripts/init_directory.py --root D:/UI/SettingsVisual --panel-name SettingsPanel --mode visual-only
python scripts/validate_directory.py --root D:/UI/SettingsVisual --mode visual-only
```

默认 `full`、1920×1080。`--width/--height` 可明确覆盖；已有工作区保留原分辨率，缺少 mode 的旧工作区按 full 读取。重复初始化不覆盖文件；显式配置或模式冲突时停止。两个分支都保留 `.html-directory.json` 这个兼容性文件名，其中 `mode` 决定路线，视觉分支不因此需要 HTML。

| 内容 | full | visual-only |
| --- | --- | --- |
| 配置、inputs/brief.md、inputs/references/ | 有 | 有 |
| design/layout/、design/element-groups/、design/review/ | 有 | 有 |
| design/element-groups.json | 有 | 有 |
| 拆分素材目录 | html/assets/ | assets/ |
| HTML、CSS、JS、元素/素材/导入清单 | html/ 下生成草稿 | 不生成 |

组图、确认及验收记录不由初始化器代填。仅建目录的请求，交付草稿或目录后停止。已有布局批准且版本适用时从元素组图阶段继续。

## 执行步骤与产出

1. 核对实际面板、原生组件、Sprite、绑定和项目路径，产出实际所需元素清单，不扩展参考类型。
2. 制作或沿用完整面板预览，取得“面板预览图确认”。
3. 每个复用结构独立制作组图，标明真实尺寸、必要状态、图层及拼接效果，取得“元素组图确认”。
4. 导出中性颜色独立 PNG；记录原始/导出像素、九宫格和 Image.color 用法。组图中演示不同 tint 不等于交付多份彩色图片。
5. full：生成并验收 HTML，再按用户需求使用既有转换器与 MCP 导入；visual-only：直接用 Unity MCP 导入并替换原 Prefab。
6. 记录 Unity 实际截图、引用/交互/清晰度检查，完成相应“导入转换完成”或“导入替换完成”。

素材、组图按 [组件前缀](component-naming.md#素材与元素组图命名) 命名。布局尺寸不决定源图像素尺寸；保持原分辨率，通过 RectTransform 控制显示大小。新建适用的屏幕 Canvas 以 1920×1080 为参考；已有 World Space/XR Canvas 保持项目配置。检查 Max Size、压缩与平台覆盖，VR 清晰度需在实际观看距离验证。

## 分支校验

```text
python scripts/validate_directory.py --root <工作区> --require-ready
```

- full 的 ready：面板批准、每组元素批准及素材哈希有效，HTML 结构和视觉验收证据一致；尚不代表 Unity 已验收。
- visual-only 的 ready：同样的设计确认成立，且已经记录 Unity 替换验收、截图及导出素材哈希；不要求 HTML 文件或浏览器证据。
- 普通校验可检查未审核草稿；不能凭校验通过制造用户确认或视觉通过。旧 full 草稿仍可检查，进入 ready 前须补齐新的元素组图确认。

各字段见 [清单契约](manifests.md)。机器核对的是文件和记录一致性，实际美术效果、Unity 引用与目标设备状态仍由真实检查证据支持。

## Unity 操作边界

遵守目标项目的 UIKit/UIRoot、Prefab 固定目录、默认 TMP 字体与 SCM 规范。只更新本次授权的素材和 Prefab，保留 GUID、绑定和 UnityEvent，不自动保存无关场景或改变 XR/平台配置。

full 转换器位置从目标项目实际 Package 配置核实；按其 README 及项目准入规则操作。默认生成目录只是转换器默认值，正式面板归位遵守项目规则。转换器不支持的实际所需控件通过现有原生组件与 MCP 装配；无法完成时报告差距，不扩展 C#。

visual-only 不调用转换器、不安装其依赖；MCP 不可用时报告阻塞。两条路线都不创建用于视觉的 C#、主题 ScriptableObject、Shader、临时 Editor 脚本。运行时交付素材与面板 Prefab；确认后的元素组图和完整面板预览也导入 Unity，置于独立 DesignReferences 目录，作为非运行时参考贴图。清单、确认记录、HTML/CSS/JS 和工具源码仍保留在工程外。

## POT 素材与预览参考图

所有元素素材贴图（底板、按钮、边框、图标、进度图形等）的宽、高分别为 2 的幂次方，例如 1024×1024 底板、1024×128 按钮、128×128 图标；不要求正方形。优先向上取整并从矢量源重制，避免降采样或直接拉伸位图。位图透明补边时记录有效区域与九宫格，保持显示形状；RectTransform 布局仍以 1920×1080 为默认基准。

两项确认完成后，将元素组图与每个完整面板预览导入 Unity。默认目标为 Assets/Art/UI/DesignReferences/{设计集}/ElementGroups/ 和 Panels/；参考图保持原始分辨率与比例，1920×1080 无须改成 POT。Texture/Default、无 mipmaps、NPOT=None、足够大的 Max Size；不挂入运行时面板或打包引用，不创建展示 Canvas。导入前核对审批版本，导入后核对文件哈希及 Texture 尺寸。运行时不引用不代表所有构建管线都会排除它们，按项目构建规则核对，不擅自变更构建设置。

正式面板预览规则：每个面板单独输出，画布使用纯色背景，不叠加场景、照片、纹理、棋盘格等干扰；在面板主体外标注画布宽×高和主体宽×高。默认画布 1920×1080；素材 POT 像素尺寸不约束 RectTransform 布局尺寸，也不要求预览图改成 POT。确认后将组图和完整面板预览原分辨率导入 Unity 独立参考目录。

纯色画布配色要求：画布背景必须与面板主体/元素图片主色明显区分，让边界、圆角和透明效果清楚可见；深色面板可使用中性灰底。不同面板可以使用相同的对比底色，不要求各自采用不同颜色。

VR 素材约束：各元素均使用无边缘描线的填充造型，不制作按钮/底板/内衬等细轮廓或额外 Outline 素材；通过填充色、明暗、透明度与留白表达层级及状态。装饰性细分隔线改用留白。九宫格切片边距和审核尺寸标注线保留；去描边不等于已完成设备抗锯齿验收。

多面板总览：每张完整画布单独成块，统一比例，块间使用不同色间隔区，让画布边界可辨。附主体宽高和占画布宽高的百分比；不能无缝拼接同色画布或逐面板裁切放大。审核边界框允许使用，不进入运行时素材。
