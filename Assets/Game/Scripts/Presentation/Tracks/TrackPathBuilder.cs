using System.Collections.Generic;
using RaceFatal.Presentation.Racing;
using UnityEngine;

namespace RaceFatal.Presentation.Tracks
{
    public class TrackPathBuilder :
        MonoBehaviour
    {
        [Header("Circuit")]

        [Tooltip(
            "Parent whose direct children are " +
            "TrackPiece instances arranged in " +
            "legal racing order.")]
        [SerializeField]
        private Transform circuitRoot;

        [SerializeField]
        private TrackProgressPath
            progressPath;

        [Header("Validation")]

        [Tooltip(
            "Maximum distance between one piece's " +
            "Exit and the next piece's Entry before " +
            "a warning is issued.")]
        [Min(0f)]
        [SerializeField]
        private float seamWarningDistance =
            2f;

        [Tooltip(
            "Maximum orientation difference between " +
            "connected piece anchors before a warning " +
            "is issued.")]
        [Range(0f, 45f)]
        [SerializeField]
        private float orientationWarningAngle =
            15f;

        [Tooltip(
            "Points this close together are treated " +
            "as duplicates.")]
        [Min(0.0001f)]
        [SerializeField]
        private float duplicatePointDistance =
            0.05f;

        private readonly List<Vector3>
            generatedControlPoints =
                new List<Vector3>();

        private readonly List<Vector3>
            piecePoints =
                new List<Vector3>();

        public int GeneratedControlPointCount =>
            generatedControlPoints.Count;

        public bool Build()
        {
            RaceStartupTrace.Mark(
                "TrackPathBuilder.Build() started.",
                this);

            generatedControlPoints.Clear();

            // -------------------------------------------------
            // VALIDATION
            // -------------------------------------------------

            if (circuitRoot == null)
            {
                RaceStartupTrace.Fail(
                    "TrackPathBuilder requires " +
                    "a Circuit Root.",
                    this);

                return false;
            }

            if (progressPath == null)
            {
                RaceStartupTrace.Fail(
                    "TrackPathBuilder requires a " +
                    "TrackProgressPath.",
                    this);

                return false;
            }

            var orderedPieces =
                new List<TrackPiece>();

            // -------------------------------------------------
            // COLLECT PIECES
            // -------------------------------------------------

            for (int i = 0;
                 i < circuitRoot.childCount;
                 i++)
            {
                Transform child =
                    circuitRoot.GetChild(i);

                TrackPiece piece =
                    child.GetComponent<
                        TrackPiece>();

                if (piece == null)
                {
                    RaceStartupTrace.Warning(
                        $"Circuit child " +
                        $"'{child.name}' has no " +
                        $"{nameof(TrackPiece)} and " +
                        "will be ignored.",
                        child);

                    continue;
                }

                if (!piece.IsConfigured)
                {
                    RaceStartupTrace.Fail(
                        $"Track piece '{piece.name}' " +
                        "does not have both Entry " +
                        "and Exit assigned.",
                        piece);

                    return false;
                }

                orderedPieces.Add(
                    piece);
            }

            if (orderedPieces.Count == 0)
            {
                RaceStartupTrace.Fail(
                    "No TrackPiece components were " +
                    "found under Circuit Root.",
                    this);

                return false;
            }

            RaceStartupTrace.Mark(
                $"TrackPathBuilder found " +
                $"{orderedPieces.Count} modular " +
                "track pieces.",
                this);

            // -------------------------------------------------
            // CONNECTION VALIDATION
            // -------------------------------------------------

            for (int i = 0;
                 i < orderedPieces.Count;
                 i++)
            {
                TrackPiece current =
                    orderedPieces[i];

                TrackPiece next =
                    orderedPieces[
                        (i + 1) %
                        orderedPieces.Count];

                ValidateConnection(
                    current,
                    next);
            }

            // -------------------------------------------------
            // BUILD SPARSE CENTERLINE
            // -------------------------------------------------

            foreach (TrackPiece piece
                     in orderedPieces)
            {
                piece.GetControlPoints(
                    piecePoints);

                for (int i = 0;
                     i < piecePoints.Count;
                     i++)
                {
                    AddPointIfUnique(
                        piecePoints[i]);
                }
            }

            // -------------------------------------------------
            // REMOVE DUPLICATE CLOSING POINT
            // -------------------------------------------------

            if (generatedControlPoints.Count >= 2)
            {
                Vector3 first =
                    generatedControlPoints[0];

                int finalIndex =
                    generatedControlPoints.Count -
                    1;

                Vector3 last =
                    generatedControlPoints[
                        finalIndex];

                if (Vector3.Distance(
                        first,
                        last) <=
                    duplicatePointDistance)
                {
                    generatedControlPoints
                        .RemoveAt(
                            finalIndex);
                }
            }

            RaceStartupTrace.Mark(
                $"Generated " +
                $"{generatedControlPoints.Count} " +
                "sparse circuit control points.",
                this);

            if (generatedControlPoints.Count < 4)
            {
                RaceStartupTrace.Fail(
                    "The assembled circuit produced " +
                    "fewer than four unique path " +
                    "control points.",
                    this);

                return false;
            }

            // -------------------------------------------------
            // GENERATE PROGRESS PATH
            // -------------------------------------------------

            progressPath.SetControlPoints(
                generatedControlPoints);

            if (!progressPath.Rebuild())
            {
                RaceStartupTrace.Fail(
                    "TrackProgressPath could not " +
                    "generate its progress samples.",
                    this);

                return false;
            }

            RaceStartupTrace.Mark(
                $"Progress path generated: " +
                $"{progressPath.SampleCount} samples, " +
                $"{progressPath.TotalLength:F1} meters.",
                this);

            return true;
        }

        private void AddPointIfUnique(
            Vector3 point)
        {
            if (generatedControlPoints.Count == 0)
            {
                generatedControlPoints.Add(
                    point);

                return;
            }

            Vector3 previous =
                generatedControlPoints[
                    generatedControlPoints.Count -
                    1];

            if (Vector3.Distance(
                    previous,
                    point) <=
                duplicatePointDistance)
            {
                return;
            }

            generatedControlPoints.Add(
                point);
        }

        private void ValidateConnection(
            TrackPiece current,
            TrackPiece next)
        {
            // -------------------------------------------------
            // POSITION
            // -------------------------------------------------

            float distance =
                Vector3.Distance(
                    current.Exit.position,
                    next.Entry.position);

            if (distance >
                seamWarningDistance)
            {
                RaceStartupTrace.Warning(
                    $"Track seam gap between " +
                    $"'{current.name}' and " +
                    $"'{next.name}' is " +
                    $"{distance:F3} meters.",
                    current);
            }

            // -------------------------------------------------
            // FORWARD
            // -------------------------------------------------

            float forwardAngle =
                Vector3.Angle(
                    current.Exit.forward,
                    next.Entry.forward);

            if (forwardAngle >
                orientationWarningAngle)
            {
                RaceStartupTrace.Warning(
                    $"Forward orientation mismatch " +
                    $"between '{current.name}' and " +
                    $"'{next.name}': " +
                    $"{forwardAngle:F1} degrees.",
                    current);
            }

            // -------------------------------------------------
            // SURFACE UP
            // -------------------------------------------------

            float upAngle =
                Vector3.Angle(
                    current.Exit.up,
                    next.Entry.up);

            if (upAngle >
                orientationWarningAngle)
            {
                RaceStartupTrace.Warning(
                    $"Surface orientation mismatch " +
                    $"between '{current.name}' and " +
                    $"'{next.name}': " +
                    $"{upAngle:F1} degrees.",
                    current);
            }
        }

        [ContextMenu(
            "Rebuild Circuit Path")]
        private void RebuildFromContextMenu()
        {
            Build();
        }
    }
}