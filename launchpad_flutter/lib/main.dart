import 'dart:io';
import 'package:flutter/material.dart';
import 'package:launchpad_flutter/src/rust/api/simple.dart';
import 'package:launchpad_flutter/src/rust/frb_generated.dart';
import 'package:launchpad_flutter/src/rust/types.dart';

Future<void> main() async {
  await RustLib.init();
  runApp(const MyApp());
}

class MyApp extends StatelessWidget {
  const MyApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'WT Launcher',
      theme: ThemeData(
        colorScheme: ColorScheme.fromSeed(
          seedColor: Colors.blueGrey,
          brightness: Brightness.light,
        ),
        useMaterial3: true,
      ),
      darkTheme: ThemeData(
        colorScheme: ColorScheme.fromSeed(
          seedColor: Colors.blueGrey,
          brightness: Brightness.dark,
        ),
        useMaterial3: true,
      ),
      home: const HomeScreen(),
    );
  }
}

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

  void _toggleTheme() {
    _isDark = !_isDark;
    _settings = AppSettings(
      confirmEnabled: _settings.confirmEnabled,
      theme: _isDark ? 'dark' : 'light',
      launchHistory: _settings.launchHistory,
      windowState: _settings.windowState,
    );
    _save();
    setState(() {});
  }

  void _toggleSelectAll() {
    final allSelected =
        _items.every((i) => i.selected) && _items.isNotEmpty;
    _items = _items
        .map((i) => LaunchItem(
              name: i.name,
              directory: i.directory,
              command: i.command,
              confirm: i.confirm,
              id: i.id,
              selected: !allSelected,
              terminal: i.terminal,
              tag: i.tag,
              group: i.group,
            ))
        .toList();
    _save();
    setState(() {});
  }

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
    _settings = AppSettings(
      confirmEnabled: _settings.confirmEnabled,
      theme: _settings.theme,
      launchHistory: [item.name, ..._settings.launchHistory].take(10).toList(),
      windowState: _settings.windowState,
    );
    _save();
    setState(() {});
  }

  void _showLaunchConfirm(LaunchItem item) {
    final dangerous = isDangerousCmd(command: item.command);
    final reason = dangerousReasonStr(command: item.command);
    showDialog(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text('Confirm Launch'),
        content: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text('Name: ${item.name}', style: const TextStyle(fontWeight: FontWeight.bold)),
            const SizedBox(height: 4),
            Text('Command: ${item.command}',
                style: const TextStyle(fontFamily: 'monospace', fontSize: 13)),
            const SizedBox(height: 4),
            Text('Directory: ${item.directory}',
                style: TextStyle(fontSize: 12, color: Colors.grey[600])),
            if (dangerous && reason != null)
              Padding(
                padding: const EdgeInsets.only(top: 8),
                child: Row(
                  children: [
                    Icon(Icons.warning, size: 18, color: Colors.red[400]),
                    const SizedBox(width: 6),
                    Expanded(
                      child: Text(reason, style: TextStyle(color: Colors.red[400], fontSize: 12)),
                    ),
                  ],
                ),
              ),
          ],
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(ctx).pop(),
            child: const Text('Cancel'),
          ),
          FilledButton(
            onPressed: () {
              Navigator.of(ctx).pop();
              _doLaunch(item);
            },
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
    setState(() { _statusText = 'Deleted'; });
  }

  void _showEditDialog({int? index}) {
    final isNew = index == null;
    final item = isNew
        ? LaunchItem(
            name: '',
            directory: '',
            command: '',
            confirm: true,
            id: '',
            selected: false,
          )
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
          setState(() { _statusText = isNew ? 'Added' : 'Updated'; });
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
    return _items.where((i) {
      return i.name.toLowerCase().contains(q) ||
          i.directory.toLowerCase().contains(q) ||
          i.command.toLowerCase().contains(q);
    }).toList();
  }

  @override
  Widget build(BuildContext context) {
    final filtered = _filtered;

    return MaterialApp(
      themeMode: _isDark ? ThemeMode.dark : ThemeMode.light,
      theme: ThemeData(
        colorScheme: ColorScheme.fromSeed(
          seedColor: Colors.blueGrey,
          brightness: Brightness.light,
        ),
        useMaterial3: true,
      ),
      darkTheme: ThemeData(
        colorScheme: ColorScheme.fromSeed(
          seedColor: Colors.blueGrey,
          brightness: Brightness.dark,
        ),
        useMaterial3: true,
      ),
      home: Scaffold(
        appBar: AppBar(
          title: const Text('WT Launcher'),
          actions: [
            Text('Theme: ${_isDark ? "Dark" : "Light"}'),
            IconButton(
              icon: Icon(_isDark ? Icons.light_mode : Icons.dark_mode),
              onPressed: _toggleTheme,
              tooltip: 'Toggle theme',
            ),
            const SizedBox(width: 4),
            FilterChip(
              label: const Text('Confirm'),
              selected: _settings.confirmEnabled,
              onSelected: (v) {
                _settings = AppSettings(
                  confirmEnabled: v,
                  theme: _settings.theme,
                  launchHistory: _settings.launchHistory,
                  windowState: _settings.windowState,
                );
                _save();
                setState(() {});
              },
            ),
            const SizedBox(width: 8),
            ElevatedButton.icon(
              icon: const Icon(Icons.add),
              label: const Text('New'),
              onPressed: () => _showEditDialog(),
            ),
            const SizedBox(width: 8),
          ],
        ),
        body: Column(
          children: [
            // Stats
            Padding(
              padding: const EdgeInsets.all(12.0),
              child: Row(
                children: [
                  _StatCard(label: 'ITEMS', value: '${_items.length}'),
                  const SizedBox(width: 12),
                  _StatCard(
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
              padding: const EdgeInsets.symmetric(horizontal: 12.0),
              child: Row(
                children: [
                  Expanded(
                    child: TextField(
                      decoration: const InputDecoration(
                        hintText: 'Search...',
                        prefixIcon: Icon(Icons.search),
                        border: OutlineInputBorder(),
                        isDense: true,
                      ),
                      onChanged: (v) => setState(() => _searchQuery = v),
                    ),
                  ),
                  const SizedBox(width: 12),
                  TextButton(
                    onPressed: _toggleSelectAll,
                    child: const Text('Select All'),
                  ),
                  Text(
                    'Selected: ${_items.where((i) => i.selected).length}',
                  ),
                  const SizedBox(width: 12),
                  FilledButton(
                    onPressed: _batchLaunch,
                    child: const Text('Launch Selected'),
                  ),
                ],
              ),
            ),
            const SizedBox(height: 8),
            // Card grid
            Expanded(
              child: filtered.isEmpty
                  ? const Center(
                      child: Text(
                        'No items. Click + New to add one.',
                        style: TextStyle(color: Colors.grey),
                      ),
                    )
                  : GridView.builder(
                      padding: const EdgeInsets.all(12),
                      gridDelegate:
                          const SliverGridDelegateWithMaxCrossAxisExtent(
                        maxCrossAxisExtent: 340,
                        childAspectRatio: 2.5,
                        crossAxisSpacing: 8,
                        mainAxisSpacing: 8,
                      ),
                      itemCount: filtered.length,
                      itemBuilder: (_, i) =>
                          _LaunchCard(
                            item: filtered[i],
                            index: _items.indexOf(filtered[i]),
                            onLaunch: _launchItem,
                            onEdit: () =>
                                _showEditDialog(index: _items.indexOf(filtered[i])),
                            onDelete: () =>
                                _deleteItem(_items.indexOf(filtered[i])),
                            onToggleSelect: () {
                              final idx = _items.indexOf(filtered[i]);
                              final old = _items[idx];
                              _items[idx] = LaunchItem(
                                name: old.name,
                                directory: old.directory,
                                command: old.command,
                                confirm: old.confirm,
                                id: old.id,
                                selected: !old.selected,
                                terminal: old.terminal,
                                tag: old.tag,
                                group: old.group,
                              );
                              _save();
                              setState(() {});
                            },
                            onMoveUp: () {
                              final idx = _items.indexOf(filtered[i]);
                              if (idx > 0) {
                                final tmp = _items[idx - 1];
                                _items[idx - 1] = _items[idx];
                                _items[idx] = tmp;
                                _save();
                                setState(() {});
                              }
                            },
                            onMoveDown: () {
                              final idx = _items.indexOf(filtered[i]);
                              if (idx < _items.length - 1) {
                                final tmp = _items[idx + 1];
                                _items[idx + 1] = _items[idx];
                                _items[idx] = tmp;
                                _save();
                                setState(() {});
                              }
                            },
                          ),
                    ),
            ),
            // Status bar
            if (_statusText.isNotEmpty)
              Container(
                width: double.infinity,
                padding: const EdgeInsets.all(8),
                color: Theme.of(context).colorScheme.surfaceContainerHighest,
                child: Text(_statusText),
              ),
          ],
        ),
      ),
    );
  }
}

class _StatCard extends StatelessWidget {
  final String label;
  final String value;
  const _StatCard({required this.label, required this.value});

  @override
  Widget build(BuildContext context) {
    return Expanded(
      child: Card(
        child: Padding(
          padding: const EdgeInsets.all(16),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(label,
                  style: TextStyle(
                    fontSize: 12,
                    color: Colors.grey[600],
                    letterSpacing: 1,
                  )),
              const SizedBox(height: 4),
              Text(value,
                  style: const TextStyle(
                    fontSize: 28,
                    fontWeight: FontWeight.bold,
                  )),
            ],
          ),
        ),
      ),
    );
  }
}

class _LaunchCard extends StatelessWidget {
  final LaunchItem item;
  final int index;
  final void Function(LaunchItem) onLaunch;
  final VoidCallback onEdit;
  final VoidCallback onDelete;
  final VoidCallback onToggleSelect;
  final VoidCallback onMoveUp;
  final VoidCallback onMoveDown;

  const _LaunchCard({
    required this.item,
    required this.index,
    required this.onLaunch,
    required this.onEdit,
    required this.onDelete,
    required this.onToggleSelect,
    required this.onMoveUp,
    required this.onMoveDown,
  });

  @override
  Widget build(BuildContext context) {
    final dangerous = isDangerousCmd(command: item.command);
    final dangerReason = dangerousReasonStr(command: item.command);

    return Card(
      color: dangerous
          ? Colors.red.shade900.withValues(alpha: 0.3)
          : Theme.of(context).colorScheme.surface,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(12),
        side: BorderSide(
          color: item.selected
              ? Theme.of(context).colorScheme.primary
              : Theme.of(context).dividerColor,
          width: item.selected ? 2 : 1,
        ),
      ),
      child: _HoverCard(
        onTap: () => onLaunch(item),
        child: Padding(
          padding: const EdgeInsets.all(8),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              // Row 1: Name + action buttons
              Row(
                children: [
                  Expanded(
                    child: Text(
                      item.name,
                      style: const TextStyle(
                        fontSize: 15,
                        fontWeight: FontWeight.w600,
                      ),
                      overflow: TextOverflow.ellipsis,
                    ),
                  ),
                  IconButton(
                    icon: const Icon(Icons.edit, size: 16),
                    onPressed: onEdit,
                    tooltip: 'Edit',
                    visualDensity: VisualDensity.compact,
                  ),
                  IconButton(
                    icon: const Icon(Icons.delete, size: 16),
                    onPressed: onDelete,
                    tooltip: 'Delete',
                    visualDensity: VisualDensity.compact,
                  ),
                  IconButton(
                    icon: const Icon(Icons.arrow_upward, size: 16),
                    onPressed: index > 0 ? onMoveUp : null,
                    tooltip: 'Move Up',
                    visualDensity: VisualDensity.compact,
                  ),
                  IconButton(
                    icon: const Icon(Icons.arrow_downward, size: 16),
                    onPressed: onMoveDown,
                    tooltip: 'Move Down',
                    visualDensity: VisualDensity.compact,
                  ),
                  Checkbox(
                    value: item.selected,
                    onChanged: (_) => onToggleSelect(),
                    visualDensity: VisualDensity.compact,
                  ),
                ],
              ),
              // Row 2: Directory
              Text(
                item.directory,
                style: TextStyle(
                  fontSize: 12,
                  color: Colors.grey[500],
                  fontFamily: 'monospace',
                ),
                maxLines: 1,
                overflow: TextOverflow.ellipsis,
              ),
              // Row 3: Command
              Text(
                item.command,
                style: TextStyle(
                  fontSize: 12,
                  color: Colors.grey[700],
                  fontFamily: 'monospace',
                ),
                maxLines: 1,
                overflow: TextOverflow.ellipsis,
              ),
              // Row 4: Tags + danger
              Row(
                children: [
                  if (item.tag != null)
                    Container(
                      margin: const EdgeInsets.only(right: 4),
                      padding: const EdgeInsets.symmetric(
                          horizontal: 6, vertical: 2),
                      decoration: BoxDecoration(
                        color: Colors.blue.shade100,
                        borderRadius: BorderRadius.circular(4),
                      ),
                      child: Text('#${item.tag}',
                          style: const TextStyle(fontSize: 10)),
                    ),
                  if (item.group != null)
                    Container(
                      margin: const EdgeInsets.only(right: 4),
                      padding: const EdgeInsets.symmetric(
                          horizontal: 6, vertical: 2),
                      decoration: BoxDecoration(
                        color: Colors.green.shade100,
                        borderRadius: BorderRadius.circular(4),
                      ),
                      child: Text('@${item.group}',
                          style: const TextStyle(fontSize: 10)),
                    ),
                  if (dangerous && dangerReason != null)
                    Expanded(
                      child: Text(
                        dangerReason,
                        style: TextStyle(
                          fontSize: 10,
                          color: Colors.red[400],
                          fontWeight: FontWeight.bold,
                        ),
                        overflow: TextOverflow.ellipsis,
                      ),
                    ),
                ],
              ),
            ],
          ),
        ),
      ),
    );
  }
}

/// Card wrapper with hover scale animation.
class _HoverCard extends StatefulWidget {
  final Widget child;
  final VoidCallback onTap;
  const _HoverCard({required this.child, required this.onTap});

  @override
  State<_HoverCard> createState() => _HoverCardState();
}

class _HoverCardState extends State<_HoverCard> {
  bool _hovered = false;

  @override
  Widget build(BuildContext context) {
    return MouseRegion(
      onEnter: (_) => setState(() => _hovered = true),
      onExit: (_) => setState(() => _hovered = false),
      child: GestureDetector(
        onTap: widget.onTap,
        child: AnimatedScale(
          scale: _hovered ? 1.03 : 1.0,
          duration: const Duration(milliseconds: 150),
          child: widget.child,
        ),
      ),
    );
  }
}

class EditDialog extends StatefulWidget {
  final LaunchItem item;
  final bool isNew;
  final void Function(LaunchItem) onSave;
  final VoidCallback? onDelete;

  const EditDialog({
    super.key,
    required this.item,
    required this.isNew,
    required this.onSave,
    this.onDelete,
  });

  @override
  State<EditDialog> createState() => _EditDialogState();
}

class _EditDialogState extends State<EditDialog> {
  late TextEditingController _nameCtrl;
  late TextEditingController _dirCtrl;
  late TextEditingController _cmdCtrl;
  late TextEditingController _termCtrl;
  late bool _confirm;
  String? _nameError;
  String? _cmdError;

  @override
  void initState() {
    super.initState();
    _nameCtrl = TextEditingController(text: widget.item.name);
    _dirCtrl = TextEditingController(text: widget.item.directory);
    _cmdCtrl = TextEditingController(text: widget.item.command);
    _termCtrl = TextEditingController(text: widget.item.terminal ?? '');
    _confirm = widget.item.confirm;
  }

  @override
  void dispose() {
    _nameCtrl.dispose();
    _dirCtrl.dispose();
    _cmdCtrl.dispose();
    _termCtrl.dispose();
    super.dispose();
  }

  Future<void> _pickDirectory() async {
    final startDir = _dirCtrl.text.isNotEmpty
        ? _dirCtrl.text
        : Directory.current.path;
    final picked = await showDialog<String>(
      context: context,
      builder: (ctx) => _DirectoryPickerDialog(startDir: startDir),
    );
    if (picked != null) {
      _dirCtrl.text = picked;
      setState(() {});
    }
  }

  Widget _dirValidation() {
    final dir = Directory(_dirCtrl.text);
    if (!dir.existsSync()) {
      return Padding(
        padding: const EdgeInsets.only(top: 4),
        child: Row(
          children: [
            Icon(Icons.warning_amber, size: 14, color: Colors.orange[400]),
            const SizedBox(width: 4),
            Text('Directory does not exist',
                style: TextStyle(fontSize: 12, color: Colors.orange[400])),
          ],
        ),
      );
    }
    return Padding(
      padding: const EdgeInsets.only(top: 4),
      child: Row(
        children: [
          Icon(Icons.check_circle, size: 14, color: Colors.green[400]),
          const SizedBox(width: 4),
          Text('Directory exists',
              style: TextStyle(fontSize: 12, color: Colors.green[400])),
        ],
      ),
    );
  }

  void _save() {
    _nameError = null;
    _cmdError = null;
    if (_nameCtrl.text.trim().isEmpty) _nameError = 'Name is required';
    if (_cmdCtrl.text.trim().isEmpty) _cmdError = 'Command is required';
    if (_nameError != null || _cmdError != null) {
      setState(() {});
      return;
    }
    final edited = LaunchItem(
      name: _nameCtrl.text.trim(),
      directory: _dirCtrl.text.trim(),
      command: _cmdCtrl.text.trim(),
      confirm: _confirm,
      id: widget.isNew ? _nameCtrl.text.trim().replaceAll(' ', '_') : widget.item.id,
      selected: false,
      terminal: _termCtrl.text.trim().isEmpty ? null : _termCtrl.text.trim(),
      tag: widget.item.tag,
      group: widget.item.group,
    );
    widget.onSave(edited);
  }

  @override
  Widget build(BuildContext context) {
    return AlertDialog(
      title: Text(widget.isNew ? 'New Item' : 'Edit Item'),
      content: SizedBox(
        width: 500,
        child: SingleChildScrollView(
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              TextField(
                controller: _nameCtrl,
                decoration: InputDecoration(
                  labelText: 'Name',
                  errorText: _nameError,
                  border: const OutlineInputBorder(),
                ),
              ),
              const SizedBox(height: 12),
              TextField(
                controller: _dirCtrl,
                decoration: InputDecoration(
                  labelText: 'Directory',
                  border: const OutlineInputBorder(),
                  suffixIcon: IconButton(
                    icon: const Icon(Icons.folder_open),
                    tooltip: 'Browse',
                    onPressed: _pickDirectory,
                  ),
                ),
                onChanged: (_) => setState(() {}),
              ),
              if (_dirCtrl.text.isNotEmpty) _dirValidation(),
              const SizedBox(height: 12),
              TextField(
                controller: _cmdCtrl,
                decoration: InputDecoration(
                  labelText: 'Command',
                  errorText: _cmdError,
                  border: const OutlineInputBorder(),
                ),
                onChanged: (_) => setState(() {}),
              ),
              if (_cmdCtrl.text.isNotEmpty && isDangerousCmd(command: _cmdCtrl.text.trim()))
                Padding(
                  padding: const EdgeInsets.only(top: 4),
                  child: Text(
                    dangerousReasonStr(command: _cmdCtrl.text.trim()) ?? 'Warning: dangerous command',
                    style: TextStyle(color: Colors.red[400], fontSize: 12),
                  ),
                ),
              const SizedBox(height: 12),
              TextField(
                controller: _termCtrl,
                decoration: const InputDecoration(
                  labelText: 'Terminal (optional)',
                  hintText: 'e.g. pwsh, gnome-terminal',
                  border: OutlineInputBorder(),
                ),
              ),
              const SizedBox(height: 12),
              CheckboxListTile(
                value: _confirm,
                onChanged: (v) => setState(() => _confirm = v ?? true),
                title: const Text('Confirm before launch'),
                contentPadding: EdgeInsets.zero,
                controlAffinity: ListTileControlAffinity.leading,
              ),
            ],
          ),
        ),
      ),
      actions: [
        if (widget.onDelete != null)
          TextButton(
            onPressed: widget.onDelete,
            child: const Text('Delete', style: TextStyle(color: Colors.red)),
          ),
        TextButton(
          onPressed: () => Navigator.of(context).pop(),
          child: const Text('Cancel'),
        ),
        FilledButton(onPressed: _save, child: const Text('Save')),
      ],
    );
  }
}

/// Simple directory picker — navigates local filesystem directories.
class _DirectoryPickerDialog extends StatefulWidget {
  final String startDir;
  const _DirectoryPickerDialog({required this.startDir});

  @override
  State<_DirectoryPickerDialog> createState() => _DirectoryPickerDialogState();
}

class _DirectoryPickerDialogState extends State<_DirectoryPickerDialog> {
  late String _currentPath;
  List<FileSystemEntity>? _entries;

  @override
  void initState() {
    super.initState();
    _currentPath = widget.startDir;
    _loadEntries();
  }

  void _loadEntries() {
    try {
      _entries = Directory(_currentPath)
          .listSync()
          .whereType<Directory>()
          .toList();
    } catch (_) {
      _entries = [];
    }
  }

  void _navigateUp() {
    final parent = Directory(_currentPath).parent;
    _currentPath = parent.path;
    _loadEntries();
    setState(() {});
  }

  void _navigateTo(String path) {
    _currentPath = path;
    _loadEntries();
    setState(() {});
  }

  @override
  Widget build(BuildContext context) {
    return AlertDialog(
      title: Text(_currentPath, style: const TextStyle(fontSize: 13)),
      content: SizedBox(
        width: 500,
        height: 350,
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            TextButton.icon(
              icon: const Icon(Icons.arrow_upward),
              label: const Text('Parent Directory'),
              onPressed: _navigateUp,
            ),
            const Divider(),
            Expanded(
              child: _entries == null
                  ? const Center(child: CircularProgressIndicator())
                  : ListView.builder(
                      itemCount: _entries!.length,
                      itemBuilder: (_, i) {
                        final entry = _entries![i];
                        final name = entry.path.split(Platform.pathSeparator).last;
                        return ListTile(
                          leading: const Icon(Icons.folder, color: Colors.amber),
                          title: Text(name),
                          dense: true,
                          onTap: () => _navigateTo(entry.path),
                        );
                      },
                    ),
            ),
          ],
        ),
      ),
      actions: [
        TextButton(
          onPressed: () => Navigator.of(context).pop(),
          child: const Text('Cancel'),
        ),
        FilledButton(
          onPressed: () => Navigator.of(context).pop(_currentPath),
          child: const Text('Select'),
        ),
      ],
    );
  }
}
