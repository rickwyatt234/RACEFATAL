using System;
using System.Collections;
using System.Collections.Generic;
using RaceFatal.Equipment;
using RaceFatal.Presentation.Vehicles;
using RaceFatal.Racing;
using TMPro;
using UnityEngine;

namespace RaceFatal.Presentation.Racing
{
    public class RaceStartSequenceController : MonoBehaviour
    {
        [Header("Countdown")]
        [Min(1)][SerializeField] private int countdownFrom = 3;
        [Min(0f)][SerializeField] private float preCountdownDelay = 0.5f;
        [Min(0.05f)][SerializeField] private float countdownStepDuration = 1f;
        [Min(0f)][SerializeField] private float goDisplayDuration = 0.75f;

        [Header("Post-Start Equipment Locks")]
        [Tooltip("Time after GO before boosting becomes legal.")]
        [Min(0f)][SerializeField] private float boostUnlockDelay = 1.5f;

        [Tooltip("Time after GO before weapons become legal.")]
        [Min(0f)][SerializeField] private float weaponUnlockDelay = 3f;

        [Header("UI")]
        [SerializeField] private CanvasGroup countdownGroup;
        [SerializeField] private TMP_Text countdownText;
        [SerializeField] private string goText = "GO!";

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip countdownClip;
        [SerializeField] private AudioClip goClip;

        [Range(0f, 1f)]
        [SerializeField] private float countdownVolume = 1f;

        [Range(0f, 1f)]
        [SerializeField] private float goVolume = 1f;

        [Header("Runtime Debug")]
        [SerializeField] private bool debugRunning;
        [SerializeField] private bool debugRaceStarted;
        [SerializeField] private bool debugBoostAllowed;
        [SerializeField] private bool debugWeaponsAllowed;
        [SerializeField] private string debugDisplay = "";
        [SerializeField] private int debugCapturedDrivers;

        private readonly List<DriverState> driverStates =
            new List<DriverState>();

        private RaceRuntimeController raceRuntime;
        private Coroutine sequenceRoutine;
        private Coroutine boostRoutine;
        private Coroutine weaponRoutine;

        public bool IsRunning =>
            debugRunning;

        public bool RaceStarted =>
            debugRaceStarted;

        public bool BoostAllowed =>
            debugBoostAllowed;

        public bool WeaponsAllowed =>
            debugWeaponsAllowed;

        public event Action OnRaceStarted;
        public event Action OnBoostUnlocked;
        public event Action OnWeaponsUnlocked;

        private void Awake()
        {
            if (audioSource != null)
            {
                audioSource.playOnAwake =
                    false;

                audioSource.loop =
                    false;
            }

            HideCountdown();
        }

        public void Begin(
            RaceRuntimeController runtime)
        {
            if (debugRunning)
                return;

            if (runtime == null)
            {
                Debug.LogError(
                    $"{nameof(RaceStartSequenceController)} " +
                    "requires a RaceRuntimeController.",
                    this);

                return;
            }

            if (runtime.Director?.State == null)
            {
                Debug.LogError(
                    $"{nameof(RaceStartSequenceController)} " +
                    "requires an initialized race runtime.",
                    this);

                return;
            }

            if (runtime.HasStarted)
            {
                Debug.LogWarning(
                    "Race start sequence cannot begin because " +
                    "the race has already started.",
                    this);

                return;
            }

            raceRuntime =
                runtime;

            debugRunning = true;
            debugRaceStarted = false;
            debugBoostAllowed = false;
            debugWeaponsAllowed = false;

            SetEquipmentPermissions(
                allowBoost: false,
                allowWeapons: false);

            CaptureAndDisableDrivers();

            sequenceRoutine =
                StartCoroutine(
                    RunSequence());
        }

        private IEnumerator RunSequence()
        {
            ShowCountdown();

            if (preCountdownDelay > 0f)
            {
                yield return
                    new WaitForSecondsRealtime(
                        preCountdownDelay);
            }

            for (int value = countdownFrom;
                 value > 0;
                 value--)
            {
                SetCountdownText(
                    value.ToString());

                PlayClip(
                    countdownClip,
                    countdownVolume);

                yield return
                    new WaitForSecondsRealtime(
                        countdownStepDuration);
            }

            StartRace();

            SetCountdownText(
                goText);

            PlayClip(
                goClip,
                goVolume);

            if (boostUnlockDelay <= 0f)
            {
                UnlockBoost();
            }
            else
            {
                boostRoutine =
                    StartCoroutine(
                        UnlockBoostAfterDelay());
            }

            if (weaponUnlockDelay <= 0f)
            {
                UnlockWeapons();
            }
            else
            {
                weaponRoutine =
                    StartCoroutine(
                        UnlockWeaponsAfterDelay());
            }

            if (goDisplayDuration > 0f)
            {
                yield return
                    new WaitForSecondsRealtime(
                        goDisplayDuration);
            }

            HideCountdown();

            sequenceRoutine = null;
            debugRunning = false;
        }

        private void StartRace()
        {
            if (raceRuntime == null ||
                raceRuntime.HasStarted)
            {
                return;
            }

            raceRuntime.StartRace();

            RestoreDrivers();

            debugRaceStarted =
                true;

            OnRaceStarted?.Invoke();
        }

        private IEnumerator UnlockBoostAfterDelay()
        {
            yield return
                new WaitForSecondsRealtime(
                    boostUnlockDelay);

            UnlockBoost();

            boostRoutine = null;
        }

        private IEnumerator UnlockWeaponsAfterDelay()
        {
            yield return
                new WaitForSecondsRealtime(
                    weaponUnlockDelay);

            UnlockWeapons();

            weaponRoutine = null;
        }

        private void UnlockBoost()
        {
            SetBoostPermission(
                true);

            debugBoostAllowed =
                true;

            OnBoostUnlocked?.Invoke();
        }

        private void UnlockWeapons()
        {
            SetWeaponPermission(
                true);

            debugWeaponsAllowed =
                true;

            OnWeaponsUnlocked?.Invoke();
        }

        #region Equipment

        private void SetEquipmentPermissions(
            bool allowBoost,
            bool allowWeapons)
        {
            if (raceRuntime?.Director?.State == null)
                return;

            foreach (RaceParticipant participant
                     in raceRuntime.Director.State.Participants)
            {
                RaceEquipmentSystem equipment =
                    participant.Vehicle?
                        .EquipmentSystem;

                equipment?.SetRaceActivationPermissions(
                    allowBoost,
                    allowWeapons);
            }
        }

        private void SetBoostPermission(
            bool allowed)
        {
            if (raceRuntime?.Director?.State == null)
                return;

            foreach (RaceParticipant participant
                     in raceRuntime.Director.State.Participants)
            {
                RaceEquipmentSystem equipment =
                    participant.Vehicle?
                        .EquipmentSystem;

                if (equipment == null)
                    continue;

                equipment.SetRaceActivationPermissions(
                    allowed,
                    equipment.WeaponsAllowed);
            }
        }

        private void SetWeaponPermission(
            bool allowed)
        {
            if (raceRuntime?.Director?.State == null)
                return;

            foreach (RaceParticipant participant
                     in raceRuntime.Director.State.Participants)
            {
                RaceEquipmentSystem equipment =
                    participant.Vehicle?
                        .EquipmentSystem;

                if (equipment == null)
                    continue;

                equipment.SetRaceActivationPermissions(
                    equipment.BoostAllowed,
                    allowed);
            }
        }

        #endregion

        #region Driver Lock

        private void CaptureAndDisableDrivers()
        {
            driverStates.Clear();

            BikeRuntimeController[] bikes =
                FindObjectsByType<
                    BikeRuntimeController>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

            for (int i = 0;
                 i < bikes.Length;
                 i++)
            {
                BikeRuntimeController bike =
                    bikes[i];

                if (bike == null ||
                    !bike.IsDriverConfigured)
                {
                    continue;
                }

                driverStates.Add(
                    new DriverState(
                        bike,
                        bike.DriverControlEnabled));

                bike.SetDriverControlEnabled(
                    false);
            }

            debugCapturedDrivers =
                driverStates.Count;
        }

        private void RestoreDrivers()
        {
            for (int i = 0;
                 i < driverStates.Count;
                 i++)
            {
                DriverState state =
                    driverStates[i];

                if (state.Bike == null)
                    continue;

                state.Bike.SetDriverControlEnabled(
                    state.WasEnabled);
            }

            driverStates.Clear();

            debugCapturedDrivers =
                0;
        }

        #endregion

        #region UI

        private void ShowCountdown()
        {
            if (countdownGroup == null)
                return;

            countdownGroup.alpha =
                1f;

            countdownGroup.interactable =
                false;

            countdownGroup.blocksRaycasts =
                false;
        }

        private void HideCountdown()
        {
            if (countdownGroup != null)
            {
                countdownGroup.alpha =
                    0f;

                countdownGroup.interactable =
                    false;

                countdownGroup.blocksRaycasts =
                    false;
            }

            if (countdownText != null)
            {
                countdownText.text =
                    "";
            }

            debugDisplay =
                "";
        }

        private void SetCountdownText(
            string text)
        {
            if (countdownText != null)
            {
                countdownText.text =
                    text;
            }

            debugDisplay =
                text;
        }

        #endregion

        #region Audio

        private void PlayClip(
            AudioClip clip,
            float volume)
        {
            if (audioSource == null ||
                clip == null)
            {
                return;
            }

            audioSource.PlayOneShot(
                clip,
                Mathf.Clamp01(
                    volume));
        }

        #endregion

        private void OnDestroy()
        {
            RestoreDrivers();
        }

        private class DriverState
        {
            public BikeRuntimeController Bike {
                get;
            }

            public bool WasEnabled {
                get;
            }

            public DriverState(
                BikeRuntimeController bike,
                bool wasEnabled)
            {
                Bike =
                    bike;

                WasEnabled =
                    wasEnabled;
            }
        }
    }
}