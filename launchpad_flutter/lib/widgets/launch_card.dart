import 'package:flutter/material.dart';
import 'package:launchpad_flutter/src/rust/api/simple.dart';
import 'package:launchpad_flutter/src/rust/types.dart';
import 'package:launchpad_flutter/theme.dart';
import 'package:lucide_icons/lucide_icons.dart';

class HoverCard extends StatefulWidget {
  final Widget child;
  final VoidCallback onTap;
  const HoverCard({super.key, required this.child, required this.onTap});

  @override
  State<HoverCard> createState() => _HoverCardState();
}

class _HoverCardState extends State<HoverCard>
    with SingleTickerProviderStateMixin {
  late final AnimationController _ctrl;
  late final Animation<double> _scale;

  @override
  void initState() {
    super.initState();
    _ctrl = AnimationController(
      vsync: this,
      duration: const Duration(milliseconds: 400),
    );
    _scale = Tween<double>(begin: 1.0, end: 1.03).animate(
      CurvedAnimation(parent: _ctrl, curve: Curves.elasticOut),
    );
    _ctrl.value = 1.0;
  }

  @override
  void dispose() {
    _ctrl.dispose();
    super.dispose();
  }

  void _enter() {
    _ctrl.forward();
  }

  void _exit() {
    _ctrl.reverse();
  }

  @override
  Widget build(BuildContext context) {
    return MouseRegion(
      onEnter: (_) => _enter(),
      onExit: (_) => _exit(),
      child: GestureDetector(
        onTap: widget.onTap,
        child: ScaleTransition(scale: _scale, child: widget.child),
      ),
    );
  }
}

class LaunchCard extends StatelessWidget {
  final LaunchItem item;
  final int index;
  final int totalItems;
  final void Function(LaunchItem) onLaunch;
  final VoidCallback onEdit;
  final VoidCallback onDelete;
  final VoidCallback onToggleSelect;
  final VoidCallback onMoveUp;
  final VoidCallback onMoveDown;

  const LaunchCard({
    super.key,
    required this.item,
    required this.index,
    required this.totalItems,
    required this.onLaunch,
    required this.onEdit,
    required this.onDelete,
    required this.onToggleSelect,
    required this.onMoveUp,
    required this.onMoveDown,
  });

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;
    final colors = ThemeColors(isDark);
    final dangerous = isDangerousCmd(command: item.command);
    final dangerReason = dangerousReasonStr(command: item.command);

    return GlassCard(
      isDark: isDark,
      borderSide: BorderSide(
        color: item.selected ? colors.accent : colors.border,
        width: item.selected ? 2 : 1,
      ),
      fillColor: dangerous ? colors.dangerBg : null,
      child: HoverCard(
        onTap: () => onLaunch(item),
        child: Padding(
          padding: const EdgeInsets.all(10),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                children: [
                  Expanded(
                    child: Text(item.name,
                        style: headingStyle(color: colors.textPrimary),
                        overflow: TextOverflow.ellipsis),
                  ),
                  _ActionIcon(icon: LucideIcons.pencil, onTap: onEdit, tooltip: 'Edit', color: colors.textSecondary),
                  _ActionIcon(icon: LucideIcons.trash2, onTap: onDelete, tooltip: 'Delete', color: colors.textSecondary),
                  _ActionIcon(
                    icon: LucideIcons.chevronUp,
                    onTap: index > 0 ? onMoveUp : null,
                    tooltip: 'Move Up',
                    color: colors.textSecondary,
                  ),
                  _ActionIcon(
                    icon: LucideIcons.chevronDown,
                    onTap: index < totalItems - 1 ? onMoveDown : null,
                    tooltip: 'Move Down',
                    color: colors.textSecondary,
                  ),
                  SizedBox(
                    width: 24,
                    height: 24,
                    child: Checkbox(
                      value: item.selected,
                      onChanged: (_) => onToggleSelect(),
                      visualDensity: VisualDensity.compact,
                      side: BorderSide(color: colors.textSecondary, width: 1.5),
                    ),
                  ),
                ],
              ),
              Text(item.directory,
                  style: codeStyle(color: colors.textTertiary),
                  maxLines: 1, overflow: TextOverflow.ellipsis),
              Text(item.command,
                  style: codeStyle(color: colors.textSecondary),
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis),
              Wrap(
                spacing: 4,
                runSpacing: 2,
                children: [
                  if (item.tag != null)
                    _Tag(text: '#${item.tag}', color: colors.accent),
                  if (item.group != null)
                    _Tag(text: '@${item.group}', color: colors.success),
                  if (dangerous && dangerReason != null)
                    Text(dangerReason,
                        style: bodyStyle(color: colors.danger)
                            .copyWith(fontSize: 10)),
                ],
              ),
            ],
          ),
        ),
      ),
    );
  }
}

class _ActionIcon extends StatelessWidget {
  final IconData icon;
  final VoidCallback? onTap;
  final String tooltip;
  final Color color;
  const _ActionIcon({required this.icon, this.onTap, required this.tooltip, required this.color});

  @override
  Widget build(BuildContext context) {
    return IconButton(
      icon: Icon(icon, size: 14),
      onPressed: onTap,
      tooltip: tooltip,
      visualDensity: VisualDensity.compact,
      color: color,
    );
  }
}

class _Tag extends StatelessWidget {
  final String text;
  final Color color;
  const _Tag({required this.text, required this.color});

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 2),
      decoration: BoxDecoration(
        color: color.withAlpha(30),
        borderRadius: BorderRadius.circular(4),
      ),
      child: Text(text,
          style: TextStyle(fontSize: 10, color: color)),
    );
  }
}
