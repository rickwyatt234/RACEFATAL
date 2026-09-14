using UnityEngine;

namespace RaceFatal.Presentation.Combat
{
    [CreateAssetMenu(
        fileName = "CountermeasureRuntimeProfile",
        menuName = "RaceFatal/Combat/Countermeasure Runtime Profile")]
    public class CountermeasureRuntimeProfileSO : ScriptableObject
    {
        [Header("Definition")]
        [Tooltip("Must exactly match the installed CountermeasureDefinition ID.")]
        [SerializeField] private string definitionId;

        [Header("Flare Presentation")]
        [SerializeField] private CountermeasureFlareView flarePrefab;

        [Tooltip("Number of visual flare objects spawned by one deployment.")]
        [Min(1)][SerializeField] private int flareCount = 2;

        [Tooltip("Initial speed added to each visual flare.")]
        [Min(0f)][SerializeField] private float flareEjectionSpeed = 30f;

        [Tooltip("Maximum random horizontal spread of the visual flares.")]
        [Range(0f, 60f)][SerializeField] private float horizontalSpread = 15f;

        [Tooltip("Upward launch angle of the visual flares.")]
        [Range(-30f, 60f)][SerializeField] private float upwardAngle = 8f;

        [Tooltip("Lifetime of each spawned visual flare.")]
        [Min(0.1f)][SerializeField] private float flareLifetime = 1.5f;

        public string DefinitionId =>
            definitionId;

        public CountermeasureFlareView FlarePrefab =>
            flarePrefab;

        public int FlareCount =>
            flareCount;

        public float FlareEjectionSpeed =>
            flareEjectionSpeed;

        public float HorizontalSpread =>
            horizontalSpread;

        public float UpwardAngle =>
            upwardAngle;

        public float FlareLifetime =>
            flareLifetime;

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(
                    definitionId))
            {
                Debug.LogWarning(
                    $"{nameof(CountermeasureRuntimeProfileSO)} " +
                    $"'{name}' has no Definition ID.",
                    this);
            }
        }
    }
}