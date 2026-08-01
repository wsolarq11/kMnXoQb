// Frontend mirror of the Rust core DangerousFlagDetector (same 6-flag table,
// case-insensitive substring match). The core remains authoritative; this
// powers instant in-edit warnings without a round-trip.

import type { LanguageKey } from "../i18n/keys";

const FLAGS: { flag: string; key: LanguageKey }[] = [
  { flag: "dangerously", key: "DangerReasonDangerously" },
  { flag: "yolo", key: "DangerReasonYolo" },
  { flag: "skip-permissions", key: "DangerReasonSkipPermissions" },
  { flag: "bypass-approvals", key: "DangerReasonBypassApprovals" },
  { flag: "bypass-sandbox", key: "DangerReasonBypassSandbox" },
  { flag: "bypass.sandbox", key: "DangerReasonBypassSandbox" },
];

export function isDangerous(command: string): boolean {
  const lower = command.toLowerCase();
  return FLAGS.some(({ flag }) => lower.includes(flag));
}

export function dangerKey(command: string): LanguageKey | null {
  const lower = command.toLowerCase();
  const hit = FLAGS.find(({ flag }) => lower.includes(flag));
  return hit ? hit.key : null;
}

/** Rust serializes the reason as the variant name; guard it to a known key. */
export function dangerKeyFromRust(name: string | null | undefined): LanguageKey | null {
  if (!name) return null;
  return FLAGS.some(({ key }) => key === name) ? (name as LanguageKey) : null;
}
