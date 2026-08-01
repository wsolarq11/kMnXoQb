import { useAppStore } from "../stores/useAppStore";
import { t } from "../i18n/keys";

export function StatBar() {
  const { items, settings, language } = useAppStore();
  const selected = items.filter((i) => i.selected).length;
  const recent = settings.launch_history[0] ?? "--";
  return (
    <div className="stat-bar">
      <span>
        {t("StatItems", language)}: {items.length}
      </span>
      <span>
        {t("StatRecent", language)}: {recent}
      </span>
      {selected > 0 && <span>✓ {selected}</span>}
    </div>
  );
}
