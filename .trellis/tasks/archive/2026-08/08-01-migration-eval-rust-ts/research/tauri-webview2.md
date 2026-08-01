# 路线 A：Rust 核心 + TS 前端（Tauri v2）— 调研结论

调研日期：2026-08-01。核实版本：Tauri v2.11.5（2026-07-01 发布，GitHub releases）。

## 总评（结论先行）

四项能力全部"可行"，但存在一个与产品约束（"接近单 exe + 目标机可能无 WebView2"）**根本冲突**的硬依赖：Tauri Windows 应用运行强依赖系统 WebView2 Runtime，且该 Runtime **无法内嵌进 exe**（只能系统安装、或随安装器外带）。因此：

- "raw exe 单文件分发"可行，但前提是目标机已装 WebView2（Win11 默认有，**Win10 包括 LTSC 2021 不保证**）。
- "完全离线、零依赖单 exe"**不可行**，这是路线 A 最大的否决性风险点。

## Q1 产物形态

- `tauri build` 产出：`src-tauri/target/release/<app>.exe`（**前端资源在编译期内嵌进 exe**，官方构建文档原文 "This step also inlines your previously generated Frontend files into the executable"）；安装器 `bundle/nsis/<app>-setup.exe` 与 `bundle/msi/<app>.msi`（WiX 或 NSIS，默认 per-user 安装到 `%LOCALAPPDATA%`）。
- 无 `.dll` 依赖时 raw exe 可直接拷贝分发（"单 exe 可分发"= 是），但需目标机已具备 WebView2 Runtime + MSVC 运行时（见风险 1/2）。
- 额外文件仅当配置了 `bundle.resources`（资源目录）或 `externalBin`（sidecar）才会出现；本项目"config 目录随 exe 放置"不依赖 bundle，属运行时文件系统逻辑（见 Q3）。

依据：https://v2.tauri.app/distribute/windows-installer 、构建流程文档（"inlines frontend files"）、https://github.com/tauri-apps/tauri/releases

## Q2 WebView2 Runtime 依赖（最大风险）

- **必须**：wry 基于 WebView2，Tauri Windows 应用运行必须有系统级 WebView2 Runtime，无回退（tauri issue #4886 "WebView2 requirement is very recent and Tauri has no fallback"）。
- **Win11 预装**；**Win10 不预装**（微软 Edge 团队 2022-06 官方博客原文："Starting with Windows 11, the WebView2 Runtime is included as part of the operating system. For Windows 10, we have recommended developers to distribute and install the runtime with their applications"；此后 Win10 消费版由 Windows Update 波浪式推送，但**不保证**）。
- **Win10 LTSC 2021**：WebView2 支持 LTSC（官方支持列表含 "Windows 10 IoT Enterprise LTSC 2019"），但 LTSC 镜像**不含 Edge/EdgeUpdate 组件，WebView2 不预装**，目标机存在与否不可假设，需手动安装或随应用分发。
- 四种分发方式（Tauri `webviewInstallMode`，见 Windows Installer 文档）：
  | 模式 | 机制 | 体积/要求 |
  | --- | --- | --- |
  | `downloadBootstrapper`（默认） | 安装器联网下载 Evergreen bootstrapper 并安装 | 需联网；Evergreen 为系统级安装，可能需管理员权限 |
  | `offlineInstaller` | 内嵌 bootstrapper，可离线安装 | **安装器 +约 127MB** |
  | `fixedRuntime`（Fixed Version） | 随应用分发固定版本 WebView2 位（Chromium 完整拷贝） | x64 单架构解压后**数百 MB 量级**，社区实测报告 "over 500MB"（tauri discussion #3048）；Fixed Version 无自动更新，需自己跟进安全补丁 |
  | `skip` | 不检查不安装 | 无 Runtime 则应用直接无法启动 |
- **结论**：Evergreen（默认）体积最小但需联网+可能需管理员；离线/受控环境只能选 offlineInstaller（+127MB）或 fixedRuntime（数百 MB）或运维侧预装。无论如何，**"一个无依赖 exe"在产品约束下不可达成**——这是路线 A 与此项目"单 exe 便携分发"定位的根本冲突点。

依据：https://v2.tauri.app/reference/webview-versions 、https://v2.tauri.app/distribute/windows-installer 、https://learn.microsoft.com/en-us/microsoft-edge/webview2/concepts/distribution 、https://blogs.windows.com/msedgedev/2022/06/27/delivering-the-microsoft-edge-webview2-runtime-to-windows-10-consumers/ 、https://github.com/tauri-apps/tauri/issues/4886 、https://github.com/orgs/tauri-apps/discussions/3048

## Q3 Rust / TS 侧能力

- **spawn 子进程（pwsh/cmd/wt.exe）+ 工作目录（零 shell）**：可行。Rust 侧 `std::process::Command`（`args` 数组 + `current_dir`）与现有 C# `Process.ArgumentList` 语义等价，底层同为 CreateProcessW。注意：`cmd /k` 的引号陷阱是 cmd 自身行为，**迁移不消除**，仍需沿用"目录经 WorkingDirectory 传递、argv 数组传参"的既有规避（pwsh `-Command` 走标准 argv 解析，单引号 `'`→`''` 转义规则照旧）。
- **定位 exe 自身路径**：可行且可靠，Rust 侧 `std::env::current_exe()` 跨平台可用；本项目 `ResolveConfigDir`（向上搜索含 `config/` 的祖先）逻辑可直接用该 API 复刻，纯 std，无 Tauri API 依赖。
- **TS 侧读写 exe 旁文件**：可行，但有一个**文档陷阱**——
  - 官方路径 API `executableDir()` 在 **Windows 为 "Not supported"**（文档原文，macOS 同，仅 Linux 有实现）；
  - 替代：`resourceDir()` 在 **Windows 恰好解析为 "the directory that contains the main executable"**（文档原文），即 exe 所在目录。TS 用 `resourceDir()` + `@tauri-apps/plugin-fs` 的 `readTextFile`/`writeTextFile` 即可读写 exe 旁文件。
  - 注意：fs 插件默认最小权限，需在 `capabilities` 中显式放行路径 scope，且插件拒绝 `../` 路径穿越；更稳妥的方案是文件 I/O 全部收进自定义 Rust command（内部用 `current_exe()`），TS 只做调用。
- 综上：三条技术路径（Rust 侧全权、TS 侧 resourceDir+fs、或混合）均可行，能力项无缺口。

依据：https://v2.tauri.app/reference/javascript/api/namespacepath/ （executableDir / resourceDir 平台说明原文）、https://v2.tauri.app/plugin/file-system 、https://docs.rs/tauri/latest/tauri/path/ 、https://www.reddit.com/r/tauri/comments/11adbst/path_to_current_executables_directory/

## Q4 构建复杂度与体积

- **构建**：`tauri build` = 前端构建（beforeBuildCommand）+ Rust 编译（内嵌前端资源）+ 打安装器。Windows 需 MSVC 工具链 + Node；WiX/NSIS 仅 Windows 主机可构建（NSIS 支持 cargo-xwin 交叉编译）。
- **CI 可复现性**：好。官方文档给出 GitHub Actions 标准流程（`tauri-action` 或 `dtolnay/rust-toolchain` + `swatinem/rust-cache` + `setup-node`），`Cargo.lock` + `package-lock.json` 双锁文件；windows-latest runner 自带 MSVC。
- **体积量级**：官方宣传最小 600KB（极限裁剪）；实际 v2 hello-world / 真实小应用 exe 约 **3–10MB**（Reddit 实测：功能完整的笔记类应用安装包 10MB、内存占用 ~50MB）。不含 WebView2（系统级）。相比当前 C# 自包含 .NET 发布（几十 MB 级）有优势，但计入 WebView2 分发后总体并不更小。

依据：https://v2.tauri.app/distribute/pipelines/github 、https://github.com/tauri-apps/tauri-action 、https://www.reddit.com/r/rust/comments/1nvvoee/built_a_desktop_app_with_tauri_20_impressions/

## 风险清单

1. **[高] WebView2 依赖与产品约束冲突**：LTSC 2021 不保证预装；离线受控环境需 offlineInstaller（安装器 +127MB）或 fixedRuntime（数百 MB）或运维预装；"零依赖单 exe"不可达成。
2. **[中] MSVC 运行时**：Tauri 安装器默认不捆绑 VC++ 运行库（vcruntime140/msvcp140），目标机缺失则启动失败；可用 `-C target-feature=+crt-static` 静态链接规避（tauri discussion #3048 实测可行）。
3. **[中] `executableDir()` 文档陷阱**：Windows 不支持，若照文档直觉写会踩坑；必须用 `resourceDir()`（Windows=exe 目录）或自定义 Rust command。
4. **[低-中] 构建链复杂度上升**：前端资源内嵌 exe → 任何前端改动都触发完整发布流程；CI 需要 Node + Rust 双工具链（当前项目只有 dotnet 单链）。
5. **[低] 安装器默认路径与便携模型冲突**：NSIS/MSI 默认装到 `%LOCALAPPDATA%`，与"exe 旁生成 config"定位不符；便携分发应直接分发 raw exe（target/release），不走安装器。
