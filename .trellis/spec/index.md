# Launchpad Spec Layers

> 项目专用编码规范与约束。

## Layers

| Layer                                       | 描述                                                             | 关键文档                           |
| ------------------------------------------- | ---------------------------------------------------------------- | ---------------------------------- |
| [winui3-csharp](./winui3-csharp/index.md)   | **在役架构**（2026-07-31 起）：六边形分层、MVVM、关键坑清单      | [index](./winui3-csharp/index.md)  |
| [cpp-core](./cpp-core/index.md)             | 历史：C++ 核心库规范（已被 WinUI 3 取代）                        | [index](./cpp-core/index.md)       |
| [slint-ui](./slint-ui/index.md)             | 历史：Slint UI 规范（已被 WinUI 3 取代）                         | [index](./slint-ui/index.md)       |
| [cmake-build](./cmake-build/index.md)       | 历史：CMake 构建规范（已被 dotnet 构建取代）                     | [index](./cmake-build/index.md)    |
| [cross-platform](./cross-platform/index.md) | 历史：跨平台抽象规范（当前仅 Windows）                           | [index](./cross-platform/index.md) |
| [security](./security/index.md)             | 安全编码规范：零 shell 执行（argv 数组）仍有效                   | [index](./security/index.md)       |
| [guides](./guides/index.md)                 | 通用思维指南：代码复用、跨层思考、增量重构工作流                 | [index](./guides/index.md)         |

## 核心原则

1. **分层依赖单向**：UI → UseCases → Core ← Infrastructure；端口接口定义在 Core/Ports。
2. **零 shell 执行**：`Process.ArgumentList` argv 数组，禁止 system/popen/字符串拼接执行。
3. **测试覆盖关键路径**：领域层全覆盖、用例层 fakes 断言 argv、配置序列化兼容性测试。
4. **纯函数核心**：模型不可变、决策纯函数化（plan）、副作用集中在基础设施层。
5. **官方 LTS 优先**：.NET 10 LTS + Windows App SDK 稳定版。
