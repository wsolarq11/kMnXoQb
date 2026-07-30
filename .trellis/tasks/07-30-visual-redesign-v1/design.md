# Design: Visual System

## Color Palette

| Token | Light | Dark |
|-------|-------|------|
| Base | #FAFAFA | #0A0A0A |
| Surface | rgba(255,255,255,0.05) + blur(20px) | same |
| Border | rgba(255,255,255,0.08) | rgba(255,255,255,0.06) |
| Accent | #6366F1 (Indigo 500) | #818CF8 (Indigo 400) |
| Danger | #EF4444 | #F87171 |
| Success | #22C55E | #4ADE80 |
| Text Primary | #0F172A | #F8FAFC |
| Text Secondary | #64748B | #94A3B8 |
| Text Tertiary | #CBD5E1 | #475569 |
| Neon Glow | #A78BFA (Violet 400) | same |

## Typography

```dart
// Headings
GoogleFonts.inter(fontWeight: FontWeight.w700, letterSpacing: -0.5)

// Body
GoogleFonts.inter(fontWeight: FontWeight.w400)

// Code / Paths
GoogleFonts.jetBrainsMono(fontSize: 12, height: 1.5)

// Stats (oversized)
GoogleFonts.inter(fontSize: 36, fontWeight: FontWeight.w900, letterSpacing: -1)
```

## Animation Curves

```dart
// Spring (card hover)
SpringDescription(mass: 1, stiffness: 200, damping: 15)

// Spring (dialog pop)
SpringDescription(mass: 0.8, stiffness: 300, damping: 20)

// Spring (scroll)
SpringDescription(mass: 0.5, stiffness: 100, damping: 12)
```

## Glass Card Architecture

```dart
ClipRRect(
  child: BackdropFilter(
    filter: ImageFilter.blur(sigmaX: 12, sigmaY: 12),
    child: Container(
      decoration: BoxDecoration(
        color: surfaceColor,
        border: Border.all(color: borderColor),
        borderRadius: BorderRadius.circular(16),
      ),
    ),
  ),
)
```

## File Structure (post-split)

```
lib/
  main.dart              ← MaterialApp + theme
  theme.dart             ← colors, typography, spring curves
  screens/
    home_screen.dart     ← HomeScreen stateful widget
  widgets/
    launch_card.dart     ← _LaunchCard + _HoverCard
    edit_dialog.dart     ← EditDialog + directory picker
    stat_card.dart       ← _StatCard
    search_bar.dart      ← search + batch bar
```

## Lucide Icon Mapping

| Material | Lucide |
|----------|--------|
| Icons.edit | Lucide.Pencil |
| Icons.delete | Lucide.Trash2 |
| Icons.arrow_upward | Lucide.ChevronUp |
| Icons.arrow_downward | Lucide.ChevronDown |
| Icons.add | Lucide.Plus |
| Icons.search | Lucide.Search |
| Icons.folder_open | Lucide.FolderOpen |
| Icons.light_mode | Lucide.Sun |
| Icons.dark_mode | Lucide.Moon |
| Icons.warning | Lucide.TriangleAlert |
| Icons.check_circle | Lucide.CircleCheck |
| Icons.warning_amber | Lucide.AlertTriangle |
