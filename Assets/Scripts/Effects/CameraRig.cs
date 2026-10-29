using UnityEngine;
using UnityEngine.UI;

namespace BustAKlud
{
    /// <summary>
    /// Keeps the whole 16:9 play area (board plus side panels) in view at any window shape, keeps the UI
    /// canvas scaled to match, and provides screen shake.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraRig : MonoBehaviour
    {
        public const float DesignWidth = 12.8f;
        public const float DesignHeight = 7.2f;

        public static CameraRig Instance { get; private set; }

        Camera cam;
        CanvasScaler scaler;
        int lastWidth;
        int lastHeight;
        float shakeAmplitude;
        float shakeDuration;
        float shakeTime;

        public float VisibleHeight => Setup().orthographicSize * 2f;
        public float VisibleWidth => VisibleHeight * Setup().aspect;

        void Awake()
        {
            Setup();
        }

        // Awake order between components isn't guaranteed, and the bootstrap queries this rig during its own
        // Awake, so every entry point goes through this idempotent setup.
        Camera Setup()
        {
            if (cam != null)
                return cam;

            Instance = this;
            cam = GetComponent<Camera>();
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.04f, 0.02f, 0.08f);
            transform.position = new Vector3(0f, 0f, -10f);
            Fit();
            return cam;
        }

        public void BindScaler(CanvasScaler canvasScaler)
        {
            Setup();
            scaler = canvasScaler;
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            lastWidth = 0;
            Fit();
        }

        public void Shake(float amplitude, float duration)
        {
            if (amplitude < shakeAmplitude * (1f - shakeTime / Mathf.Max(0.01f, shakeDuration)))
                return;
            shakeAmplitude = amplitude;
            shakeDuration = duration;
            shakeTime = 0f;
        }

        void LateUpdate()
        {
            if (Screen.width != lastWidth || Screen.height != lastHeight)
                Fit();

            Vector3 offset = Vector3.zero;
            if (shakeTime < shakeDuration)
            {
                shakeTime += Time.unscaledDeltaTime;
                float remaining = Mathf.Clamp01(1f - shakeTime / shakeDuration);
                offset = (Vector3)(Random.insideUnitCircle * (shakeAmplitude * remaining * remaining));
            }

            transform.position = new Vector3(offset.x, offset.y, -10f);
        }

        void Fit()
        {
            lastWidth = Screen.width;
            lastHeight = Screen.height;
            float aspect = Mathf.Max(0.1f, lastWidth / (float)Mathf.Max(1, lastHeight));
            float designAspect = DesignWidth / DesignHeight;

            bool wide = aspect >= designAspect;
            cam.orthographicSize = wide ? DesignHeight * 0.5f : DesignWidth * 0.5f / aspect;
            if (scaler != null)
                scaler.matchWidthOrHeight = wide ? 1f : 0f;
        }
    }
}
