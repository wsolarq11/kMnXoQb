import { Plus, CheckSquare, Square, Rocket, Sun, Moon, Settings } from "lucide-react";
import { useAppStore } from "../stores/useAppStore";
import { t } from "../i18n/keys";

interface Props {
  onNew: () => void;
}

export function HeaderBar({ onNew }: Props) {
  const { settings, language, searchQuery, setSearchQuery, toggleSelectAll, launchSelected, toggleTheme, toggleLanguage } =
    useAppStore();
  const allSelected =
    useAppStore.getState().items.length > 0 &&
    useAppStore.getState().items.every((i) => i.selected);

  const themeGlyph =
    settings.theme === "dark" ? (
      <Sun size={16} />
    ) : settings.theme === "light" ? (
      <Moon size={16} />
    ) : (
      <Settings size={16} />
    );

  return (
    <header className="header-bar">
      <input
        id="search-input" className="search-input"
        value={searchQuery}
        onChange={(e) => setSearchQuery(e.currentTarget.value)}
        placeholder={t("SearchPlaceholder", language)}
      />
      <button className="icon-btn" title={t("BtnSelectAll", language)} onClick={toggleSelectAll}>
        {allSelected ? <CheckSquare size={16} /> : <Square size={16} />}
      </button>
      <button className="icon-btn" title={t("BtnLaunchSelected", language)} onClick={launchSelected}>
        <Rocket size={16} />
      </button>
      <button className="primary-btn" onClick={onNew}>
        <Plus size={16} />
        {t("BtnNew", language)}
      </button>
      <button className="icon-btn" title={t("TooltipTheme", language)} onClick={toggleTheme}>
        {themeGlyph}
      </button>
      <button className="icon-btn" title={t("TooltipLanguage", language)} onClick={toggleLanguage}>
        {language === "zh-CN" ? "中" : "EN"}
      </button>
    </header>
  );
}
