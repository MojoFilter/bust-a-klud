using UnityEngine;

namespace BustAKlud
{
    /// <summary>
    /// The only component the scene needs: it assembles the camera rig, audio, backdrop and game at startup,
    /// so the whole game is driven from code with no scene wiring to break.
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {
        void Awake()
        {
#if !UNITY_WEBGL
            Application.targetFrameRate = 60;
#endif
            Application.runInBackground = true;

            Camera cam = Camera.main;
            if (cam == null)
            {
                var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
                cam = camGo.AddComponent<Camera>();
                camGo.AddComponent<AudioListener>();
            }

            var rig = cam.GetComponent<CameraRig>();
            if (rig == null)
                rig = cam.gameObject.AddComponent<CameraRig>();

            new GameObject("Audio").AddComponent<AudioManager>();

            var backdrop = new GameObject("Backdrop").AddComponent<SpookyBackdrop>();
            backdrop.Init(rig);

            var game = new GameObject("Game").AddComponent<GameController>();
            game.Init(cam, rig);
        }
    }
}
