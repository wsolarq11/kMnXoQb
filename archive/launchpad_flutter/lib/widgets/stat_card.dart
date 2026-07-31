import 'package:flutter/material.dart';
import 'package:launchpad_flutter/theme.dart';

class StatCard extends StatelessWidget {
  final String label;
  final String value;
  const StatCard({super.key, required this.label, required this.value});

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;
    final colors = ThemeColors(isDark);
    return Expanded(
      child: GlassCard(
        isDark: isDark,
        borderRadius: BorderRadius.circular(12),
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(label, style: labelStyle(color: colors.textSecondary)),
            const SizedBox(height: 4),
            Text(value, style: statNumberStyle(color: colors.textPrimary)),
          ],
        ),
      ),
    );
  }
}
