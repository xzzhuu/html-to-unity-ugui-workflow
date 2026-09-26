# Unity sample project

使用 Unity 2022.3 打开此目录。原始本机工程版本为 `2022.3.62f3c1`；其他 2022.3 补丁版的兼容性尚未重新验证。

`Packages/manifest.json` 通过 `file:../../Packages/com.nexvr.html-to-ugui` 引用仓库内的包，路径相对于项目 `Packages/`。Unity 会在首次打开时重新解析依赖与生成 lock 文件。本机 Unity MCP SDK 不属于示例运行依赖。

本工程保留示例 Prefab、PNG 和场景，但不分发第三方字体、TMP Essential Resources 和厂商示例。首次打开需：

1. 用 `Window → TextMeshPro → Import TMP Essential Resources` 恢复 TMP 必需资源。
2. 自行准备具有中文字符的 TMP 字体资源。
3. 从 `../Examples/` 重新导入所需 HTML，在导入窗口选择字体，以重建字体和材质引用。
4. 检查 Console、场景和 Prefab，手动保存场景。

现有 Prefab 的字体引用可能在重新导入前缺失。不要将本机旧字体直接提交；确认许可后再引入替代资源。仅运行 Python 验证无法确认工程能在新机器中编译或达到视觉一致性。
