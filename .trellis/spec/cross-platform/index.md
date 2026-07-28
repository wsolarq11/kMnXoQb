# 跨平台抽象规范

## 接口设计

- 平台抽象使用**抽象基类 + 工厂模式**
- 基类：纯虚析构函数 + 纯虚方法
- 工厂函数：`static auto create() -> std::unique_ptr<Interface>`
- 接口方法返回 `std::expected<T, Error>` 而非裸指针

## 平台选择策略

- 使用 CMake 编译期选择源文件（如 `terminal_launcher_win.cpp` 仅 Windows 编译）
- 运行时多态通过工厂函数 `ClassName::create()` 实现
- 不在头文件中使用 `#ifdef _WIN32` 暴露平台差异给上层
- `#ifdef` 块仅在 `.cpp` 实现文件中使用

## 已有抽象

| 接口 | 实现 | 职责 |
|------|------|------|
| `TerminalLauncher` | Win/CreateProcessW, macOS/posix_spawn, Linux/posix_spawn | 子进程启动（零 shell） |
| `PathResolver` | Win/ShGetKnownFolderPath, macOS/Linux/环境变量 | 配置路径解析 |
| `SingleInstance` | Win/CreateMutexW, macOS/Linux/lockfile+flock | 单实例锁 |
| `ThemeDetector` | Win/注册表, macOS/reproc+defaults, Linux/reproc+gsettings | 系统主题检测 |

### 工厂模式一致性

所有平台抽象必须遵循同一模式：

```cpp
// theme_detector.h
class ThemeDetector {
public:
    virtual ~ThemeDetector() = default;
    virtual auto is_system_dark() -> bool = 0;
    static auto create() -> std::unique_ptr<ThemeDetector>;
    ThemeDetector(const ThemeDetector&) = delete;
    ThemeDetector& operator=(const ThemeDetector&) = delete;
};

// theme_detector.cpp
auto ThemeDetector::create() -> std::unique_ptr<ThemeDetector> {
    class RealThemeDetector final : public ThemeDetector {
        auto is_system_dark() -> bool override { /* 平台实现 */ }
    };
    return std::make_unique<RealThemeDetector>();
}
```

## 子进程启动规范

- 所有子进程通过 **reproc** 库启动，以 `std::vector<std::string>` argv 数组传参
- **禁止**使用 `::popen()` / `::system()` / 字符串拼接构造命令
- Windows 下通过 `CreateProcessW`（由 reproc 封装）
- macOS/Linux 下通过 `posix_spawn`（由 reproc 封装）

## 测试要求

- 每个平台抽象应有跨平台测试
- 测试使用 **Trompeloeil** mock 框架在 Windows 上验证所有平台实现逻辑
- trompeloeil 与 doctest 集成：`#include <trompeloeil.hpp>`
- `if(WIN32)` 块只能在 `.cpp` 实现文件中使用，CMakeLists 不做平台限制
