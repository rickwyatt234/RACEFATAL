from pathlib import Path
import re
root = Path(__file__).resolve().parents[2]
scene = root / 'Assets/Game/Scenes/01_Career.unity'
text = scene.read_text()
docs = dict((m[1], m[2]) for m in re.finditer(r'^--- !u!\d+ &(\d+)\n([\s\S]*?)(?=^--- !u!|\Z)', text, re.M))
assert len(docs) == len(re.findall(r'^--- !u!', text, re.M)), 'Duplicate scene identity'
for ident, doc in docs.items():
    for target in re.findall(r'^  - component: \{fileID: (\d+)\}', doc, re.M):
        assert target in docs, f'Dangling component {target}'
    children = re.search(r'^  m_Children:\n([\s\S]*?)^  m_Father:', doc, re.M)
    if children:
        for target in re.findall(r'fileID: (\d+)', children[1]):
            assert target in docs, f'Dangling child {target}'
assert 'CareerResearch' not in text and 'ResearchTree' not in text
for field in ['brandsRoot','brandsButton','lifetimeFameText']:
    targets = re.findall(r'^  '+field+r': \{fileID: (\d+)\}', text, re.M)
    assert targets and all(t in docs for t in targets if t != '0'), f'Missing {field} wiring'
root_go = re.search(r'^  brandsRoot: \{fileID: (\d+)\}', text, re.M)[1]
assert 'm_Name: BrandsRoot' in docs[root_go]
# Compare local references with the pre-change scene; no new dangling IDs are allowed.
def missing(s):
    ids = set(re.findall(r'^--- !u!\d+ &(\d+)',s,re.M))
    return set(re.findall(r'\{fileID: (\d+)\}',s)) - ids - {'0'}
baseline_missing = set()
assert not (missing(text)-baseline_missing), "New dangling scene references: "+str(missing(text)-baseline_missing)
# Legacy development inventory preserves asset identity when moved.
legacy_hardware = {'DEV_ENGINE_HYBRID.asset': ('DEV_ENGINE_HYBRID', 'f7bcede6c056440449503888c565450c'), 'DEV_ENGINE_SPONSORED.asset': ('DEV_ENGINE_RESEARCH', '259e9c0df2b16a24e8a2a7f434f0aa09'), 'DEV_SHIELD_HYBRID.asset': ('DEV_SHIELD_HYBRID', 'bb6a6cc17bb543341b219672bc3536b3'), 'DEV_SHIELD_SPONSORED.asset': ('DEV_SHIELD_RESEARCH', '4692efe3d0ebf514dbc55424425f01d9')}
for p in (root/'Assets/Game/BrandDevelopment').glob('*.asset'):
    assert 'requiredTechnologyId:' not in p.read_text()
    assert Path(str(p)+'.meta').exists()
    expected_id, expected_guid = legacy_hardware[p.name]
    assert re.search(r'^  id: (.*)$',p.read_text(),re.M)[1] == expected_id, 'Legacy hardware ID changed'
    assert re.search(r'guid: (\w+)',Path(str(p)+'.meta').read_text())[1] == expected_guid, 'Legacy hardware GUID changed'
for p in (root/'Assets/Game/Scripts').rglob('*.cs'):
    assert not re.search(r'\b(?:ResearchPoints|ResearchService|ResearcherService|RequiredTechnologyId|TechnologyDefinition|ResearchField)\b',p.read_text()), f'Retired API in {p}'
print('PASS: Brands scene wiring, hierarchy, removed APIs and legacy hardware metadata')
