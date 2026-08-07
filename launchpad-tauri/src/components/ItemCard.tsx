import { Pencil, Trash2, ChevronUp, ChevronDown, AlertTriangle } from "lucide-react";
import { useAppStore } from "../stores/useAppStore";
import { t } from "../i18n/keys";
import { highlight } from "../lib/highlight";
import type { LaunchItem } from "../types";

interface Props {
  item: LaunchItem;
  index: number;
  total: number;
  query: string;
}

export function ItemCard({ item, index, total, query }: Props) {
  const { language, setSelect, moveItem, askDelete, openEdit, launchOne, dirStatus } =
    useAppStore();
  const directoryMissing = dirStatus[item.id] === false;
  const canMoveUp = index > 0;
  const canMoveDown = index < total - 1;

  const dangerous =
    item.command.includes("--dangerously") ||
    /--yolo|--skip-permissions|--bypass-approvals|--bypass-sandbox|--bypass\.sandbox/i.test(
      item.command,
    );

  return (
    // Whole card launches on click; the checkbox and the action buttons are
    // semantic regions and stop propagation so they never trigger a launch.
    <div
      className={`item-card${dangerous ? " dangerous" : ""}${item.selected ? " selected" : ""}`}
      onClick={() => launchOne(item.id)}
      tabIndex={0}
      onKeyDown={(e) => {
        // Enter launches only when the card itself holds focus; buttons and
        // the checkbox handle their own Enter without bubbling into launch.
        if (e.key === "Enter" && e.target === e.currentTarget) {
          e.preventDefault();
          launchOne(item.id);
        }
      }}
    >
      <label className="card-check" onClick={(e) => e.stopPropagation()}>
        <input
          type="checkbox"
          checked={item.selected}
          onChange={(e) => setSelect(item.id, e.currentTarget.checked)}
        />
      </label>
      <div className="card-main">
        <div className="card-title">
          {/* Span wrapper: ellipsis needs a single text flex item; highlight
              may return a Fragment (query match), which would otherwise
              render as several anonymous items and never truncate. */}
          <span className="card-title-text">{highlight(item.name, query)}</span>
          {dangerous && <AlertTriangle size={14} className="danger-icon" />}
        </div>
        <div className="card-dir">{highlight(item.directory, query)}</div>
        {directoryMissing && (
          <div className="card-dir-status">{t("ValidationDirectoryMissing", language)}</div>
        )}
        <code className="card-cmd">{highlight(item.command, query)}</code>
      </div>
      <div className="card-actions" onClick={(e) => e.stopPropagation()}>
        <button
          className="icon-btn"
          title={t("TooltipMoveUp", language)}
          disabled={!canMoveUp}
          onClick={() => moveItem(item.id, -1)}
        >
          <ChevronUp size={14} />
        </button>
        <button
          className="icon-btn"
          title={t("TooltipMoveDown", language)}
          disabled={!canMoveDown}
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
