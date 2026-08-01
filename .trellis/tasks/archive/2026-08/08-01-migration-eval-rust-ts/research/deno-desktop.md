# 调研：`deno desktop` 作为 Windows 启动器单 exe 方案的可行性

调研时间：2026-08-01。对象：Deno 2.9.x（稳定版 2.9.3，2026-07-15 发布）新增的 `deno desktop` 命令（experimental），将 TS/Web 项目编译为自包含桌面应用。

---

## 1. `deno desktop` 当前状态：experimental，版本要求 v2.9.0+，Windows 支持不成熟

**结论**：`deno desktop` 自 Deno v2.9.0（2026-06-25 发布）起可用，官方明确标注 **experimental**，API 仍在稳定化过程中，Windows 上有已关闭但未公开根因的官方示例失败 bug。不适合作为生产依赖。

**依据**：
- 官方发布博客明确原文："`deno desktop` is experimental in 2.9. The surface described here is stabilizing and some platform features are still landing."（https://deno.com/blog/v2.9）
- 官方文档："`deno desktop` is available starting in Deno v2.9.0"（https://docs.deno.com/runtime/desktop 、https://docs.deno.com/runtime/desktop/backends）
- Windows 官方示例失败 bug：#35562 中 Deno v2.9.0 官方示例在 Windows 上 webview/raw 后端直接报错（CEF 正常，mac/Linux 正常），issue 已关闭并分配给维护者，但抓取到的内容中没有公开根因说明（https://github.com/denoland/deno/issues/35562）
- 讨论 #36118：Deno 2.9.3 / Windows 11 上 webview 后端窗口事件（close/move/resize）不触发、`frameless: true` 无效、Nuxt 4 示例无法运行，用户已安装 WebView2（https://github.com/denoland/deno/discussions/36118）

**风险**：🔴 高。experimental 阶段 API 可能变动（Deno 团队自己称 "The surface ... is stabilizing"），且 Windows 平台是 bug 重灾区；社区已有"官方示例在 Windows 跑不起来"的一手报告。

---

## 2. 产物形态：Windows 默认输出是"目录 + DLL"，不是单 exe；无 WebView2 时 webview 后端白屏

**结论**：Windows 上默认输出**不是单个 exe**，而是一个目录（`.bat` 启动器 + `denort.dll` + 渲染后端 DLL + CEF 支持文件）；`--compress` 可产出"单文件自解压包"（首次启动解压到每用户数据目录）；`.msi` 安装器按机器安装到 %ProgramFiles%。webview 后端依赖系统 WebView2（Windows）/ WebKit（mac/Linux）。**Win10 LTSC 2021 默认没有 WebView2 Runtime**，webview 后端会白屏/冻结。

**依据**：
- Windows 默认目录布局（官方 Distribution 文档原文）：`MyApp/` 下为 `MyApp.bat`（launcher）、`denort.dll`（Deno runtime + 你的代码）、`*.dll`（rendering backend and CEF libraries）、`resources.pak, locales/`（CEF support files）、`AppIcon.ico`（可选）；签名需对"backend .exe 和 denort.dll"外部 signtool（https://docs.deno.com/runtime/desktop/distribution）
- `--compress`：自解压包，"unpacked to a per-user data directory on first launch"，示例 "a webview hello-world drops from about 66 MB to 19 MB"（xz 编码）（https://docs.deno.com/runtime/desktop/distribution）
- 博客称产物是 "a standalone binary ... `.exe` or an `.msi` on Windows"（https://deno.com/blog/v2.9）——与 Distribution 文档的"目录默认输出"存在口径差异，实践中以目录为准（社区实测也是目录：https://news.ycombinator.com/item?id=48626137 中 Windows 10 实测 CEF hello world = 442 MB，libcef.dll 247 MB + deno-test.dll 78 MB）
- 后端体积：CEF 后端 "~150 MB for the framework alone"；webview 后端最小（"just your code + the backend shim"）（https://docs.deno.com/runtime/desktop/backends）
- webview 后端 = "WebView2 on Windows, WebKitGTK on Linux, WKWebView on macOS"（https://docs.deno.com/runtime/desktop/backends）
- WebView2 依赖：Microsoft 官方分发文档明确 "WebView2 requires that Microsoft Edge WebView2 Runtime is installed"，且 "A small number of Windows 10 devices don't have the WebView2 Runtime installed. We recommend that you handle this edge case"（https://learn.microsoft.com/en-us/microsoft-edge/webview2/concepts/distribution）；WebView2 Runtime 是独立组件，不在 Windows 10 出厂镜像中（2021 年起靠 Windows Update / Office 自动分发，https://learn.microsoft.com/en-us/microsoft-365-apps/deploy/webview2-install）；LTSC 长期服务版不接收 Edge/WebView2 更新流，**LTSC 2021 需手动安装 Evergreen 或固定版本 Runtime**
- 社区实证：HN 上 Windows 10 用户用默认 webview 后端跑官方 hello world，"opens an empty white window, freezed (with waiting cursor...)"（https://news.ycombinator.com/item?id=48626137）

**风险**：🔴 高（对本项目）。本项目明确要求"单 exe 分发"：默认输出是目录（bat + dll），`--compress` 虽单文件但首次启动要解压到 %LOCALAPPDATA%；且 webview 后端在目标环境（Win10 LTSC 2021，无 WebView2）直接不可用，只能用 CEF 后端 → 体积 ~400 MB+。体积与"单文件可移植"诉求双双落空。

---

## 3. UI 能力：标准 Web 技术栈，本项目的 UI 需求全部可做；bindings 为进程内 JSON 通道

**结论**：UI 层就是 Chromium 系引擎里的 HTML/CSS/JS——列表卡片、复选框、模态对话框、主题切换、中文 IME 全部是标准能力（CEF = 完整 Chromium；WebView2 亦支持 IME）。webview ↔ Deno 的 bindings 是**进程内通道**（非 IPC），参数/返回值按 JSON 序列化（额外支持 Uint8Array）。菜单（应用/上下文菜单）已有 `Deno.Menu` 原生 API。

**依据**：
- bindings 文档："Calls go through in-process channels, and the backend dispatches them from its run loop"；`win.bind("name", handler)` ↔ webview 侧 `bindings.name(...)`，返回 Promise；序列化限制：`Date`/`Map`/`Set`/`RegExp`/typed arrays（除 Uint8Array）/函数/循环引用不可传递，"Stick to plain data and Uint8Array on both sides"（https://docs.deno.com/runtime/desktop/bindings）
- CEF 后端 "Full web platform support, including modern CSS, ES modules, WebGPU, and WebRTC"；DevTools 仅 CEF 支持（https://docs.deno.com/runtime/desktop/backends）
- 原生菜单、托盘、窗口生命周期文档已列出（Menus、Windows 章节：https://docs.deno.com/runtime/desktop）
- 中文 IME：无官方专项文档，属 Chromium 渲染引擎标准能力（WebView2/CEF 均基于 Chromium），推断风险低

**风险**：🟡 中。UI 本身无风险；bindings 的 JSON 序列化限制需要设计上把跨层数据限制为纯数据（本项目模型为不可变 record，天然适配）。webview 后端存在功能差异（"Some web features may be missing or behave differently"），但本项目 UI 简单，影响小。

---

## 4. 系统能力：Deno.Command 支持 cwd 零 shell 启动；Deno.execPath 可取 exe 自身路径（桌面应用无禁用证据）

**结论**：可以。`Deno.Command`（稳定 API）支持 argv 数组 + `cwd` 选项 + 权限（`--allow-run`），等价于本项目"零 shell 启动"约束（Process.ArgumentList 对应物）；`Deno.execPath` 返回当前可执行文件路径，可用于"exe 旁找 config/"。未发现 deno desktop 应用禁用这些能力的文档或报告。

**依据**：
- 官方教程：`Deno.Command` 通过 args 数组 + cwd 选项 spawn 子进程，"Spawned subprocesses do not run in a security sandbox"（https://docs.deno.com/examples/subprocess_tutorial 、https://docs.deno.com/examples/subprocess_env_cwd 、https://docs.deno.com/api/deno/subprocess）
- `Deno.execPath` 是获取编译产物 exe 路径的标准方式（社区问答确认编译产物中可用：https://stackoverflow.com/questions/70289014/how-to-get-absolute-path-of-compiled-deno-executable）；API 文档页 https://docs.deno.com/api/deno/Deno.execPath/
- deno compile 内嵌虚拟文件系统解压目录优先级为"exe 旁目录"→平台数据目录（Windows: %LOCALAPPDATA%）（https://docs.deno.com/runtime/reference/cli/compile）；desktop 的 --compress 模式解压到每用户数据目录（https://docs.deno.com/runtime/desktop/distribution）

**风险**：🟡 中。能力存在但有两个坑：(a) `--compress`/自解压模式下代码实际运行于解压目录，但 `Deno.execPath` 仍指向真实 exe，定位自身没问题；需要 spike 验证。(b) 若用 .msi 安装到 %ProgramFiles%，"exe 旁写配置"需要管理员权限——本项目是 unpackaged 自包含部署，应分发目录或单文件包而非 MSI，规避此问题。另注意 `deno compile` 曾有限制（chdir 在编译产物中受限，https://github.com/denoland/deno/issues/26175），subprocess cwd 不受影响。

---

## 5. 局限性 / 已知坑 / 社区反馈：experimental 稳定性证据充分，社区评价两极但普遍认为不成熟

**结论**：官方承认 experimental；社区一手反馈显示 Windows 上 webview 后端故障率高（官方示例白屏/报错、窗口事件不触发）、文档与实现有出入（Deno 团队在 HN 上承认 docs 不准确）、体积宣传与实际测量差距大（CEF hello world 实测 442 MB）。"适合生产"的正面声音主要来自 Deno 生态内人士，且集中在体积小/免 Chromium 两点；独立开发者实测多为负面。

**依据**：
- 稳定性证据：#35562（官方示例 Windows 失败，closed 无根因说明）、#36118（Win11 webview 事件失灵等，2.9.3 仍存在）（https://github.com/denoland/deno/issues/35562 、https://github.com/denoland/deno/discussions/36118）
- 文档准确性问题：HN 上用户按 docs 默认配置跑出白屏，Deno 团队成员 crowlKats 回复 "apologies, this is inaccurate currently, will get things updated"（https://news.ycombinator.com/item?id=48626137）
- 体积实测：Windows 10 CEF hello world 442 MB（libcef.dll 247 MB + deno-test.dll 78 MB）——"I thought it would be smaller than an Electron build, but it's far worse"；webview 后端 + --compress 可达 ~15-19 MB（社区与官方数据一致）（https://news.ycombinator.com/item?id=48626137 、https://docs.deno.com/runtime/desktop/distribution）
- 批评观点：v3ss0n "A strip down version of electron TBH"；aabhay 担心框架长期维护风险（Deno 生态收购潮）；kodablah 报告 Google 拦截内嵌浏览器框架登录（CEF 通用问题）（https://news.ycombinator.com/item?id=48626137）
- 中文社区亦有跟进报道，未超出上述结论（https://azukiazusa.dev/en/blog/deno-desktop-app 、https://www.developersdigest.tech/blog/deno-desktop-native-apps-2026 明确 "experimental in 2.9: APIs can move"）

**风险**：🔴 高（生产采纳层面）。experimental + Windows 高频故障 + 文档失真，三者叠加。

---

## 最终判断：**不适合**作为本项目（Windows 启动器）的单 exe 方案

理由按权重排序：

1. **产物形态不符**：Windows 默认输出是"目录（.bat + denort.dll + 后端 DLL）"，不是单 exe；`--compress` 的单文件是自解压包（首启解压到 %LOCALAPPDATA%），不是可移植单 exe。本项目的"单 exe + exe 旁 config/"分发模型需要绕过默认形态，靠 spike 验证。
2. **目标环境 WebView2 缺失**：Win10 LTSC 2021 无 WebView2 Runtime，默认 webview 后端白屏；被迫用 CEF 后端 → 体积 ~400 MB（远超 Electron 的单例 100 MB 对比口径），且 libcef.dll 仍需随包分发，进一步偏离"单文件"。
3. **experimental 稳定性**：官方明示 API 稳定化中；Windows 上官方示例即失败的 closed bug、2.9.3 仍存在的 webview 事件问题、团队承认的文档失实——距生产依赖差距大。
4. 系统能力（spawn + cwd、execPath）本身满足需求，但这是 Deno 运行时既有能力，非 desktop 加分项；若真要评估 Deno 路线，应等到 desktop 脱离 experimental 且 Windows webview 故障收敛后再复审。

**保留意见**：若未来 (a) desktop 稳定、(b) 默认输出支持真正单 exe（或接受自解压模型）、(c) 提供 WebView2 缺失检测/自备运行时方案，则纯 TS 方案的开发效率（HTML/CSS/TS + 进程内 bindings + Deno.Command）确实高于 C# WinUI。当前时间点不建议立项。

## 附：关键来源清单

- https://deno.com/blog/v2.9 （官方发布博客：experimental 声明、产物与后端说明）
- https://docs.deno.com/runtime/desktop （desktop 主文档）
- https://docs.deno.com/runtime/desktop/backends （三种后端：体积、依赖、平台差异）
- https://docs.deno.com/runtime/desktop/distribution （Windows 目录布局、--compress、MSI、签名）
- https://docs.deno.com/runtime/desktop/bindings （bindings 机制与序列化限制）
- https://docs.deno.com/runtime/desktop/configuration （desktop 配置块、deep links 未实现等限制）
- https://docs.deno.com/examples/subprocess_tutorial 、https://docs.deno.com/examples/subprocess_env_cwd 、https://docs.deno.com/api/deno/subprocess （Deno.Command / cwd）
- https://docs.deno.com/api/deno/Deno.execPath/ （可执行文件路径 API）
- https://github.com/denoland/deno/issues/35562 （官方示例 Windows webview/raw 失败）
- https://github.com/denoland/deno/discussions/36118 （Win11 webview 事件失灵、frameless 无效）
- https://news.ycombinator.com/item?id=48626137 （HN 讨论：体积实测 442 MB、Win10 白屏、团队承认文档失实、稳定性争论）
- https://learn.microsoft.com/en-us/microsoft-edge/webview2/concepts/distribution 、https://learn.microsoft.com/en-us/microsoft-365-apps/deploy/webview2-install （WebView2 Runtime 分发与缺失处理）
- https://www.developersdigest.tech/blog/deno-desktop-native-apps-2026 （第三方："experimental in 2.9: APIs can move"）
