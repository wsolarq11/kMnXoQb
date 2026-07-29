#ifdef _WIN32

#include "platform/dialog_provider.h"
#include <shlobj.h>
#include <windows.h>

namespace pal {

class WinDialogProvider final : public DialogProvider {
public:
    auto browse_directory() -> std::optional<std::filesystem::path> override {
        IFileDialog* pfd = nullptr;
        HRESULT hr = CoCreateInstance(CLSID_FileOpenDialog, nullptr,
            CLSCTX_INPROC_SERVER, IID_PPV_ARGS(&pfd));
        if (FAILED(hr)) return std::nullopt;

        DWORD options;
        pfd->GetOptions(&options);
        pfd->SetOptions(options | FOS_PICKFOLDERS);

        std::optional<std::filesystem::path> result;
        if (SUCCEEDED(pfd->Show(nullptr))) {
            IShellItem* psi = nullptr;
            if (SUCCEEDED(pfd->GetResult(&psi))) {
                PWSTR path = nullptr;
                if (SUCCEEDED(psi->GetDisplayName(SIGDN_FILESYSPATH, &path))) {
                    int len = WideCharToMultiByte(CP_UTF8, 0, path, -1,
                        nullptr, 0, nullptr, nullptr);
                    std::string dir(len, '\0');
                    WideCharToMultiByte(CP_UTF8, 0, path, -1,
                        &dir[0], len, nullptr, nullptr);
                    result = std::filesystem::path(dir);
                    CoTaskMemFree(path);
                }
                psi->Release();
            }
        }
        pfd->Release();
        return result;
    }
};

auto create_dialog_provider() -> std::unique_ptr<DialogProvider> {
    return std::make_unique<WinDialogProvider>();
}

} // namespace pal

#endif // _WIN32
