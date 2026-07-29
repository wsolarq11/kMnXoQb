#ifdef __APPLE__

#include "platform/dialog_provider.h"
#include <array>
#include <reproc++/reproc.hpp>

namespace pal {

class MacDialogProvider final : public DialogProvider {
public:
    auto browse_directory() -> std::optional<std::filesystem::path> override {
        reproc::process process;
        std::error_code ec = process.start({"/usr/bin/osascript", "-e",
            "tell app \"Finder\" to POSIX path of (choose folder with prompt \"Select directory\")"});
        if (ec) return std::nullopt;

        std::array<char, 4096> buf{};
        auto [bytes_read, read_ec] = process.read(reproc::stream::out, buf.data(), buf.size());
        process.wait(reproc::infinite);
        process.stop();

        if (read_ec || bytes_read <= 0) return std::nullopt;

        std::string dir(buf.data(), static_cast<size_t>(bytes_read));
        while (!dir.empty() && (dir.back() == '\n' || dir.back() == '\r'))
            dir.pop_back();
        if (dir.empty()) return std::nullopt;

        return std::filesystem::path(dir);
    }
};

auto create_dialog_provider() -> std::unique_ptr<DialogProvider> {
    return std::make_unique<MacDialogProvider>();
}

} // namespace pal

#endif // __APPLE__
