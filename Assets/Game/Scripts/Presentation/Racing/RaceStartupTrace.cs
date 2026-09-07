using UnityEngine;

namespace RaceFatal.Presentation.Racing
{
    public static class RaceStartupTrace
    {
        private static int step;

        public static void Reset()
        {
            step = 0;

            Debug.Log(
                "========================================\n" +
                "RACE STARTUP PIPELINE BEGIN\n" +
                "========================================");
        }

        public static void Mark(
            string message,
            Object context = null)
        {
            step++;

            string text =
                $"[RACE STARTUP {step:00}] {message}";

            if (context != null)
            {
                Debug.Log(
                    text,
                    context);
            }
            else
            {
                Debug.Log(text);
            }
        }

        public static void Warning(
            string message,
            Object context = null)
        {
            string text =
                $"[RACE STARTUP WARNING] {message}";

            if (context != null)
            {
                Debug.LogWarning(
                    text,
                    context);
            }
            else
            {
                Debug.LogWarning(text);
            }
        }

        public static void Fail(
            string message,
            Object context = null)
        {
            string text =
                $"[RACE STARTUP FAILED] {message}";

            if (context != null)
            {
                Debug.LogError(
                    text,
                    context);
            }
            else
            {
                Debug.LogError(text);
            }
        }

        public static void Complete()
        {
            Debug.Log(
                "========================================\n" +
                "RACE STARTUP PIPELINE COMPLETE\n" +
                "========================================");
        }
    }
}