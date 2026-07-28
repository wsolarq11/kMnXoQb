# 子任务 A：根除命令注入源头（argv + 无 shell 执行）

## 设计目标

把项目里所有「字符串拼接 + shell 执行」反模式替换为「`LaunchPlan` argv 结构 + 直接 exec」，从结构上消除命令注入面。本任务只覆盖安全契约与 platform 层执行路径，不引入后台线程（子任务 B）、不动 SingleInstance/ThemeDetector（子任务 C）。

## 范围边界

### 本任务负责
- 引入 `core::LaunchPlan` 与 `core::LaunchPlanBuilder`
- 重定义 `pal::ProcessLauncher` 抽象接口，签名由 `launch(directory, command)` 改为 `launch(LaunchPlan)`
- 三平台实现全部用 argv 直传，删除 `std::system` 与命令字符串拼接
- 删除 `core::Launcher::build_command` 与 `pal::build_wt_command_string` / `pal::build_pwsh_command_string`
- `app.cpp` 的 `terminal.value()+" "+command` 拼接改为构造 `LaunchPlan`
- 调整 core / platform / tests 三处 CMake 源列表
- 更新两份测试以匹配新契约

### 本任务不负责
- 后台线程 + `slint::invoke_from_event_loop`（子任务 B）
- `ProcessHandle` 的 RAII 改造（子任务 B）
- `AppSettings` Glaze `modify` alias（子任务 B）
- mac/linux `SingleInstance` lockfile（子任务 C）
- `ThemeDetector` 系统主题跟随（子任务 C）
- `tests/platform/CMakeLists.txt` 的 `if(WIN32)` 限制移除（子任务 C）

> 注意：platform 测试目前 `if(WIN32)` 包裹，本任务只更新 Windows 路径的 platform 测试内容；跨平台编译放开留给子任务 C。

## 数据结构

### `core::LaunchPlan`（新文件 `core/launch_plan.h`）

```cpp
// core/launch_plan.h
#pragma once
#include <filesystem>
#include <optional>
#include <string>
#include <vector>

#include "core/error.h"

namespace core {

struct LaunchPlan {
    std::filesystem::path executable;        // 例如 "wt" / "gnome-terminal" / "osascript"
    std::vector<std::string> args;           // argv 数组，每个元素独立，不经 shell 解释
    std::filesystem::path working_dir;       // 子进程工作目录，等价原 directory
    std::optional<std::string> terminal_override;  // 原 LaunchItem.terminal，透传给 UI/日志
    bool is_dangerous = false;               // 预计算标记，供 UI 高亮，本任务不消费但需填充
};

} // namespace core
```

设计要点：
- 结构体纯数据、零平台依赖；放 core 层，`platform` 与 `app` 都可依赖。
- `args` 是字符串数组而非单个命令字符串，这是注入根除的关键。
- `executable` 用 `std::filesystem::path` 而非 `std::string`，与 `working_dir` 类型对称，便于 UTF-16 / UTF-8 转换。
- `terminal_override` 保留语义信息但不参与拼接，避免重新引入拼接点。
- 不引入 `ProcessHandle` 字段——句柄由 `launch` 的返回值承载（子任务 B 会做 RAII 包装，但签名不变）。

### `core::LaunchPlanBuilder`（新文件 `core/launch_plan_builder.{h,cpp}`）

纯逻辑、零平台依赖的构造器。从 `LaunchItem` 派生平台无关的 `LaunchPlan`，但 `executable` 与 `args` 的具体值需要平台信息——因此 Builder 提供「无终端覆盖」的默认路径，终端选择策略下沉到 platform 层的 `populate(plan)` 钩子。

```cpp
// core/launch_plan_builder.h
#pragma once
#include <expected>
#include "core/error.h"
#include "core/launch_item.h"
#include "core/launch_plan.h"

namespace core {

class LaunchPlanBuilder {
public:
    // 从 LaunchItem 构造 LaunchPlan 的公共部分。
    // 不做目录存在性校验（由 Launcher::validate_item 在上层完成），
    // 也不决定 executable / args（由 platform::TerminalLauncher::populate 填充）。
    // 只填充：working_dir = item.directory，terminal_override = item.terminal，
    //         is_dangerous = core::is_dangerous(item.command)。
    static auto build(const LaunchItem& item) -> std::expected<LaunchPlan, Error>;
};

} // namespace core
```

Builder 算法：
1. 若 `item.command` 为空，返回 `Error::CommandEmpty()`。
2. 构造 `LaunchPlan plan`。
3. `plan.working_dir = std::filesystem::path(item.directory)`，`plan.terminal_override = item.terminal`。
4. `plan.is_dangerous = core::is_dangerous(item.command)`。
5. 返回 `plan`。

> `executable` 与 `args` 留空，由 `platform::TerminalLauncher::populate(plan)` 在平台层填充。这样 Builder 保持零平台依赖，且 `quote_arg` 不进入 Builder 路径。

## platform 层接口变更

### `platform/terminal_launcher.h` 重定义

```cpp
// platform/terminal_launcher.h （重命名概念不强制，保持 TerminalLauncher 类名以减少改动）
#pragma once
#include <memory>
#include <expected>
#include "core/error.h"
#include "core/launch_plan.h"

namespace pal {

struct ProcessHandle {
#ifdef _WIN32
    void* handle = nullptr;
    unsigned long pid = 0;
#else
    int pid = -1;
#endif
};

class TerminalLauncher {
public:
    virtual ~TerminalLauncher() = default;

    // 填充 plan.executable / plan.args（平台感知），不启动进程。
    // 返回修改后的 plan 或错误（如终端未找到）。
    virtual auto populate(LaunchPlan plan) const
        -> std::expected<LaunchPlan, core::Error> = 0;

    // 用已填充 argv 的 plan 直接 exec，全程不经 shell。
    virtual auto launch(const LaunchPlan& plan)
        -> std::expected<ProcessHandle, core::Error> = 0;

    virtual auto default_terminal_name() const -> std::string = 0;

    static auto create() -> std::unique_ptr<TerminalLauncher>;
};

} // namespace pal
```

关键变化：
1. 旧 `launch(directory, command)` 已删除，新 `launch(LaunchPlan)` 是唯一启动入口。
2. 拆出 `populate(plan)` 把 argv 构造与进程执行分离——便于纯逻辑测试。
3. `ProcessHandle` 结构保留，RAII 化由子任务 B 处理，本任务签名留作稳定契约。

### Windows 实现 `WinLauncher`（原 `WinTerminalLauncher`）

`populate(LaunchPlan)`：按 `TerminalType` 填 argv。
- `kWt`：`plan.executable="wt.exe"`，`plan.args={"new-tab","-d",working_dir.string(),"pwsh","-NoExit","-Command",item.command}`。
  > 依据 Microsoft Learn Windows Terminal 文档：`wt new-tab -d <dir> <commandline>` 天然 argv 形式。
  > 注意：`-d` 后跟目录作为独立参数，`pwsh -NoExit -Command <cmd>` 作为 commandline 整体，wt 会将其传给 shell，但因为我们直接 exec wt（不经 cmd），`item.command` 仍作为单 arg 不被 shell 中转解析。
- `kPwsh`：`plan.executable="pwsh.exe"`，`plan.args={"-NoExit","-Command","cd "+working_dir.string()+"; "+item.command}`。
  > pwsh 自身需要复合命令字符串作为 `-Command` 单个参数，这是 PowerShell 的契约而非 shell 注入——pwsh 是命令解释器但 argv 已隔离，且本路径仅当 wt 不可用时回退。
- `kCmd`：`plan.executable="cmd.exe"`，`plan.args={"/k","cd /d "+working_dir.string()+" && "+item.command}`。
  > 与现状一致（现状直接透传 command），但改为 argv 形式 cmd 知识库内不再有裸字符串拼接。
- `terminal_override` 非空时：`populate` 忽略上述选择，直接 `plan.executable=terminal_override`、`plan.args` 为 command 按空白拆分（**有限拆分，不引 shell**）。
  > 语义说明：原代码 `item.terminal.value()+" "+item.command` 走 shell 让 shell 拆分。本任务用「空白拆分」在 C++ 内完成等价行为，避免 shell。对于含引号的复杂 terminal_override，拆分会退化，但这是用户显式覆盖场景，且 is_dangerous 黑名单 + confirm 双层防护仍在。

`launch(LaunchPlan)`：
- `executable` 转 UTF-16 (`std::wstring`) 作为 `lpApplicationName`。
- `args` 拼成单个 `std::wstring cmd_line`（CreateProcessW 要求单字符串，但需用 `CommandLineToArgvW` 兼容的反引号化规则，**不是** `core::quote_arg` 的旧规则——因为 lpApplicationName 已分离，cmd_line 仅是 argv 的引号化拼接）。
  > 关键：CreateProcessW 在 `lpApplicationName` 非空时不解析可执行路径里的元字符，`lpCommandLine` 仍按 Microsoft「Parsing C++ command-line arguments」规则被新进程重新拆 argv。这一步是 Windows argv 协议的强制序列化，不是 shell。
- 调用 `CreateProcessW(lpApplicationName, lpCommandLine, ..., lpCurrentDirectory=&wdir, ...)`。
- 失败返回 `Error::LaunchFailed("CreateProcess failed: error N")`。
- 成功：`CloseHandle(pi.hThread)`，`ProcessHandle.handle=pi.hProcess; handle.pid=pi.dwProcessId`。RAII 留给子任务 B。

> 安全论证：本路径从 `wt.exe`/`pwsh.exe`/`cmd.exe`/用户 override 作为 executable 直接 exec，`item.command` 仅作为 argv 元素，不经 `cmd.exe /c` 等元 shell，注入面与 argv 一致。

### macOS 实现 `MacLauncher`（原 `MacTerminalLauncher`）

`populate(LaunchPlan)`：
- AppleScript 内容作为 `-e` 的参数（osascript 是标准可执行文件，不经 shell）：
  `plan.executable="/usr/bin/osascript"`，`plan.args={"-e","tell app \"Terminal\" to do script \""+inner+"\""}`，其中 `inner="cd "+working_dir.string()+"; "+item.command`。
  > AppleScript 字符串内的双引号需转义为 `\"`（AppleScript 转义规则），反斜杠需转义为 `\\`。这是 AppleScript 字面量转义，不是 shell 转义——因为我们直接 exec osascript，参数已是独立 argv。
- `terminal_override` 非空：`plan.executable=terminal_override`，`plan.args` 同 Windows 空白拆分逻辑。

`launch(LaunchPlan)`：用 `posix_spawn`（`<spawn.h>`）。
- `posix_spawnp` 不需要——`executable` 已是绝对路径 `/usr/bin/osascript` 或 override 全路径，用 `posix_spawn`。
- 构造 `char* argv[]`（C 数组，以 NULL 结尾），`char* envp[]` 继承 `environ`。
- `working_dir` 转 `const char*` 作为 `posix_spawn` 的 `chdir`（通过 `posix_spawn_file_actions_addchdir_np` 或 `POSIX_SPAWN_SETEXGROUP` 等价——macOS 支持 `posix_spawn_file_actions_addchdir_np`）。
- 失败返回 `Error::LaunchFailed("posix_spawn failed: "+errno)`。
- 成功：`ProcessHandle.pid=child_pid`（mac/linux 平台不存 handle）。

> 安全论证：`posix_spawn` 直接 fork/exec，不经 `/bin/sh -c`，osascript 是可执行文件而非 shell，AppleScript 内的命令由 Terminal.app 解释执行——Terminal.app 的 do script 是用户终端环境，不属于本启动器的 shell 注入面（这是 Terminal.app 自身契约）。

### Linux 实现 `LinuxLauncher`（原 `LinuxTerminalLauncher`）

`populate(LaunchPlan)`：保留多终端探测优先级（gnome-terminal / konsole / xfce4-terminal / xterm），探测方式改用 `access(term, X_OK)` 而非 `std::system("which ...")`。
- 探测命中：`plan.executable=term`，`plan.args={"--","bash","-c","cd "+working_dir.string()+" && "+item.command+"; exec bash"}`。
  > `--` 后的 `bash -c <script>` 中 `<script>` 是单 argv 元素，bash 在子进程内解释——这是 bash 契约而非 shell 注入，与现状语义一致但不再经 `std::system`。
- 全部未命中：返回 `Error::TerminalNotFound("No supported terminal emulator found")`。
- `terminal_override` 非空：同前空白拆分逻辑。

`launch(LaunchPlan)`：`posix_spawn`。
- `executable` 是相对名（如 `gnome-terminal`），用 `posix_spawnp`（自动 PATH 查找）。
- `argv[]` 来自 `plan.args` 加开头的 `executable` 名。
- `working_dir` 通过 `posix_spawn_file_actions_addchdir_np`（glibc 2.29+ / macOS 10.15+ 支持）；若不可用则回退 `chdir` 在 fork 后 hook（POSIX_SPAWN_SETEXGROUP 不可用场景）。
  > 现代发行版均支持 `posix_spawn_file_actions_addchdir_np`，构建脚本需检查符号存在性，缺少时编译期报错（避免静默回退到 chdir race）。
- 成功：`ProcessHandle.pid=pid`。

## app.cpp 集成点

`launch_item`（src/app.cpp:181-215）改造：

```text
旧（src/app.cpp:192-203）：
  auto create_and_launch = [&](dir, cmd) {
    auto terminal = pal::TerminalLauncher::create();
    return terminal->launch(dir, cmd);           // 字符串");
  };
  if (item.terminal.has_value())
    result = create_and_launch(item.directory, item.terminal.value() + " " + item.command);  // 拼接
  else
    result = create_and_launch(item.directory, item.command);

新：
  auto builder_result = core::LaunchPlanBuilder::build(item);
  if (!builder_result) { window_->set_status_text(...); return; }
  auto plan = std::move(*builder_result);
  auto terminal = pal::TerminalLauncher::create();
  auto populated = terminal->populate(std::move(plan));
  if (!populated) { window_->set_status_text(...); return; }
  result = terminal->launch(*populated);
```

要点：
- `terminal_override + " " + command` 拼接点（app.cpp:200）彻底消失。覆盖逻辑下沉到 `populate`。
- Builder 调用前可仍保留 `if (!std::filesystem::exists(item.directory))` 校验（app.cpp:186-190），与 `validate_item` 形成双重保险。
- `result` 仍为 `std::expected<pal::ProcessHandle, core::Error>`，下游 `update_history` / `refresh_ui` 不变。
- 不引入后台线程——本任务 launch 仍在 UI 线程，阻塞问题归子任务 B。

## 向后兼容

### `core::quote_arg` 保留但脱离主路径
- `quote_arg.{h,cpp}` 与 `quote_arg_test.cpp` 保留不动。
- 主路径不再引用 `quote_arg`：launcher.cpp 删除 `#include "core/quote_arg.h"`，launcher.h 删除 `quote_arg` 方法。
- `Launcher::quote_arg` 委托方法删除（launcher.h:36, launcher.cpp:77-79）。
- launcher_test.cpp 的 `Launcher: quote_arg delegation` 用例删除或改为直接测试 `core::quote_arg`（推荐保留 quote_arg_test.cpp 已覆盖，删除 launcher_test.cpp 的委托用例）。

### `pal::build_wt_command_string` / `build_pwsh_command_string` 删除
- 删除 `terminal_command.{h,cpp}` 整个文件。
- 删除 `terminal_command_test.cpp`。
- `src/CMakeLists.txt` 移除 `platform/terminal_command.cpp`。
- `tests/platform/CMakeLists.txt` 移除 `terminal_command_test.cpp`，新增 `launch_plan_test.cpp`（platform 层对 populate 的纯逻辑测试，见验证章节）。

### `core::Launcher::build_command` 删除
- launcher.h:25-26 删除声明。
- launcher.cpp:28-33 删除定义。
- launcher_test.cpp:33-41 `Launcher: build_command formats correctly` 用例删除（被 `LaunchPlanBuilder` 测试取代）。

## 测试策略

### 新增
- `tests/core/launch_plan_builder_test.cpp`：测试 `LaunchPlanBuilder::build` 纯逻辑（command 空返错、working_dir/terminal_override/is_dangerous 字段正确填充）。挂到 `tests/core/CMakeLists.txt` 并链接 `core_lib`。
- `tests/platform/launch_plan_test.cpp`：测试 Windows `WinLauncher::populate` 产出的 argv（kWt/kPwsh/kCmd 三分支 + terminal_override 分支）。**仅 Windows 编译**，包裹 `#ifdef _WIN32`（与现有 terminal_command_test.cpp 一致），跨平台放开归子任务 C。

### 修改
- `tests/core/launcher_test.cpp`：删除 `quote_arg delegation` 与 `build_command formats correctly` 两个用例，其余保留。

### 保留不动
- `tests/core/quote_arg_test.cpp`：quote_arg 仍存在，测试继续有效。
- `tests/core/is_dangerous_test.cpp`：is_dangerous 不变。
- `tests/core/selected_store_test.cpp` / `config_test.cpp`：不涉及。

## 与子任务 B / C 的接口约定

为避免子任务 B 改造时再次破坏签名：
- `LaunchPlan` 结构定义后**不再增删字段**（B 只包裹 `ProcessHandle`，不改 `LaunchPlan`）。
- `TerminalLauncher::launch(const LaunchPlan&)` 签名**冻结**，B 在内部包线程，不改签名。
- `populate` 是本任务新增抽象，B/C 不应改其签名。

## 风险与对策

| 风险 | 影响 | 对策 |
|------|------|------|
| CreateProcessW 的 lpCommandLine 序列化规则与 argv 不一致，导致带空格路径被错误拆分 | Windows 启动失败或路径错误 | 用 Microsoft「Parsing C++ command-line arguments」规则实现 `join_args_for_createprocess`，覆盖反斜杠+引号边界；在 launch_plan_test.cpp 增加「路径含空格+引号」用例 |
| macOS `posix_spawn_file_actions_addchdir_np` 在旧 SDK 不可用 | 编译失败 | CMake `check_symbol_exists` 探测，缺符号时 #error 阻止编译（不静默回退） |
| Linux `posix_spawnp` PATH 查找与 `which` 结果不一致 | 终端选择变化 | 探测改为逐个 `access(X_OK)` + PATH 遍历（用 `std::getenv("PATH")`）模拟 which，行为等价 |
| `terminal_override` 空白拆分破坏含引号的复杂命令 | 自定义终端场景退化 | 文档化为「覆盖场景为简单命令」；is_dangerous + confirm 双层防护保留；不引入 shell 以保安全 |
| `create_linux_launcher` 当前未定义（factory.cpp:20 调用但无实现） | 链接错误 | 子任务 A 顺便补齐 `create_linux_launcher` 实现 |
| 删除 `quote_arg` 委托破坏其他模块引用 | 编译错误 | find_references 确认仅 launcher.cpp / terminal_command.cpp / launcher_test.cpp 引用，前两者本任务改，测试同步改 |

## 依赖关系

- 仅依赖 C++23 标准库（`<filesystem>`, `<expected>`, `<vector>`, `<optional>`, `<spawn.h>` POSIX side）。
- 不引入 Boost.Process 或任何第三方库（约束）。
- `core` 仍只依赖 `glaze`（通过 launch_plan_builder 间接经 launch_item）；新增 `core/launch_plan.h` 无新外部依赖。
- `platform` 仍只依赖 `core`。

## 置信度

- 注入根除：99%+（argv + 直接 exec，无 shell 解释器环节）。
- 跨平台契约对称：95%（posix_spawn 在 mac/linux 等价 exec，CreateProcessW 在 win 等价 exec；残留 5% 为 chdir_np 符号兼容性）。
- 现有功能不回归：核心 8 个用例（quote_arg × N、is_dangerous、selected_store、config、launcher launch_selected × 2）保留，新增 builder + populate 覆盖。
