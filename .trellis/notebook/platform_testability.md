# 平台抽象层可测试化笔记

## 设计决策

ThemeDetector 和 SingleInstance 改为虚接口 + 工厂模式，支持 Trompeloeil mock。

### ThemeDetector
- 纯虚基类，`is_system_dark()` 为纯虚函数
- 工厂方法 `create()` 返回 `unique_ptr<ThemeDetector>`
- 原有实现移到匿名命名空间中的 `RealThemeDetector` 内部类

### SingleInstance
- 纯虚基类，`is_only_instance()` 为纯虚函数
- 工厂方法 `create()` 返回 `unique_ptr<SingleInstance>`
- 原有实现（mutex/lockfile）移到 `RealSingleInstance`
- app.cpp 中改为 `auto instance = pal::SingleInstance::create()`

### app.cpp 主题检测
- 使用 `get_theme_detector()` 辅助函数（static local 缓存）
- 避免多次创建 ThemeDetector 实例
