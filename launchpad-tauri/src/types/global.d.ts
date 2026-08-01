// Tauri v2 global bridge (enabled via withGlobalTauri in tauri.conf.json);
// used only for the best-effort window restore in App.tsx.

interface Window {
  __TAURI__?: {
    window?: {
      getCurrent?: () => {
        setPosition?: (x: number, y: number) => Promise<void> | void;
        setSize?: (size: { width: number; height: number }) => Promise<void> | void;
      };
    };
  };
}
