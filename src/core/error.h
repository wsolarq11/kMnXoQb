#pragma once

#include <string>
#include <expected>

namespace core {

enum class ErrorCode {
    kConfigNotFound,
    kConfigParseError,
    kConfigWriteError,
    kDirectoryNotFound,
    kCommandEmpty,
    kTerminalNotFound,
    kLaunchFailed,
    kInvalidItem,
    kInternalError
};

class Error {
public:
    explicit Error(ErrorCode code, std::string message = "")
        : code_(code), message_(std::move(message)) {}

    ErrorCode code() const { return code_; }
    const std::string& message() const { return message_; }

    static Error ConfigNotFound(const std::string& path) {
        return Error(ErrorCode::kConfigNotFound, "Config not found: " + path);
    }
    static Error ConfigParseError(const std::string& detail) {
        return Error(ErrorCode::kConfigParseError, "Config parse error: " + detail);
    }
    static Error ConfigWriteError(const std::string& detail) {
        return Error(ErrorCode::kConfigWriteError, "Config write error: " + detail);
    }
    static Error DirectoryNotFound(const std::string& path) {
        return Error(ErrorCode::kDirectoryNotFound, "Directory not found: " + path);
    }
    static Error CommandEmpty() {
        return Error(ErrorCode::kCommandEmpty, "Command is empty");
    }
    static Error TerminalNotFound(const std::string& name) {
        return Error(ErrorCode::kTerminalNotFound, "Terminal not found: " + name);
    }
    static Error LaunchFailed(const std::string& detail) {
        return Error(ErrorCode::kLaunchFailed, "Launch failed: " + detail);
    }
    static Error InvalidItem(const std::string& detail) {
        return Error(ErrorCode::kInvalidItem, "Invalid item: " + detail);
    }
    static Error Internal(const std::string& detail) {
        return Error(ErrorCode::kInternalError, "Internal error: " + detail);
    }

private:
    ErrorCode code_;
    std::string message_;
};

} // namespace core
