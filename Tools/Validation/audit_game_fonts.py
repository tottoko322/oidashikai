"""Check the game's serialized TMP font and atlas references without editing assets."""
from pathlib import Path
import json
import re
import sys

root = Path(sys.argv[1]).resolve() if len(sys.argv) > 1 else Path(__file__).resolve().parents[2]
font = root / 'Assets/TextMesh Pro/Fonts/JikiTourai SDF_Complete.asset'
guid = re.search(r'^guid: (\w+)$', Path(str(font) + '.meta').read_text(encoding='utf-8'), re.M)[1]
with font.open(encoding='utf-8') as stream:
    material_id = re.search(r'--- !u!21 &(-?\d+)', stream.read(4096))[1]
expected_font = f'  m_fontAsset: {{fileID: 11400000, guid: {guid}, type: 2}}'
expected_material = f'  m_sharedMaterial: {{fileID: {material_id}, guid: {guid}, type: 2}}'
files = sorted((root / 'Assets/Scenes').glob('*.unity')) + sorted((root / 'Assets').glob('*.prefab'))
report = []
for path in files:
    count = 0
    for block in re.split(r'(?=--- !u!)', path.read_text(encoding='utf-8')):
        if '  m_fontAsset:' not in block:
            continue
        count += 1
        assert expected_font in block.splitlines(), f'Font mismatch: {path}'
        assert expected_material in block.splitlines(), f'Material mismatch: {path}'
    report.append({'path': path.relative_to(root).as_posix(), 'texts': count})
settings = (root / 'Assets/Script/Effect/Resources/TMP Settings.asset').read_text(encoding='utf-8')
assert f'  m_defaultFontAsset: {{fileID: 11400000, guid: {guid}, type: 2}}' in settings.splitlines()
print(json.dumps({'font': font.name, 'texts': sum(row['texts'] for row in report), 'files': report}, ensure_ascii=False, indent=2))
