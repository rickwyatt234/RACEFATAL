using UnityEngine;

namespace RaceFatal.Presentation.Combat
{
    public class WeaponMountFeedbackView : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Particle system played whenever this weapon successfully fires.")]
        [SerializeField] private ParticleSystem muzzleFlash;

        [Tooltip("Optional weapon firing audio source.")]
        [SerializeField] private AudioSource fireAudio;

        public void PlayFire()
        {
            if (muzzleFlash != null)
            {
                muzzleFlash.Stop(
                    true,
                    ParticleSystemStopBehavior.StopEmittingAndClear);

                muzzleFlash.Play(true);
            }

            if (fireAudio != null &&
                fireAudio.clip != null)
            {
                fireAudio.PlayOneShot(
                    fireAudio.clip);
            }
        }
    }
}