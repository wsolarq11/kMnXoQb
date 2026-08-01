//! Pure form validation; mirrors the Flutter edit dialog rules (ported 1:1
//! from C# ItemValidator). Errors are language-independent keys.

use crate::core::i18n::LanguageKey;

#[derive(Debug, Clone, PartialEq)]
pub struct ValidationErrors {
    pub name_error: Option<LanguageKey>,
    pub command_error: Option<LanguageKey>,
}

impl ValidationErrors {
    pub fn is_valid(&self) -> bool {
        self.name_error.is_none() && self.command_error.is_none()
    }
}

pub fn validate(name: Option<&str>, command: Option<&str>) -> ValidationErrors {
    let name_error = match name {
        Some(n) if !n.trim().is_empty() => None,
        _ => Some(LanguageKey::ValidationNameRequired),
    };
    let command_error = match command {
        Some(c) if !c.trim().is_empty() => None,
        _ => Some(LanguageKey::ValidationCommandRequired),
    };
    ValidationErrors {
        name_error,
        command_error,
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn valid_when_both_present() {
        let r = validate(Some("snow"), Some("snow run"));
        assert!(r.is_valid());
        assert_eq!(None, r.name_error);
        assert_eq!(None, r.command_error);
    }

    #[test]
    fn name_required_when_blank() {
        for name in [None, Some(""), Some("   ")] {
            let r = validate(name, Some("snow"));
            assert_eq!(Some(LanguageKey::ValidationNameRequired), r.name_error);
            assert!(r.command_error.is_none());
        }
    }

    #[test]
    fn command_required_when_blank() {
        for command in [None, Some(""), Some("  ")] {
            let r = validate(Some("snow"), command);
            assert_eq!(Some(LanguageKey::ValidationCommandRequired), r.command_error);
            assert!(r.name_error.is_none());
        }
    }

    #[test]
    fn both_errors_when_both_blank() {
        let r = validate(None, None);
        assert!(!r.is_valid());
        assert!(r.name_error.is_some());
        assert!(r.command_error.is_some());
    }
}
