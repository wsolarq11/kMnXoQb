# 旧 Rust POC（archive/launchpad-rs）与当前 C# 实现的功能差距

核验日期：2026-08-01。基准：当前 `launchpad/src/`（C# 版）。POC 为 egui 0.31，约 1500 行。

## 可复用（行为一致或差异可修）

| 项 | POC 现状 | C# 现状 | 差距 |
|---|---|---|---|
| 数据模型（types.rs） | name/directory/command/confirm(默认 true)/id/selected/terminal?/tag?/group? + AppSettings + WindowState，serde snake_case | 完全同构 | 仅 settings.json 多 language 字段（C# 新增） |
| config.json / settings.json 读写 | ConfigIO：读写 + 写前备份 config.json.bak | 同 + 损坏自动恢复（备份优先）、恢复提示 | POC 备份忽略错误且无恢复流程 |
| DangerousFlag 检测（launch.rs） | 6 flag 完全一致，英文硬编码 reason | 一致，reason 为 LanguageKey | reason 需改为键表 |
| LaunchPlanner 主分支 | wt/pwsh/cmd 三级回退，argv 数组，零 shell | 一致 | 见"必须按 C# 修复" |
| 批量启动 + 批量确认 | 有（batch_confirm 索引列表） | 有（RequireConfirm + LaunchMany 逐项错误捕获） | POC 无逐项失败索引 |
| 确认策略 | 有（confirm 开关 + 危险检测） | NeedsConfirm（全局开关 && (confirm || 危险)） | 对齐语义 |
| 编辑对话框 | 四字段 + confirm 开关 + 校验 | 同 + 目录存在性校验 + 系统目录选择器 + 危险警告 | POC 校验弱、无目录选择器 |
| 搜索过滤 | 有 | Filter 三字段子串不区分大小写 | 对齐断言 |
| 主题 | dark/light 两态 | system→dark→light 三态循环 + Mica/Acrylic | 需补 system 态与背景回退 |
| 单实例 | .lock PID 锁（stale 自清理） | Win32 单实例 | 机制不同，行为等价即可 |
| 窗口状态 | 仅 size 恢复 | x/y/width/height + Restored 语义 + ClampToVisible | 需补位置与纠偏 |
| 终端探测 | 每次 plan 跑 where.exe（无缓存） | 探测缓存 | 需补缓存 |

## 必须按 C# 修复（POC 含旧 bug，禁止直接复用）

1. **cmd fallback 引号陷阱**：POC 用 `["/k", "cd /d \"{dir}\" && {cmd}"]`——cmd /k 不走标准 argv 引号规则，含引号/空格目录整条不执行。C# 修复为 `["/k", cmd]` + WorkingDirectory 传目录（TerminalContractTests 验证）。
2. **pwsh 单引号转义**：POC `cd '{dir}'` 无转义；C# 加 `EscapePwshQuotes`（`'`→`''`）。
3. **错误分类**：POC 用 anyhow 字符串；C# 用 Win32ErrorCode 分类（267/3→WorkingDirectoryMissing、2→ProcessNotFound、5→AccessDenied）上状态栏。

## POC 有而 C# 无（对齐基准外，可选）

- CLI 子命令：check/list/launch/dry-run（clap）。C# 纯 GUI。若 Rust 路线要保留 CLI，属于增量功能。
- macos/linux 平台分支。产品已收敛 Windows-only（WinUI 版），Rust 路线按 Windows 单平台评估。

## POC 无而 C# 有（R1 路线必须补齐）

1. i18n：~60 个 LanguageKey 中英双语 + 三层语言优先级（auto/zh-CN/en-US）+ 热切换全量刷新
2. settings.json 的 language 字段
3. 配置目录"向上搜索含 config/ 的祖先"（POC 为硬编码 ../config，release TODO 未实现）
4. 配置损坏自动从备份恢复 + 状态栏恢复提示（LastRecoveryNoteKey）
5. 探测缓存（wt/pwsh 可用性）
6. WindowPosition.ClampToVisible（-32000 离屏纠偏）
7. 批量启动逐项失败索引 + 部分成功状态文案（StatusLaunchedPartial）
8. 历史记录（PushHistory 去重置顶 10 条）与最近启动统计栏
9. 危险项三处警告（编辑框内/卡片/确认对话框）
10. GenerateId 冲突后缀规则（_2、_3…）、SetSelectById 稳定 id 解析
11. 图标 lucide 字形
12. 单测/契约测试/快照测试资产（POC 仅少量 proptest）

结论：R1 路线非零成本复用——领域层约 60% 可借鉴（模型/planner/flag 检测），但 i18n、配置目录解析、恢复流程、UI 现代交互（危险警告、目录选择器、三态主题）需新建。C# 版 15+ 个测试文件是行为的权威契约。
