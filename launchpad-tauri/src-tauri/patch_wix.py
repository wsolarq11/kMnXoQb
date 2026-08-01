import io
import re

with io.open('wix/per-user-main.wxs', encoding='utf-8') as f:
    c = f.read()

# 1. Path component: file KeyPath -> HKCU registry KeyPath
old_path = re.search(
    r'(<Component Id="Path" Guid="8d90192b-1613-5adf-8617-72894d2670ed" Win64="\$\(var\.Win64\)">.*?</Component>)',
    c, re.S)
assert old_path, 'path component not found'
path_body = old_path.group(1)
assert 'KeyPath="yes"' in path_body
path_body = path_body.replace('KeyPath="yes" Checksum="yes"', 'KeyPath="no" Checksum="yes"')
path_body = path_body.replace(
    '</Component>',
    '<RegistryValue Root="HKCU" Key="Software\\Launchpad\\launchpad-tauri" Name="PathComponent" Type="integer" Value="1" KeyPath="yes" />\n            </Component>')
c = c.replace(old_path.group(1), path_body)

# 2. dll component: file KeyPath -> HKCU registry KeyPath
dll = re.search(
    r'(<Component Id="I6559177a34264e2786ca57a022fe301e" Guid="[^"]+" Win64="\$\(var\.Win64\)" KeyPath="yes"><File Id="PathFile_I6559177a34264e2786ca57a022fe301e" Source="[^"]+" /></Component>)',
    c)
assert dll, 'dll component not found'
old_dll = dll.group(1)
new_dll = old_dll.replace('KeyPath="yes"><File', 'KeyPath="no"><File')
new_dll = new_dll.replace(
    '</Component>',
    '<RegistryValue Root="HKCU" Key="Software\\Launchpad\\launchpad-tauri" Name="LibComponent" Type="integer" Value="1" KeyPath="yes" /></Component>')
c = c.replace(old_dll, new_dll)

# 3. ICE64: dedicated RemoveFile component for the Programs dir
old_dirs = re.search(
    r'(            <Directory Id="ProgramMenuFolder">.*?</Directory>\n        </Directory>)',
    c, re.S)
assert old_dirs, 'program menu block not found'
new_dirs = old_dirs.group(1) + '''

        <DirectoryRef Id="TauriLocalAppDataPrograms">
            <Component Id="CMP_RemoveProgramsDir" Guid="*">
                <RemoveFolder Id="RemoveTauriLocalAppDataPrograms" On="uninstall" />
                <RegistryValue Root="HKCU" Key="Software\\Launchpad\\launchpad-tauri" Name="ProgramsDir" Type="integer" Value="1" KeyPath="yes" />
            </Component>
        </DirectoryRef>'''
c = c.replace(old_dirs.group(1), new_dirs)

# 4. Feature: reference the new component
old_feat = '''                <ComponentRef Id="ApplicationShortcutDesktop" />
            </Feature>'''
assert old_feat in c, 'feature ref block not found'
c = c.replace(
    old_feat,
    '''                <ComponentRef Id="ApplicationShortcutDesktop" />
                <ComponentRef Id="CMP_RemoveProgramsDir" />
            </Feature>''')

with io.open('wix/per-user-main.wxs', 'w', encoding='utf-8') as f:
    f.write(c)
print('patched ok')
