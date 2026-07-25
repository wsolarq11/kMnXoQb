#include "platform/terminal_launcher.h"

// Factory helpers declared at namespace scope
#if defined(_WIN32)
extern auto create_win_launcher() -> std::unique_ptr<pal::TerminalLauncher>;
#elif defined(__APPLE__)
extern auto create_macos_launcher() -> std::unique_ptr<pal::TerminalLauncher>;
#else
extern auto create_linux_launcher() -> std::unique_ptr<pal::TerminalLauncher>;
#endif

namespace pal {

auto TerminalLauncher::create() -> std::unique_ptr<TerminalLauncher> {
#if defined(_WIN32)
    return create_win_launcher();
#elif defined(__APPLE__)
    return create_macos_launcher();
#else
    return create_linux_launcher();
#endif
}

} // namespace pal
