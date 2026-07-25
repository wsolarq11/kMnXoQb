#pragma once

#include <string>
#include <unordered_map>
#include <vector>

#include "core/launch_item.h"

namespace core {

class SelectedStore {
public:
    // 从 items 加载选中状态
    void load_from(const std::vector<LaunchItem>& items);

    // 设置/取消选中
    void set_selected(const std::string& id, bool selected);

    // 全选/全不选
    void select_all(const std::vector<LaunchItem>& items);
    void deselect_all();

    // 查询
    bool is_selected(const std::string& id) const;
    auto count() const -> size_t;
    auto selected_ids() const -> std::vector<std::string>;

    // 保存到 items（合并 selected 状态到 items 数组）
    void save_to(std::vector<LaunchItem>& items) const;

private:
    std::unordered_map<std::string, bool> selected_;
};

} // namespace core
