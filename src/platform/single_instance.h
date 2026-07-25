#pragma once

namespace pal {

class SingleInstance {
public:
    SingleInstance();
    ~SingleInstance();

    bool is_only_instance() const { return is_only_; }

private:
    bool is_only_ = true;
#ifdef _WIN32
    void* mutex_ = nullptr;
#endif
};

} // namespace pal
