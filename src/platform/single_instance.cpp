#include "platform/single_instance.h"

#ifdef _WIN32
#include <windows.h>
#else
#include <unistd.h>
#include <fcntl.h>
#include <sys/file.h>
#include <filesystem>
#include <cstdlib>
#include <string>
#endif

namespace pal {

namespace {

// SingleInstance 的真实实现，封装平台特定的单实例检测逻辑。
class RealSingleInstance final : public SingleInstance {
public:
    RealSingleInstance();
    ~RealSingleInstance() override;

    bool is_only_instance() const override { return is_only_; }

private:
    bool is_only_ = true;
#ifdef _WIN32
    void* mutex_ = nullptr;
#else
    int lock_fd_ = -1;
#endif
};

RealSingleInstance::RealSingleInstance() {
#ifdef _WIN32
    HANDLE h = CreateMutexW(nullptr, FALSE, L"WTLauncher-SingleInstance");
    if (h && GetLastError() == ERROR_ALREADY_EXISTS) {
        is_only_ = false;
    }
    mutex_ = h;
#else
    // macOS/Linux: lockfile + flock
    // 优先用 XDG_RUNTIME_DIR（POSIX 标准，进程退出自动清理），
    // 回退到 /tmp。
    const char* runtime_dir = std::getenv("XDG_RUNTIME_DIR");
    std::filesystem::path lock_path;
    if (runtime_dir && *runtime_dir) {
        lock_path = std::filesystem::path(runtime_dir) / "launchpad.lock";
    } else {
        lock_path = "/tmp/launchpad.lock";
    }

    lock_fd_ = ::open(lock_path.c_str(), O_CREAT | O_RDWR, 0600);
    if (lock_fd_ < 0) {
        // 无法创建 lockfile，保守假设已有实例运行
        is_only_ = false;
        return;
    }

    // flock 非阻塞独占锁：已锁定则返回 EWOULDBLOCK
    if (::flock(lock_fd_, LOCK_EX | LOCK_NB) != 0) {
        is_only_ = false;
        ::close(lock_fd_);
        lock_fd_ = -1;
        return;
    }

    // 锁定成功，写入 pid 供调试（非必需）
    std::string pid_str = std::to_string(::getpid());
    ::write(lock_fd_, pid_str.c_str(), pid_str.size());
    ::ftruncate(lock_fd_, static_cast<off_t>(pid_str.size()));
#endif
}

RealSingleInstance::~RealSingleInstance() {
#ifdef _WIN32
    if (mutex_) CloseHandle(mutex_);
#else
    if (lock_fd_ >= 0) {
        ::flock(lock_fd_, LOCK_UN);
        ::close(lock_fd_);
        lock_fd_ = -1;
    }
#endif
}

} // anonymous namespace

auto SingleInstance::create() -> std::unique_ptr<SingleInstance> {
    return std::make_unique<RealSingleInstance>();
}

} // namespace pal
