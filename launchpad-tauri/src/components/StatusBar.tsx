import { useAppStore } from "../stores/useAppStore";
import { format } from "../i18n/keys";

export function StatusBar() {
  const { status, language } = useAppStore();
  if (!status) return null;
  return <footer className="status-bar">{format(status.key, language, status.args ?? [])}</footer>;
}
