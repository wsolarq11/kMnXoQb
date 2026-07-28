# Launchpad Spec Layers

> 项目专用编码规范与约束。分层架构：core -> platform -> app

## Layers

| Layer                                       | 描述                                                                         | 关键文档                           |
| ------------------------------------------- | ---------------------------------------------------------------------------- | ---------------------------------- |
| [cpp-core](./cpp-core/index.md)             | C++ 核心库规范：零外部依赖、Glaze 序列化、安全边界                           | [index](./cpp-core/index.md)       |
| [slint-ui](./slint-ui/index.md)             | Slint UI 规范：组件结构、主题系统、C++ 双向绑定                              | [index](./slint-ui/index.md)       |
| [cmake-build](./cmake-build/index.md)       | CMake 构建规范：target 分层、vcpkg、跨平台编译                               | [index](./cmake-build/index.md)    |
| [cross-platform](./cross-platform/index.md) | 跨平台抽象规范：平台接口设计、条件编译、平台测试                             | [index](./cross-platform/index.md) |
| [security](./security/index.md)             | 安全编码规范：命令注入防御（reproc argv）、clang-tidy 门禁、pre-commit hooks | [index](./security/index.md)       |
| [guides](./guides/index.md)                 | 通用思维指南：代码复用、跨层思考、增量重构工作流                             | [index](./guides/index.md)         |

## 核心原则

1. **分层依赖单向**：core 不依赖 platform 和 Slint，platform 仅依赖 core，app 汇聚三层
2. **零 shell 执行**：所有子进程启动必须走 posix_spawn / CreateProcessW，禁止 system/popen
3. **测试覆盖关键路径**：安全边界、平台抽象、配置序列化必须有单元测试
4. **跨平台一致性**：平台差异封装在 platform 层，上层通过接口多态调用
