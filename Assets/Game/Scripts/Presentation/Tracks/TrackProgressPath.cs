using System.Collections.Generic;
using UnityEngine;

namespace RaceFatal.Presentation.Tracks
{
    public class TrackProgressPath :
        MonoBehaviour
    {
        [Header("Generated Path")]

        [Tooltip(
            "Number of evenly spaced progress samples " +
            "generated around the complete circuit.")]
        [Min(16)]
        [SerializeField]
        private int sampleCount = 200;

        [Tooltip(
            "Higher values improve even-spacing accuracy.")]
        [Range(2, 16)]
        [SerializeField]
        private int oversampleMultiplier = 8;

        private readonly List<Vector3>
            controlPoints =
                new List<Vector3>();

        private Vector3[] samples;

        private float[] segmentLengths;

        private float[] cumulativeLengths;

        private float totalLength;

        public float TotalLength =>
            totalLength;

        public int SegmentCount =>
            segmentLengths?.Length ?? 0;

        public int SampleCount =>
            samples?.Length ?? 0;

        public int ControlPointCount =>
            controlPoints.Count;

        // -----------------------------------------------------
        // CONTROL POINT INPUT
        // -----------------------------------------------------

        public void SetControlPoints(
            IReadOnlyList<Vector3> points)
        {
            controlPoints.Clear();

            if (points == null)
                return;

            for (int i = 0;
                 i < points.Count;
                 i++)
            {
                controlPoints.Add(
                    points[i]);
            }
        }

        // -----------------------------------------------------
        // GENERATION
        // -----------------------------------------------------

        public bool Rebuild()
        {
            if (controlPoints.Count < 4)
            {
                samples = null;
                segmentLengths = null;
                cumulativeLengths = null;
                totalLength = 0f;

                return false;
            }

            GenerateEvenlySpacedSamples();

            CalculateFinalLengths();

            return true;
        }

        private void GenerateEvenlySpacedSamples()
        {
            int finalSampleCount =
                Mathf.Max(
                    16,
                    sampleCount);

            int rawSampleCount =
                Mathf.Max(
                    finalSampleCount *
                    oversampleMultiplier,
                    controlPoints.Count * 16);

            Vector3[] rawSamples =
                new Vector3[
                    rawSampleCount + 1];

            float[] rawCumulative =
                new float[
                    rawSampleCount + 1];

            rawSamples[0] =
                EvaluateSpline(0f);

            rawCumulative[0] = 0f;

            float rawTotalLength = 0f;

            for (int i = 1;
                 i <= rawSampleCount;
                 i++)
            {
                float normalized =
                    (float)i /
                    rawSampleCount;

                rawSamples[i] =
                    EvaluateSpline(
                        normalized);

                rawTotalLength +=
                    Vector3.Distance(
                        rawSamples[i - 1],
                        rawSamples[i]);

                rawCumulative[i] =
                    rawTotalLength;
            }

            samples =
                new Vector3[
                    finalSampleCount];

            samples[0] =
                rawSamples[0];

            int rawIndex = 1;

            for (int i = 1;
                 i < finalSampleCount;
                 i++)
            {
                float targetDistance =
                    rawTotalLength *
                    ((float)i /
                     finalSampleCount);

                while (
                    rawIndex <
                    rawCumulative.Length - 1 &&
                    rawCumulative[rawIndex] <
                    targetDistance)
                {
                    rawIndex++;
                }

                int previous =
                    Mathf.Max(
                        0,
                        rawIndex - 1);

                float previousDistance =
                    rawCumulative[
                        previous];

                float nextDistance =
                    rawCumulative[
                        rawIndex];

                float distance =
                    nextDistance -
                    previousDistance;

                float t =
                    distance <= 0.0001f
                        ? 0f
                        : (targetDistance -
                           previousDistance) /
                          distance;

                samples[i] =
                    Vector3.Lerp(
                        rawSamples[
                            previous],
                        rawSamples[
                            rawIndex],
                        t);
            }
        }

        private Vector3 EvaluateSpline(
            float normalized)
        {
            normalized =
                Mathf.Repeat(
                    normalized,
                    1f);

            int count =
                controlPoints.Count;

            float scaled =
                normalized *
                count;

            int current =
                Mathf.FloorToInt(
                    scaled);

            float t =
                scaled -
                Mathf.Floor(
                    scaled);

            int p0 =
                WrapControlPoint(
                    current - 1);

            int p1 =
                WrapControlPoint(
                    current);

            int p2 =
                WrapControlPoint(
                    current + 1);

            int p3 =
                WrapControlPoint(
                    current + 2);

            return CatmullRom(
                controlPoints[p0],
                controlPoints[p1],
                controlPoints[p2],
                controlPoints[p3],
                t);
        }

        private static Vector3 CatmullRom(
            Vector3 p0,
            Vector3 p1,
            Vector3 p2,
            Vector3 p3,
            float t)
        {
            float t2 = t * t;
            float t3 = t2 * t;

            return 0.5f *
            (
                2f * p1
                +
                (-p0 + p2) * t
                +
                (
                    2f * p0
                    - 5f * p1
                    + 4f * p2
                    - p3
                ) * t2
                +
                (
                    -p0
                    + 3f * p1
                    - 3f * p2
                    + p3
                ) * t3
            );
        }

        private int WrapControlPoint(
            int index)
        {
            int count =
                controlPoints.Count;

            index %= count;

            if (index < 0)
                index += count;

            return index;
        }

        private void CalculateFinalLengths()
        {
            int count =
                samples.Length;

            segmentLengths =
                new float[count];

            cumulativeLengths =
                new float[count];

            totalLength = 0f;

            for (int i = 0;
                 i < count;
                 i++)
            {
                cumulativeLengths[i] =
                    totalLength;

                int next =
                    (i + 1) %
                    count;

                float length =
                    Vector3.Distance(
                        samples[i],
                        samples[next]);

                segmentLengths[i] =
                    length;

                totalLength += length;
            }
        }

        // -----------------------------------------------------
        // RACER PROGRESS
        // -----------------------------------------------------

        public float GetProgress(
            Vector3 worldPosition,
            int previousSegment,
            int searchRadius,
            out int bestSegment)
        {
            bestSegment = -1;

            if (SegmentCount == 0 ||
                totalLength <= 0f)
            {
                return 0f;
            }

            float bestDistanceSqr =
                float.MaxValue;

            float bestDistanceAlong =
                0f;

            if (previousSegment < 0 ||
                previousSegment >=
                SegmentCount)
            {
                for (int i = 0;
                     i < SegmentCount;
                     i++)
                {
                    EvaluateSegment(
                        i,
                        worldPosition,
                        ref bestDistanceSqr,
                        ref bestDistanceAlong,
                        ref bestSegment);
                }
            }
            else
            {
                int radius =
                    Mathf.Max(
                        1,
                        searchRadius);

                for (int offset = -radius;
                     offset <= radius;
                     offset++)
                {
                    int index =
                        WrapSegment(
                            previousSegment +
                            offset);

                    EvaluateSegment(
                        index,
                        worldPosition,
                        ref bestDistanceSqr,
                        ref bestDistanceAlong,
                        ref bestSegment);
                }
            }

            return Mathf.Repeat(
                bestDistanceAlong /
                totalLength,
                1f);
        }

        private void EvaluateSegment(
            int index,
            Vector3 worldPosition,
            ref float bestDistanceSqr,
            ref float bestDistanceAlong,
            ref int bestSegment)
        {
            Vector3 start =
                samples[index];

            int next =
                (index + 1) %
                samples.Length;

            Vector3 end =
                samples[next];

            Vector3 segment =
                end - start;

            float segmentLengthSqr =
                segment.sqrMagnitude;

            if (segmentLengthSqr <=
                0.0001f)
            {
                return;
            }

            float t =
                Vector3.Dot(
                    worldPosition - start,
                    segment) /
                segmentLengthSqr;

            t =
                Mathf.Clamp01(t);

            Vector3 closest =
                start +
                segment * t;

            float distanceSqr =
                (worldPosition -
                 closest).sqrMagnitude;

            if (distanceSqr >=
                bestDistanceSqr)
            {
                return;
            }

            bestDistanceSqr =
                distanceSqr;

            bestSegment = index;

            bestDistanceAlong =
                cumulativeLengths[index] +
                segmentLengths[index] *
                t;
        }

        // -----------------------------------------------------
        // AI LOOK-AHEAD
        // -----------------------------------------------------

        public Vector3 GetPositionAtProgress(
            float normalizedProgress)
        {
            if (samples == null ||
                samples.Length == 0)
            {
                return transform.position;
            }

            normalizedProgress =
                Mathf.Repeat(
                    normalizedProgress,
                    1f);

            float scaled =
                normalizedProgress *
                samples.Length;

            int index =
                Mathf.FloorToInt(
                    scaled) %
                samples.Length;

            int next =
                (index + 1) %
                samples.Length;

            float t =
                scaled -
                Mathf.Floor(
                    scaled);

            return Vector3.Lerp(
                samples[index],
                samples[next],
                t);
        }

        public Vector3 GetForwardAtProgress(
            float normalizedProgress)
        {
            if (samples == null ||
                samples.Length < 2)
            {
                return transform.forward;
            }

            normalizedProgress =
                Mathf.Repeat(
                    normalizedProgress,
                    1f);

            float scaled =
                normalizedProgress *
                samples.Length;

            int index =
                Mathf.FloorToInt(
                    scaled) %
                samples.Length;

            int next =
                (index + 1) %
                samples.Length;

            Vector3 direction =
                samples[next] -
                samples[index];

            if (direction.sqrMagnitude <
                0.0001f)
            {
                return transform.forward;
            }

            return direction.normalized;
        }

        public float AdvanceProgressByDistance(
            float normalizedProgress,
            float distanceMeters)
        {
            if (totalLength <= 0f)
            {
                return normalizedProgress;
            }

            float normalizedDistance =
                distanceMeters /
                totalLength;

            return Mathf.Repeat(
                normalizedProgress +
                normalizedDistance,
                1f);
        }

        // -----------------------------------------------------
        // SAMPLE ACCESS
        // -----------------------------------------------------

        public Vector3 GetSamplePosition(
            int index)
        {
            if (samples == null ||
                samples.Length == 0)
            {
                return transform.position;
            }

            index %= samples.Length;

            if (index < 0)
                index += samples.Length;

            return samples[index];
        }

        public Vector3 GetSegmentForward(
            int index)
        {
            if (samples == null ||
                samples.Length < 2)
            {
                return transform.forward;
            }

            int current =
                WrapSegment(
                    index);

            int next =
                WrapSegment(
                    current + 1);

            Vector3 direction =
                samples[next] -
                samples[current];

            if (direction.sqrMagnitude <
                0.0001f)
            {
                return transform.forward;
            }

            return direction.normalized;
        }

        private int WrapSegment(
            int index)
        {
            index %= SegmentCount;

            if (index < 0)
                index += SegmentCount;

            return index;
        }

        // -----------------------------------------------------
        // DEBUG
        // -----------------------------------------------------

        private void OnDrawGizmosSelected()
        {
            if (samples == null ||
                samples.Length < 2)
            {
                return;
            }

            for (int i = 0;
                 i < samples.Length;
                 i++)
            {
                int next =
                    (i + 1) %
                    samples.Length;

                Gizmos.DrawLine(
                    samples[i],
                    samples[next]);
            }
        }
    }
}