#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace BustAKlud
{
    /// <summary>
    /// Automated playthrough for development builds. Run the exe with:
    ///   -bak-autoplay [-bak-shots 120] [-bak-out C:\folder] [-bak-lose]
    /// It starts a game, plays it with a greedy bot (or deliberately badly with -bak-lose), writes screenshots
    /// and logs a status line per shot. Compiled out of release builds.
    /// </summary>
    public class DevHarness : MonoBehaviour
    {
        GameController game;
        string outputFolder;
        int maxShots = 120;
        int shotNumber;
        bool pauseShot;
        bool loseOnPurpose;

        public static void Attach(GameController controller)
        {
            string[] args = Environment.GetCommandLineArgs();
            if (Array.IndexOf(args, "-bak-autoplay") < 0)
                return;

            var harness = new GameObject("DevHarness").AddComponent<DevHarness>();
            harness.game = controller;
            harness.loseOnPurpose = Array.IndexOf(args, "-bak-lose") >= 0;
            harness.outputFolder = Path.Combine(Application.persistentDataPath, "shots");
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == "-bak-shots" && int.TryParse(args[i + 1], out int shots))
                    harness.maxShots = shots;
                if (args[i] == "-bak-out")
                    harness.outputFolder = args[i + 1];
            }

            Directory.CreateDirectory(harness.outputFolder);
            harness.StartCoroutine(harness.Run());
        }

        // Screenshots are captured at the end of the frame, so never change the scene in the same frame
        // after calling this (yield a frame first).
        void Snap(string name)
        {
            ScreenCapture.CaptureScreenshot(Path.Combine(outputFolder, name + ".png"));
            Debug.Log($"[BAK] screenshot {name}  ({game.DevStatus})");
        }

        IEnumerator Run()
        {
            yield return new WaitForSeconds(2.5f);
            Snap("01_title");

            yield return new WaitForSeconds(0.5f);
            game.DevStartGame();
            yield return new WaitForSeconds(1.2f);
            Snap("02_level_intro");
            yield return new WaitForSeconds(1.2f);
            Snap("03_ready");

            bool clearShot = false;
            bool overShot = false;
            while (shotNumber < maxShots)
            {
                if (game.DevLevelClear)
                {
                    if (!clearShot)
                    {
                        yield return new WaitForSeconds(2.0f);
                        Snap("90_level_clear");
                        clearShot = true;
                    }

                    yield return null;
                    game.DevNextLevel();
                    clearShot = false;
                    yield return new WaitForSeconds(2.2f);
                    continue;
                }

                if (game.DevGameOver)
                {
                    if (!overShot)
                    {
                        yield return new WaitForSeconds(2.5f);
                        Snap("91_game_over");
                        overShot = true;
                    }

                    break;
                }

                if (!game.DevReady)
                {
                    yield return null;
                    continue;
                }

                yield return new WaitForSeconds(0.2f);
                float angle = game.DevChooseAngle(loseOnPurpose);

                if (shotNumber == 2)
                {
                    // Hold still while aiming so the guide path and landing ring can be inspected.
                    game.DevAim(angle);
                    yield return new WaitForSeconds(1.0f);
                    Snap("20_aiming");
                    yield return null;
                }

                if (shotNumber == 5 && !pauseShot)
                {
                    pauseShot = true;
                    game.DevSetPaused(true);
                    yield return new WaitForSecondsRealtime(0.8f);
                    Snap("30_pause");
                    yield return null;
                    game.DevSetPaused(false);
                    yield return new WaitForSecondsRealtime(0.3f);
                    continue;
                }

                game.DevFire(angle);
                shotNumber++;
                Debug.Log($"[BAK] shot {shotNumber}: {game.DevStatus}");

                if (shotNumber == 1 || shotNumber == 4 || shotNumber % 12 == 0)
                {
                    yield return new WaitForSeconds(0.35f);
                    Snap($"10_shot_{shotNumber:D3}");
                    yield return null;
                }

                yield return new WaitForSeconds(0.3f);
            }

            yield return new WaitForSeconds(1.0f);
            Debug.Log($"[BAK] finished after {shotNumber} shots: {game.DevStatus}");
            Application.Quit();
        }
    }
}
#endif
