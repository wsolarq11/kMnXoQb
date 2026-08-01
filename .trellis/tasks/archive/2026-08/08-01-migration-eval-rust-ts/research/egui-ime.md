# egui/eframe（Rust）重写可行性研究：IME、版本、发布、字体、系统能力

核验日期：2026-08-01。目标：纯 Rust + egui/eframe 重做 Windows 桌面启动器（单 exe、中文界面 + 中文输入）的可行性。
旧 POC（`archive/launchpad-rs`）为 egui 0.31，本报告核实 0.35 时间点的现状。配套文档：`poc-gap.md`（POC 与 C# 版功能差距）。

---

## 1. 中文 IME 输入（最关键）— 结论：最新版可靠，0.34 起已修复

**结论先行：在最新 egui 0.35.0 中，Windows 中文输入（拼音组词、候选、上屏、光标）可靠，达到"与原生应用一致"的水准。** 历史问题集中在 0.23~0.28 时代，2024-2026 年 egui 团队（作者 umajho）对 IME 做了系统性重做，0.34.0 是分水岭。

### 关键修复时间线

| 版本 | 日期 | 内容 | 依据 |
|---|---|---|---|
| 0.23.0 | 2023-09-27 | 仅 TextEdit 聚焦时启用 IME/软键盘（#3362） | egui-winit CHANGELOG |
| 0.26.0 | 2024-02-05 | `clipboard_text` / `allow_ime` 状态公开（#3724） | 同上 |
| 0.28.0 | 2024-07-03 | "IME for chinese"（#4436）——修复 #3532/#4430 | 同上；issue 关闭记录 |
| 0.29.0 | 2024-09-26 | 修复 IME 输入后退格失效（#4912） | 同上 |
| 0.31.0 | 2025-02-04 | 重开 Linux IME 支持（#5198） | 同上 |
| **0.34.0** | **2026-03-26** | **"Much improved IME"（#7967）——本报告认为的核心修复** | 同上 |
| **0.35.0** | **2026-06-25** | **组词（preedit）视觉效果 + 光标/激活段渲染（#8083）**；`owns_ime_events` 公开（#7983） | egui CHANGELOG 0.35.0 |

### PR #7967 "Much improved IME"（0.34.0）详情

- 方案：在 **egui-winit 平台层**过滤 IME 已消费的键盘事件（事件进 egui 后"简化丢信息"），并回滚 #4794（在 egui 内过滤导致事件顺序错乱）。
- Windows 检测方式：`logical_key == NamedKey::Process`（即 VK_PROCESSKEY），绕开 winit bug [rust-windowing/winit#4508](https://github.com/rust-windowing/winit/issues/4508)（Windows 上 IME 处理过的 KeyboardInput 仍被发出）。
- 修复的具体 issue：
  - [#7809](https://github.com/emilk/egui/issues/7809) — Windows 日文 IME 候选确认后单行 TextEdit 焦点丢失
  - [#7876](https://github.com/emilk/egui/issues/7876) — Windows 多行 TextEdit 按 Enter 确认日文时误插换行
  - [#7908](https://github.com/emilk/egui/issues/7908) — Windows 中文 IME（如搜狗）组词期间退格误删字符
  - 韩文组词时光标/退格异常（#4794 引入的回归）
- 测试矩阵含 **Windows 11 23H2 + 微软拼音**；中/日文 IME 用户在 Win11 上确认"working very well, like other applications that supports IME"。

### 候选窗位置（组词候选框跟随光标）

egui-winit（main，即 0.35 线）逐帧调用 `window.set_ime_cursor_area(rect, Position::Bottom)` 与 `set_ime_purpose`（源码 `crates/egui-winit/src/lib.rs`，`ime_rect_px` 来自 `egui::Event::Ime` 的 rect）。候选窗是 IME 自己绘制的，位置由该 API 锚定——候选窗贴输入框光标，无"候选窗飘在左上角"问题（该问题只出现在不调此 API 的旧应用）。

### 已知残留问题（风险标注）

1. **Windows 韩文 IME 光标索引 bug**（winit 上报 `Some((0,0))` 应为 `Some((1,1))`）：egui-winit 有 workaround，0.35 提供运行时开关 `ui.style.visuals.ime_composition.legacy_visuals`（默认开启旧视觉）。**中文不受影响**。依据：[#8083](https://github.com/emilk/egui/pull/8083) 讨论。
2. **winit #4508 未在上游修复**：egui-winit 用 VK_PROCESSKEY 检测兜底（源码 TODO 注释），行为正确但依赖该 workaround 持续存在。
3. **web 端**（Firefox 部分、Safari 较差）preedit 视觉不完整——与本项目 Windows 桌面无关。
4. **#3532 "IME input broken"（2023-11）名义上仍 open**：维护者 2024-05 评论"most likely fixed by #4436"，属陈旧未关闭，非现行问题。
5. **中文显示必须自配字体**：egui 内置字体不含 CJK（#3532 评论区"中文全是方块"），不配字体输入/显示中文皆失败。见第 4 节。
6. 历史 issue 关闭记录（均已修复）：[#4354](https://github.com/emilk/egui/issues/4354)（中文只能成功输入一次，closed 2024-04-22）、[#4430](https://github.com/emilk/egui/issues/4430)（只能在首位置输入，closed 2024-05-10）、[#3060](https://github.com/emilk/egui/issues/3060)（"Supporting CJK" closed as not planned——指内置 CJK 字体不做，非 IME 不做）。

---

## 2. 当前版本号与 Windows 成熟度

**结论：最新稳定版 egui/eframe/egui-winit 0.35.0（2026-06-25 发布），Windows 平台成熟，有大量生产级桌面应用。** 旧 POC 的 0.31 → 0.35 跨度大（0.32~0.35 均有 breaking change），迁移 POC 需按新 API 改写。

- 版本线：0.35.0（2026-06-25，标题 "Inspection, egui_mcp, classes and improved IME"）→ 0.34.x（2026-03~05）→ 0.33.0（2025-10-09，`egui::Plugin`）→ 0.32.0（2025-07-10）。依据：[Releases](https://github.com/emilk/egui/releases)、[docs.rs/crate/egui](https://docs.rs/crate/egui/latest)。
- **MSRV：Rust 1.92+**（0.34.0 起，[#7793](https://github.com/emilk/egui/pull/7793)；0.33.0 为 1.88）。
- 渲染器：0.33.0 起 **wgpu 成为 eframe 默认渲染器**（[#7615](https://github.com/emilk/egui/pull/7615)），glow（OpenGL）仍可选；Windows 上两者均可用（wgpu 走 D3D12/Vulkan，glow 走系统 opengl32.dll）。
- Windows 成熟度佐证：eframe 是原生 winit 窗口（非自绘窗口），窗口状态持久化内置；生产应用如 komorebi（Windows 平铺 WM，也是 Rust+eframe）、Rerun 等。
- 风险标注：egui 版本间 breaking change 频繁（README 明示 "New releases will have breaking changes"），锁版本 + 升级计划需纳入路线。

---

## 3. 单 exe 发布

**结论：`cargo build --release` 产物即单 exe——无运行时、无额外 DLL、无需安装器，天然满足"exe 旁生成配置文件"的分发模型。** 体积量级几 MB 到 ~20MB，取决于渲染器与内嵌资源。

- 无运行时依赖：Rust MSVC 目标默认静态链接 CRT（无需 VC++ Redist）；glow 用系统 opengl32.dll、wgpu 用系统 d3d12/vulkan，均不随产物分发。对比 WinUI 3 需 WinAppSDK 运行时 + xbf/pri 补齐（本项目 publish.ps1 的痛点），这是 Rust 路线的结构性优势。
- 体积参考：glow 渲染器骨架 ~2.5MB（[discussion #1651](https://github.com/emilk/egui/discussions/1651)）；eframe 模板 release ~6MB（2024 实测）；**0.33+ 默认 wgpu 渲染器显著更大**（wgpu+naga 本身 ~10MB 量级，未优化时总 15~25MB）。体积敏感可用 `default-features = false, features = ["glow"]` 切回 glow。
- 建议 release 配置（Cargo.toml `[profile.release]`）：
  ```toml
  lto = "fat"          # 或 "thin"（编译时间敏感时）
  codegen-units = 1
  strip = true         # 默认已随 Rust 1.59+ 默认行为 + 显式声明
  opt-level = "s"      # 或 "z"，体积敏感时
  ```
- 风险标注：内嵌中文字体（若用开源全量 CJK 字体如 Noto Sans SC ~10-16MB）才是体积大头；见第 4 节"运行时加载微软雅黑"可零体积解决。

---

## 4. 字形 / 图标 / 中文字体

**结论：lucide.ttf 可按既有机制内嵌（`include_bytes!`），中文字体有"运行时加载系统微软雅黑（零体积）"和"内嵌开源字体"两条路，均可行；egui 字体回退（fallback 链）是内建机制。**

- 自定义字体机制：`FontDefinitions { font_data, families }`——`font_data` 注册字体（`FontData::from_static(include_bytes!(...))`，支持 .ttf/.otf），`families` 是**有序回退列表**（"start with the first font and then move to the second"），回退是内建行为。依据：[FontDefinitions 文档](https://docs.rs/egui/latest/egui/struct.FontDefinitions.html)。
- **TTC 支持**：epaint 已用 skrifa 解析字体，`skrifa::FontRef::from_index(data, index)`；`FontData` 带 `index: u32` 字段（"Which font face in the file to use. When in doubt, use 0"）。**微软雅黑 msyh.ttc 可直接加载**（[discussion #1344](https://github.com/emilk/egui/discussions/1344) 有此用法：`FontData::from_owned(std::fs::read("C:/Windows/Fonts/msyh.ttc"))`）。
- 中文字体两条路：
  - **运行时加载系统字体**（推荐）：`C:\Windows\Fonts\msyh.ttc`（微软雅黑）或 simsun.ttc，`FontData::from_owned` 读文件即可，exe 零体积增加。风险：目标机字体缺失时需回退逻辑（如 fallback 到 simsun / 提示）；无法再分发微软雅黑（版权），但本方案只是运行时引用，无分发问题。
  - **内嵌开源字体**：Noto Sans SC / Source Han Sans（OFL 授权可再分发），全量 ~10-16MB，可子集化（仅收录用到的汉字）到 ~3-5MB。
- lucide 图标：Lucide.ttf 是普通 TTF，`include_bytes!` 嵌入 + 注册 `FontFamily::Name("lucide")` + 按码点引用即可——与 C# 版 `FontIcon Glyph` / `LucideGlyph.cs` 机制同构，POC 的图标码点表可复用。体积 ~100KB 量级。
- 社区现成 crate（可选，非必须）：`egui_zhcn_fonts`（0.1 对应 egui 0.31，已旧）、`egui-cjk-font`、`egui-chinese-font`（lib.rs 可查）。风险标注：这些是社区小 crate，按本项目"禁社区偏方"惯例，直接自写 20 行字体加载更可控。
- 风险标注：中文字体必须配——egui 内置字体不含 CJK（[#3060](https://github.com/emilk/egui/issues/3060) closed as not planned）；早期"中文渲染慢"（[#962](https://github.com/emilk/egui/issues/962)，0.19 时代字体图集问题）已随字体系统重写解决，不构成现行风险。

---

## 5. 系统能力（文件夹选择、Mica/Acrylic、主题）

### 5.1 系统文件夹选择对话框 — 可行（rfd）

- **rfd 0.17.2**（约 2026-02 发布）：跨平台原生对话框 crate，Windows 走原生 **IFileDialog**，同步 + 异步 API 都有，与 egui 组合是标准做法。依据：[crates.io/crates/rfd](https://crates.io/crates/rfd)。
- 风险标注：rfd 的 Windows 实现基于 windows crate（IID 注入式对话框），与 egui 无冲突；egui 侧需处理模态期间的 repaint。

### 5.2 半透明背景（Mica/Acrylic）— 无内建，社区方案成熟

**结论：eframe 无内建 Mica/Acrylic（feature request [#3050](https://github.com/emilk/egui/issues/3050) 已 close，方案落地在社区 crate），但实现路径明确，可对齐 C# 版"Win11 Mica / Win10 Acrylic 回退"行为。**

- 标准做法：通过 `raw_window_handle` 拿 HWND（eframe/egui 渲染后端已暴露）→ 用 [window-vibrancy](https://github.com/tauri-apps/window-vibrancy) crate：
  - `apply_mica` — Windows 11
  - `apply_acrylic` — Windows 10/11
  - `apply_blur` — Windows 7/10/11(22H1)
  - 配合 `transparent: true` 窗口 + 透明背景绘制（[#3050](https://github.com/emilk/egui/issues/3050) 评论区给出做法）。
- 风险标注：window-vibrancy 文档自注性能坑——Win11 22H1+ 下 blur/acrylic 在拖拽/缩放窗口时性能差；Mica 仅 Win11（Win10 用 acrylic 回退，与本项目 C# 版策略一致）。另 [eframe #4451](https://github.com/emilk/egui/issues/4451)（Windows 上透明窗口渲染问题）说明透明模式需要实测验证。**建议：R1 阶段可只做不透明 + 纯色背景，Mica 作为 P2 加分项。**

### 5.3 主题切换（dark/light）— 完全可行

- `eframe::NativeOptions { follow_system_theme: true, default_theme: Theme::Dark }` 跟随系统；运行时 `ctx.set_visuals(...)` / `ctx.set_theme(...)` 即时切换。egui 自带成对 dark/light Visuals，三态（system→dark→light）与 C# 版行为对齐无阻碍。
- 注意：eframe 的 `persist_window` 只持久化窗口几何（且是 winit 层），**没有** C# 版 `WindowPosition.ClampToVisible`（-32000 离屏纠偏）的等价物，恢复位置需自写纠偏（可用 winit `outer_position` + 虚拟屏边界钳制）。此点在 `poc-gap.md` 已列为必补项。

---

## 结论汇总

| 维度 | 结论 | 置信度 |
|---|---|---|
| 中文 IME（Windows） | 0.34+ 可靠，0.35 有组词视觉；残留问题仅韩文光标索引（有 workaround）与 winit #4508（有 workaround） | 高 |
| 版本 | egui/eframe 0.35.0（2026-06-25），MSRV Rust 1.92 | 高 |
| 单 exe | 天然成立，无 DLL 分发；glow 骨架 ~2.5-6MB，wgpu 默认 ~15-25MB；lto+strip 建议 | 高 |
| 字体/图标 | lucide TTF 可嵌入；msyh.ttc 可运行时加载（skrifa 支持 TTC index）；fallback 链内建 | 高 |
| 文件对话框 | rfd 0.17.2 原生 IFileDialog | 高 |
| Mica/Acrylic | 无内建，window-vibrancy + 透明窗口可行；性能坑需实测，建议 P2 | 中 |
| 主题切换 | 内建，三态可行 | 高 |

**最大风险**：IME 在 0.34/0.35 才完成重做，且依赖 egui-winit 对 winit #4508 的 workaround 与 0.35 的组词视觉新 API——若沿用 POC 的 egui 0.31，上述修复全部缺失（0.31 时代 Windows 中文输入存在退格/焦点/换行类已知缺陷）；必须升级到 0.35 并用其新 IME API，且 R1 阶段应把"微软拼音/搜狗 + 单行/多行 TextEdit + 组词中退格/光标/候选确认"列为专项验收用例。

## 参考链接

- [egui CHANGELOG（0.35.0）](https://github.com/emilk/egui/blob/master/CHANGELOG.md)
- [egui-winit CHANGELOG](https://github.com/emilk/egui/blob/main/crates/egui-winit/CHANGELOG.md)
- [PR #7967 Much improved IME](https://github.com/emilk/egui/pull/7967) / [PR #8083 IME composition visuals](https://github.com/emilk/egui/pull/8083) / [PR #7983 owns_ime_events](https://github.com/emilk/egui/pull/7983)
- [issue #3532](https://github.com/emilk/egui/issues/3532) / [#4354](https://github.com/emilk/egui/issues/4354) / [#4430](https://github.com/emilk/egui/issues/4430) / [#4436](https://github.com/emilk/egui/issues/4436) / [#3060](https://github.com/emilk/egui/issues/3060)
- [winit #4508（Windows IME 键盘事件重复发出）](https://github.com/rust-windowing/winit/issues/4508)
- [discussion #1651 exe 体积](https://github.com/emilk/egui/discussions/1651) / [discussion #1344 系统字体加载](https://github.com/emilk/egui/discussions/1344)
- [egui FontDefinitions / FontData 文档](https://docs.rs/egui/latest/egui/struct.FontDefinitions.html)
- [rfd crate](https://crates.io/crates/rfd) / [window-vibrancy](https://github.com/tauri-apps/window-vibrancy) / [eframe #3050 Mica 请求](https://github.com/emilk/egui/issues/3050) / [eframe #4451 透明窗口](https://github.com/emilk/egui/issues/4451)
- [docs.rs/crate/egui 版本列表](https://docs.rs/crate/egui/latest)
