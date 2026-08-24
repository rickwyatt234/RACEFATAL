using RaceFatal.Content.Tracks;
using RaceFatal.Racing;
using RaceFatal.Shared;
using UnityEngine;

namespace RaceFatal.Content.Racing
{
    [CreateAssetMenu(fileName = "RaceDefinition", menuName = "RaceFatal/Racing/Race")]
    public class RaceDefinitionSO : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string id;

        [SerializeField] private string displayName;

        [Header("Race")]
        [SerializeField] private TrackDefinitionSO track;

        [SerializeField] private EngineClass engineClass;

        [Min(1)]
        [SerializeField] private int lapCount = 3;

        [Min(1)]
        [SerializeField] private int entrantCount = 12;

        [Min(1)]
        [SerializeField] private int teamSize = 2;

        public RaceDefinition CreateRaceDefinition()
        {
            return new RaceDefinition(
                id,
                displayName,
                track.CreateTrackDefinition().Id,
                engineClass,
                lapCount,
                entrantCount,
                teamSize);
        }
    }
}