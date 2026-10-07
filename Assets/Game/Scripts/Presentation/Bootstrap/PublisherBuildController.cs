using System.Collections.Generic;
using System.Linq;
using RaceFatal.Content.Vehicles;
using RaceFatal.Infrastructure;
using RaceFatal.Presentation.Racing;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RaceFatal.Presentation.Bootstrap
{
    public class PublisherBuildController : MonoBehaviour
    {
        private enum ScreenState
        {
            Hidden,
            Splash,
            Loadout,
            PostRace
        }

        private const string BootstrapSceneName = "00_Bootstrap";
        private const int MaxPresetCount = 3;

        public static PublisherBuildController Instance { get; private set; }
        public static bool IsActive => Instance != null && Instance.active;

        private readonly List<BikeBuildDefinitionSO> presets = new List<BikeBuildDefinitionSO>();
        private PrototypeRaceLauncher launcher;
        private BikeBuildDefinitionSO selectedBuild;
        private ScreenState state = ScreenState.Hidden;
        private bool active;

        private GUIStyle titleStyle;
        private GUIStyle headingStyle;
        private GUIStyle bodyStyle;
        private GUIStyle buttonStyle;
        private GUIStyle panelStyle;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void Begin()
        {
            active = true;
            launcher = GetComponent<PrototypeRaceLauncher>();
            if (launcher == null)
            {
                Debug.LogError("[PublisherBuild] PrototypeRaceLauncher is required.", this);
                return;
            }

            BuildPresetList();
            state = ScreenState.Splash;
            ReleaseCursor();
        }

        public static bool TryReturnToPublisherMenu()
        {
            if (!IsActive)
                return false;
            Instance.ReturnToPublisherMenu();
            return true;
        }

        private void ReturnToPublisherMenu()
        {
            GameContext context = BootstrapController.Context;
            if (context != null)
            {
                context.RaceLaunch.Clear();
                context.Sessions.ClearSession();
            }

            state = ScreenState.PostRace;
            ReleaseCursor();

            if (Application.CanStreamedLevelBeLoaded(BootstrapSceneName))
                SceneManager.LoadScene(BootstrapSceneName);
            else
                Debug.LogError($"[PublisherBuild] Scene '{BootstrapSceneName}' is not in the build profile.", this);
        }

        private void BuildPresetList()
        {
            presets.Clear();
            var catalog = BootstrapController.ContentCatalog;
            if (catalog != null)
            {
                foreach (BikeBuildDefinitionSO build in catalog.BikeBuildDefinitions.Where(b => b != null && !string.IsNullOrWhiteSpace(b.Id)))
                {
                    if (presets.Count >= MaxPresetCount)
                        break;
                    presets.Add(build);
                }
            }

            if (launcher != null && launcher.DefaultPlayerBuild != null && !presets.Contains(launcher.DefaultPlayerBuild))
                presets.Insert(0, launcher.DefaultPlayerBuild);

            if (presets.Count > MaxPresetCount)
                presets.RemoveRange(MaxPresetCount, presets.Count - MaxPresetCount);

            if (selectedBuild == null)
                selectedBuild = launcher != null ? launcher.DefaultPlayerBuild : presets.FirstOrDefault();
        }

        private void OnGUI()
        {
            if (!active || state == ScreenState.Hidden)
                return;

            EnsureStyles();

            float width = Mathf.Min(860f, Screen.width - 80f);
            float height = state == ScreenState.Splash ? 520f : 560f;
            Rect panel = new Rect((Screen.width - width) * 0.5f, (Screen.height - height) * 0.5f, width, height);
            GUI.Box(panel, GUIContent.none, panelStyle);

            GUILayout.BeginArea(new Rect(panel.x + 42f, panel.y + 34f, panel.width - 84f, panel.height - 68f));
            GUILayout.Label("RACE//FATAL", titleStyle);
            GUILayout.Space(6f);

            if (state == ScreenState.Splash)
                DrawSplash();
            else if (state == ScreenState.Loadout)
                DrawLoadout();
            else if (state == ScreenState.PostRace)
                DrawPostRace();

            GUILayout.EndArea();
        }

        private void DrawSplash()
        {
            GUILayout.Label("PUBLISHER PROTOTYPE", headingStyle);
            GUILayout.Space(20f);
            GUILayout.Label(
                "This build demonstrates the current racing/combat loop, AI, loadouts and race outcome flow. " +
                "Environment art, animation, audio, UI and visual effects remain in active development and are not representative of final quality.",
                bodyStyle);
            GUILayout.Space(24f);
            GUILayout.Label("CONTROLS", headingStyle);
            GUILayout.Space(8f);
            GUILayout.Label(
                "W / RT — Accelerate\nS / LT — Brake\nA / D / Left Stick — Steer\nShift / A — Boost\nMouse 1 / RB — Activate selected equipment\nQ / E — Cycle equipment\nTab — Cycle focus target",
                bodyStyle);
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("CONTINUE TO LOADOUT", buttonStyle, GUILayout.Height(56f)))
                state = ScreenState.Loadout;
        }

        private void DrawLoadout()
        {
            GUILayout.Label("SELECT A TEST BUILD", headingStyle);
            GUILayout.Space(10f);
            GUILayout.Label("Choose one preset, then launch directly into the prototype race.", bodyStyle);
            GUILayout.Space(20f);

            if (presets.Count == 0)
            {
                GUILayout.Label("No valid bike build definitions were found in the content catalog.", bodyStyle);
                return;
            }

            for (int i = 0; i < presets.Count; i++)
            {
                BikeBuildDefinitionSO build = presets[i];
                string label = BuildLabel(build, i);
                if (GUILayout.Button(label, buttonStyle, GUILayout.Height(62f)))
                {
                    selectedBuild = build;
                    LaunchSelected();
                    return;
                }

                GUILayout.Space(10f);
            }

            GUILayout.FlexibleSpace();
            if (GUILayout.Button("BACK", buttonStyle, GUILayout.Height(48f)))
                state = ScreenState.Splash;
        }

        private void DrawPostRace()
        {
            GUILayout.Label("PROTOTYPE COMPLETE", headingStyle);
            GUILayout.Space(18f);
            GUILayout.Label(
                selectedBuild != null
                    ? $"Last build: {selectedBuild.DisplayName}\n\nReplay immediately, or return to loadout selection to compare another build."
                    : "Replay immediately, or return to loadout selection to compare another build.",
                bodyStyle);
            GUILayout.FlexibleSpace();

            GUI.enabled = selectedBuild != null;
            if (GUILayout.Button("REPLAY SAME BUILD", buttonStyle, GUILayout.Height(58f)))
            {
                LaunchSelected();
                return;
            }
            GUI.enabled = true;

            GUILayout.Space(12f);
            if (GUILayout.Button("TRY ANOTHER BUILD", buttonStyle, GUILayout.Height(58f)))
                state = ScreenState.Loadout;
        }

        private void LaunchSelected()
        {
            if (launcher == null)
                launcher = GetComponent<PrototypeRaceLauncher>();
            if (launcher == null || selectedBuild == null)
            {
                Debug.LogError("[PublisherBuild] Cannot launch without a launcher and selected build.", this);
                return;
            }

            state = ScreenState.Hidden;
            launcher.LaunchPublisherRace(selectedBuild);
        }

        private static string BuildLabel(BikeBuildDefinitionSO build, int index)
        {
            if (build == null)
                return "UNAVAILABLE";

            string prefix;
            switch (index)
            {
                case 0: prefix = "BALANCED"; break;
                case 1: prefix = "AGGRESSIVE"; break;
                case 2: prefix = "HEAVY"; break;
                default: prefix = "BUILD"; break;
            }

            return $"{prefix}  //  {build.DisplayName}";
        }

        private void EnsureStyles()
        {
            if (titleStyle != null)
                return;

            Color lime = new Color(0.72f, 1f, 0f);
            Color white = new Color(0.95f, 0.97f, 1f);
            Color panel = new Color(0.025f, 0.035f, 0.055f, 0.98f);

            titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 42,
                fontStyle = FontStyle.Bold,
                normal = { textColor = white }
            };
            headingStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 22,
                fontStyle = FontStyle.Bold,
                normal = { textColor = lime }
            };
            bodyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                wordWrap = true,
                normal = { textColor = white }
            };
            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            panelStyle = new GUIStyle(GUI.skin.box);
            Texture2D background = new Texture2D(1, 1);
            background.SetPixel(0, 0, panel);
            background.Apply();
            panelStyle.normal.background = background;
        }

        private static void ReleaseCursor()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
