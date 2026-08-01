import { Rocket } from "lucide-react";
import { useAppStore } from "../stores/useAppStore";
import { t } from "../i18n/keys";

export const APP_VERSION = "0.1.0";

export function AboutDialog() {
  const { language, closeDialogs } = useAppStore();
  return (
    <div className="modal-overlay" onClick={closeDialogs}>
      <div className="modal about-modal" onClick={(e) => e.stopPropagation()}>
        <div className="about-header">
          <Rocket size={36} className="about-logo" />
          <h2>Launchpad v{APP_VERSION}</h2>
        </div>
        <p className="about-text">
          {language === "zh-CN"
            ? "AI CLI 工具启动器 — 在一个界面里集中启动 snow / codex / claude / opencode。"
            : "AI CLI launcher — start snow / codex / claude / opencode from one place."}
        </p>
        <div className="about-shortcuts">
          <div>
            <kbd>Enter</kbd> {t("BtnLaunch", language)}
          </div>
          <div>
            <kbd>Ctrl+F</kbd> {t("SearchPlaceholder", language)}
          </div>
          <div>
            <kbd>Esc</kbd> {t("BtnCancel", language)}
          </div>
        </div>
        <div className="modal-actions">
          <button className="ghost-btn" onClick={closeDialogs}>
            {t("BtnCancel", language)}
          </button>
        </div>
      </div>
    </div>
  );
}
