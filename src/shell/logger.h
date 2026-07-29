#pragma once

// ============================================================================
// Logger wrapper — shell layer owns logging (spdlog).
// Log file path: <config_dir>/launchpad.log
// Compile-time filtering: SPDLOG_ACTIVE_LEVEL
// ============================================================================

#include <spdlog/spdlog.h>
#include <spdlog/sinks/rotating_file_sink.h>
#include <spdlog/sinks/stdout_color_sinks.h>
#include <memory>
#include <string>

namespace shell {

class Logger {
public:
    /// Initializes the global logger. config_dir = directory where log file is written.
    static void init(const std::string& config_dir) {
        auto file_sink = std::make_shared<spdlog::sinks::rotating_file_sink_mt>(
            config_dir + "/launchpad.log", 1024 * 1024 * 5, 3);
        file_sink->set_pattern("[%Y-%m-%d %H:%M:%S.%e] [%^%l%$] %v");

        auto console_sink = std::make_shared<spdlog::sinks::stdout_color_sink_mt>();
        console_sink->set_pattern("[%^%l%$] %v");

        std::vector<spdlog::sink_ptr> sinks{console_sink, file_sink};
        auto logger = std::make_shared<spdlog::logger>("launchpad", sinks.begin(), sinks.end());
        logger->set_level(spdlog::level::trace);
        logger->flush_on(spdlog::level::warn);

        spdlog::register_logger(logger);
        spdlog::set_default_logger(logger);
    }
};

} // namespace shell

// Shortcut macros (support SPDLOG_ACTIVE_LEVEL compile-time filtering)
#ifndef APP_LOG_TRACE
#define APP_LOG_TRACE(...)   SPDLOG_TRACE(__VA_ARGS__)
#endif
#ifndef APP_LOG_DEBUG
#define APP_LOG_DEBUG(...)   SPDLOG_DEBUG(__VA_ARGS__)
#endif
#ifndef APP_LOG_INFO
#define APP_LOG_INFO(...)    SPDLOG_INFO(__VA_ARGS__)
#endif
#ifndef APP_LOG_WARN
#define APP_LOG_WARN(...)    SPDLOG_WARN(__VA_ARGS__)
#endif
#ifndef APP_LOG_ERROR
#define APP_LOG_ERROR(...)   SPDLOG_ERROR(__VA_ARGS__)
#endif
