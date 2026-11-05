using System.Collections.Generic;
using UnityEngine;

namespace RaceFatal.Presentation.Tracks
{
    // A greybox road and its racing infrastructure share a single closed centerline.
    public sealed class CareerCircuitLayout : MonoBehaviour
    {
        [SerializeField] private float radius = 200f, aspect = 1f, bends = 3f, bendDepth = .15f, climb = 0f;
        [SerializeField] private bool reverse;
        [SerializeField] private Color roadColor = Color.gray;
        private Mesh roadMesh;
        private Material roadMaterial;

        private void Awake()
        {
            const int count = 160;
            const float halfWidth = 12f;
            var points = new List<Vector3>(count);
            var rotations = new List<Quaternion>(count);
            for (int i = 0; i < count; i++)
            {
                float angle = (reverse ? -1f : 1f) * i * 2f * Mathf.PI / count;
                float radial = radius * (1f + bendDepth * Mathf.Sin(bends * angle) + .045f * Mathf.Cos(2f * angle));
                points.Add(new Vector3(radial * Mathf.Cos(angle), climb * Mathf.Sin(2f * angle), aspect * radial * Mathf.Sin(angle)));
            }

            var vertices = new Vector3[count * 2];
            var triangles = new int[count * 6];
            var worldPoints = new List<Vector3>(count);
            for (int i = 0; i < count; i++)
            {
                int next = (i + 1) % count;
                Vector3 forward = (points[next] - points[(i + count - 1) % count]).normalized;
                Quaternion rotation = Quaternion.LookRotation(forward, Vector3.up);
                rotations.Add(transform.rotation * rotation);
                worldPoints.Add(transform.TransformPoint(points[i]));
                Vector3 right = rotation * Vector3.right;
                vertices[i * 2] = points[i] - right * halfWidth;
                vertices[i * 2 + 1] = points[i] + right * halfWidth;
                int t = i * 6;
                triangles[t] = i * 2; triangles[t + 1] = next * 2; triangles[t + 2] = i * 2 + 1;
                triangles[t + 3] = i * 2 + 1; triangles[t + 4] = next * 2; triangles[t + 5] = next * 2 + 1;
            }
            roadMesh = new Mesh { name = "Circuit road", vertices = vertices, triangles = triangles };
            roadMesh.RecalculateNormals();
            roadMesh.RecalculateBounds();
            gameObject.layer = LayerMask.NameToLayer("TrackSurface");
            gameObject.AddComponent<MeshFilter>().sharedMesh = roadMesh;
            gameObject.AddComponent<MeshCollider>().sharedMesh = roadMesh;
            var renderer = gameObject.AddComponent<MeshRenderer>();
            var shader = Shader.Find("HDRP/Lit") ?? Shader.Find("Standard");
            if (shader != null)
            {
                roadMaterial = new Material(shader);
                if (roadMaterial.HasProperty("_BaseColor")) roadMaterial.SetColor("_BaseColor", roadColor);
                renderer.sharedMaterial = roadMaterial;
            }
            var path = gameObject.AddComponent<TrackProgressPath>();
            path.SetControlPoints(worldPoints, rotations);
            path.Rebuild();
            var grid = new List<Transform>(12);
            for (int i = 0; i < 12; i++)
            {
                float progress = (4f + (5 - i / 2) * 6f) / path.TotalLength;
                Quaternion rot = path.GetRotationAtProgress(progress);
                var slot = Child("Grid " + (i + 1));
                slot.SetPositionAndRotation(path.GetPositionAtProgress(progress) + rot * new Vector3(i % 2 == 0 ? -3f : 3f, 1f, 0f), rot);
                grid.Add(slot);
            }
            var line = Child("Lap line");
            line.SetPositionAndRotation(worldPoints[0], rotations[0]);
            var trigger = line.gameObject.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = new Vector3(halfWidth * 2f, 10f, 2f);
            for (int side = 0; side < 2; side++)
            for (int i = 0; i < count; i++)
            {
                Vector3 from = vertices[i * 2 + side], to = vertices[((i + 1) % count) * 2 + side];
                var wall = Child("Track barrier");
                wall.localPosition = (from + to) * .5f + Vector3.up;
                wall.localRotation = Quaternion.LookRotation(to - from);
                wall.gameObject.layer = LayerMask.NameToLayer("Wall");
                wall.gameObject.AddComponent<BoxCollider>().size = new Vector3(.45f, 2f, Vector3.Distance(from, to) + .1f);
                wall.gameObject.AddComponent<TrackWall>();
            }
            gameObject.AddComponent<TrackRuntimeController>().ConfigureGenerated(path, line.gameObject.AddComponent<LapLineTrigger>(), grid);
        }

        private Transform Child(string label)
        {
            var child = new GameObject(label).transform;
            child.SetParent(transform, false);
            return child;
        }

        private void OnDestroy()
        {
            if (roadMesh != null) Destroy(roadMesh);
            if (roadMaterial != null) Destroy(roadMaterial);
        }
    }
}
