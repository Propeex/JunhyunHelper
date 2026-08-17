from pathlib import Path

path = Path('src/JunhyunHelper.Desktop/MainWindow.LegacyMapHost.cs')
text = path.read_text(encoding='utf-8')
lines = text.splitlines()
filtered = [
    line for line in lines
    if 'LegacyMapQuestSidebarPolishBridge' not in line
    and '_legacyMapQuestSidebarPolish' not in line
]
if len(filtered) == len(lines):
    raise RuntimeError('No obsolete map sidebar polish reference was found')
updated = '\n'.join(filtered) + '\n'
if 'LegacyMapQuestSidebarPolishBridge' in updated or '_legacyMapQuestSidebarPolish' in updated:
    raise RuntimeError('Obsolete map sidebar polish reference remains')
path.write_text(updated, encoding='utf-8', newline='\n')
print('Obsolete map sidebar polish references removed')
