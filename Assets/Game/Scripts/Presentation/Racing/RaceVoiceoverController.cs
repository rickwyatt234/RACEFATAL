using System;
using System.Collections.Generic;
using RaceFatal.Career;
using RaceFatal.Presentation.Bootstrap;
using RaceFatal.Presentation.Combat;
using RaceFatal.Presentation.Tactics;
using RaceFatal.Racing;
using UnityEngine;

namespace RaceFatal.Presentation.Racing
{
    [DisallowMultipleComponent]
    public sealed class RaceVoiceoverController : MonoBehaviour
    {
        [Header("Race Voiceover Clips")]
        [SerializeField] private AudioClip energyLow;
        [SerializeField] private AudioClip finalLap;
        [SerializeField] private AudioClip hostileLock;
        [SerializeField] private AudioClip integrityCritical;
        [SerializeField] private AudioClip leadAcquired;
        [SerializeField] private AudioClip leadLost;
        [Tooltip("Overrides the start sequence's existing boost clip when assigned.")]
        [SerializeField] private AudioClip propulsionActivated;
        [SerializeField] private AudioClip raceComplete;
        [SerializeField] private AudioClip racerEliminated;
        [SerializeField] private AudioClip shieldsCritical;
        [SerializeField] private AudioClip shieldsRestored;
        [SerializeField] private AudioClip sponsorObjectiveComplete;
        [SerializeField] private AudioClip tacticalMode;
        [Tooltip("Overrides the start sequence's existing weapons clip when assigned.")]
        [SerializeField] private AudioClip weaponsActivated;
        [Header("Voiceover Mix")]
        [Range(0f, 1f)] [SerializeField] private float voiceVolume = 1f;
        [Tooltip("Other race audio is reduced to this fraction while speaking. Voice still respects the pre-duck listener volume and pause.")]
        [Range(0f, 1f)] [SerializeField] private float backgroundVolumeWhileSpeaking = .25f;
        [Min(.01f)] [SerializeField] private float duckFadeSeconds = .12f;
        [Min(0f)] [SerializeField] private float gapBetweenLines = .15f;
        [Header("Warnings")]
        [Tooltip("Fraction of starting energy capacity, including capacity lost to boost strain.")]
        [Range(.01f, .95f)] [SerializeField] private float lowEnergyThreshold = .2f;
        [Min(0f)] [SerializeField] private float repeatCooldownSeconds = 6f;

        private RaceRuntimeController runtime;
        private RaceDirector director;
        private RaceAnnouncementState state;
        private AudioSource voice;
        private TargetLockThreatReceiver threats;
        private readonly List<PendingLine> pending = new List<PendingLine>();
        private readonly Dictionary<RaceAnnouncement, float> lastQueued = new Dictionary<RaceAnnouncement, float>();
        private readonly List<BrandContract> contracts = new List<BrandContract>();
        private readonly HashSet<string> announcedContracts = new HashSet<string>();
        private bool playerDone, completeAnnounced, ownsListenerVolume;
        private float listenerBaseVolume, nextLineAt, audioClock;

        public void Initialize(RaceRuntimeController race)
        {
            Unsubscribe();
            StopVoice();
            runtime = race;
            director = race.Director;
            state = new RaceAnnouncementState();
            playerDone = completeAnnounced = false;
            threats = null;
            contracts.Clear();
            announcedContracts.Clear();
            lastQueued.Clear();
            // Snapshot the pre-race baseline; settlement may mutate the live contracts
            // before RaceCompleted is dispatched. This also avoids counting kills twice.
            var brands = BootstrapController.Context?.Sessions?.Current?.PlayerTeam?.Brands;
            if (brands != null)
                foreach (var c in brands.contracts)
                    if (c.active && !c.completed)
                        contracts.Add(new BrandContract { instanceId = c.instanceId, progress = c.progress, terms = c.terms.Copy() });
            director.RacerFinished += OnRacerFinished;
            director.RacerDestroyed += OnRacerDestroyed;
            director.RacerRetired += OnRacerRetired;
            director.RaceCompleted += OnRaceCompleted;
        }

        private void LateUpdate()
        {
            if (RacePauseController.IsGameplayBlocked) return;
            var player = director?.State.FindParticipant(runtime.PlayerRacerId);
            if (runtime != null && runtime.IsRaceActive && player != null &&
                player.Status == RaceParticipantStatus.Racing && !player.Vehicle.IsDestroyed)
            {
                if (threats == null && runtime.TryGetRacerView(runtime.PlayerRacerId, out var view))
                    threats = view.GetComponent<TargetLockThreatReceiver>();
                var vehicle = player.Vehicle;
                var energy = vehicle.EnergyPool;
                var shield = vehicle.EquipmentSystem.Shield;
                state.Sample(energy.StartingCapacity > 0f ? energy.CurrentEnergy / energy.StartingCapacity : 1f,
                    vehicle.Damage.CurrentIntegrity / vehicle.Damage.MaxIntegrity,
                    shield != null && shield.Maximum > 0f ? shield.Current / shield.Maximum : 1f,
                    shield != null && shield.Maximum > 0f, director.State.GetCurrentPosition(player.RacerId),
                    player.CompletedLaps, director.State.RaceDefinition.LapCount, director.State.Deathmatch == null,
                    threats != null && threats.HasHardLock, TacticalRaceController.IsActive, lowEnergyThreshold, Announce);
                CheckContracts(player);
            }
            audioClock += Time.unscaledDeltaTime;
            PumpAudio();
        }

        // Existing authored unlock clips remain valid without copying references.
        public void PlayEquipmentUnlock(bool propulsion, AudioClip fallback, float fallbackVolume)
        {
            var cue = propulsion ? RaceAnnouncement.PropulsionActivated : RaceAnnouncement.WeaponsActivated;
            var assigned = Clip(cue);
            Queue(cue, assigned != null ? assigned : fallback,
                assigned != null ? 1f : Mathf.Clamp01(fallbackVolume * (4f / 3f)));
        }

        private void Announce(RaceAnnouncement cue) => Queue(cue, Clip(cue), 1f);

        private void Queue(RaceAnnouncement cue, AudioClip clip, float volume)
        {
            if (!isActiveAndEnabled || clip == null || volume <= 0f || voiceVolume <= 0f) return;
            if (lastQueued.TryGetValue(cue, out float last) && audioClock - last < repeatCooldownSeconds) return;
            if (pending.Exists(p => p.Cue == cue)) return;
            lastQueued[cue] = audioClock;
            // The newest lead state supersedes an announcement waiting in the queue.
            if (cue == RaceAnnouncement.LeadAcquired || cue == RaceAnnouncement.LeadLost)
                pending.RemoveAll(p => p.Cue == RaceAnnouncement.LeadAcquired || p.Cue == RaceAnnouncement.LeadLost);
            pending.Add(new PendingLine { Cue = cue, Clip = clip, Volume = volume, QueuedAt = audioClock });
        }

        private void PumpAudio()
        {
            // Prune conditions that recovered before their warning could be spoken.
            pending.RemoveAll(p => (audioClock - p.QueuedAt > 10f && p.Cue != RaceAnnouncement.RaceComplete &&
                p.Cue != RaceAnnouncement.SponsorObjectiveComplete) || !StillRelevant(p.Cue));
            if ((voice == null || !voice.isPlaying) && audioClock >= nextLineAt && pending.Count > 0)
            {
                int best = 0;
                for (int i = 1; i < pending.Count; i++)
                    if (Priority(pending[i].Cue) > Priority(pending[best].Cue)) best = i;
                var line = pending[best];
                pending.RemoveAt(best);
                EnsureSource();
                if (!ownsListenerVolume)
                {
                    listenerBaseVolume = AudioListener.volume;
                    ownsListenerVolume = true;
                }
                voice.volume = listenerBaseVolume * voiceVolume * line.Volume;
                voice.clip = line.Clip;
                voice.Play();
                nextLineAt = audioClock + line.Clip.length + gapBetweenLines;
            }
            if (!ownsListenerVolume) return;
            bool speaking = voice != null && voice.isPlaying;
            float target = listenerBaseVolume * (speaking ? backgroundVolumeWhileSpeaking : 1f);
            AudioListener.volume = Mathf.MoveTowards(AudioListener.volume, target, Time.unscaledDeltaTime / Mathf.Max(.01f, duckFadeSeconds));
            if (!speaking && Mathf.Approximately(AudioListener.volume, listenerBaseVolume))
                RestoreMix();
        }

        private bool StillRelevant(RaceAnnouncement cue)
        {
            var p = director?.State.FindParticipant(runtime.PlayerRacerId);
            if (cue == RaceAnnouncement.RaceComplete || cue == RaceAnnouncement.SponsorObjectiveComplete) return true;
            if (p == null || playerDone || !runtime.IsRaceActive || p.Status != RaceParticipantStatus.Racing) return false;
            var shield = p.Vehicle.EquipmentSystem.Shield;
            switch (cue)
            {
                case RaceAnnouncement.HostileLock: return threats != null && threats.HasHardLock;
                case RaceAnnouncement.TacticalMode: return TacticalRaceController.IsActive;
                case RaceAnnouncement.EnergyLow: return p.Vehicle.EnergyPool.CurrentEnergy < p.Vehicle.EnergyPool.StartingCapacity * lowEnergyThreshold;
                case RaceAnnouncement.IntegrityCritical: return p.Vehicle.Damage.CurrentIntegrity < p.Vehicle.Damage.MaxIntegrity * .1f;
                case RaceAnnouncement.ShieldsCritical: return shield != null && shield.Maximum > 0f && shield.Current < shield.Maximum * .1f;
                case RaceAnnouncement.ShieldsRestored: return shield != null && shield.IsFullyCharged;
                case RaceAnnouncement.LeadAcquired: return director.State.GetCurrentPosition(p.RacerId) == 1;
                case RaceAnnouncement.LeadLost: return director.State.GetCurrentPosition(p.RacerId) != 1;
                default: return true;
            }
        }

        private void EnsureSource()
        {
            if (voice != null) return;
            var child = new GameObject("RaceVoiceoverAudio");
            child.transform.SetParent(transform, false);
            voice = child.AddComponent<AudioSource>();
            voice.playOnAwake = false;
            voice.loop = false;
            voice.spatialBlend = 0f;
            voice.priority = 0;
            voice.dopplerLevel = 0f;
            voice.pitch = 1f;
            voice.ignoreListenerPause = false;
            // Direct master output, independently scaled by the pre-duck master volume.
            voice.ignoreListenerVolume = true;
        }

        private void CheckContracts(RaceParticipant player)
        {
            var race = director.State.RaceDefinition;
            foreach (var c in contracts)
            {
                if (announcedContracts.Contains(c.instanceId)) continue;
                int position = player.FinishPosition > 0 ? player.FinishPosition : director.State.GetCurrentPosition(player.RacerId);
                if (BrandProgress.PreviewRaceProgress(c, race.Id, race.TrackId, player.Eliminations, player.Status, position) < c.terms.Target) continue;
                announcedContracts.Add(c.instanceId);
                Announce(RaceAnnouncement.SponsorObjectiveComplete);
            }
        }

        private void OnRacerFinished(RaceParticipant p)
        {
            if (p.RacerId != runtime.PlayerRacerId) return;
            CompleteAnnouncement();
            CheckContracts(p);
        }
        private void OnRacerDestroyed(RaceParticipant p)
        {
            if (p.RacerId == runtime.PlayerRacerId) { playerDone = true; pending.Clear(); }
            else if (!playerDone) Announce(RaceAnnouncement.RacerEliminated);
            var player = director.State.FindParticipant(runtime.PlayerRacerId);
            if (player != null) CheckContracts(player);
        }
        private void OnRacerRetired(RaceParticipant p)
        {
            if (p.RacerId == runtime.PlayerRacerId) { playerDone = true; pending.Clear(); }
        }
        private void OnRaceCompleted(RaceResult result)
        {
            CompleteAnnouncement();
            var player = director.State.FindParticipant(runtime.PlayerRacerId);
            if (player != null) CheckContracts(player);
        }
        private void CompleteAnnouncement()
        {
            if (completeAnnounced) return;
            completeAnnounced = playerDone = true;
            pending.RemoveAll(p => p.Cue != RaceAnnouncement.SponsorObjectiveComplete);
            if (voice != null) voice.Stop();
            nextLineAt = 0f;
            Announce(RaceAnnouncement.RaceComplete);
        }

        private static int Priority(RaceAnnouncement cue)
        {
            switch (cue)
            {
                case RaceAnnouncement.RaceComplete: return 100;
                case RaceAnnouncement.IntegrityCritical: return 90;
                case RaceAnnouncement.HostileLock: return 80;
                case RaceAnnouncement.ShieldsCritical: return 70;
                case RaceAnnouncement.FinalLap: return 60;
                case RaceAnnouncement.SponsorObjectiveComplete: return 55;
                default: return 10;
            }
        }
        private AudioClip Clip(RaceAnnouncement cue)
        {
            switch (cue)
            {
                case RaceAnnouncement.EnergyLow: return energyLow;
                case RaceAnnouncement.FinalLap: return finalLap;
                case RaceAnnouncement.HostileLock: return hostileLock;
                case RaceAnnouncement.IntegrityCritical: return integrityCritical;
                case RaceAnnouncement.LeadAcquired: return leadAcquired;
                case RaceAnnouncement.LeadLost: return leadLost;
                case RaceAnnouncement.PropulsionActivated: return propulsionActivated;
                case RaceAnnouncement.RaceComplete: return raceComplete;
                case RaceAnnouncement.RacerEliminated: return racerEliminated;
                case RaceAnnouncement.ShieldsCritical: return shieldsCritical;
                case RaceAnnouncement.ShieldsRestored: return shieldsRestored;
                case RaceAnnouncement.SponsorObjectiveComplete: return sponsorObjectiveComplete;
                case RaceAnnouncement.TacticalMode: return tacticalMode;
                case RaceAnnouncement.WeaponsActivated: return weaponsActivated;
                default: return null;
            }
        }
        private void RestoreMix()
        {
            if (!ownsListenerVolume) return;
            AudioListener.volume = listenerBaseVolume;
            ownsListenerVolume = false;
        }
        private void StopVoice()
        {
            pending.Clear();
            if (voice != null) voice.Stop();
            nextLineAt = 0f;
            RestoreMix();
        }
        private void Unsubscribe()
        {
            if (director == null) return;
            director.RacerFinished -= OnRacerFinished;
            director.RacerDestroyed -= OnRacerDestroyed;
            director.RacerRetired -= OnRacerRetired;
            director.RaceCompleted -= OnRaceCompleted;
        }
        private void OnDisable() { StopVoice(); }
        private void OnDestroy() { Unsubscribe(); StopVoice(); }

        private sealed class PendingLine
        {
            public RaceAnnouncement Cue;
            public AudioClip Clip;
            public float Volume, QueuedAt;
        }
    }
}
