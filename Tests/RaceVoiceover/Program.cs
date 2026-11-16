using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RaceFatal.Career;
using RaceFatal.Presentation.Racing;
using RaceFatal.Racing;
using UnityEngine;

static class Program
{
    private static int checks;
    static void Check(bool condition, string name)
    {
        if (!condition) throw new Exception(name);
        checks++;
    }
    static void Main()
    {
        var state = new RaceAnnouncementState();
        var cues = new List<RaceAnnouncement>();
        void Sample(float energy = 1f, float integrity = 1f, float shields = 1f, bool hasShield = true,
            int position = 2, int laps = 0, bool lapRace = true, bool locked = false, bool tactical = false) =>
            state.Sample(energy, integrity, shields, hasShield, position, laps, 3, lapRace, locked, tactical, .2f, cues.Add);
        Sample();
        Check(cues.Count == 0, "No spawn warnings or restored-shield cue");
        Sample(energy: .2f, integrity: .1f, shields: .1f);
        Check(cues.Count == 0, "Exact critical thresholds do not trigger");
        Sample(energy: .19f, integrity: .09f, shields: .09f, locked: true, tactical: true);
        Check(cues.Count == 5 && cues.Contains(RaceAnnouncement.HostileLock), "All threshold and mode edges");
        cues.Clear();
        Sample(energy: .19f, integrity: .09f, shields: 0f, locked: true, tactical: true);
        Check(cues.Count == 0, "Sustained low values, depleted shield and held modes do not repeat");
        Sample(energy: .21f, integrity: .11f, shields: .11f, locked: true, tactical: true);
        Sample(energy: .19f, integrity: .09f, shields: .09f, locked: true, tactical: true);
        Check(cues.Count == 0, "Hysteresis prevents boundary chatter");
        Sample(); cues.Clear(); Sample(energy: .19f, integrity: .09f, shields: .09f);
        Check(cues.Count == 3, "Warnings rearm after meaningful recovery");
        cues.Clear(); Sample();
        Check(cues.SequenceEqual(new[] { RaceAnnouncement.ShieldsRestored }), "Restored only after damage and exact full recharge");
        cues.Clear(); Sample(); Sample(hasShield: false, shields: 0f);
        Check(cues.Count == 0, "No repeat full or missing-shield warnings");
        Sample(position: 1); Sample(position: 1); Sample(position: 2);
        Check(cues.SequenceEqual(new[] { RaceAnnouncement.LeadAcquired, RaceAnnouncement.LeadLost }), "Actual lead transitions only");
        cues.Clear(); Sample(laps: 2); Sample(laps: 2);
        Check(cues.SequenceEqual(new[] { RaceAnnouncement.FinalLap }), "Final lap once");
        cues.Clear(); state = new RaceAnnouncementState(); Sample(laps: 2, lapRace: false);
        Check(cues.Count == 0, "Deathmatch has no final lap");
        state = new RaceAnnouncementState();
        state.Sample(1, 1, 1, false, 1, 0, 1, true, false, false, .2f, cues.Add);
        Check(cues.SequenceEqual(new[] { RaceAnnouncement.FinalLap }), "Single lap event announces its only lap, not a lead acquisition");

        var contract = new BrandContract { progress = 2, active = true, terms = new BrandContractTemplate {
            Id = "test", Title = "Test", Objective = BrandObjective.DestroyRacers, Target = 3, RaceId = "race", TrackId = "track" } };
        int Preview(int kills, RaceParticipantStatus status = RaceParticipantStatus.Racing, int position = 1, string track = "track") =>
            BrandProgress.PreviewRaceProgress(contract, "race", track, kills, status, position);
        Check(Preview(1) == 3 && Preview(1) == 3 && contract.progress == 2 && contract.active && !contract.completed,
            "Live kill projection is idempotent and does not mutate settlement state");
        Check(Preview(3, track: "wrong") == 2, "Track restrictions honored");
        foreach (var objective in new[] { BrandObjective.FinishRaces, BrandObjective.WinRaces, BrandObjective.PodiumFinishes, BrandObjective.ConsecutivePodiums })
        {
            contract.terms.Objective = objective;
            Check(Preview(9) == 2, objective + " cannot complete before finishing");
            Check(Preview(0, RaceParticipantStatus.Finished) == 3, objective + " completes on finish");
        }
        contract.terms.Objective = BrandObjective.ConsecutivePodiums;
        Check(Preview(0, RaceParticipantStatus.Retired) == 0 && Preview(0, RaceParticipantStatus.Finished, 4) == 0, "Streak resets on failure");

        // Exercise the production playback queue and mix with minimal audio doubles.
        var controller = new RaceVoiceoverController();
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        void Set(string name, object value) => typeof(RaceVoiceoverController).GetField(name, flags).SetValue(controller, value);
        T Get<T>(string name) => (T)typeof(RaceVoiceoverController).GetField(name, flags).GetValue(controller);
        void Call(string name) => typeof(RaceVoiceoverController).GetMethod(name, flags).Invoke(controller, null);
        Set("raceComplete", new AudioClip());
        Set("sponsorObjectiveComplete", new AudioClip());
        AudioListener.volume = .8f;
        Call("CompleteAnnouncement");
        Call("PumpAudio");
        var voice = Get<AudioSource>("voice");
        Check(voice.isPlaying && voice.ignoreListenerVolume && !voice.ignoreListenerPause && voice.spatialBlend == 0f && voice.pitch == 1f,
            "Voice uses centered normal-pitch audio and respects pause");
        Check(Math.Abs(voice.volume - .8f) < .001f && Math.Abs(AudioListener.volume - .2f) < .001f,
            "Full voice respects master and other audio ducks to 25 percent");
        Call("CompleteAnnouncement");
        Check(Get<IList>("pending").Count == 0, "Race completion never repeats");
        RacePauseController.IsGameplayBlocked = true;
        float clock = Get<float>("audioClock");
        Call("LateUpdate");
        Check(clock == Get<float>("audioClock") && voice.isPlaying, "Pause freezes queue age and does not dequeue or interrupt voice");
        RacePauseController.IsGameplayBlocked = false;
        Call("OnDisable");
        Check(!voice.isPlaying && Math.Abs(AudioListener.volume - .8f) < .001f, "Disable stops voice and restores original mix");
        Call("OnDestroy");
        Check(Math.Abs(AudioListener.volume - .8f) < .001f, "Repeated cleanup preserves original mix");
        controller.PlayEquipmentUnlock(true, null, .75f);
        Check(Get<IList>("pending").Count == 0, "Unassigned clips are silent and do not duck audio");
        Console.WriteLine($"Passed {checks} race voiceover checks.");
    }
}
