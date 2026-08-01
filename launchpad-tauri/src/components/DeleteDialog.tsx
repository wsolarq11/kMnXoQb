import { useAppStore } from "../stores/useAppStore";
import { format, t } from "../i18n/keys";
import type { LaunchItem } from "../types";

interface Props {
  target: LaunchItem;
}

export function DeleteDialog({ target }: Props) {
  const { language, deleteItem, closeDialogs } = useAppStore();

  async function confirm() {
    closeDialogs();
    await deleteItem(target.id);
  }

  return (
    <div
      className="modal-overlay"
      onClick={closeDialogs}
      onKeyDown={(e) => {
        if (e.key === "Enter") void confirm();
      }}
    >
      <div className="modal" onClick={(e) => e.stopPropagation()}>
        <h2>{t("DialogDeleteItemTitle", language)}</h2>
        <p>{format("DialogDeleteItemMessage", language, [target.name])}</p>
        <div className="modal-actions">
          <button className="danger-btn" onClick={confirm}>
            {t("BtnDelete", language)}
          </button>
          <button className="ghost-btn" onClick={closeDialogs}>
            {t("BtnCancel", language)}
          </button>
        </div>
      </div>
    </div>
  );
}
