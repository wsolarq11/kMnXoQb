import { useState } from "react";
import { FolderOpen, AlertTriangle } from "lucide-react";
import { useAppStore } from "../stores/useAppStore";
import { t } from "../i18n/keys";
import { pickDirectory } from "../lib/invoke";
import type { LaunchItem } from "../types";

interface Props {
  item: LaunchItem | null; // null = new item
}

export function EditDialog({ item }: Props) {
  const { language, createItem, updateItem, closeDialogs } = useAppStore();
  const isNew = item === null;

  const [name, setName] = useState(item?.name ?? "");
  const [directory, setDirectory] = useState(item?.directory ?? "");
  const [command, setCommand] = useState(item?.command ?? "");
  const [confirm, setConfirm] = useState(item?.confirm ?? true);
  const [terminal, setTerminal] = useState(item?.terminal ?? "");
  const [error, setError] = useState<string | null>(null);

  const dangerous =
    command.includes("--dangerously") ||
    /--yolo|--skip-permissions|--bypass-approvals|--bypass-sandbox|--bypass\.sandbox/i.test(
      command,
    );

  async function pickDir() {
    const dir = await pickDirectory();
    if (dir) setDirectory(dir);
  }

  async function save() {
    if (!name.trim()) {
      setError(t("ValidationNameRequired", language));
      return;
    }
    if (!command.trim()) {
      setError(t("ValidationCommandRequired", language));
      return;
    }
    const input = {
      name: name.trim(),
      directory: directory.trim(),
      command: command.trim(),
      confirm,
      terminal: terminal.trim() || null,
    };
    const ok = isNew
      ? await createItem(input)
      : await updateItem(item.id, input);
    if (ok) closeDialogs();
  }

  return (
    <div className="modal-overlay" onClick={closeDialogs}>
      <div className="modal" onClick={(e) => e.stopPropagation()}>
        <h2>{t(isNew ? "EditTitleNew" : "EditTitleEdit", language)}</h2>

        <label>{t("FieldName", language)}</label>
        <input value={name} onChange={(e) => setName(e.currentTarget.value)} placeholder={t("PlaceholderRequired", language)} />

        <label>{t("FieldDirectory", language)}</label>
        <div className="row">
          <input value={directory} onChange={(e) => setDirectory(e.currentTarget.value)} placeholder={t("PlaceholderDirectory", language)} />
          <button className="icon-btn" onClick={pickDir} title={t("FieldDirectory", language)}>
            <FolderOpen size={16} />
          </button>
        </div>

        <label>{t("FieldCommand", language)}</label>
        <textarea value={command} onChange={(e) => setCommand(e.currentTarget.value)} placeholder={t("PlaceholderRequired", language)} />
        {dangerous && (
          <p className="danger-warning">
            <AlertTriangle size={14} /> {t("DangerReasonDangerously", language)}
          </p>
        )}

        <label>{t("FieldTerminal", language)}</label>
        <input value={terminal} onChange={(e) => setTerminal(e.currentTarget.value)} placeholder={t("PlaceholderTerminal", language)} />

        <label className="row">
          <input type="checkbox" checked={confirm} onChange={(e) => setConfirm(e.currentTarget.checked)} />
          {t("CheckboxConfirmBeforeLaunch", language)}
        </label>

        {error && <p className="error-text">{error}</p>}

        <div className="modal-actions">
          <button className="primary-btn" onClick={save}>
            {t("BtnSave", language)}
          </button>
          <button className="ghost-btn" onClick={closeDialogs}>
            {t("BtnCancel", language)}
          </button>
        </div>
      </div>
    </div>
  );
}
