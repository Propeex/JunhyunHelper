from pathlib import Path

root = Path('src/JunhyunHelper.Desktop')
changed = 0
for path in root.glob('MainWindow*.cs'):
    text = path.read_text(encoding='utf-8')
    lines = text.splitlines()
    filtered = [
        line for line in lines
        if 'LegacyMapQuestSidebarPolishBridge' not in line
        and '_legacyMapQuestSidebarPolish' not in line
    ]
    if filtered != lines:
        path.write_text('\n'.join(filtered) + '\n', encoding='utf-8', newline='\n')
        changed += 1

if changed == 0:
    raise RuntimeError('No obsolete map sidebar polish reference was found')

for path in root.glob('MainWindow*.cs'):
    text = path.read_text(encoding='utf-8')
    if 'LegacyMapQuestSidebarPolishBridge' in text or '_legacyMapQuestSidebarPolish' in text:
        raise RuntimeError(f'Obsolete map sidebar polish reference remains in {path}')

print(f'Obsolete map sidebar polish references removed from {changed} file(s)')
