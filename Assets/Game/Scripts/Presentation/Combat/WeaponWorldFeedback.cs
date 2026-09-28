using UnityEngine;

namespace RaceFatal.Presentation.Combat
{
    public static class WeaponWorldFeedback
    {
        public static void Play(
            WeaponPresentationProfile profile,
            Vector3 position,
            Quaternion rotation)
        {
            if (profile == null)
                return;

            if (profile.ImpactPrefab != null)
            {
                GameObject effect =
                    Object.Instantiate(
                        profile.ImpactPrefab,
                        position,
                        rotation);

                Object.Destroy(
                    effect,
                    ResolveLifetime(
                        effect,
                        profile.ImpactFallbackLifetime));
            }

            AudioClip[] clips =
                profile.ImpactClips;

            if (clips == null ||
                clips.Length == 0)
            {
                return;
            }

            AudioClip clip =
                SelectClip(
                    clips);

            if (clip == null)
                return;

            GameObject audioObject =
                new GameObject(
                    "Weapon Impact Audio");

            audioObject.transform.position =
                position;

            AudioSource source =
                audioObject.AddComponent<
                    AudioSource>();

            source.playOnAwake = false;
            source.loop = false;
            source.clip = clip;
            source.volume =
                Mathf.Clamp01(
                    profile.ImpactVolume);

            source.pitch =
                Random.Range(
                    Mathf.Min(
                        profile.MinimumFirePitch,
                        profile.MaximumFirePitch),
                    Mathf.Max(
                        profile.MinimumFirePitch,
                        profile.MaximumFirePitch));

            source.spatialBlend =
                profile.FireSpatialBlend;

            source.rolloffMode =
                profile.FireRolloffMode;

            source.minDistance =
                profile.FireMinDistance;

            source.maxDistance =
                Mathf.Max(
                    profile.FireMinDistance,
                    profile.FireMaxDistance);

            source.dopplerLevel =
                profile.FireDopplerLevel;

            source.Play();

            Object.Destroy(
                audioObject,
                clip.length /
                Mathf.Max(
                    0.1f,
                    Mathf.Abs(
                        source.pitch)) +
                0.1f);
        }

        private static AudioClip SelectClip(
            AudioClip[] clips)
        {
            int validCount = 0;

            for (int i = 0;
                 i < clips.Length;
                 i++)
            {
                if (clips[i] != null)
                    validCount++;
            }

            if (validCount <= 0)
                return null;

            int ordinal =
                Random.Range(
                    0,
                    validCount);

            for (int i = 0;
                 i < clips.Length;
                 i++)
            {
                if (clips[i] == null)
                    continue;

                if (ordinal == 0)
                    return clips[i];

                ordinal--;
            }

            return null;
        }

        private static float ResolveLifetime(
            GameObject effect,
            float fallback)
        {
            if (effect == null)
                return Mathf.Max(0.01f, fallback);

            ParticleSystem[] systems =
                effect.GetComponentsInChildren<
                    ParticleSystem>(true);

            float longest = 0f;

            for (int i = 0;
                 i < systems.Length;
                 i++)
            {
                ParticleSystem system =
                    systems[i];

                if (system == null)
                    continue;

                ParticleSystem.MainModule main =
                    system.main;

                float lifetime =
                    main.duration +
                    main.startDelay.constantMax +
                    main.startLifetime.constantMax;

                longest =
                    Mathf.Max(
                        longest,
                        lifetime);
            }

            return longest > 0f
                ? longest + 0.2f
                : Mathf.Max(0.01f, fallback);
        }
    }
}
