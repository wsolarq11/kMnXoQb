#include "platform/dialog_provider.h"

namespace pal {

#if defined(_WIN32)
extern auto create_dialog_provider() -> std::unique_ptr<DialogProvider>;
#elif defined(__APPLE__)
extern auto create_dialog_provider() -> std::unique_ptr<DialogProvider>;
#else
extern auto create_dialog_provider() -> std::unique_ptr<DialogProvider>;
#endif

auto DialogProvider::create() -> std::unique_ptr<DialogProvider> {
    return create_dialog_provider();
}

} // namespace pal
