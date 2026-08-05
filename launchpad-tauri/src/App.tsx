import { useEffect } from "react";
import { useAppStore } from "./stores/useAppStore";
import { HeaderBar } from "./components/HeaderBar";
import { StatBar } from "./components/StatBar";
import { ItemCard } from "./components/ItemCard";
import { EmptyState } from "./components/EmptyState";
import { EditDialog } from "./components/EditDialog";
import { DeleteDialog } from "./components/DeleteDialog";
import { ConfirmDialog } from "./components/ConfirmDialog";
import { AboutDialog } from "./components/AboutDialog";
import { StatusBar } from "./components/StatusBar";
import { applyTheme } from "./lib/theme";
import { windowMaterial } from "./lib/invoke";
import { installGlobalKeys } from "./lib/keyboard";
import { useGridPlan } from "./hooks/useGridPlan";
import { t } from "./i18n/keys";
import "./App.css";

function App() {
  const {
    items,
    searchQuery,
    editing,
    newDialogOpen,
    deleteTarget,
    pendingConfirm,
    loading,
    language,
    aboutOpen,
    openNew,
    init,
    closeDialogs,
    cancelPending,
  } = useAppStore();

  useEffect(() => {
    void init();
  }, [init]);

  // Theme three-state wiring (settings -> data-theme -> CSS). Window geometry
  // restore/persist is handled on the Rust side (lib.rs setup + CloseRequested).
  const theme = useAppStore((s) => s.settings.theme);
  useEffect(() => {
    applyTheme(theme);
  }, [theme]);

  // Window material (Mica/Acrylic) flips body.material so the CSS chrome
  // turns translucent; without it the body stays opaque.
  useEffect(() => {
    let cancelled = false;
    void windowMaterial().then((state) => {
      if (!cancelled) {
        document.body.classList.toggle("material", state !== "none");
      }
    });
    return () => {
      cancelled = true;
    };
  }, []);

  // Global keys: Esc closes dialogs, Ctrl+F focuses the search box.
  useEffect(() => {
    return installGlobalKeys(
      () => {
        closeDialogs();
        cancelPending();
      },
      () => document.getElementById("search-input")?.focus(),
    );
  }, [closeDialogs, cancelPending]);

  const visible = items.filter((i) =>
    [i.name, i.directory, i.command].some((f) =>
      f.toLowerCase().includes(searchQuery.trim().toLowerCase()),
    ),
  );

  const { ref, columns } = useGridPlan(visible.length);

  return (
    <main className="app">
      {loading && (
        <div className="boot-screen">
          <div className="boot-spinner" />
          <p>{t("BootLoading", language)}</p>
        </div>
      )}
      <HeaderBar onNew={openNew} />
      <StatBar />
      <section
        ref={ref}
        className="item-grid"
        style={
          columns > 0
            ? { gridTemplateColumns: `repeat(${columns}, minmax(0, 1fr))` }
            : undefined
        }
      >
        {visible.map((item, index) => (
          <ItemCard key={item.id} item={item} index={index} total={visible.length} query={searchQuery} />
        ))}
        <EmptyState />
      </section>
      <StatusBar />

      {(newDialogOpen || editing) && <EditDialog item={editing} />}
      {deleteTarget && <DeleteDialog target={deleteTarget} />}
      {pendingConfirm && <ConfirmDialog pending={pendingConfirm} />}
      {aboutOpen && <AboutDialog />}
    </main>
  );
}

export default App;
