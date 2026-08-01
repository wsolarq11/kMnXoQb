# C-2: XAML 静态检查进 CI（x:DefaultBindMode 门禁）

## Goal

防「新页面忘了 x:DefaultBindMode 根属性」回归——BUG-1/2/3 绑定默认值陷阱的第一道 CI 防线。

## Requirements

1. CI（.github/workflows/ci.yml）加一步：扫描 src/ 下所有 .xaml（排除 obj），要求每个文件含 `x:DefaultBindMode`；缺失即失败。
2. 本地等价验证：对当前工程跑该检查 → 应通过（两个页面都已声明）；对临时无属性 XAML 跑 → 应失败。
3. DataTemplate 分离文件（若有）与页面规则一致（当前无独立模板文件，规则覆盖即可）。

## Acceptance Criteria

- [ ] CI yml 新增步骤合并后 YAML 合法（本地 lint 或结构检查）
- [ ] 本地执行等价命令：现工程通过、缺属性样例被抓出
- [ ] 不影响现有 CI 步骤（构建/测试）

## Notes

- 检查脚本放 CI 内联（5 行 PowerShell）即可，不引入额外文件（KISS）。
