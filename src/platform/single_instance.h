#pragma once

#include <memory>

namespace pal {

// 单实例检测器。
// Windows: 命名互斥体（CreateMutexW），进程崩溃 OS 自动释放。
// macOS/Linux: lockfile + flock（写入 /tmp 或 $XDG_RUNTIME_DIR），进程崩溃 OS 自动释放。
// 析构释放锁资源。
class SingleInstance {
public:
    virtual ~SingleInstance() = default;

    SingleInstance(const SingleInstance&) = delete;
    SingleInstance& operator=(const SingleInstance&) = delete;

    virtual bool is_only_instance() const = 0;

    // 工厂方法，创建平台相关的 SingleInstance 实例
    static auto create() -> std::unique_ptr<SingleInstance>;

protected:
    SingleInstance() = default;
};

} // namespace pal
