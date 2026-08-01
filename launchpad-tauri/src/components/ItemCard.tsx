import { Pencil, Trash2, ChevronUp, ChevronDown, AlertTriangle } from "lucide-react";
import { useAppStore } from "../stores/useAppStore";
import { t } from "../i18n/keys";
import type { LaunchItem } from "../types";

interface Props {
  item: LaunchItem;
}

export function ItemCard({ item }: Props) {
  const { language, setSelect, moveItem, askDelete, openEdit, launchOne } = useAppStore();

  const dangerous =
    item.command.includes("--dangerously") ||
    /--yolo|--skip-permissions|--bypass-approvals|--bypass-sandbox|--bypass\.sandbox/i.test(
      item.command,
    );

  return (
    <div className={`item-card${dangerous ? " dangerous" : ""}${item.selected ? " selected" : ""}`}>
      <label className="card-check">
        <input
          type="checkbox"
          checked={item.selected}
          onChange={(e) => setSelect(item.id, e.currentTarget.checked)}
        />
      </label>
      <div className="card-main" onClick={() => launchOne(item.id)}>
        <div className="card-title">
          {item.name}
          {dangerous && <AlertTriangle size={14} className="danger-icon" />}
        </div>
        <div className="card-dir">{item.directory}</div>
        <code className="card-cmd">{item.command}</code>
      </div>
      <div className="card-actions">
        <button
          className="icon-btn"
          title={t("TooltipMoveUp", language)}
          onClick={() => moveItem(item.id, -1)}
        >
          <ChevronUp size={14} />
        </button>
        <button
          className="icon-btn"
          title={t("TooltipMoveDown", language)}
          onClick={() => moveItem(item.id, 1)}
        >
          <ChevronDown size={14} />
        </button>
        <button
          className="icon-btn"
          title={t("TooltipEdit", language)}
          onClick={() => openEdit(item)}
        >
          <Pencil size={14} />
        </button>
        <button
          className="icon-btn danger"
          title={t("TooltipDelete", language)}
          onClick={() => askDelete(item)}
        >
          <Trash2 size={14} />
        </button>
      </div>
    </div>
  );
}
