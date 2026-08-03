//! Flags a command as dangerous. Case-insensitive substring match over a fixed
//! table; the reason is a language-independent LanguageKey (ported 1:1 from
//! C# DangerousFlagDetector, which itself mirrors the legacy Rust is_dangerous).

use crate::core::i18n::LanguageKey;

pub const DANGEROUS_FLAGS: &[(&str, LanguageKey)] = &[
    ("dangerously", LanguageKey::DangerReasonDangerously),
    ("yolo", LanguageKey::DangerReasonYolo),
    ("skip-permissions", LanguageKey::DangerReasonSkipPermissions),
    ("bypass-approvals", LanguageKey::DangerReasonBypassApprovals),
    ("bypass-sandbox", LanguageKey::DangerReasonBypassSandbox),
    ("bypass.sandbox", LanguageKey::DangerReasonBypassSandbox),
];

pub fn is_dangerous(command: &str) -> bool {
    let lower = command.to_ascii_lowercase();
    DANGEROUS_FLAGS.iter().any(|(flag, _)| lower.contains(flag))
}

/// Explicit loop, not find().map(): the first matching flag wins and safe
/// commands return None (no default-key pitfall on the enum side).
pub fn dangerous_reason(command: &str) -> Option<LanguageKey> {
    let lower = command.to_ascii_lowercase();
    for (flag, reason) in DANGEROUS_FLAGS {
        if lower.contains(flag) {
            return Some(*reason);
        }
    }
    None
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn flags_known_dangerous_commands() {
        for command in [
            "codex --dangerously-bypass-approvals-and-sandbox",
            "npm i --yolo",
            "claude --dangerously-skip-permissions",
            "tool --bypass-approvals run",
            "tool --bypass-sandbox run",
            "tool --bypass.sandbox run",
        ] {
            assert!(is_dangerous(command), "should flag: {command}");
        }
    }

    #[test]
    fn is_case_insensitive() {
        assert!(is_dangerous("claude --DANGEROUSLY-skip-permissions"));
    }

    #[test]
    fn does_not_flag_safe_commands() {
        for command in ["snow", "opencode", "echo safe", "git status"] {
            assert!(!is_dangerous(command), "should be safe: {command}");
        }
    }

    #[test]
    fn reason_returns_matching_flag_key() {
        assert_eq!(
            Some(LanguageKey::DangerReasonDangerously),
            dangerous_reason("claude --dangerously-skip-permissions")
        );
    }

    #[test]
    fn reason_returns_none_for_safe_command() {
        assert_eq!(None, dangerous_reason("snow"));
    }
}
