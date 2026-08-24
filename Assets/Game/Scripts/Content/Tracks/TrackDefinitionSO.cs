using RaceFatal.Tracks;
using UnityEngine;

namespace RaceFatal.Content.Tracks
{
    [CreateAssetMenu(
        fileName = "TrackDefinition",
        menuName = "RaceFatal/Racing/Track")]
    public sealed class TrackDefinitionSO : ScriptableObject
    {
        [SerializeField] private string id;

        [SerializeField] private string displayName;

        public TrackDefinition CreateTrackDefinition()
        {
            return new TrackDefinition(
                id,
                displayName
            );
        }
    }
}