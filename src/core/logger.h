#pragma once

// ============================================================================
// Logger 封装层
// 基于 spdlog 编译模式（compiled mode），提供全局 logger 访问。
// 日志文件路径: <config_dir>/launchpad.log
// 编译时日志级别: SPDLOG_ACTIVE_LEVEL (release 可移除 debug 级别)
// ============================================================================

#include <spdlog/spdlog.h>
#include <spdlog/sinks/rotating_file_sink.h>
#include <spdlog/sinks/stdout_color_sinks.h>
#include <memory>
#include <string>

namespace core {

class Logger {
public:
    // 初始化全局 logger。config_dir = 配置目录（日志文件写入该目录）
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

    // 便捷宏包装（编译时移除 debug/trace 日志）
    // 使用方式: CORE_LOG_INFO("launch_item {} started", item_name);
};

} // namespace core

// ------------------------------------------------------------------
// 快捷宏（支持 SPDLOG_ACTIVE_LEVEL 编译时过滤）
// ------------------------------------------------------------------
#ifndef CORE_LOG_TRACE
#define CORE_LOG_TRACE(...)   SPDLOG_TRACE(__VA_ARGS__)
#endif
#ifndef CORE_LOG_DEBUG
#define CORE_LOG_DEBUG(...)   SPDLOG_DEBUG(__VA_ARGS__)
#endif
#ifndef CORE_LOG_INFO
#define CORE_LOG_INFO(...)    SPDLOG_INFO(__VA_ARGS__)
#endif
#ifndef CORE_LOG_WARN
#define CORE_LOG_WARN(...)    SPDLOG_WARN(__VA_ARGS__)
#endif
#ifndef CORE_LOG_ERROR
#define CORE_LOG_ERROR(...)   SPDLOG_ERROR(__VA_ARGS__)
#endif
