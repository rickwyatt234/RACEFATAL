using System;
using System.Collections.Generic;
using RaceFatal.Presentation.Tracks;
using UnityEditor;
using UnityEngine;

// Uses the production path API and Unity's quaternion implementation, without
// requiring a test-framework package. Run in Edit Mode after compilation.
public static class TrackCameraValidation
{
    [MenuItem("RACE//FATAL/Validation/Track Camera Frames")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Run track frame validation outside Play Mode.");
        var root = new GameObject("TrackFrameValidation") { hideFlags = HideFlags.HideAndDontSave };
        try
        {
            var path = root.AddComponent<TrackProgressPath>();
            ValidateCircuit(path, false);
            ValidateCircuit(path, true);
            // Old position-only callers remain valid, and rebuilding an empty
            // path must discard previously generated orientation data.
            path.SetControlPoints(new List<Vector3>());
            Require(!path.Rebuild(), "Empty path must fail rebuilding.");
            Require(Quaternion.Angle(path.GetRotationAtProgress(0.5f), root.transform.rotation) < 0.01f,
                "Empty rebuild must clear old frames.");
            Debug.Log("Track camera frame validation passed: vertical loop, full corkscrew, seam, continuity and rebuild cleanup.");
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }

    private static void ValidateCircuit(TrackProgressPath path, bool corkscrew)
    {
        const int count = 16;
        var points = new List<Vector3>();
        var frames = new List<Quaternion>();
        for (int i = 0; i < count; i++)
        {
            float angle = i * Mathf.PI * 2f / count;
            Vector3 outward = new Vector3(0f, Mathf.Sin(angle), Mathf.Cos(angle));
            Vector3 tangent = new Vector3(0f, Mathf.Cos(angle), -Mathf.Sin(angle));
            points.Add(outward * 100f);
            Quaternion frame = Quaternion.LookRotation(tangent, outward);
            if (corkscrew) frame = Quaternion.AngleAxis(i * 360f / count, tangent) * frame;
            frames.Add(frame);
        }
        path.SetControlPoints(points, frames);
        Require(path.Rebuild(), "Synthetic circuit must rebuild.");
        for (int i = 0; i < count; i++)
        {
            Quaternion actual = path.GetRotationAtProgress((float)i / count);
            Require(Quaternion.Angle(actual, frames[i]) < 2f, "Frame lost authored banking at anchor " + i);
            Require(Vector3.Dot(actual * Vector3.up, frames[i] * Vector3.up) > 0.99f,
                "Surface-up flipped at anchor " + i);
        }
        Quaternion previous = path.GetRotationAtProgress(0f);
        for (int i = 1; i <= 2048; i++)
        {
            Quaternion current = path.GetRotationAtProgress(i / 2048f);
            Require(Quaternion.Angle(previous, current) < 2f, "Camera frame discontinuity at sample " + i);
            previous = current;
        }
        Require(Quaternion.Angle(path.GetRotationAtProgress(-0.0001f), path.GetRotationAtProgress(0.0001f)) < 1f,
            "Lap seam must interpolate continuously in both directions.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
