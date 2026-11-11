using System.Collections.Generic;
using RaceFatal.Presentation.Racing;
using RaceFatal.Presentation.Tactics;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RaceFatal.Presentation.FrontEnd
{
    internal static class MenuTypography
    {
        private const float Scale = 0.70f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            SceneManager.sceneLoaded -= Apply;
            SceneManager.sceneLoaded += Apply;
        }

        private static void Apply(Scene scene, LoadSceneMode mode)
        {
            var labels = new HashSet<TextMeshProUGUI>();
            foreach (var root in scene.GetRootGameObjects())
            {
                if (scene.name == "03_MainMenu" || scene.name == "01_Career" || scene.name == "00_Bootstrap")
                {
                    Collect(root, labels);
                    continue;
                }

                foreach (var pause in root.GetComponentsInChildren<RacePauseView>(true))
                    Collect(pause.gameObject, labels);
                foreach (var results in root.GetComponentsInChildren<RaceOutcomeController>(true))
                {
                    Collect(results.OutcomePanel, labels);
                    Collect(results.PayoutPanel, labels);
                }
                foreach (var standings in root.GetComponentsInChildren<RaceStandingsView>(true))
                    Collect(standings.gameObject, labels);
                foreach (var tactics in root.GetComponentsInChildren<TacticalRaceUI>(true))
                    Collect(tactics.MenuCanvas, labels);
            }

            foreach (var label in labels)
            {
                // Use the authored maximum for auto-sized labels. Their current
                // fontSize can already reflect wrapping in the current viewport.
                if (!label.enableAutoSizing) label.fontSize *= Scale;
                label.fontSizeMin *= Scale;
                label.fontSizeMax *= Scale;
            }
        }

        private static void Collect(GameObject root, HashSet<TextMeshProUGUI> labels)
        {
            if (root != null) labels.UnionWith(root.GetComponentsInChildren<TextMeshProUGUI>(true));
        }
    }
}
