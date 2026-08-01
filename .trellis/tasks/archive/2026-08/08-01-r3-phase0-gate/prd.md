# 阶段 0 验证门：Tauri2 最小工程 + WebView2 实测 + 便携 config/ 验证 + 中文输入实测

## Goal

在目标环境（本机 Win10 IoT LTSC 2021）验证 R3 路线（Tauri 2.11 + React）的四个硬前提。**未通过不进入阶段 1**（回滚：维持 C# 现状或转 R1 egui 备选）。

## 验证要素

| # | 要素 | 判定标准 |
|---|---|---|
| V1 | Tauri 2.11 最小工程可编译可运行 | cargo check + tauri build 成功；产物 exe 可启动、窗口存活 |
| V2 | WebView2 在目标环境可用 | 本机安装检测（版本记录）+ 运行中 JS 执行成功（注入脚本回传 UA 与探测结果） |
| V3 | 便携 config/ 解析与可写 | current_exe() 向上搜索含 config/ 的祖先正确；探测文件可写可删（exe 旁落盘 probe_result.json 双证） |
| V4 | 中文渲染与输入 | 窗口标题/界面中文渲染正常；输入框中文 IME 可输入（自动回传 + 人工确认项） |

## 验收标准

- [ ] V1：cargo check 零警告；release exe 生成并可启动（进程存活 ≥10s）
- [ ] V2：probe_result.json 生成且含真实 UserAgent（证明 WebView2 渲染 + JS 执行）
- [ ] V3：probe_result.json 中 dir 指向正确的 config 祖先（或 exe 目录回退）；writable=true
- [ ] V4：窗口标题中文正常（截图留档）；IME 输入人工确认（阶段 5 质检复验）
- [ ] 验证结论写入本任务结论：PASS/FAIL + 证据 + 后续影响

## Notes

- 本机已检测到 WebView2 Runtime 150.0.4078.105（EdgeWebView\Application 目录）——V2 前提成立，但"其他 LTSC 机器可能没有"的检测指引逻辑（D4）仍需保留。
- 验证门产物（launchpad-tauri/ 最小工程）作为阶段 1-4 的工程地基继续演进，不删除。
