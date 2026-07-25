#include "app.h"
#include "main_window.h"

int main() {
    auto main_window = MainWindow::create();
    App app(main_window);
    return app.run();
}
