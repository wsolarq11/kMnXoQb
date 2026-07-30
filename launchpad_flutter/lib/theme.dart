import 'dart:ui';
import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';

// ── Color Tokens ──

class AppColors {
  // Shared
  static const accent = Color(0xFF6366F1); // Indigo 500
  static const danger = Color(0xFFEF4444);
  static const success = Color(0xFF22C55E);
  static const neonGlow = Color(0xFFA78BFA);

  // Dark
  static const darkBase = Color(0xFF0A0A0A);
  static const darkSurface = Color(0x0DFFFFFF);
  static const darkBorder = Color(0x14FFFFFF);
  static const darkTextPrimary = Color(0xFFF8FAFC);
  static const darkTextSecondary = Color(0xFF94A3B8);
  static const darkTextTertiary = Color(0xFF475569);
  static const darkDangerBg = Color(0x20EF4444);

  // Light
  static const lightBase = Color(0xFFF8FAFC);
  static const lightSurface = Color(0xFFFFFFFF);
  static const lightBorder = Color(0x14000000);
  static const lightTextPrimary = Color(0xFF0F172A);
  static const lightTextSecondary = Color(0xFF64748B);
  static const lightTextTertiary = Color(0xFF94A3B8);
  static const lightDangerBg = Color(0x0DEF4444);
}

/// Theme-aware color accessor.
class ThemeColors {
  final bool isDark;
  const ThemeColors(this.isDark);

  Color get base => isDark ? AppColors.darkBase : AppColors.lightBase;
  Color get surface => isDark ? AppColors.darkSurface : AppColors.lightSurface;
  Color get border => isDark ? AppColors.darkBorder : AppColors.lightBorder;
  Color get textPrimary => isDark ? AppColors.darkTextPrimary : AppColors.lightTextPrimary;
  Color get textSecondary => isDark ? AppColors.darkTextSecondary : AppColors.lightTextSecondary;
  Color get textTertiary => isDark ? AppColors.darkTextTertiary : AppColors.lightTextTertiary;
  Color get dangerBg => isDark ? AppColors.darkDangerBg : AppColors.lightDangerBg;

  Color get accent => AppColors.accent;
  Color get danger => AppColors.danger;
  Color get success => AppColors.success;
  Color get neonGlow => AppColors.neonGlow;
}

// ── Typography ──

TextStyle headingStyle({Color? color}) => GoogleFonts.inter(
      fontSize: 16,
      fontWeight: FontWeight.w700,
      letterSpacing: -0.3,
      color: color,
    );

TextStyle bodyStyle({Color? color, double? fontSize}) => GoogleFonts.inter(
      fontSize: fontSize ?? 13,
      fontWeight: FontWeight.w400,
      color: color,
    );

TextStyle codeStyle({Color? color}) => GoogleFonts.jetBrainsMono(
      fontSize: 12,
      height: 1.5,
      color: color,
    );

TextStyle statNumberStyle({Color? color}) => GoogleFonts.inter(
      fontSize: 36,
      fontWeight: FontWeight.w900,
      letterSpacing: -1,
      color: color,
    );

TextStyle labelStyle({Color? color}) => GoogleFonts.inter(
      fontSize: 11,
      fontWeight: FontWeight.w600,
      letterSpacing: 1.0,
      color: color,
    );

// ── Spring Curves ──

const springHover = SpringDescription(mass: 1, stiffness: 200, damping: 15);
const springDialog = SpringDescription(mass: 0.8, stiffness: 300, damping: 20);
const springScroll = SpringDescription(mass: 0.5, stiffness: 100, damping: 12);

// ── Glass Card ──

class GlassCard extends StatelessWidget {
  final Widget child;
  final BorderSide? borderSide;
  final Color? fillColor;
  final EdgeInsetsGeometry? padding;
  final double blurSigma;
  final BorderRadiusGeometry? borderRadius;
  final bool isDark;

  const GlassCard({
    super.key,
    required this.child,
    this.borderSide,
    this.fillColor,
    this.padding,
    this.blurSigma = 12,
    this.borderRadius,
    this.isDark = true,
  });

  @override
  Widget build(BuildContext context) {
    final colors = ThemeColors(isDark);
    return ClipRRect(
      borderRadius: borderRadius ?? BorderRadius.circular(16),
      child: BackdropFilter(
        filter: ImageFilter.blur(sigmaX: blurSigma, sigmaY: blurSigma),
        child: Container(
          padding: padding,
          decoration: BoxDecoration(
            color: fillColor ?? colors.surface,
            border: Border.all(
              color: borderSide?.color ?? colors.border,
              width: borderSide?.width ?? 1,
            ),
            borderRadius: borderRadius ?? BorderRadius.circular(16),
          ),
          child: child,
        ),
      ),
    );
  }
}

// ── App Theme ──

ThemeData buildAppTheme({required bool isDark}) {
  final colors = ThemeColors(isDark);

  return ThemeData(
    brightness: isDark ? Brightness.dark : Brightness.light,
    scaffoldBackgroundColor: colors.base,
    colorScheme: ColorScheme.fromSeed(
      seedColor: colors.accent,
      brightness: isDark ? Brightness.dark : Brightness.light,
    ),
    appBarTheme: AppBarTheme(
      backgroundColor: colors.base,
      elevation: 0,
      titleTextStyle: GoogleFonts.inter(
        fontSize: 18,
        fontWeight: FontWeight.w700,
        letterSpacing: -0.5,
        color: colors.textPrimary,
      ),
    ),
    cardTheme: CardThemeData(
      color: colors.surface,
      elevation: 0,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(16),
        side: BorderSide(color: colors.border),
      ),
    ),
    textTheme: TextTheme(
      bodyLarge: bodyStyle(color: colors.textPrimary),
      bodyMedium: bodyStyle(color: colors.textSecondary),
    ),
    useMaterial3: true,
  );
}
