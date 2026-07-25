#include "core/selected_store.h"

namespace core {

void SelectedStore::load_from(const std::vector<LaunchItem>& items) {
    selected_.clear();
    for (const auto& item : items) {
        if (item.selected) {
            selected_[item.id] = true;
        }
    }
}

void SelectedStore::set_selected(const std::string& id, bool selected) {
    if (selected) {
        selected_[id] = true;
    } else {
        selected_.erase(id);
    }
}

void SelectedStore::select_all(const std::vector<LaunchItem>& items) {
    for (const auto& item : items) {
        selected_[item.id] = true;
    }
}

void SelectedStore::deselect_all() {
    selected_.clear();
}

bool SelectedStore::is_selected(const std::string& id) const {
    return selected_.contains(id);
}

auto SelectedStore::count() const -> size_t {
    return selected_.size();
}

auto SelectedStore::selected_ids() const -> std::vector<std::string> {
    std::vector<std::string> ids;
    ids.reserve(selected_.size());
    for (const auto& [id, _] : selected_) {
        ids.push_back(id);
    }
    return ids;
}

void SelectedStore::save_to(std::vector<LaunchItem>& items) const {
    for (auto& item : items) {
        item.selected = selected_.contains(item.id);
    }
}

} // namespace core
