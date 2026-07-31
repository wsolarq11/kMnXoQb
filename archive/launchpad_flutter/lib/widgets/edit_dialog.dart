import 'dart:io';
import 'package:flutter/material.dart';
import 'package:launchpad_flutter/src/rust/api/simple.dart';
import 'package:launchpad_flutter/src/rust/types.dart';
import 'package:launchpad_flutter/theme.dart';
import 'package:lucide_icons/lucide_icons.dart';

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
    final startDir = _dirCtrl.text.isNotEmpty ? _dirCtrl.text : Directory.current.path;
    final picked = await showDialog<String>(
      context: context,
      builder: (ctx) => DirectoryPickerDialog(startDir: startDir),
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
            Icon(LucideIcons.alertTriangle, size: 14, color: Colors.orange[400]),
            const SizedBox(width: 4),
            Text('Directory does not exist',
                style: bodyStyle(color: Colors.orange[400]).copyWith(fontSize: 12)),
          ],
        ),
      );
    }
    return Padding(
      padding: const EdgeInsets.only(top: 4),
      child: Row(
        children: [
          Icon(LucideIcons.checkCircle, size: 14, color: Colors.green[400]),
          const SizedBox(width: 4),
          Text('Directory exists',
              style: bodyStyle(color: Colors.green[400]).copyWith(fontSize: 12)),
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
      id: widget.isNew
          ? _nameCtrl.text.trim().replaceAll(' ', '_')
          : widget.item.id,
      selected: false,
      terminal: _termCtrl.text.trim().isEmpty ? null : _termCtrl.text.trim(),
      tag: widget.item.tag,
      group: widget.item.group,
    );
    widget.onSave(edited);
  }

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;
    final colors = ThemeColors(isDark);

    return AlertDialog(
      backgroundColor: colors.base,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(20),
        side: BorderSide(color: colors.border),
      ),
      title: Text(widget.isNew ? 'New Item' : 'Edit Item',
          style: headingStyle(color: colors.textPrimary)),
      content: SizedBox(
        width: 500,
        child: SingleChildScrollView(
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              TextField(
                controller: _nameCtrl,
                style: bodyStyle(color: colors.textPrimary),
                decoration: _inputDeco(colors, 'Name', errorText: _nameError),
              ),
              const SizedBox(height: 12),
              TextField(
                controller: _dirCtrl,
                style: codeStyle(color: colors.textPrimary),
                decoration: _inputDeco(colors, 'Directory').copyWith(
                  suffixIcon: IconButton(
                    icon: const Icon(LucideIcons.folderOpen, size: 18),
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
                style: codeStyle(color: colors.textPrimary),
                decoration: _inputDeco(colors, 'Command', errorText: _cmdError),
                onChanged: (_) => setState(() {}),
              ),
              if (_cmdCtrl.text.isNotEmpty &&
                  isDangerousCmd(command: _cmdCtrl.text.trim()))
                Padding(
                  padding: const EdgeInsets.only(top: 4),
                  child: Row(
                    children: [
                      Icon(LucideIcons.alertTriangle, size: 14, color: colors.danger),
                      const SizedBox(width: 4),
                      Expanded(
                        child: Text(
                          dangerousReasonStr(command: _cmdCtrl.text.trim()) ??
                              'Warning: dangerous command',
                          style: bodyStyle(color: colors.danger).copyWith(fontSize: 12),
                        ),
                      ),
                    ],
                  ),
                ),
              const SizedBox(height: 12),
              TextField(
                controller: _termCtrl,
                style: bodyStyle(color: colors.textPrimary),
                decoration: _inputDeco(colors, 'Terminal (optional)',
                    hintText: 'e.g. pwsh, gnome-terminal'),
              ),
              const SizedBox(height: 12),
              CheckboxListTile(
                value: _confirm,
                onChanged: (v) => setState(() => _confirm = v ?? true),
                title: Text('Confirm before launch', style: bodyStyle()),
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
            child: Text('Delete', style: bodyStyle(color: colors.danger)),
          ),
        TextButton(
          onPressed: () => Navigator.of(context).pop(),
          child: Text('Cancel', style: bodyStyle()),
        ),
        FilledButton(
          onPressed: _save,
          style: FilledButton.styleFrom(
            backgroundColor: colors.accent,
          ),
          child: const Text('Save'),
        ),
      ],
    );
  }

  InputDecoration _inputDeco(ThemeColors colors, String label, {String? errorText, String? hintText}) {
    return InputDecoration(
      labelText: label,
      hintText: hintText,
      errorText: errorText,
      labelStyle: labelStyle(color: colors.textSecondary),
      hintStyle: bodyStyle(color: colors.textTertiary),
      errorStyle: bodyStyle(color: colors.danger).copyWith(fontSize: 11),
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
        borderSide: BorderSide(color: colors.accent, width: 1.5),
      ),
      filled: true,
      fillColor: colors.surface,
    );
  }
}

/// Simple directory picker dialog.
class DirectoryPickerDialog extends StatefulWidget {
  final String startDir;
  const DirectoryPickerDialog({super.key, required this.startDir});

  @override
  State<DirectoryPickerDialog> createState() => _DirectoryPickerDialogState();
}

class _DirectoryPickerDialogState extends State<DirectoryPickerDialog> {
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
      _entries = Directory(_currentPath).listSync().whereType<Directory>().toList();
    } catch (_) {
      _entries = [];
    }
  }

  void _navigateUp() {
    _currentPath = Directory(_currentPath).parent.path;
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
    final isDark = Theme.of(context).brightness == Brightness.dark;
    final colors = ThemeColors(isDark);
    return AlertDialog(
      backgroundColor: colors.base,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(16),
        side: BorderSide(color: colors.border),
      ),
      title: Text(_currentPath,
          style: codeStyle(color: colors.textSecondary).copyWith(fontSize: 12)),
      content: SizedBox(
        width: 500,
        height: 350,
        child: Column(
          children: [
            TextButton.icon(
              icon: const Icon(LucideIcons.arrowUp, size: 16),
              label: Text('Parent Directory', style: bodyStyle(color: colors.textSecondary)),
              onPressed: _navigateUp,
            ),
            Divider(color: colors.border),
            Expanded(
              child: _entries == null
                  ? const Center(child: CircularProgressIndicator())
                  : ListView.builder(
                      itemCount: _entries!.length,
                      itemBuilder: (_, i) {
                        final name = _entries![i]
                            .path
                            .split(Platform.pathSeparator)
                            .last;
                        return ListTile(
                          leading: Icon(LucideIcons.folder, size: 20, color: Colors.amber[600]),
                          title: Text(name, style: bodyStyle(color: colors.textPrimary)),
                          dense: true,
                          onTap: () => _navigateTo(_entries![i].path),
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
          child: Text('Cancel', style: bodyStyle(color: colors.textSecondary)),
        ),
        FilledButton(
          onPressed: () => Navigator.of(context).pop(_currentPath),
          style: FilledButton.styleFrom(backgroundColor: colors.accent),
          child: const Text('Select'),
        ),
      ],
    );
  }
}
