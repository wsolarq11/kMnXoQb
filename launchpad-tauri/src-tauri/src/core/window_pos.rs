//! Pure window-position sanitizer (ported 1:1 from C# WindowPosition).
//! Guards against minimized-offscreen coordinates (-32000) and degenerate
//! sizes persisted on close.

use crate::core::models::WindowState;

pub const DEFAULT_WIDTH: u32 = 800;
pub const DEFAULT_HEIGHT: u32 = 600;

const MIN_WIDTH: i32 = 200;
const MIN_HEIGHT: i32 = 100;
const DEFAULT_X: i32 = 100;
const DEFAULT_Y: i32 = 100;

fn default_state() -> WindowState {
    WindowState {
        x: DEFAULT_X,
        y: DEFAULT_Y,
        width: DEFAULT_WIDTH,
        height: DEFAULT_HEIGHT,
    }
}

pub fn clamp_to_visible(
    state: &WindowState,
    virtual_left: i32,
    virtual_top: i32,
    virtual_width: i32,
    virtual_height: i32,
    min_visible: i32,
) -> WindowState {
    if state.width < MIN_WIDTH as u32 || state.height < MIN_HEIGHT as u32 {
        return default_state();
    }

    let overlap_x = (state.x + state.width as i32).min(virtual_left + virtual_width)
        - (state.x.max(virtual_left));
    let overlap_y = (state.y + state.height as i32).min(virtual_top + virtual_height)
        - (state.y.max(virtual_top));
    if overlap_x < min_visible || overlap_y < min_visible {
        return default_state();
    }

    state.clone()
}

#[cfg(test)]
mod tests {
    use super::*;

    // 1920x1080 primary screen (no negative multi-screen origin).
    const SCREEN_LEFT: i32 = 0;
    const SCREEN_TOP: i32 = 0;
    const SCREEN_WIDTH: i32 = 1920;
    const SCREEN_HEIGHT: i32 = 1080;

    fn state(x: i32, y: i32, width: u32, height: u32) -> WindowState {
        WindowState { x, y, width, height }
    }

    #[test]
    fn keeps_visible_state() {
        let s = state(100, 100, 900, 700);
        assert_eq!(s, clamp_to_visible(&s, SCREEN_LEFT, SCREEN_TOP, SCREEN_WIDTH, SCREEN_HEIGHT, 100));
    }

    #[test]
    fn resets_minimized_offscreen_coordinates() {
        // -32000 is the classic minimized-window coordinate (current bad state).
        let s = state(-32000, -32000, 237, 39);
        let r = clamp_to_visible(&s, SCREEN_LEFT, SCREEN_TOP, SCREEN_WIDTH, SCREEN_HEIGHT, 100);
        assert_eq!(100, r.x);
        assert_eq!(100, r.y);
        assert_eq!(DEFAULT_WIDTH, r.width);
        assert_eq!(DEFAULT_HEIGHT, r.height);
    }

    #[test]
    fn resets_fully_offscreen_right() {
        let s = state(2000, 100, 900, 700);
        let r = clamp_to_visible(&s, SCREEN_LEFT, SCREEN_TOP, SCREEN_WIDTH, SCREEN_HEIGHT, 100);
        assert_eq!(DEFAULT_WIDTH, r.width);
        assert_eq!(100, r.x);
    }

    #[test]
    fn resets_tiny_size() {
        let s = state(50, 50, 10, 10);
        let r = clamp_to_visible(&s, SCREEN_LEFT, SCREEN_TOP, SCREEN_WIDTH, SCREEN_HEIGHT, 100);
        assert_eq!(DEFAULT_WIDTH, r.width);
        assert_eq!(DEFAULT_HEIGHT, r.height);
    }

    #[test]
    fn keeps_window_spanning_two_screens() {
        // Dual screen: virtual 0-3840, window straddles the seam (overlap 800px).
        let s = state(1900, 100, 800, 600);
        assert_eq!(s, clamp_to_visible(&s, SCREEN_LEFT, SCREEN_TOP, 3840, SCREEN_HEIGHT, 100));
    }

    #[test]
    fn negative_virtual_origin_keeps_window_on_left_screen() {
        // Primary is the right screen (negative virtual origin); window sits on
        // the left screen (negative coordinate zone).
        let s = state(-1800, 100, 800, 600);
        assert_eq!(s, clamp_to_visible(&s, -1920, 0, 3840, 1080, 100));
    }

    #[test]
    fn zero_size_falls_back_to_defaults() {
        let s = state(100, 100, 0, 0);
        let r = clamp_to_visible(&s, SCREEN_LEFT, SCREEN_TOP, SCREEN_WIDTH, SCREEN_HEIGHT, 100);
        assert_eq!(DEFAULT_WIDTH, r.width);
        assert_eq!(DEFAULT_HEIGHT, r.height);
    }
}
