import { AlertTriangle } from "lucide-react";
import { useAppStore, type PendingBatchConfirm, type PendingConfirm } from "../stores/useAppStore";
import { format, t } from "../i18n/keys";
import { isDangerous } from "../lib/danger";

interface Props {
  pending: PendingConfirm | PendingBatchConfirm;
}

export function ConfirmDialog({ pending }: Props) {
  const { language, items, confirmPending, cancelPending } = useAppStore();

  if (pending.kind === "single") {
    const item = items.find((i) => i.id === pending.id);
    const dangerText = pending.dangerKey ? t(pending.dangerKey as never, language) : null;
    return (
      <div className="modal-overlay">
        <div className="modal">
          <h2>{t("DialogConfirmLaunchTitle", language)}</h2>
          {item && (
            <>
              <p>{format("DialogLabelName", language, [item.name])}</p>
              <p>{format("DialogLabelCommand", language, [item.command])}</p>
              <p>{format("DialogLabelDirectory", language, [item.directory])}</p>
            </>
          )}
          {dangerText && (
            <p className="danger-warning">
              <AlertTriangle size={14} /> {dangerText}
            </p>
          )}
          <div className="modal-actions">
            <button className="primary-btn" onClick={confirmPending}>
              {t("BtnLaunch", language)}
            </button>
            <button className="ghost-btn" onClick={cancelPending}>
              {t("BtnCancel", language)}
            </button>
          </div>
        </div>
      </div>
    );
  }

  const confirmItems = items.filter((i) => pending.confirmIds.includes(i.id));
  return (
    <div className="modal-overlay">
      <div className="modal">
        <h2>{format("DialogBatchTitle", language, [pending.confirmIds.length])}</h2>
        <ul className="confirm-list">
          {confirmItems.map((i) => (
            <li key={i.id}>
              {i.name}
              {isDangerous(i.command) && <AlertTriangle size={12} className="danger-icon" />}
              <code>{i.command}</code>
            </li>
          ))}
        </ul>
        <div className="modal-actions">
          <button className="primary-btn" onClick={confirmPending}>
            {t("BtnLaunchAll", language)}
          </button>
          <button className="ghost-btn" onClick={cancelPending}>
            {t("BtnCancel", language)}
          </button>
        </div>
      </div>
    </div>
  );
}
