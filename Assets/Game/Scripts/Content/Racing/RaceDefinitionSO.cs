using System;
using RaceFatal.Content.Tracks;
using RaceFatal.Racing;
using RaceFatal.Shared;
using UnityEngine;

namespace RaceFatal.Content.Racing
{
    [CreateAssetMenu(fileName = "RaceDefinition", menuName = "RaceFatal/Racing/Race")]
    public class RaceDefinitionSO : ScriptableObject
    {
        [Header("Identity")] [SerializeField] private string id;
        public string Id => id;

        [SerializeField] private string displayName;
        [Header("Race")] [SerializeField] private TrackDefinitionSO track;
        [SerializeField] private EngineClass engineClass;
        [Min(1)] [SerializeField] private int lapCount = 3;
        [Min(1)] [SerializeField] private int entrantCount = 12;
        [Min(1)] [SerializeField] private int teamSize = 2;
        [Header("Research reward")] [Tooltip("Bonus RP for a finalized race, including a resolved DNF.")] [Min(0)] [SerializeField] private
            int researchPointBonus;
        [Header("Deathmatch (laps do not end this event)")] [SerializeField] private bool deathmatch;
        [SerializeField] private DeathmatchVictoryMode victoryMode = DeathmatchVictoryMode.Team;
        [Min(1)] [SerializeField] private int allowedWinners = 1;
        [Tooltip("Allow tied timeout ranks to exceed winner slots. Disabled: break ties by course distance, then original grid order.")] [SerializeField] private
            bool shareTimeoutTies;
        [Min(1)] [SerializeField] private float timeLimitSeconds = 300;
        [Min(0)] [SerializeField] private float minimumSpeedKph = 30;
        [Min(0)] [SerializeField] private float startGraceSeconds = 15;
        [Min(0.1f)] [SerializeField] private float belowSpeedGraceSeconds = 8;
        public RaceDefinition CreateRaceDefinition()
        {
            if (!TryCreateRaceDefinition(out var definition, out var error))
                throw new InvalidOperationException(error);
            return definition;
        }

        public bool TryCreateRaceDefinition(out RaceDefinition definition, out string error)
        {
            definition = null;
            string asset = $"Race asset '{name}' (ID '{id}')";
            if (track == null)
            {
                error = $"{asset} has no resolved Track reference. Assign a TrackDefinition asset in its Track field.";
                return false;
            }
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(track.Id))
            {
                error = $"{asset} needs a non-empty race ID and track ID.";
                return false;
            }
            try
            {
                definition = new RaceDefinition(id, displayName, track.Id, engineClass, lapCount, entrantCount, teamSize, researchPointBonus, deathmatch ? new DeathmatchRules(victoryMode, allowedWinners, timeLimitSeconds, minimumSpeedKph, startGraceSeconds, belowSpeedGraceSeconds, shareTimeoutTies) : null);
                error = null;
                return true;
            }
            catch (ArgumentException exception)
            {
                error = $"{asset} has invalid race rules: {exception.Message}";
                return false;
            }
        }
    }
}
