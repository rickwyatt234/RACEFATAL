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

# A rival build can be registered and still fail on race entry if a mount belongs
# to another frame or if one of its parts was omitted from the runtime catalog.
def field(path, key):
    return re.search(r"^  " + key + r": (.*)$", path.read_text(), re.M)[1]

names = ("smallNodeCount", "mediumNodeCount", "largeNodeCount")
bike_ids = set(references("bikeDefinitions"))
gear_ids = set(references("equipmentDefinitions"))
engines = set(references("engineDefinitions"))
chassis = set(references("chassisDefinitions"))
build_ids = set(references("bikeBuildDefinitions"))
racers = set(references("racerDefinitions"))
active_racers, signatures = set(), set()
teams = [assets[g] for g in references("opponentTeamDefinitions") if "  legacyRoster: 1" not in assets[g].read_text()]
assert len(teams) == 21
for team in teams:
    body = team.read_text().split("  racers:\n")[1]
    roster, garage = body.split("  bikeBuilds:\n")
    team_racers = re.findall(r"guid: (\w+)", roster)
    team_builds = re.findall(r"guid: (\w+)", garage)
    assert len(set(team_racers)) == len(set(team_builds)) == 2
    assert set(team_racers) <= racers and not (active_racers & set(team_racers))
    assert set(team_builds) <= build_ids and not (signatures & set(team_builds))
    active_racers.update(team_racers)
    signatures.update(team_builds)
    for g in team_builds:
        build = assets[g]
        def part(key):
            return re.search(r"guid: (\w+)", field(build, key))[1]
        assert part("bike") in bike_ids and part("engine") in engines and part("chassis") in chassis
        frame = assets[part("bike")]
        occupied = set()
        for item, size, index in re.findall(r"  - equipment: .*guid: (\w+).*\n    nodeSize: (\d+)\n    nodeIndex: (\d+)", build.read_text()):
            assert item in gear_ids and int(field(assets[item], "requiredNodeSize")) <= int(size)
            assert int(index) < int(field(frame, names[int(size)])) and (size, index) not in occupied
            occupied.add((size, index))
print("PASS: 21 career teams, 42 distinct racers, 42 legal signature loadouts")
