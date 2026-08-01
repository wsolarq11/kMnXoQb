import { useEffect } from "react";
import { useAppStore } from "./stores/useAppStore";
import { HeaderBar } from "./components/HeaderBar";
import { StatBar } from "./components/StatBar";
import { ItemCard } from "./components/ItemCard";
import { EmptyState } from "./components/EmptyState";
import { EditDialog } from "./components/EditDialog";
import { DeleteDialog } from "./components/DeleteDialog";
import { ConfirmDialog } from "./components/ConfirmDialog";
import { StatusBar } from "./components/StatusBar";
import "./App.css";

function App() {
  const { items, searchQuery, editing, newDialogOpen, deleteTarget, pendingConfirm, openNew, init } =
    useAppStore();

  useEffect(() => {
    void init();
    void restoreWindow();
  }, [init]);

  const visible = items.filter((i) =>
    [i.name, i.directory, i.command].some((f) =>
      f.toLowerCase().includes(searchQuery.trim().toLowerCase()),
    ),
  );

  return (
    <main className="app">
      <HeaderBar onNew={openNew} />
      <StatBar />
      <section className="item-grid">
        {visible.map((item) => (
          <ItemCard key={item.id} item={item} />
        ))}
        <EmptyState />
      </section>
      <StatusBar />

      {(newDialogOpen || editing) && <EditDialog item={editing} />}
      {deleteTarget && <DeleteDialog target={deleteTarget} />}
      {pendingConfirm && <ConfirmDialog pending={pendingConfirm} />}
    </main>
  );
}

async function restoreWindow() {
  try {
    const { loadWindowState } = await import("./lib/invoke");
    const state = await loadWindowState();
    if (state) {
      // Apply via the webview's host API (phase 5 wires this to the Tauri
      // window); position/size are already clamped by the core.
      window.__TAURI__?.window?.getCurrent?.().setPosition?.(state.x, state.y);
      window.__TAURI__?.window?.getCurrent?.().setSize?.({ width: state.width, height: state.height });
    }
  } catch {
    // window restore is best-effort; the core clamp already guards geometry
  }
}

export default App;
