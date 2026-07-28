#include "platform/terminal_command.h"
#include "core/quote_arg.h"

#ifdef _WIN32

namespace pal {

auto build_wt_command_string(const std::string& directory, const std::string& command) -> std::string {
    // wt -d "<directory>" pwsh -NoExit -Command "<command>"
    return "wt -d " + core::quote_arg(directory)
        + " pwsh -NoExit -Command " + core::quote_arg(command);
}

auto build_pwsh_command_string(const std::string& directory, const std::string& command) -> std::string {
    // pwsh -NoExit -Command "cd <dir>; <command>"
    return "pwsh -NoExit -Command " + core::quote_arg("cd " + directory + "; " + command);
}

} // namespace pal

#endif // _WIN32
