#include "platform/single_instance.h"

#ifdef _WIN32
#include <windows.h>
#endif

namespace pal {

SingleInstance::SingleInstance() {
#ifdef _WIN32
    HANDLE h = CreateMutexW(nullptr, FALSE, L"WTLauncher-SingleInstance");
    if (h && GetLastError() == ERROR_ALREADY_EXISTS) {
        is_only_ = false;
    }
    mutex_ = h;
#endif
}

SingleInstance::~SingleInstance() {
#ifdef _WIN32
    if (mutex_) CloseHandle(mutex_);
#endif
}

} // namespace pal
