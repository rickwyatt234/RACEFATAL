using UnityEngine;
using UnityEngine.EventSystems;

namespace RaceFatal.Presentation.Racing
{
    // Runs before race input, combat and focus-mode updates.
    [DefaultExecutionOrder(-10000)]
    [DisallowMultipleComponent]
    public sealed class RacePauseController : MonoBehaviour
    {
        [SerializeField] private KeyCode pauseKey = KeyCode.Escape;
        [SerializeField] private KeyCode hideMenuKey = KeyCode.F1;
        private static RacePauseController owner;
        private static int resumeFrame = -1;
        private RaceRuntimeController runtime;
        private RacePauseView view;
        private float savedTimeScale;
        private float savedFixedDeltaTime;
        private bool savedAudioPause;
        private CursorLockMode savedCursorLock;
        private bool savedCursorVisible;
        private GameObject savedSelection;

        public static bool IsPaused => owner != null;
        public static bool IsGameplayBlocked => IsPaused || Time.frameCount == resumeFrame;
        public bool IsMenuVisible => owner == this && view != null && view.IsVisible;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            owner = null;
            resumeFrame = -1;
        }

        private void Awake() => runtime = GetComponent<RaceRuntimeController>();

        private void Update()
        {
            if (Input.GetKeyDown(pauseKey))
            {
                if (owner != this)
                    Pause();
                else if (!view.IsVisible)
                    view.ShowMenu();
                else if (view.IsConfirmingQuit)
                    view.ShowMenu();
                else
                    Resume();
            }
            else if (Input.GetKeyDown(hideMenuKey))
            {
                if (owner != this)
                    Pause();
                if (owner == this)
                    ToggleMenu();
            }
        }

        private void LateUpdate()
        {
            if (owner != this)
                return;
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = view.IsVisible;
        }

        public void Pause()
        {
            if (IsPaused || !isActiveAndEnabled || runtime == null || !runtime.IsInitialized || runtime.Director.State.IsFinished)
                return;

            savedSelection = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            if (view == null)
            {
                var root = new GameObject("RacePauseUI", typeof(RectTransform));
                root.transform.SetParent(transform, false);
                view = root.AddComponent<RacePauseView>();
                view.Initialize(this, pauseKey, hideMenuKey);
            }

            savedTimeScale = Time.timeScale;
            savedFixedDeltaTime = Time.fixedDeltaTime;
            savedAudioPause = AudioListener.pause;
            savedCursorLock = Cursor.lockState;
            savedCursorVisible = Cursor.visible;
            owner = this;
            Time.timeScale = 0f;
            // Keep a positive fixed timestep; focus mode's exact value is restored on resume.
            AudioListener.pause = true;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            view.ShowMenu();
        }

        public void Resume()
        {
            if (owner != this)
                return;

            // Releases while in the menu must not leave a held weapon firing or discharge a charged shot.
            if (runtime != null && runtime.TryGetRacerView(runtime.PlayerRacerId, out var player) && player != null)
                player.Participant?.Vehicle?.EquipmentSystem.CancelSelectedActivation();

            owner = null;
            resumeFrame = Time.frameCount;
            Time.timeScale = savedTimeScale;
            Time.fixedDeltaTime = savedFixedDeltaTime;
            AudioListener.pause = savedAudioPause;
            Cursor.lockState = savedCursorLock;
            Cursor.visible = savedCursorVisible;
            if (view != null)
                view.Close();
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(savedSelection != null && savedSelection.activeInHierarchy ? savedSelection : null);
            savedSelection = null;
        }

        public void ToggleMenu()
        {
            if (owner != this)
                return;
            if (view.IsVisible)
                view.HideMenu();
            else
                view.ShowMenu();
            Cursor.visible = view.IsVisible;
        }

        // A focus controller disabled during pause must not restart time or leave slow motion stuck on resume.
        public static void RestoreGameplayTime(float timeScale, float fixedDeltaTime)
        {
            if (IsPaused)
            {
                owner.savedTimeScale = timeScale;
                owner.savedFixedDeltaTime = fixedDeltaTime;
                return;
            }
            Time.timeScale = timeScale;
            Time.fixedDeltaTime = fixedDeltaTime;
        }

        public void QuitGame()
        {
            if (owner != this || !view.IsConfirmingQuit)
                return;
            Resume();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void OnDisable() => Resume();

        private void OnDestroy()
        {
            Resume();
            if (view != null)
                Destroy(view.gameObject);
        }
    }
}
