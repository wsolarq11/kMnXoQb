# Slint UI Layer 规范

## 组件结构

- `main_window.slint`：顶层组件，声明所有状态属性和回调
- `launch_card.slint`：启动卡片组件（`LauncherCard`）
- `dialog.slint`：编辑/确认对话框组件
- `theme.slint`：主题 token 定义

## 数据流

- C++ 通过 `slint::ComponentHandle<T>` 持有 UI 引用
- 属性绑定使用 `set_*()` 方法
- Model 使用 `slint::VectorModel<T>` 类型
- UI 更新必须在主线程：使用 `slint::invoke_from_event_loop()` 从后台线程回调

## 主题系统

- 支持三种模式：`light` / `dark` / `system`
- `system` 模式通过 `pal::ThemeDetector::is_system_dark()` 检测
- 主题切换不重启应用，仅更新 `is_dark` 属性

## 命名约定

- .slint 文件使用 kebab-case：`main-window.slint`、`launch-card.slint`、`dialog.slint`
- UI 回调使用 `on_*` 前缀：`on_launch`、`on_toggle_theme`
- Slint 导出的 C++ 结构体使用 `struct` 而非 class
