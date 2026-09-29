using RaceFatal.Racing;
using TMPro;
using UnityEngine;

namespace RaceFatal.Presentation.Vehicles
{
    public sealed class CockpitAudienceHUD : MonoBehaviour
    {
        private AudienceEqualizerGraphic equalizer;
        private TextMeshProUGUI favorText, averageText;
        private AudioSource cheers, boos;
        private RaceDirector director;
        private RaceParticipant participant;
        private float shownFavor = 100f, masterVolume;

        public void Initialize(RaceDirector raceDirector, RaceParticipant player, TMP_FontAsset font, AudioClip cheering, AudioClip booing, float volume)
        {
            director = raceDirector;
            participant = player;
            masterVolume = Mathf.Clamp01(volume);
            if (equalizer == null)
            {
                equalizer = CreateRect("Crowd Equalizer", new Vector2(0f, 0.28f), new Vector2(1f, 0.7f)).gameObject.AddComponent<AudienceEqualizerGraphic>();
                equalizer.raycastTarget = false;
                favorText = CreateText("Audience Favor", new Vector2(0f, 0.72f), Vector2.one, font, 18f);
                averageText = CreateText("Average Fame", Vector2.zero, new Vector2(1f, 0.25f), font, 13f);
                cheers = CreateAudio(cheering);
                boos = CreateAudio(booing);
            }
            Refresh(0f);
        }

        private RectTransform CreateRect(string objectName, Vector2 minimum, Vector2 maximum)
        {
            var rect = new GameObject(objectName, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(transform, false);
            rect.gameObject.layer = gameObject.layer;
            rect.anchorMin = minimum; rect.anchorMax = maximum;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            return rect;
        }

        private TextMeshProUGUI CreateText(string objectName, Vector2 minimum, Vector2 maximum, TMP_FontAsset font, float size)
        {
            var label = CreateRect(objectName, minimum, maximum).gameObject.AddComponent<TextMeshProUGUI>();
            if (font != null) label.font = font;
            label.fontSize = size;
            label.color = new Color(0.65f, 0.95f, 1f);
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;
            return label;
        }

        private AudioSource CreateAudio(AudioClip clip)
        {
            if (clip == null) return null;
            var source = gameObject.AddComponent<AudioSource>();
            source.clip = clip; source.loop = true; source.playOnAwake = false;
            source.spatialBlend = 0f; source.volume = 0f;
            return source;
        }

        private void Update() => Refresh(Time.deltaTime);

        private void Refresh(float deltaTime)
        {
            if (director == null || equalizer == null) return;
            if (Time.timeScale <= 0f)
            {
                if (cheers != null) cheers.Pause();
                if (boos != null) boos.Pause();
                return;
            }
            RaceAudience audience = director.Audience;
            bool active = director.State.IsStarted && !director.State.IsFinished && participant?.Status == RaceParticipantStatus.Racing;
            shownFavor = Mathf.Lerp(shownFavor, audience.Favor, 1f - Mathf.Exp(-8f * deltaTime));
            equalizer.SetAudience(shownFavor, active ? Time.time : 0f);
            favorText.text = $"AUDIENCE // {audience.Favor:0}/200";
            averageText.text = $"AVG {audience.AverageFavor:0}  FAME {(audience.FameMultiplier - 1f) * 100f:+0;-0;0}%";
            UpdateAudio(cheers, active ? Mathf.InverseLerp(40f, 200f, shownFavor) : 0f, active, deltaTime);
            UpdateAudio(boos, active ? 1f - Mathf.InverseLerp(0f, 85f, shownFavor) : 0f, active, deltaTime);
        }

        private void UpdateAudio(AudioSource source, float amount, bool active, float deltaTime)
        {
            if (source == null) return;
            if (active && !source.isPlaying)
            {
                source.UnPause();
                if (!source.isPlaying) source.Play();
            }
            source.volume = Mathf.MoveTowards(source.volume, amount * masterVolume, deltaTime * 0.5f);
            if (!active && source.volume <= 0f) source.Stop();
        }

        private void OnDisable()
        {
            if (cheers != null) { cheers.Stop(); cheers.volume = 0f; }
            if (boos != null) { boos.Stop(); boos.volume = 0f; }
        }
    }
}
