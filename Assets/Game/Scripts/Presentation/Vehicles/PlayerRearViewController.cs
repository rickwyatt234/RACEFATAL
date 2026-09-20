using UnityEngine;

namespace RaceFatal.Presentation.Vehicles
{
    public class PlayerRearViewController :
        MonoBehaviour
    {
        [Header("References")]

        [SerializeField]
        private Camera rearLeftCamera;

        [SerializeField]
        private Camera rearRightCamera;

        [SerializeField]
        private PlayerCockpitView cockpitView;

        [Header("Refresh Rate")]

        [Tooltip(
            "How many times per second each rear-view camera updates.")]
        [Range(1f, 60f)]
        [SerializeField]
        private float refreshRate =
            24f;

        [Tooltip(
            "Offsets the left and right camera renders so they do not both render on the same frame.")]
        [SerializeField]
        private bool staggerCameraUpdates =
            true;

        [Tooltip(
            "Immediately populate both RenderTextures when the player view becomes active.")]
        [SerializeField]
        private bool renderImmediately =
            true;

        [Header("Runtime Debug")]

        [SerializeField]
        private bool debugPlayerViewActive;

        [SerializeField]
        private int debugLeftRenderCount;

        [SerializeField]
        private int debugRightRenderCount;

        [SerializeField]
        private float debugSecondsPerUpdate;

        private double nextLeftRenderTime;
        private double nextRightRenderTime;

        private bool wasPlayerViewActive;

        private void Awake()
        {
            if (cockpitView == null)
            {
                cockpitView =
                    GetComponent<
                        PlayerCockpitView>();
            }

            if (cockpitView == null)
            {
                cockpitView =
                    GetComponentInParent<
                        PlayerCockpitView>();
            }

            /*
             * Critical:
             *
             * These cameras must NOT be enabled normally,
             * otherwise Unity will continue rendering them
             * automatically every frame.
             *
             * Camera.Render() still works on disabled
             * Camera components.
             */
            SetAutomaticRendering(
                false);

            ScheduleInitialRenders();
        }

        private void OnEnable()
        {
            SetAutomaticRendering(
                false);

            ScheduleInitialRenders();
        }

        private void LateUpdate()
        {
            /*
             * Use unscaled time so rear-camera refresh
             * frequency stays stable during Focus Mode
             * slow motion.
             */
            double now =
                Time.unscaledTimeAsDouble;

            bool playerViewActive =
                cockpitView != null &&
                cockpitView.IsActivePlayerView &&
                !cockpitView.IsDeathViewActive;

            debugPlayerViewActive =
                playerViewActive;

            if (!playerViewActive)
            {
                wasPlayerViewActive =
                    false;

                return;
            }

            /*
             * The cockpit may not become the active player
             * view until after this component awakens.
             *
             * When that happens, populate the feeds
             * immediately rather than waiting for the
             * first scheduled interval.
             */
            if (!wasPlayerViewActive)
            {
                HandlePlayerViewActivated(
                    now);

                wasPlayerViewActive =
                    true;
            }

            double interval =
                GetRenderInterval();

            debugSecondsPerUpdate =
                (float)interval;

            if (rearLeftCamera != null &&
                now >= nextLeftRenderTime)
            {
                RenderLeftCamera();

                /*
                 * Schedule from the current time rather
                 * than accumulating missed frames.
                 *
                 * If performance stalls, we don't want
                 * several rear renders firing at once
                 * trying to "catch up."
                 */
                nextLeftRenderTime =
                    now +
                    interval;
            }

            if (rearRightCamera != null &&
                now >= nextRightRenderTime)
            {
                RenderRightCamera();

                nextRightRenderTime =
                    now +
                    interval;
            }
        }

        private void HandlePlayerViewActivated(
            double now)
        {
            double interval =
                GetRenderInterval();

            if (renderImmediately)
            {
                RenderLeftCamera();

                /*
                 * If staggering is enabled, don't render
                 * the right camera on this exact frame.
                 * Give it half an interval offset.
                 */
                if (!staggerCameraUpdates)
                {
                    RenderRightCamera();
                }
            }

            nextLeftRenderTime =
                now +
                interval;

            nextRightRenderTime =
                staggerCameraUpdates
                    ? now +
                      interval *
                      0.5
                    : now +
                      interval;
        }

        private void ScheduleInitialRenders()
        {
            double now =
                Time.unscaledTimeAsDouble;

            double interval =
                GetRenderInterval();

            nextLeftRenderTime =
                now;

            nextRightRenderTime =
                staggerCameraUpdates
                    ? now +
                      interval *
                      0.5
                    : now;

            wasPlayerViewActive =
                false;
        }

        private double GetRenderInterval()
        {
            float safeRefreshRate =
                Mathf.Max(
                    1f,
                    refreshRate);

            return
                1.0 /
                safeRefreshRate;
        }

        private void RenderLeftCamera()
        {
            if (!CanRender(
                    rearLeftCamera))
            {
                return;
            }

            rearLeftCamera.Render();

            debugLeftRenderCount++;
        }

        private void RenderRightCamera()
        {
            if (!CanRender(
                    rearRightCamera))
            {
                return;
            }

            rearRightCamera.Render();

            debugRightRenderCount++;
        }

        private bool CanRender(
            Camera camera)
        {
            if (camera == null)
                return false;

            if (!camera.gameObject
                    .activeInHierarchy)
            {
                return false;
            }

            /*
             * A rear camera without a RenderTexture
             * should never accidentally render to the
             * player's display.
             */
            if (camera.targetTexture == null)
            {
                return false;
            }

            return true;
        }

        private void SetAutomaticRendering(
            bool enabled)
        {
            if (rearLeftCamera != null)
            {
                rearLeftCamera.enabled =
                    enabled;
            }

            if (rearRightCamera != null)
            {
                rearRightCamera.enabled =
                    enabled;
            }
        }

        private void OnDisable()
        {
            SetAutomaticRendering(
                false);

            wasPlayerViewActive =
                false;
        }
    }
}