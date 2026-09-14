using System;
using System.Collections.Generic;
using UnityEngine;

namespace RaceFatal.Presentation.Combat
{
    [CreateAssetMenu(
        fileName = "CountermeasureRuntimeCatalog",
        menuName = "RACE FATAL/Combat/Countermeasure Runtime Catalog")]
    public class CountermeasureRuntimeCatalogSO : ScriptableObject
    {
        [SerializeField] private List<CountermeasureRuntimeProfileSO> profiles =
            new List<CountermeasureRuntimeProfileSO>();

        public IReadOnlyList<CountermeasureRuntimeProfileSO> Profiles =>
            profiles;

        public bool TryGetProfile(
            string definitionId,
            out CountermeasureRuntimeProfileSO profile)
        {
            profile = null;

            if (string.IsNullOrWhiteSpace(
                    definitionId))
            {
                return false;
            }

            for (int i = 0;
                 i < profiles.Count;
                 i++)
            {
                CountermeasureRuntimeProfileSO candidate =
                    profiles[i];

                if (candidate == null)
                    continue;

                if (!string.Equals(
                        candidate.DefinitionId,
                        definitionId,
                        StringComparison.Ordinal))
                {
                    continue;
                }

                profile = candidate;
                return true;
            }

            return false;
        }

        private void OnValidate()
        {
            for (int i = 0;
                 i < profiles.Count;
                 i++)
            {
                CountermeasureRuntimeProfileSO first =
                    profiles[i];

                if (first == null)
                    continue;

                for (int j = i + 1;
                     j < profiles.Count;
                     j++)
                {
                    CountermeasureRuntimeProfileSO second =
                        profiles[j];

                    if (second == null)
                        continue;

                    if (!string.Equals(
                            first.DefinitionId,
                            second.DefinitionId,
                            StringComparison.Ordinal))
                    {
                        continue;
                    }

                    Debug.LogWarning(
                        $"{nameof(CountermeasureRuntimeCatalogSO)} " +
                        $"'{name}' contains multiple profiles for " +
                        $"'{first.DefinitionId}'.",
                        this);

                    return;
                }
            }
        }
    }
}