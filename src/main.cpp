#include "app.h"
#include "main_window.h"

int main() {
    auto main_window = MainWindow::create();
    auto app = std::make_shared<App>(main_window);
    return app->run();
}
