# 评估 C# → Rust/TS 再迁移方案（完整功能对齐，便携 zip/MSI 双形态 + 双轨配置）

## Goal

评估将当前 C#/WinUI 3 实现（`launchpad/`）再迁移到 **Rust 或 TypeScript**（可单独、可组合）的可行性、代价与风险，交付一份**评估报告**（含推荐路线与可选迁移计划骨架）。不实施迁移，只做评估与规划。

评估基准：当前 `launchpad/src/` 全部功能（"完整对齐"），以现有单元测试/契约测试/快照测试的行为断言为对齐依据（行为对齐以测试断言为准，不信任注释）。

## 硬性约束（用户给定，2026-08-01 修订）

1. 编程语言限制为 **Rust 和 TypeScript**（二选一或组合，如 Tauri 式 Rust 核心 + TS 前端）。
2. 产品形态（**修订**）：**便携 zip 解压即用 + 接受 WebView2 依赖；同时接受 MSI 安装器 + Portable.zip**（原约束"一个 exe"放宽，采用 cc-switch 式分发模式）。
3. 配置位置（**修订**）：**双轨**——便携 zip 在 exe 旁生成 `config/`（可整体移动，从 exe 位置向上搜索含 `config/` 的祖先）；MSI 安装版写用户数据目录（%APPDATA%）。
4. 中文环境：界面中英双语、中文输入（编辑框需支持中文 IME）、中文路径/目录。
5. 目标系统：Windows 10（含 LTSC 2021 类精简环境）为主，不考虑 Win11 独占特性作为硬依赖（Mica 可作 Win11 增强，Win10 需回退方案）。WebView2 依赖被接受，但需启动检测与安装指引。

## 功能基准清单（评估报告必须逐项覆盖的对齐范围）

### A. 数据模型与配置（字节兼容旧 config.json / settings.json）
- [ ] `config.json`：LaunchItem 列表，snake_case；字段 name/directory/command/confirm(缺省 true)/id/selected(缺省 false)/terminal?/tag?/group?；未知字段静默忽略（读兼容）；写时省略 null 可选字段。
- [ ] `settings.json`：confirm_enabled/theme(默认 system)/language(默认 auto)/launch_history(默认 [])/window_state?；未知字段写回保留（C# 用 JsonExtensionData，评估新方案等价机制）。
- [ ] 写前备份 `config.json.bak`；config.json 损坏自动从备份恢复（状态栏提示），备份也损坏才报错（含路径与原因）。
- [ ] 配置目录解析：exe 位置向上搜索含 `config/` 的祖先；首次运行自动建目录；配置缺失时空列表/默认设置不崩溃。
- [ ] 序列化输出缩进、字段顺序、可选字段省略与 C# 现有输出字节兼容（快照断言为准）。

### B. 领域纯逻辑（可 1:1 移植，现有单测即对齐契约）
- [ ] LaunchPlanner：wt.exe → pwsh.exe → cmd.exe 三级回退；wt 用 `new-tab -d <dir> <terminal> -NoExit -Command <cmd>`；pwsh 用 `cd '<dir 单引号转义>'; <cmd>`；cmd 用 `/k <cmd>`（工作目录走 process 启动参数，禁止字符串拼接命令）。
- [ ] DangerousFlagDetector：6 个 flag 子串匹配（dangerously / yolo / skip-permissions / bypass-approvals / bypass-sandbox / bypass.sandbox），返回结构化原因（本地化键）。
- [ ] ItemValidator：名称/命令必填、目录存在性。
- [ ] WindowPosition.ClampToVisible：离屏坐标（-32000）纠偏。
- [ ] Item 集合纯函数：GenerateId（小写、空格→下划线、冲突 `_2` 后缀）、Filter（名称/目录/命令子串不区分大小写）、Upsert/Delete/Move/SetSelectById（id 稳定解析）/ClearSelection/ToggleSelectAll。
- [ ] 启动编排：NeedsConfirm（全局开关 && (item.confirm || 危险)）、批量启动逐项错误捕获（成功数 + 失败索引）、历史 PushHistory（去重置顶、上限 10）。
- [ ] 错误分类：目录缺失(ERROR_DIRECTORY 267 / PATH_NOT_FOUND 3 且目录确实不存在) → WorkingDirectoryMissing；可执行缺失(2) → ProcessNotFound；拒绝访问(5) → AccessDenied；其余 → Unknown；结构化错误上状态栏（异常仅用于编程错误）。

### C. 启动与系统集成（命令式壳）
- [ ] 零 shell 启动：argv 数组 API（对应 Rust `std::process::Command` / Node `child_process.spawn` 的 args 数组），禁止字符串命令构造。
- [ ] 工作目录经启动参数传递，不经 cd 前缀（cmd 引号陷阱）。
- [ ] 终端可用性探测（wt.exe / pwsh.exe）与探测缓存。
- [ ] 单实例（第二实例直接退出）。

### D. UI 功能面
- [ ] 主界面：搜索框、卡片列表（名称/目录/命令）、多选复选框、全选、批量启动、单项启动、上移/下移/删除/编辑、空状态与无匹配提示。
- [ ] 统计栏：项目数/已选数/最近启动名；状态栏（错误、恢复提示、成功提示）。
- [ ] 编辑对话框：四字段 + 终端覆盖 + 确认开关 + 目录选择器（系统文件夹对话框）+ 校验错误展示。
- [ ] 确认对话框：单项（危险原因）、批量（项目列表）。
- [ ] 危险项三处警告：编辑框内、卡片、确认对话框。
- [ ] 主题三态循环 system→dark→light；Win11 Mica 增强 + Win10 Acrylic/纯色回退；运行时热切换（无重启）。
- [ ] 语言三态循环 auto→zh-CN→en-US；三层优先级（显式 > 系统语言 > 英文兜底）；热切换全量刷新（含卡片内文案）。
- [ ] 窗口状态保存/恢复：仅保存最后 Restored 坐标，恢复前 ClampToVisible。
- [ ] 图标：仅 lucide（当前为 Lucide.ttf 码点表，新方案需等价字形方案）。
- [ ] i18n 键表：现有 ~60 个 LanguageKey 中英文案全量覆盖。

### E. 质量资产
- [ ] 领域层单测 1:1 移植（断言不变），含边界：GenerateId 冲突、Move 越界、History 去重、pwsh 引号转义、DangerousFlag 各 flag。
- [ ] 契约测试等价物：真实 spawn pwsh/cmd/wt（无 Windows Terminal 时跳过）。
- [ ] 快照测试等价物（配置序列化字节兼容）。
- [ ] 架构约束等价物：依赖方向（UI → 用例 → 纯核心）、端口注入（测试替身可替换 I/O）。

## 评估范围之外（明确不做）

- 不实施任何代码迁移；本任务交付物是评估报告（含路线对比、风险、推荐、迁移计划骨架）。
- 不评估 C# 路线本身（当前实现已是既定基线）。
- 不评估非 Rust/TS 技术（Kotlin、Go、C++ 等）。

## 交付物

1. 评估报告文档（落盘到任务目录 `research/` 或仓库 docs，最终并入任务结论）：候选路线对比矩阵、逐项功能对齐差距、风险清单（含规避/回退）、明确推荐 + 理由。
2. 若推荐路线明确，附迁移计划骨架（阶段划分、每阶段验收、回滚点）。

## Acceptance Criteria

- [ ] 评估报告覆盖上述 A–E 全部功能基准项，每项给出"可行/有差距/不可行 + 依据"。
- [ ] 至少评估：纯 Rust（egui/Slint 等）、Rust+TS 组合（Tauri v2）、纯 TS（Deno desktop 等 2026 现有方案）三类路线，并说明为何采纳/排除。
- [ ] 对每类路线的"分发形态达成度"（便携 zip / MSI+Portable 双产物 / 单 exe / 运行时依赖，含 Win10 LTSC WebView2 场景）给出明确判断。
- [ ] 中文输入（IME）在各 UI 方案中的风险有明确结论（含验证方式）。
- [ ] 给出推荐路线（或"维持现状"），理由可追溯：每项结论能指回功能基准清单或测试断言。
- [ ] 迁移计划骨架（若推荐迁移）：阶段划分、每阶段验收标准、回滚点。
- [ ] 评估过程与研究结论记录到任务 `research/` 目录，供后续任务引用。

## Notes

- 需求来源：用户口述（2026-08-01），已确认评估对象 = 当前 C# 实现 → Rust/TS 再迁移；范围 = 完整对齐；允许建任务并规划。
- 项目历史：2026-07-31 刚完成 Rust/egui POC + Flutter 版 → C# 迁移；`archive/launchpad-rs` 为旧 Rust POC（egui 0.31，约 1500 行，含批量启动/备份/JSON Schema），可作为 Rust 路线复用基础与行为参考。
- 2026-06-25 Deno 2.9 发布 `deno desktop`（experimental）：TS → 单二进制 + WebView/CEF 渲染，是 TS 路线的重要新变量，需核验稳定性与 Win10 WebView2 依赖。
