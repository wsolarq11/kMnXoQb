# 执行计划：阶段 0 验证门

## 步骤

1. [x] 环境检查：cargo 1.97.1 / rustc 1.97.1 / Node 24.18.0 齐备
2. [x] WebView2 检测：本机已装 Runtime 150.0.4078.105（EdgeWebView\Application\150.0.4078.105）
3. [x] 脚手架：`npm create tauri-app@latest --template react-ts` → `launchpad-tauri/`
4. [x] 最小 Rust command：probe_config_dir（便携向上搜索 + 可写探测）+ report_probe（exe 旁落盘）
5. [x] 前端：中文 IME 输入框 + 验证显示；withGlobalTauri 开启
6. [x] 前端构建：vite build 通过（782ms）
7. [x] Rust 编译：cargo check 零警告 → tauri build release 成功（exe 9MB，58s）
8. [x] 运行验证：probe_result.json 生成（V2/V3 自动证据；注意 WebView2 冷启动 ~35s，等待需 >30s）
9. [x] 截图留档：phase0-gate-screenshot.png（V4 渲染证据；IME 交互输入为人工确认项，阶段 5 复验）
10. [x] 写验证门结论：PASS（meta: result=PASS）
11. [ ] 注：MSI bundle 因 WiX 工具下载超时（github 网络）暂缓——不阻塞阶段 0，阶段 5 发布时处理（镜像/重试）

## 验证命令

```bash
cd launchpad-tauri/src-tauri
cargo check          # 零警告
cargo build --release
./target/release/launchpad-tauri.exe &   # 运行 10s
cat <exe目录>/probe_result.json          # 验证结果
```

## 评审门

- V1–V4 全 PASS → 阶段 1（r3-phase1-core）start。
- 任一 FAIL → 暂停并向用户报告（回滚 C# / 转 R1 备选由用户定）。
