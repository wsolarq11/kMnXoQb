# P1: 构建治理（JSON 源生成 + UseArtifactsOutput）

## Goal

源码瘦身（去反射序列化）与构建产物可控（bin/obj 集中）。

## Requirements

1. `LauncherJson` 换 `JsonSerializerContext` 源生成：snake_case 命名策略、大小写不敏感读取、未知字段保留（JsonExtensionData）语义完全不变。
2. `UseArtifactsOutput=true`（新建 Directory.Build.props）：全部 bin/obj 收敛到 `artifacts/`。
3. `publish.ps1` 的 xbf/pri 拷贝路径同步 artifacts 布局（以构建后实际路径核对）。
4. CI 无需改动；契约测试路径不受影响。

## Acceptance Criteria

- [ ] JsonRoundTripTests 全绿（字节兼容语义锁定）
- [ ] 构建零错误；`artifacts/` 目录结构正确（bin/obj/publish 分离）
- [ ] `publish.ps1` 全流程成功，产物可运行（xbf/pri 拷贝路径正确）
- [ ] 干净构建零警告

## Notes

- 先实施 JSON 源生成（独立小步），再动 artifacts 布局（影响面大，最后一步验证 publish）。
- .NET 10 JsonSourceGenerationOptions 无循环引用场景，最简单模式即可。
