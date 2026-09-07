using RaceFatal.Tracks;
using UnityEngine;

namespace RaceFatal.Content.Tracks
{
    [CreateAssetMenu(
        fileName = "TrackDefinition",
        menuName = "RaceFatal/Racing/Track")]
    public sealed class TrackDefinitionSO :
        ScriptableObject
    {
        [Header("Identity")]
        [SerializeField]
        private string id;

        [SerializeField]
        private string displayName;

        [Header("Scene Content")]
        [SerializeField]
        private GameObject trackPrefab;

        public string Id =>
            id;

        public string DisplayName =>
            displayName;

        public GameObject TrackPrefab =>
            trackPrefab;

        public TrackDefinition CreateTrackDefinition()
        {
            return new TrackDefinition(
                id,
                displayName);
        }
    }
}