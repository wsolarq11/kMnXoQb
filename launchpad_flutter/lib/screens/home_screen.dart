import 'package:flutter/material.dart';
import 'package:launchpad_flutter/src/rust/api/simple.dart';
import 'package:launchpad_flutter/src/rust/types.dart';
import 'package:launchpad_flutter/theme.dart';
import 'package:launchpad_flutter/widgets/edit_dialog.dart';
import 'package:launchpad_flutter/widgets/launch_card.dart';
import 'package:launchpad_flutter/widgets/stat_card.dart';
import 'package:lucide_icons/lucide_icons.dart';

class HomeScreen extends StatefulWidget {
  const HomeScreen({super.key});
  @override
  State<HomeScreen> createState() => _HomeScreenState();
}

class _HomeScreenState extends State<HomeScreen> {
  static const configDir = r'../config';

  late List<LaunchItem> _items;
  late AppSettings _settings;
  String _searchQuery = '';
  bool _isDark = false;
  String _statusText = '';

  @override
  void initState() {
    super.initState();
    _loadData();
  }

  void _loadData() {
    _items = readItems(configDir: configDir);
    _settings = readSettings(configDir: configDir);
    _isDark = _settings.theme == 'dark';
    setState(() {});
  }

  void _save() {
    writeItems(configDir: configDir, items: _items);
    writeSettings(configDir: configDir, settings: _settings);
  }

  AppSettings _copySettings({bool? confirmEnabled, String? theme, List<String>? launchHistory}) {
    return AppSettings(
      confirmEnabled: confirmEnabled ?? _settings.confirmEnabled,
      theme: theme ?? _settings.theme,
      launchHistory: launchHistory ?? _settings.launchHistory,
      windowState: _settings.windowState,
    );
  }

  void _toggleTheme() {
    _isDark = !_isDark;
    _settings = _copySettings(theme: _isDark ? 'dark' : 'light');
    _save();
    setState(() {});
  }

  void _toggleSelectAll() {
    final allSelected = _items.every((i) => i.selected) && _items.isNotEmpty;
    _items = _items.map((i) => _copyItem(i, selected: !allSelected)).toList();
    _save();
    setState(() {});
  }

  LaunchItem _copyItem(LaunchItem i, {bool? selected}) => LaunchItem(
        name: i.name,
        directory: i.directory,
        command: i.command,
        confirm: i.confirm,
        id: i.id,
        selected: selected ?? i.selected,
        terminal: i.terminal,
        tag: i.tag,
        group: i.group,
      );

  void _launchItem(LaunchItem item) {
    final needsConfirm = _settings.confirmEnabled &&
        (item.confirm || isDangerousCmd(command: item.command));
    if (needsConfirm) {
      _showLaunchConfirm(item);
      return;
    }
    _doLaunch(item);
  }

  void _doLaunch(LaunchItem item) {
    launchItem(item: item);
    _statusText = 'Launched: ${item.name}';
    _settings = _copySettings(
      launchHistory: [item.name, ..._settings.launchHistory].take(10).toList(),
    );
    _save();
    setState(() {});
  }

  void _showLaunchConfirm(LaunchItem item) {
    final colors = ThemeColors(_isDark);
    final dangerous = isDangerousCmd(command: item.command);
    final reason = dangerousReasonStr(command: item.command);
    showDialog(
      context: context,
      builder: (ctx) => AlertDialog(
        backgroundColor: colors.base,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(20),
          side: BorderSide(color: colors.border),
        ),
        title: Text('Confirm Launch', style: headingStyle(color: colors.textPrimary)),
        content: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text('Name: ${item.name}',
                style: headingStyle(color: colors.textPrimary).copyWith(fontSize: 14)),
            Text('Command: ${item.command}',
                style: codeStyle(color: colors.textTertiary)),
            const SizedBox(height: 4),
            Text('Directory: ${item.directory}',
                style: bodyStyle(fontSize: 12)),
            if (dangerous && reason != null)
              Padding(
                padding: const EdgeInsets.only(top: 8),
                child: Row(
                  children: [
                    Icon(LucideIcons.alertTriangle, size: 18, color: colors.danger),
                    const SizedBox(width: 6),
                    Expanded(
                        child: Text(reason,
                            style: bodyStyle(color: colors.danger))),
                  ],
                ),
              ),
          ],
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(ctx).pop(),
            child: Text('Cancel', style: bodyStyle(color: colors.textSecondary)),
          ),
          FilledButton(
            onPressed: () {
              Navigator.of(ctx).pop();
              _doLaunch(item);
            },
            style: FilledButton.styleFrom(backgroundColor: colors.accent),
            child: const Text('Launch'),
          ),
        ],
      ),
    );
  }

  void _batchLaunch() {
    final selected = _items.where((i) => i.selected).toList();
    for (final item in selected) {
      launchItem(item: item);
    }
    _statusText = 'Launched ${selected.length} items';
    setState(() {});
  }

  void _deleteItem(int index) {
    _items = [..._items]..removeAt(index);
    _save();
    setState(() => _statusText = 'Deleted');
  }

  void _showEditDialog({int? index}) {
    final isNew = index == null;
    final item = isNew
        ? LaunchItem(
            name: '', directory: '', command: '', confirm: true,
            id: '', selected: false)
        : _items[index];
    showDialog(
      context: context,
      builder: (ctx) => EditDialog(
        item: item,
        isNew: isNew,
        onSave: (edited) {
          if (isNew) {
            _items = [..._items, edited];
          } else {
            _items = [..._items]..[index] = edited;
          }
          _save();
          setState(() => _statusText = isNew ? 'Added' : 'Updated');
          Navigator.of(ctx).pop();
        },
        onDelete: isNew
            ? null
            : () {
                _deleteItem(index);
                Navigator.of(ctx).pop();
              },
      ),
    );
  }

  List<LaunchItem> get _filtered {
    if (_searchQuery.isEmpty) return _items;
    final q = _searchQuery.toLowerCase();
    return _items.where((i) =>
        i.name.toLowerCase().contains(q) ||
        i.directory.toLowerCase().contains(q) ||
        i.command.toLowerCase().contains(q)).toList();
  }

  @override
  Widget build(BuildContext context) {
    final colors = ThemeColors(_isDark);
    final filtered = _filtered;
    final selectedCount = _items.where((i) => i.selected).length;

    return Theme(
      data: buildAppTheme(isDark: _isDark),
      child: Scaffold(
      backgroundColor: colors.base,
      appBar: AppBar(
        backgroundColor: colors.base,
        elevation: 0,
        title: Text('WT Launcher', style: headingStyle(color: colors.textPrimary)),
        actions: [
          FilterChip(
            label: Text('Confirm', style: labelStyle(color: colors.textSecondary)),
            selected: _settings.confirmEnabled,
            onSelected: (v) {
              _settings = _copySettings(confirmEnabled: v);
              _save();
              setState(() {});
            },
            backgroundColor: colors.surface,
            selectedColor: colors.accent.withAlpha(30),
            checkmarkColor: colors.accent,
            side: BorderSide.none,
          ),
          const SizedBox(width: 8),
          IconButton(
            icon: Icon(_isDark ? LucideIcons.moon : LucideIcons.sun, size: 18),
            onPressed: _toggleTheme,
            tooltip: 'Toggle theme',
            color: colors.textSecondary,
          ),
          const SizedBox(width: 4),
          FilledButton.icon(
            icon: const Icon(LucideIcons.plus, size: 16),
            label: const Text('New'),
            onPressed: () => _showEditDialog(),
            style: FilledButton.styleFrom(backgroundColor: colors.accent),
          ),
          const SizedBox(width: 12),
        ],
      ),
      body: Column(
        children: [
          // Stats
          Padding(
            padding: const EdgeInsets.all(12),
            child: Row(
              children: [
                StatCard(label: 'ITEMS', value: '${_items.length}'),
                const SizedBox(width: 12),
                StatCard(
                  label: 'RECENT',
                  value: _settings.launchHistory.isNotEmpty
                      ? _settings.launchHistory.first
                      : '--',
                ),
              ],
            ),
          ),
          // Search + Batch
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 12),
            child: Row(
              children: [
                Expanded(
                  child: TextField(
                    style: bodyStyle(color: colors.textPrimary),
                    decoration: InputDecoration(
                      hintText: 'Search...',
                      hintStyle: bodyStyle(color: colors.textTertiary),
                      prefixIcon: Icon(LucideIcons.search, size: 18, color: colors.textSecondary),
                      border: OutlineInputBorder(
                        borderRadius: BorderRadius.circular(12),
                        borderSide: BorderSide(color: colors.border),
                      ),
                      enabledBorder: OutlineInputBorder(
                        borderRadius: BorderRadius.circular(12),
                        borderSide: BorderSide(color: colors.border),
                      ),
                      focusedBorder: OutlineInputBorder(
                        borderRadius: BorderRadius.circular(12),
                        borderSide: BorderSide(color: colors.accent),
                      ),
                      filled: true,
                      fillColor: colors.surface,
                      isDense: true,
                    ),
                    onChanged: (v) => setState(() => _searchQuery = v),
                  ),
                ),
                const SizedBox(width: 12),
                TextButton(
                  onPressed: _toggleSelectAll,
                  child: Text('Select All', style: labelStyle(color: colors.textSecondary)),
                ),
                Text('Selected: $selectedCount', style: bodyStyle(color: colors.textSecondary)),
                const SizedBox(width: 12),
                FilledButton(
                  onPressed: _batchLaunch,
                  style: FilledButton.styleFrom(backgroundColor: colors.accent),
                  child: const Text('Launch Selected'),
                ),
              ],
            ),
          ),
          const SizedBox(height: 8),
          // Card Grid
          Expanded(
            child: filtered.isEmpty
                ? _EmptyState(onAdd: () => _showEditDialog())
                : GridView.builder(
                    physics: const BouncingScrollPhysics(),
                    padding: const EdgeInsets.all(12),
                    gridDelegate: const SliverGridDelegateWithMaxCrossAxisExtent(
                      maxCrossAxisExtent: 340,
                      childAspectRatio: 2.5,
                      crossAxisSpacing: 8,
                      mainAxisSpacing: 8,
                    ),
                    itemCount: filtered.length,
                    itemBuilder: (_, i) {
                      final idx = _items.indexOf(filtered[i]);
                      return LaunchCard(
                        item: filtered[i],
                        index: idx,
                        totalItems: _items.length,
                        onLaunch: _launchItem,
                        onEdit: () => _showEditDialog(index: idx),
                        onDelete: () => _deleteItem(idx),
                        onToggleSelect: () {
                          _items[idx] = _copyItem(_items[idx],
                              selected: !_items[idx].selected);
                          _save();
                          setState(() {});
                        },
                        onMoveUp: () {
                          if (idx > 0) {
                            final tmp = _items[idx - 1];
                            _items[idx - 1] = _items[idx];
                            _items[idx] = tmp;
                            _save();
                            setState(() {});
                          }
                        },
                        onMoveDown: () {
                          if (idx < _items.length - 1) {
                            final tmp = _items[idx + 1];
                            _items[idx + 1] = _items[idx];
                            _items[idx] = tmp;
                            _save();
                            setState(() {});
                          }
                        },
                      );
                    },
                  ),
          ),
          // Status bar
          if (_statusText.isNotEmpty)
            Container(
              width: double.infinity,
              padding: const EdgeInsets.all(8),
              color: colors.surface,
              child: Text(_statusText,
                  style: codeStyle(color: colors.textSecondary)),
            ),
        ],
      ),
      ),
    );
  }
}

class _EmptyState extends StatefulWidget {
  final VoidCallback onAdd;
  const _EmptyState({required this.onAdd});

  @override
  State<_EmptyState> createState() => _EmptyStateState();
}

class _EmptyStateState extends State<_EmptyState>
    with SingleTickerProviderStateMixin {
  late final AnimationController _ctrl;

  @override
  void initState() {
    super.initState();
    _ctrl = AnimationController(
      vsync: this,
      duration: const Duration(seconds: 3),
    )..repeat(reverse: true);
  }

  @override
  void dispose() {
    _ctrl.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;
    final colors = ThemeColors(isDark);
    return Center(
      child: FadeTransition(
        opacity: Tween<double>(begin: 0.4, end: 0.8).animate(_ctrl),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(LucideIcons.zap, size: 48,
                color: colors.accent.withAlpha(isDark ? 100 : 60)),
            const SizedBox(height: 16),
            Text('No items yet',
                style: headingStyle(color: colors.textSecondary)),
            const SizedBox(height: 8),
            Text('Click + New to add your first launch item',
                style: bodyStyle(color: colors.textTertiary)),
            const SizedBox(height: 24),
            FilledButton.icon(
              icon: const Icon(LucideIcons.plus, size: 16),
              label: const Text('New Item'),
              onPressed: widget.onAdd,
              style: FilledButton.styleFrom(backgroundColor: colors.accent),
            ),
          ],
        ),
      ),
    );
  }
}
