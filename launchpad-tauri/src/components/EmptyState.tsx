import { useAppStore } from "../stores/useAppStore";
import { t } from "../i18n/keys";

export function EmptyState() {
  const { language, items, searchQuery, openNew } = useAppStore();
  const noMatch = items.length > 0 && searchQuery.trim() !== "";

  if (noMatch) {
    return (
      <div className="empty-state">
        <h2>{t("NoMatchTitle", language)}</h2>
        <p>{t("NoMatchSubtitle", language)}</p>
      </div>
    );
  }
  if (items.length > 0) return null;
  return (
    <div className="empty-state">
      <h2>{t("EmptyTitle", language)}</h2>
      <p>{t("EmptySubtitle", language)}</p>
      <button className="primary-btn" onClick={openNew}>
        {t("EmptyButton", language)}
      </button>
    </div>
  );
}
