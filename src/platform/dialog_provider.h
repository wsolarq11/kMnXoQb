#pragma once

#include <filesystem>
#include <memory>
#include <optional>
#include <string>

namespace pal {

/// Abstract interface for native directory-picker dialogs.
class DialogProvider {
public:
    virtual ~DialogProvider() = default;

    /// Opens a native directory-picker dialog.
    /// Returns the selected path, or std::nullopt if cancelled.
    virtual auto browse_directory() -> std::optional<std::filesystem::path> = 0;

    static auto create() -> std::unique_ptr<DialogProvider>;
};

} // namespace pal
