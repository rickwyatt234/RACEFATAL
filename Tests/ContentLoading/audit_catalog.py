"""Check committed catalog GUID references without importing the project in Unity."""
from pathlib import Path
import re

root = Path(__file__).resolve().parents[2]
content = root / "Assets/Game/Scripts/Content"
assets = {}
for meta in content.rglob("*.asset.meta"):
    match = re.search(r"^guid: (\w+)", meta.read_text(), re.M)
    if match:
        assets[match[1]] = meta.with_suffix("")
catalog = (content / "Catalogs/GameContentCatalog.asset").read_text()
def references(section):
    block = re.search(r"^  " + section + r":\n(?:  - .*\n)*", catalog, re.M)
    return re.findall(r"guid: (\w+)", block[0])
tracks = references("trackDefinitions")
races = references("raceDefinitions")
events = references("careerEventDefinitions")
for guid in tracks + races + events:
    assert guid in assets, f"Catalog contains a missing asset: {guid}"
for guid in races:
    race = assets[guid]
    track = re.search(r"^  track: \{fileID: 11400000, guid: (\w+), type: 2\}", race.read_text(), re.M)
    assert track and track[1] in tracks and track[1] in assets, f"Unresolved/unregistered track: {race}"
for guid in events:
    event = assets[guid]
    block = re.search(r"^  rounds:\n(?:  - .*\n)*", event.read_text(), re.M)
    assert block, f"Missing event rounds: {event}"
    for race_guid in re.findall(r"guid: (\w+)", block[0]):
        assert race_guid in races, f"Event references an unregistered race: {event}"
print(f"PASS: catalog references resolve ({len(tracks)} track, {len(races)} races, {len(events)} events)")
