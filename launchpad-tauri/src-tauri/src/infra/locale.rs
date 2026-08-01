//! System language probe (mirrors C# GlobalizationPreferences.Languages):
//! returns the user's first preferred language tag ("zh-CN", "en-US", ...).
//! Implemented via GetUserDefaultLocaleName.

use windows::Win32::Globalization::GetUserDefaultLocaleName;

pub fn first_system_language() -> Option<String> {
    let mut buf = [0u16; 128];
    // SAFETY: buf is a valid writable buffer of the declared length.
    let len = unsafe { GetUserDefaultLocaleName(&mut buf) };
    if len == 0 {
        return None;
    }
    let end = buf.iter().position(|&c| c == 0).unwrap_or(buf.len());
    Some(String::from_utf16_lossy(&buf[..end]))
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn system_locale_is_a_nonempty_tag() {
        let lang = first_system_language();
        assert!(lang.is_some(), "GetUserDefaultLocaleName should always return a locale");
        let lang = lang.unwrap();
        assert!(!lang.is_empty());
        assert!(lang.contains('-'), "locale tags look like zh-CN, got {lang}");
    }
}
