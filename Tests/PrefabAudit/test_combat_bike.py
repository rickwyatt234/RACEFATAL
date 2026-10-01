"""Static prefab regression checks. Requires Python 3 and PyYAML; does not run Unity.

Run from the repository root: python Tests/PrefabAudit/test_combat_bike.py
Optionally pass an alternate CombatBike.prefab path as the first argument.
"""
import re
import sys
import unittest
from pathlib import Path

import yaml

PREFAB = Path(sys.argv.pop(1)) if len(sys.argv) > 1 else Path(
    "Assets/Game/Prefabs/BikeModels/CombatBike.prefab")


def load_objects(path):
    text = path.read_text()
    text = re.sub(r"^%.*\n", "", text, flags=re.M)
    text = re.sub(r"^--- !u!\d+ &(-?\d+)(?: stripped)?", r"---\nfileID: \1", text, flags=re.M)
    return list(yaml.safe_load_all(text))


class CombatBikeAudit(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.objects = load_objects(PREFAB)
        cls.ids = {d["fileID"]: d for d in cls.objects}
        cls.transforms = {d["fileID"]: d.get("Transform", d.get("RectTransform"))
                          for d in cls.objects if "Transform" in d or "RectTransform" in d}
        cls.go_transforms = {t["m_GameObject"]["fileID"]: i for i, t in cls.transforms.items()}

    def scripts(self, name):
        return [(d["fileID"], d["MonoBehaviour"]) for d in self.objects
                if "MonoBehaviour" in d and
                str(d["MonoBehaviour"].get("m_EditorClassIdentifier", "")).endswith("." + name)]

    def one(self, name):
        scripts = self.scripts(name)
        self.assertEqual(len(scripts), 1, name)
        return scripts[0][1]

    def is_descendant(self, child_go, parent_go):
        current = self.go_transforms[child_go]
        parent = self.go_transforms[parent_go]
        while current:
            if current == parent:
                return True
            current = self.transforms[current]["m_Father"]["fileID"]
        return False

    def test_all_internal_references_and_component_ownership(self):
        self.assertEqual(len(self.objects), len(self.ids), "Duplicate fileIDs")
        def walk(v):
            if isinstance(v, dict):
                if "fileID" in v and not v.get("guid") and v["fileID"]:
                    self.assertIn(v["fileID"], self.ids)
                for x in v.values(): walk(x)
            elif isinstance(v, list):
                for x in v: walk(x)
        for d in self.objects:
            for k, b in d.items():
                if k == "fileID": continue
                walk(b)
                if k == "GameObject":
                    for c in b["m_Component"]:
                        component = self.ids[c["component"]["fileID"]]
                        data = next(v for key, v in component.items() if key != "fileID")
                        self.assertEqual(data["m_GameObject"]["fileID"], d["fileID"])

    def test_driver_and_physics_placement(self):
        runtime = self.one("BikeRuntimeController")
        root = runtime["m_GameObject"]["fileID"]
        for name in ["RacerViewController", "BikeMotor", "TrackSurfaceProbe", "AIRacerSensor",
                     "TrackProgressReporter", "RacerCollisionDamage", "RacerDestructionView",
                     "CollisionFeedbackView", "MissileThreatReceiver", "AutomaticCountermeasureController",
                     "WeaponChargePresentationController", "GuidedTargetLockState", "TargetLockThreatReceiver",
                     "ShieldBubbleEffectView"]:
            self.assertEqual(self.one(name)["m_GameObject"]["fileID"], root, name)
        for field, root_field in [("playerController", "playerDriverRoot"), ("aiDriver", "aiDriverRoot")]:
            controller = self.ids[runtime[field]["fileID"]]["MonoBehaviour"]
            self.assertTrue(self.is_descendant(controller["m_GameObject"]["fileID"], runtime[root_field]["fileID"]))
        components = [self.ids[c["component"]["fileID"]] for c in self.ids[root]["GameObject"]["m_Component"]]
        self.assertTrue(any("Rigidbody" in c for c in components))
        self.assertTrue(any("BoxCollider" in c for c in components))

    def test_audio_owners_have_independent_sources(self):
        refs = []
        mounts = self.scripts("WeaponMountFeedbackView")
        self.assertEqual(len(mounts), 6)
        for _, b in mounts:
            source = b["fireAudioSource"]["fileID"]
            self.assertEqual(self.ids[source]["AudioSource"]["m_GameObject"], b["m_GameObject"])
            refs.append(source)
        for name, fields in [("CollisionFeedbackView", ["impactAudioSource", "damageAudioSource"]),
                             ("RacerDestructionView", ["audioSource"]),
                             ("AutomaticCountermeasureController", ["audioSource"]),
                             ("PlayerCockpitTargetingHUD", ["targetAcquireAudioSource", "targetLockAudioSource"])]:
            for field in fields: refs.append(self.one(name)[field]["fileID"])
        self.assertEqual(len(refs), len(set(refs)), "Independent systems share a mutable AudioSource")
        for i in refs:
            self.assertIn("AudioSource", self.ids[i])
            self.assertEqual(self.ids[i]["AudioSource"]["m_PlayOnAwake"], 0)

    def test_lap_entries_control_their_own_indicator(self):
        boxes = self.one("PlayerCockpitHUD")["lapBoxes"]
        self.assertEqual(len({b["root"]["fileID"] for b in boxes}), len(boxes))
        for box in boxes:
            image_go = self.ids[box["centerImage"]["fileID"]]["MonoBehaviour"]["m_GameObject"]["fileID"]
            self.assertTrue(self.is_descendant(image_go, box["root"]["fileID"]))
            for other in boxes:
                if other != box:
                    other_go = self.ids[other["centerImage"]["fileID"]]["MonoBehaviour"]["m_GameObject"]["fileID"]
                    self.assertFalse(self.is_descendant(other_go, box["root"]["fileID"]))

    def test_camera_pose_has_one_writer(self):
        lean = self.one("BikeLeanController")
        cockpit = self.one("PlayerCockpitView")
        self.assertNotEqual(lean["cameraLeanPivot"]["fileID"], cockpit["cameraRigRoot"]["fileID"])
        anchor_go = self.transforms[cockpit["cameraAnchor"]["fileID"]]["m_GameObject"]["fileID"]
        visual_go = self.transforms[lean["visualLeanRoot"]["fileID"]]["m_GameObject"]["fileID"]
        self.assertTrue(self.is_descendant(anchor_go, visual_go), "Camera should inherit model lean")

    def test_manual_cameras_and_listener_start_disabled(self):
        rear = self.one("PlayerRearViewController")
        for field in ["rearLeftCamera", "rearRightCamera"]:
            camera = self.ids[rear[field]["fileID"]]["Camera"]
            self.assertEqual(camera["m_Enabled"], 0)
            self.assertNotEqual(camera["m_TargetTexture"]["fileID"], 0)
        listener = self.one("PlayerCockpitView")["audioListener"]["fileID"]
        self.assertEqual(self.ids[listener]["AudioListener"]["m_Enabled"], 0)

    def test_bubble_prefabs_are_wired_in_health_order(self):
        bubble = self.one("ShieldBubbleEffectView")
        for color in ["Blue", "Teal", "Purple", "Red"]:
            asset = Path(f"Assets/Game/Prefabs/Shields/Shield_{color}.prefab")
            ref = bubble[color.lower() + "Prefab"]
            self.assertEqual(ref["guid"], yaml.safe_load(Path(str(asset) + ".meta").read_text())["guid"])
            self.assertTrue(any(d["fileID"] == ref["fileID"] and "GameObject" in d for d in load_objects(asset)))
        self.assertGreater(bubble["blueThreshold"], bubble["tealThreshold"])
        self.assertGreater(bubble["tealThreshold"], bubble["purpleThreshold"])
        bound = self.one("CollisionFeedbackView")["bubbleShield"]["fileID"]
        self.assertEqual(self.ids[bound]["MonoBehaviour"], bubble)


if __name__ == "__main__":
    unittest.main()
