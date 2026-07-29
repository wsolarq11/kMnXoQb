#ifndef _WIN32
#ifndef __APPLE__

#include "platform/dialog_provider.h"
#include <array>
#include <cstring>
#include <reproc++/reproc.hpp>

namespace pal {

class LinuxDialogProvider final : public DialogProvider {
public:
    auto browse_directory() -> std::optional<std::filesystem::path> override {
        const char* const candidates[] = {"zenity", "kdialog", "Xdialog"};
        for (const char* bin : candidates) {
            reproc::process process;
            std::error_code ec;

            if (std::strcmp(bin, "zenity") == 0)
                ec = process.start({bin, "--file-selection", "--directory"});
            else if (std::strcmp(bin, "kdialog") == 0)
                ec = process.start({bin, "--getexistingdirectory", "."});
            else
                ec = process.start({bin, "--dirstdout", "."});

            if (ec) continue;

            std::array<char, 4096> buf{};
            auto [bytes_read, read_ec] = process.read(reproc::stream::out,
                buf.data(), buf.size());
            process.wait(reproc::infinite);
            process.stop();

            if (!read_ec && bytes_read > 0) {
                std::string dir(buf.data(), static_cast<size_t>(bytes_read));
                while (!dir.empty() && (dir.back() == '\n' || dir.back() == '\r'))
                    dir.pop_back();
                if (!dir.empty())
                    return std::filesystem::path(dir);
            }
        }
        return std::nullopt;
    }
};

auto create_dialog_provider() -> std::unique_ptr<DialogProvider> {
    return std::make_unique<LinuxDialogProvider>();
}

} // namespace pal

#endif // !__APPLE__
#endif // !_WIN32
