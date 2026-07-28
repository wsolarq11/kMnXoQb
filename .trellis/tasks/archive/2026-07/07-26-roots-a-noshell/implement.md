# 子任务 A：实施执行清单

> 对应 `design.md`。本文件给出按文件粒度的有序执行步骤、每步验证命令、回归测试点与回滚点。
> 所有阶段在 Windows + PowerShell 7 环境（项目当前工作目录 `<PROJECT_ROOT>\launchpad`）执行；mac/linux 路径仅做编译可行性保证，本机不可运行。

## 约定

- 构建目录：`build/`（如不存在则 `cmake -B build -S .`）。
- 构建命令：`cmake --build build --config Debug`。
- 测试命令：`ctest --test-dir build --build-config Debug --output-on-failure`。
- 每个阶段结束必须满足：（1）构建通过；（2）受影响测试通过；（3）无新增 IDE 诊断错误。
- 全程禁止 git commit / push / merge（子代理约束）。
- 修改按完整语法单元（函数/结构体/文件），禁止半段编辑。

---

## Phase 1：core 层引入 LaunchPlan 与 LaunchPlanBuilder（纯增量，零破坏）

**目标**：先建立新数据结构与构造器，不动现有任何调用点，保证此阶段整个项目仍可编译通过旧路径。

**文件**：
- 新增 `src/core/launch_plan.h`
- 新增 `src/core/launch_plan_builder.h`
- 新增 `src/core/launch_plan_builder.cpp`
- 修改 `src/CMakeLists.txt`（core_lib 源列表追加 `core/launch_plan_builder.cpp`）
- 新增 `tests/core/launch_plan_builder_test.cpp`
- 修改 `tests/core/CMakeLists.txt`（追加 `launch_plan_builder_test.cpp`）

**步骤**：
1. 创建 `src/core/launch_plan.h`，按 design.md 定义 `struct LaunchPlan`（5 个字段，零平台依赖）。
2. 创建 `src/core/launch_plan_builder.h`，声明 `class LaunchPlanBuilder` 与静态 `build(const LaunchItem&)`。
3. 创建 `src/core/launch_plan_builder.cpp`，实现算法：command 空→`Error::CommandEmpty()`；否则填 `working_dir` / `terminal_override` / `is_dangerous`，`executable` 与 `args` 留空。
   - `#include "core/is_dangerous.h"` 复用现有黑名单逻辑。
   - 不 `#include "core/quote_arg.h"`（明确脱离主路径）。
4. 修改 `src/CMakeLists.txt`：`add_library(core_lib STATIC ...)` 列表追加 `core/launch_plan_builder.cpp`。
5. 创建 `tests/core/launch_plan_builder_test.cpp`，用例：
   - `LaunchPlanBuilder: empty command returns CommandEmpty`
   - `LaunchPlanBuilder: fills working_dir from directory`
   - `LaunchPlanBuilder: forwards terminal_override`
   - `LaunchPlanBuilder: computes is_dangerous`
   - `LaunchPlanBuilder: leaves executable and args empty`
6. 修改 `tests/core/CMakeLists.txt`：`add_executable(core_tests ...)` 列表追加 `launch_plan_builder_test.cpp`。

**验证**：
- `cmake --build build --config Debug` 通过。
- `ctest --test-dir build --build-config Debug -R core_tests --output-on-failure` 通过，新增 5 个用例全绿。
- 旧路径（launcher.cpp 的 build_command、terminal_command.cpp 等）未被触碰，全部旧测试仍通过。

**回归测试点**：`quote_arg_test.cpp`、`is_dangerous_test.cpp`、`selected_store_test.cpp`、`config_test.cpp`、`launcher_test.cpp` 全部仍绿。

**回滚点**：本阶段全部为新增文件 + 两处 CMake 追加行，回滚 = 删除新增文件 + 还原 CMake 即可。建议在进入 Phase 2 前确认此点构建绿色。

---

## Phase 2：platform 层接口重定义（破坏性变更，三平台占位实现）

**目标**：把 `TerminalLauncher` 抽象接口从 `launch(directory, command)` 改为 `populate(plan) + launch(plan)`。此阶段三平台实现必须同步改完，否则项目不可编译。

**文件**：
- 修改 `src/platform/terminal_launcher.h`：删旧 `launch` 虚函数，新增 `populate` 与 `launch(const LaunchPlan&)` 虚函数；`#include "core/launch_plan.h"`。
- 修改 `src/platform/terminal_launcher_win.cpp`
- 修改 `src/platform/terminal_launcher_macos.cpp`
- 修改 `src/platform/terminal_launcher_linux.cpp`
- 修改 `src/platform/terminal_launcher_factory.cpp`（补 `create_linux_launcher` 缺失定义）

**步骤**：

1. **改 `terminal_launcher.h`**：
   - 加 `#include "core/launch_plan.h"`。
   - `ProcessHandle` 结构不动（RAII 留给子任务 B）。
   - 删除 `virtual auto launch(const std::string& directory, const std::string& command)`。
   - 新增 `virtual auto populate(LaunchPlan plan) const -> std::expected<LaunchPlan, core::Error> = 0;`。
   - 新增 `virtual auto launch(const LaunchPlan& plan) -> std::expected<ProcessHandle, core::Error> = 0;`。
   - `default_terminal_name()` 与 `create()` 保留。

2. **改 `terminal_launcher_win.cpp`**（`#ifdef _WIN32` 内）：
   - 删除 `#include "platform/terminal_command.h"`，不再调用 `build_wt_command_string` / `build_pwsh_command_string`。
   - 给 `WinTerminalLauncher` 加 `populate(LaunchPlan plan) const override` 与 `launch(const LaunchPlan& plan) override`。
   - `populate` 实现 design.md 的 kWt/kPwsh/kCmd 三分支 + terminal_override 空白拆分分支。
     - 空白拆分用一个本地 helper `split_by_whitespace(string) -> vector<string>`，简单按空格/制表符切分，不处理引号（语义已在 design 风险表声明）。
   - `launch` 实现：
     - `executable.wstring()` 作为 `lpApplicationName`。
     - 实现 `join_args_for_createprocess(const vector<string>&) -> std::wstring`，按 Microsoft「Parsing C++ command-line arguments」规则引号化每个 arg 并用单空格拼接，作为 `lpCommandLine`。
     - `working_dir.wstring()` 作为 `lpCurrentDirectory`。
     - 调用 `CreateProcessW`，失败返 `Error::LaunchFailed`，成功 `CloseHandle(pi.hThread)`，填 `ProcessHandle.handle=pi.hProcess; pid=pi.dwProcessId`。
   - 保留 `WinTerminalLauncher()` 构造里的注册表/PATH 探测逻辑（kWt/kPwsh/kCmd 选择不变）。

3. **改 `terminal_launcher_macos.cpp`**（`#if defined(__APPLE__)` 内）：
   - 删除 `#include <cstdlib>`（不再用 `std::system`），加 `#include <spawn.h>`、`#include <unistd.h>`、`#include <cerrno>`、`#include <vector>`、`extern char** environ;`。
   - 删除旧 `launch(directory, command)` 实现。
   - 加 `populate`：`plan.executable="/usr/bin/osascript"`，`plan.args={"-e", "tell app \"Terminal\" to do script \""+escape_applescript(inner)+"\""}`，`inner="cd "+working_dir.string()+"; "+item.command`。
     - `escape_applescript` 本地 helper：`\\`→`\\\\`、`"`→`\\"`。
     - 注意：`item.command` 从哪来？populate 接收的 `LaunchPlan` 已无 `command` 字段。**修正**：Builder 阶段需把 `item.command` 编码进 `plan.args` 或新增字段。见下方「字段补充」修正。
   - 加 `launch`：`posix_spawn`，`argv[]` 由 `plan.executable.string()` + `plan.args` 构造，`envp=environ`，`posix_spawn_file_actions_addchdir_np` 设 `working_dir`。
   - 失败返 `Error::LaunchFailed("posix_spawn failed: "+errno)`，成功 `ProcessHandle.pid=pid`。

4. **改 `terminal_launcher_linux.cpp`**（`#if defined(__linux__)` 内）：
   - 删除 `#include <cstdlib>`、`#include "core/quote_arg.h"`，加 `#include <spawn.h>`、`#include <unistd.h>`、`#include <cerrno>`、`#include <vector>`、`extern char** environ;`。
   - 删除旧 `launch` 实现。
   - 加 `populate`：保留 `terminals[]` 优先级数组，探测改用 `access(term, X_OK)`（`<unistd.h>`），命中则填 `plan.executable=term`、`plan.args={"--","bash","-c","cd "+working_dir.string()+" && "+item.command+"; exec bash"}`。未命中返 `Error::TerminalNotFound`。terminal_override 分支同前。
   - 加 `launch`：`posix_spawnp`（executable 是相对名需 PATH 查找），`argv[]` 构造同 mac，`working_dir` 通过 `posix_spawn_file_actions_addchdir_np`。
   - CMake 需在 `cmake/CompileOptions.cmake` 或主 CMake 增加 `check_symbol_exists(posix_spawn_file_actions_addchdir_np spawn.h HAVE_ADDCHDIR_NP)`，缺符号时 `#error` 阻止编译。

5. **改 `terminal_launcher_factory.cpp`**：
   - 现有 `create_linux_launcher` 在 `#else` 分支被声明 `extern` 但项目里无定义（确认：当前 linux.cpp 只定义 `create_linux_launcher`？查证步骤 5a）。
   - **5a 查证**：用 `ace-search find_references symbolName=create_linux_launcher` 确认 linux.cpp 是否已有定义。若无，在 `terminal_launcher_linux.cpp` 末尾补 `auto create_linux_launcher() -> std::unique_ptr<pal::TerminalLauncher> { return std::make_unique<pal::LinuxTerminalLauncher>(); }`。

**字段补充修正**（影响 Phase 1 已写文件）：
- `LaunchPlan` 需新增 `std::string command;` 字段，存放原始 `item.command`，供 platform 层 populate 构造 argv。
- 回到 Phase 1 文件 `src/core/launch_plan.h` 增 `std::string command;` 字段。
- `LaunchPlanBuilder::build` 增 `plan.command = item.command;`。
- `tests/core/launch_plan_builder_test.cpp` 增用例 `LaunchPlanBuilder: preserves command`。
- 此修正在 Phase 2 步骤 3 之前完成，作为 Phase 2 的前置 sub-step。

**验证**：
- `cmake --build build --config Debug` 通过（三平台代码同时编译，但在 Windows 上只编译 win 分支；mac/linux 分支用 `#if` 隔离不参与当前编译）。
- 由于此阶段 app.cpp 还未改，`app.cpp:194 terminal->launch(dir, cmd)` 调用旧签名会导致编译错误——**这是预期的**，必须在 Phase 3 立即修复，不能停留在 Phase 2 验证。
- 因此 Phase 2 与 Phase 3 必须作为原子单元提交（不可中间停顿）。

**回归测试点**：本阶段破坏编译，回归测试在 Phase 3 末统一跑。

**回滚点**：回滚 = 还原 4 个 platform 文件 + 删除 LaunchPlan 的 `command` 字段。建议 Phase 2 + Phase 3 一起做，不留中间断点。

---

## Phase 3：app.cpp 集成 + 删除旧命令字符串路径

**目标**：把 `app.cpp` 切到新接口，同时删除 `core::Launcher::build_command`、`pal::build_wt_command_string` / `build_pwsh_command_string`、`terminal_command.{h,cpp}`、相关测试。与 Phase 2 配合恢复编译。

**文件**：
- 修改 `src/app.cpp`（`launch_item` 方法，约 181-215 行）
- 修改 `src/core/launcher.h`（删除 `build_command` 与 `quote_arg` 声明）
- 修改 `src/core/launcher.cpp`（删除 `build_command` 定义、`quote_arg` 委托、`#include "core/quote_arg.h"`）
- 删除 `src/platform/terminal_command.h`
- 删除 `src/platform/terminal_command.cpp`
- 删除 `tests/platform/terminal_command_test.cpp`
- 修改 `src/CMakeLists.txt`（core_lib 不变；platform_lib 移除 `platform/terminal_command.cpp`）
- 修改 `tests/platform/CMakeLists.txt`（移除 `terminal_command_test.cpp`，新增 `launch_plan_test.cpp`）
- 新增 `tests/platform/launch_plan_test.cpp`
- 修改 `tests/core/launcher_test.cpp`（删除两个失效用例）
- 修改 `tests/core/launch_plan_builder_test.cpp`（若 Phase 2 字段补充修正未覆盖 command 字段用例，此处补）

**步骤**：

1. **改 `src/app.cpp` 的 `launch_item`**（替换 192-203 行的 `create_and_launch` lambda 与 if/else）：
   - 删除 `create_and_launch` lambda。
   - 改为：
     ```cpp
     auto builder_result = core::LaunchPlanBuilder::build(item);
     if (!builder_result) {
         window_->set_status_text(slint::SharedString(
             "Build plan failed: " + builder_result.error().message()));
         return;
     }
     auto plan = std::move(*builder_result);
     auto terminal = pal::TerminalLauncher::create();
     auto populated = terminal->populate(std::move(plan));
     if (!populated) {
         window_->set_status_text(slint::SharedString(
             "Populate failed: " + populated.error().message()));
         return;
     }
     auto result = terminal->launch(*populated);
     ```
   - 保留 186-190 行的 `std::filesystem::exists(item.directory)` 校验不动。
   - 保留 197 行的 `std::expected<pal::ProcessHandle, core::Error> result;`？ 改为直接 `auto result = ...`（见上），删除空声明。
   - 下游 `if (result) { update_history; refresh_ui; }` 不变，但需处理 `result` 类型仍为 `std::expected<pal::ProcessHandle, core::Error>`（一致）。
   - 头部 `#include "core/launch_plan.h"` 与 `#include "core/launch_plan_builder.h"` 已隐式经 `core/launcher.h`？不——`launcher.h` 不 include `launch_plan.h`。需在 `app.cpp` 顶部加 `#include "core/launch_plan_builder.h"`。

2. **改 `src/core/launcher.h`**：
   - 删除 25-26 行 `// 构建启动命令字符串` + `auto build_command(const LaunchItem&) -> std::expected<std::string, Error>;`。
   - 删除 36 行 `auto quote_arg(const std::string& arg) -> std::string;`。

3. **改 `src/core/launcher.cpp`**：
   - 删除 `#include "core/quote_arg.h"`（第 2 行）。
   - 删除 28-33 行 `build_command` 定义。
   - 删除 77-79 行 `Launcher::quote_arg` 委托。

4. **删除 `src/platform/terminal_command.h` 与 `src/platform/terminal_command.cpp`**（用户禁止命令行删除文件——使用 `filesystem` 工具或请求用户手动删；按约束「不允许使用命令行工具编辑文件（允许移动文件）」，可移动到 `.trellis/trash/` 备份目录而非删除）。
   - 实际操作：用 `terminal-execute` 的 `Move-Item` 把两文件移到 `<PROJECT_ROOT>\launchpad\.trellis\trash\` 目录。

5. **删除 `tests/platform/terminal_command_test.cpp`**：同样 `Move-Item` 到 `.trellis/trash/`。

6. **改 `src/CMakeLists.txt`**：`platform_lib` 源列表移除 `platform/terminal_command.cpp`（第 15 行）。

7. **改 `tests/platform/CMakeLists.txt`**：
   - 移除 `terminal_command_test.cpp`。
   - 新增 `launch_plan_test.cpp`。
   - 保留 `if(WIN32)` 包裹（跨平台放开归子任务 C）。

8. **新增 `tests/platform/launch_plan_test.cpp`**（`#ifdef _WIN32` 包裹）：
   - 用例 `WinLauncher::populate: kWt produces wt argv`：构造 `WinTerminalLauncher`，调用 `populate`（默认应走 kWt 但测试机可能无 wt——需 mock 或直接测 kPwsh/kCmd 分支）。**修正**：由于 `WinTerminalLauncher` 构造时探测真实环境，测试不可控。改为测试 `populate` 的 argv 构造逻辑抽到自由函数 `build_wt_argv(working_dir, command) -> vector<string>` 等纯函数，platform_test 测纯函数。
   - 更优方案：在 `terminal_launcher_win.cpp` 内抽 `namespace pal::detail { auto build_wt_argv(...) -> vector<string>; auto build_pwsh_argv(...) -> vector<string>; auto build_cmd_argv(...) -> vector<string>; auto split_override(string) -> vector<string>; }`，populate 调用这些。测试覆盖纯函数。
   - 用例列表：
     - `build_wt_argv: produces new-tab -d <dir> pwsh -NoExit -Command <cmd>`
     - `build_wt_argv: directory with spaces preserved as single arg`
     - `build_pwsh_argv: produces -NoExit -Command "cd <dir>; <cmd>"`
     - `build_cmd_argv: produces /k cd /d <dir> && <cmd>`
     - `split_override: splits by whitespace`
     - `join_args_for_createprocess: quotes arg with spaces`
     - `join_args_for_createprocess: escapes backslash-quote boundary`

9. **改 `tests/core/launcher_test.cpp`**：
   - 删除 21-25 行 `Launcher: quote_arg delegation` 用例。
   - 删除 33-41 行 `Launcher: build_command formats correctly` 用例。
   - 其余用例（validate_item、launch_selected × 2、is_dangerous delegation）保留。

10. **若 Phase 2 字段补充修正未在 builder_test 加 command 用例**：在 `tests/core/launch_plan_builder_test.cpp` 补 `LaunchPlanBuilder: preserves command`。

**验证**：
- `cmake --build build --config Debug` 通过（app.cpp、launcher.cpp、platform 全部用新契约）。
- `ctest --test-dir build --build-config Debug --output-on-failure` 全绿：
  - core_tests：保留用例 + 新增 builder 用例。
  - platform_tests：新增 launch_plan 纯函数用例（仅 Win 编译运行）。
- `ide-get_diagnostics` 检查 `src/app.cpp`、`src/core/launcher.cpp`、`src/platform/terminal_launcher_win.cpp` 无错误。

**回归测试点**：
- `quote_arg_test.cpp` 全绿（quote_arg 仍存在，独立函数）。
- `is_dangerous_test.cpp` 全绿。
- `selected_store_test.cpp` 全绿。
- `config_test.cpp` 全绿。
- `launcher_test.cpp` 剩余用例全绿（validate_item、launch_selected × 2、is_dangerous delegation）。
- 手动验证：运行 `wt-launcher.exe`，选择一个启动项点击启动，确认终端正常打开（这是端到端冒烟，不在自动化内但建议执行）。

**回滚点**：回滚 = 从 `.trellis/trash/` 恢复 `terminal_command.{h,cpp}` 与 `terminal_command_test.cpp`，还原 `launcher.{h,cpp}`、`app.cpp`、两处 CMake。Phase 3 改动较多，建议执行前确认 Phase 1+2 已稳定。

---

## Phase 4：CMake 跨平台符号探测与文档更新

**目标**：补齐 posix_spawn_file_actions_addchdir_np 符号探测，更新 README 与 spec 注记，确保子任务 B/C 接手时无隐藏陷阱。

**文件**：
- 修改 `CMakeLists.txt` 或 `cmake/CompileOptions.cmake`（加 `check_symbol_exists`）
- 修改 `src/platform/terminal_launcher_macos.cpp` 与 `terminal_launcher_linux.cpp`（消费 `HAVE_ADDCHDIR_NP` 宏，缺失时 `#error`）
- 更新 `README.md`（架构图改为 LaunchPlan 流向）
- 更新 `.trellis/spec/` 相关注记（若存在）

**步骤**：

1. **查证 `cmake/CompileOptions.cmake` 现状**：用 `filesystem-read` 读 `<PROJECT_ROOT>\launchpad\cmake\CompileOptions.cmake`，确认是否已有 `check_symbol_exists` 机制。
2. **加符号探测**：在 `CompileOptions.cmake` 加：
   ```cmake
   include(CheckSymbolExists)
   check_symbol_exists(posix_spawn_file_actions_addchdir_np "spawn.h" HAVE_ADDCHDIR_NP)
   ```
   并在 `terminal_launcher_macos.cpp` / `terminal_launcher_linux.cpp` 顶部：
   ```cpp
   #ifndef HAVE_ADDCHDIR_NP
   #error "posix_spawn_file_actions_addchdir_np not available; upgrade toolchain."
   #endif
   ```
3. **更新 README.md**：`src/platform/` 描述从「TerminalLauncher/SingleInstance」改为「ProcessLauncher (LaunchPlan argv + posix_spawn/CreateProcessW) / SingleInstance」。
4. **更新 spec**：若 `.trellis/spec/platform/` 存在 layer spec，加注记「LaunchPlan 是 platform 与 app 的唯一启动契约，禁止再引入命令字符串拼接」。

**验证**：
- `cmake -B build -S .` 重新配置通过，CMake 输出 `HAVE_ADDCHDIR_NP` 探测结果。
- `cmake --build build --config Debug` 通过。
- `ctest --test-dir build --build-config Debug --output-on-failure` 全绿（无回归）。
- `ide-get_diagnostics` 检查两个 platform 文件无错误。

**回归测试点**：全部测试集仍绿。

**回滚点**：还原 `CompileOptions.cmake` 与两个 platform 文件的 `#ifdef` 块。

---

## 全任务验收清单

- [ ] `cmake --build build --config Debug` 通过。
- [ ] `ctest --test-dir build --build-config Debug --output-on-failure` 全绿。
- [ ] `src/app.cpp` 不再出现 `terminal.value() + " " +` 或类似字符串拼接。
- [ ] `src/core/launcher.cpp` 不再出现 `build_command` 或 `quote_arg` 引用。
- [ ] `src/platform/` 不再出现 `std::system` 或 `build_wt_command_string` / `build_pwsh_command_string`。
- [ ] `src/platform/` 不再 `#include "core/quote_arg.h"`（quote_arg 脱离主路径）。
- [ ] grep 全仓 `std::system` 无命中（除 maybe `app.cpp` 的 `popen` 用于目录选择对话框——那是子任务 C 范围，本任务不动）。
- [ ] 新增 `core/launch_plan.h`、`core/launch_plan_builder.{h,cpp}`、`tests/core/launch_plan_builder_test.cpp`、`tests/platform/launch_plan_test.cpp`。
- [ ] 删除 `platform/terminal_command.{h,cpp}` 与 `tests/platform/terminal_command_test.cpp`（移到 `.trellis/trash/`）。
- [ ] `.trellis/tasks/07-26-roots-a-noshell/` 下 design.md 与 implement.md 齐全。
- [ ] TODO 状态全部更新（完成项标记 done，遗留项「移到子任务 B/C」明确标注）。

## 遗留项（显式标注，不本任务完成）

- 后台线程 + `slint::invoke_from_event_loop`：**子任务 B**。本任务 `launch_item` 仍在 UI 线程，posix_spawn 在 mac/linux 通常很快但仍是同步调用。
- `ProcessHandle` RAII：**子任务 B**。本任务 `WinLauncher::launch` 仍手动 `CloseHandle(pi.hThread)`，`hProcess` 存入裸 `void*`。
- `AppSettings` Glaze `modify` alias：**子任务 B**。
- mac/linux `SingleInstance` lockfile：**子任务 C**。
- `ThemeDetector` 系统主题：**子任务 C**。
- `tests/platform/CMakeLists.txt` 的 `if(WIN32)` 移除：**子任务 C**。本任务只更新 Win 路径测试内容。
- mac/linux 实机测试：本机为 Windows，mac/linux 路径仅做编译可行性保证，实机回归在子任务 C 或 CI 接入后执行。
