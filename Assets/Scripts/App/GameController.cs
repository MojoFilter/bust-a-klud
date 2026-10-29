using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BustAKlud
{
    /// <summary>
    /// Runs a game: title -> level intro -> aim/shoot/resolve loop -> level clear or game over. All the rules
    /// live here (matching, orphans, the creeping ceiling, scoring); views only draw what it tells them to.
    /// </summary>
    public class GameController : MonoBehaviour
    {
        enum State
        {
            Title,
            Intro,
            Aiming,
            Shooting,
            Resolving,
            LevelClear,
            GameOver
        }

        static readonly string[] OpeningLines = { "Let's raise some bubbles!", "Boo! Let's play.", "Fresh graves, fresh bubbles!" };
        static readonly string[] BigPopLines = { "Ghoulishly good!", "Spooktacular!", "Boo-yah!", "Wicked!", "Bone-chilling!" };
        static readonly string[] DropLines = { "Down they go!", "Timber... er, tombstone!", "Gravity's a ghoul." };
        static readonly string[] MissLines = { "Tut tut.", "My bat flies straighter.", "Try a bank shot!", "Not quite!" };
        static readonly string[] CeilingLines = { "The ceiling's hungry!", "Mind the ceiling!", "It creeps closer..." };
        static readonly string[] DangerLines = { "Eek! Too close!", "Pop something. Quickly!", "The line! The line!" };
        static readonly string[] ClearLines = { "Marvelous mayhem!", "Delightfully dreadful!", "Frightfully good!" };
        static readonly string[] ComboLines = { "Combo!", "On a roll!", "Unstoppable!", "Hex appeal!" };

        Camera cam;
        CameraRig rig;
        AudioManager audioManager;
        FxManager fx;
        UIManager ui;
        BoardView board;
        Cannon cannon;
        AimGuide guide;
        GameObject worldRoot;

        readonly HexGrid grid = new HexGrid();
        readonly ShotPath path = new ShotPath();
        readonly List<Cell> cluster = new List<Cell>(64);
        readonly List<Cell> floating = new List<Cell>(64);
        readonly List<BubbleType> present = new List<BubbleType>(BubbleTypes.Count);

        State state = State.Title;
        bool paused;
        bool dangerAnnounced;
        int score;
        int level;
        int streak;
        int shotsSinceDrop;
        int shotLimit;

        public void Init(Camera worldCamera, CameraRig cameraRig)
        {
            cam = worldCamera;
            rig = cameraRig;
            audioManager = AudioManager.Instance;

            fx = new GameObject("Fx").AddComponent<FxManager>();

            worldRoot = new GameObject("World");
            board = new GameObject("Board").AddComponent<BoardView>();
            board.transform.SetParent(worldRoot.transform, false);
            board.Init(grid, fx);

            cannon = new GameObject("Cannon").AddComponent<Cannon>();
            cannon.transform.SetParent(worldRoot.transform, false);
            cannon.Init(board.BubbleRoot);

            guide = new GameObject("AimGuide").AddComponent<AimGuide>();
            guide.transform.SetParent(worldRoot.transform, false);
            worldRoot.SetActive(false);

            ui = new GameObject("Interface").AddComponent<UIManager>();
            ui.Build(cam, rig);
            ui.PlayClicked += BeginNewGame;
            ui.RetryClicked += BeginNewGame;
            ui.NextLevelClicked += NextLevel;
            ui.PauseClicked += () => SetPaused(true);
            ui.ResumeClicked += () => SetPaused(false);
            ui.QuitToTitleClicked += ReturnToTitle;
            ui.QuitGameClicked += QuitGame;
            ui.ShowTitle();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            DevHarness.Attach(this);
#endif
        }

        void OnDestroy()
        {
            Time.timeScale = 1f;
        }

        // ------------------------------------------------------------------ flow

        void BeginNewGame()
        {
            SetPaused(false);
            ui.Transition(() =>
            {
                StopAllCoroutines();
                ResetWorld();
                score = 0;
                level = 1;
                ui.ShowHud();
                ui.Hud.SetScore(0, true);
                ui.Hud.SetBest(GameSettings.HighScore);
                worldRoot.SetActive(true);
                StartCoroutine(StartLevelRoutine(true));
            });
        }

        void NextLevel()
        {
            level++;
            ui.Transition(() =>
            {
                ui.HideAllModals();
                StartCoroutine(StartLevelRoutine(false));
            });
        }

        void ReturnToTitle()
        {
            SetPaused(false);
            ui.Transition(() =>
            {
                StopAllCoroutines();
                ResetWorld();
                worldRoot.SetActive(false);
                state = State.Title;
                ui.ShowTitle();
            });
        }

        void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        void ResetWorld()
        {
            guide.Hide();
            cannon.Clear();
            board.Clear();
            grid.Clear();
        }

        void SetPaused(bool value)
        {
            if (paused == value)
                return;

            paused = value;
            Time.timeScale = value ? 0f : 1f;
            if (value)
                ui.ShowPause();
            else
                ui.HideAllModals();
        }

        IEnumerator StartLevelRoutine(bool firstLevel)
        {
            state = State.Intro;
            LevelSpec spec = LevelGenerator.GetSpec(level);

            LevelGenerator.Fill(grid, level);
            board.Rebuild(true);

            shotsSinceDrop = 0;
            streak = 0;
            shotLimit = spec.ShotLimit;
            dangerAnnounced = false;

            ui.Hud.SetLevel(level, spec.Name);
            ui.Hud.SetCeiling(shotLimit, shotLimit);
            ui.ShowBanner("LEVEL " + level, spec.Name);
            if (firstLevel)
                Say(OpeningLines);

            cannon.Load(PickType(), PickType());
            UpdateDanger();

            yield return new WaitForSeconds(1.1f);
            state = State.Aiming;
        }

        // ------------------------------------------------------------------ per-frame input

        void Update()
        {
            if (state == State.Title)
                return;

            PlayerCommands cmd = PlayerInput.Poll(cam);

            if (cmd.Pause && state != State.GameOver && state != State.LevelClear)
            {
                SetPaused(!paused);
                return;
            }

            if (paused || state != State.Aiming)
                return;

            if (cmd.HasAimPoint)
                cannon.AimAt(cmd.AimPoint);
            if (cmd.Rotate != 0f)
                cannon.Rotate(cmd.Rotate * GameConfig.KeyboardAimSpeed * Time.deltaTime);

            RefreshGuide();

            bool clickedNextBall = cmd.Fire && cmd.FireByPointer && cannon.HitsNextBall(cmd.AimPoint);
            if (cmd.Swap || clickedNextBall)
                SwapBubbles();
            else if (cmd.Fire)
                StartCoroutine(ShotRoutine());
        }

        void RefreshGuide()
        {
            if (!GameSettings.AimGuide)
            {
                guide.Hide();
                return;
            }

            ShotPathSimulator.Simulate(grid, cannon.ShotOrigin, cannon.Direction, path);
            guide.Show(path, grid.CellCenter(path.LandingCell), cannon.Loaded.Tint());
        }

        void SwapBubbles()
        {
            cannon.Swap();
            audioManager.Play(Sfx.Swap, 0.8f);
        }

        // ------------------------------------------------------------------ shooting

        IEnumerator ShotRoutine()
        {
            state = State.Shooting;
            guide.Hide();

            BubbleType type = cannon.Loaded;
            Vector2 direction = cannon.Direction;
            ShotPathSimulator.Simulate(grid, cannon.ShotOrigin, direction, path);
            Cell landing = path.LandingCell;

            BubbleView ball = cannon.Fire(PickType());
            audioManager.Play(Sfx.Fire, 0.9f);
            fx.Smoke(cannon.Muzzle, direction);
            rig.Shake(0.035f, 0.14f);

            float length = path.Length;
            float travelled = 0f;
            int nextBounce = 1;
            while (travelled < length)
            {
                travelled += GameConfig.ShotSpeed * Time.deltaTime;
                while (nextBounce < path.Points.Count - 1 && path.DistanceAtPoint(nextBounce) <= travelled)
                {
                    audioManager.Play(Sfx.Bounce, 0.8f, Random.Range(0.95f, 1.1f));
                    fx.Sparkles(path.Points[nextBounce], 3, type.Tint(), 0.15f);
                    nextBounce++;
                }

                ball.TeleportTo(path.PointAt(travelled));
                yield return null;
            }

            ball.TeleportTo(path.End);
            yield return StartCoroutine(LandRoutine(ball, landing, type));
        }

        IEnumerator LandRoutine(BubbleView ball, Cell cell, BubbleType type)
        {
            state = State.Resolving;

            grid.Set(cell, type);
            board.Adopt(cell, ball);
            audioManager.Play(Sfx.Stick, 0.8f, Random.Range(0.92f, 1.08f));
            board.Ripple(grid.CellCenter(cell), 2.4f);
            ball.Pulse(0.2f);

            // Every shot counts towards the ceiling drop, whether or not it matches.
            shotsSinceDrop++;
            ui.Hud.SetCeiling(Mathf.Max(0, shotLimit - shotsSinceDrop), shotLimit);

            grid.FindCluster(cell, cluster);
            if (cluster.Count >= GameConfig.MinMatch)
            {
                streak++;
                yield return StartCoroutine(PopClusterRoutine(cell));

                grid.FindFloating(floating);
                if (floating.Count > 0)
                    yield return StartCoroutine(DropFloatingRoutine());

                if (streak >= 3 && !grid.IsEmpty)
                    Say(ComboLines);
            }
            else
            {
                streak = 0;
                yield return new WaitForSeconds(0.12f);
                if (Random.value < 0.3f)
                    Say(MissLines);
            }

            // No point lowering the ceiling on a board that was just cleared.
            if (shotsSinceDrop >= shotLimit && !grid.IsEmpty)
                yield return StartCoroutine(CeilingRoutine());

            if (grid.IsEmpty)
            {
                yield return StartCoroutine(LevelClearRoutine());
                yield break;
            }

            if (grid.ExceedsLimit())
            {
                yield return StartCoroutine(GameOverRoutine());
                yield break;
            }

            RefreshQueue();
            UpdateDanger();
            state = State.Aiming;
        }

        IEnumerator PopClusterRoutine(Cell origin)
        {
            Vector2 start = grid.CellCenter(origin);
            cluster.Sort((a, b) =>
                Vector2.Distance(grid.CellCenter(a), start).CompareTo(Vector2.Distance(grid.CellCenter(b), start)));

            BubbleType type = grid.Get(origin);
            int count = cluster.Count;
            float wait = Mathf.Min(0.055f, 0.7f / count);
            Vector2 centre = Vector2.zero;

            if (count >= 5)
                audioManager.Play(Sfx.Bang, 0.7f, Random.Range(0.95f, 1.05f));
            if (count >= 6)
                rig.Shake(0.05f + 0.01f * Mathf.Min(count, 10), 0.25f);

            for (int i = 0; i < count; i++)
            {
                Cell cell = cluster[i];
                Vector2 position = grid.CellCenter(cell);
                centre += position;

                BubbleView view = board.TakeView(cell);
                if (view != null)
                    Destroy(view.gameObject);
                grid.Set(cell, BubbleType.None);

                fx.PopBubble(type, position);
                audioManager.PlayPop(i);
                board.Ripple(position, 0.8f, 1.3f);
                yield return new WaitForSeconds(wait);
            }

            centre /= count;
            int points = 10 * count + 10 * Mathf.Max(0, count - 3);
            AwardPoints(points, centre, type.Tint(), 1f + Mathf.Min(count, 10) * 0.05f);

            if (count >= 6)
                Say(BigPopLines);
        }

        IEnumerator DropFloatingRoutine()
        {
            int count = floating.Count;
            float wait = Mathf.Min(0.045f, 0.5f / count);
            Vector2 centre = Vector2.zero;

            for (int i = 0; i < count; i++)
            {
                Cell cell = floating[i];
                centre += grid.CellCenter(cell);

                BubbleView view = board.TakeView(cell);
                grid.Set(cell, BubbleType.None);
                if (view != null)
                {
                    view.StartFalling(new Vector2(Random.Range(-1.4f, 1.4f), Random.Range(0.8f, 3f)), Random.Range(-260f, 260f));
                    view.Pulse(0.15f);
                }

                if (i % 2 == 0)
                    audioManager.Play(Sfx.Fall, 0.55f, Random.Range(0.85f, 1.2f));
                yield return new WaitForSeconds(wait);
            }

            centre /= count;
            int points = 20 * count + 10 * count * (count - 1) / 2;
            AwardPoints(points, centre, UIKit.Slime, 1f + Mathf.Min(count, 10) * 0.06f);

            if (count >= 4)
                Say(DropLines);
        }

        IEnumerator CeilingRoutine()
        {
            Say(CeilingLines);
            audioManager.Play(Sfx.CeilingDrop, 0.9f);
            shotsSinceDrop = 0;
            grid.AdvanceCeiling();
            yield return StartCoroutine(board.DropCeiling());
            audioManager.Play(Sfx.Stick, 1f, 0.6f);
            ui.Hud.SetCeiling(shotLimit, shotLimit);
        }

        IEnumerator LevelClearRoutine()
        {
            state = State.LevelClear;
            guide.Hide();
            audioManager.Play(Sfx.LevelClear);
            Say(ClearLines, 3.2f);

            int bonus = 500 + level * 250 + Mathf.Max(0, shotLimit - shotsSinceDrop) * 100;   // spare shots before the ceiling
            score += bonus;
            ui.Hud.SetScore(score);
            if (score > GameSettings.HighScore)
            {
                GameSettings.HighScore = score;
                ui.Hud.SetBest(score);
            }

            for (int i = 0; i < 8; i++)
            {
                Vector2 spot = new Vector2(Random.Range(-2f, 2f), Random.Range(-1.5f, 2.5f));
                fx.Sparkles(spot, 5, Random.value < 0.5f ? UIKit.Gold : UIKit.Slime, 0.5f);
                fx.Flash(spot, 1.2f, UIKit.Pumpkin);
                yield return new WaitForSeconds(0.1f);
            }

            yield return new WaitForSeconds(0.6f);
            ui.ShowLevelClear(level, bonus, score);
        }

        IEnumerator GameOverRoutine()
        {
            state = State.GameOver;
            guide.Hide();
            audioManager.Play(Sfx.GameOver);
            Say(new[] { "The ghouls got you!" }, 3.5f);
            rig.Shake(0.12f, 0.6f);

            // Everything falls away, bottom row first.
            for (int r = grid.LowestRow(); r >= 0; r--)
            {
                for (int c = 0; c < HexGrid.ColumnsInRow(r); c++)
                {
                    var cell = new Cell(r, c);
                    BubbleView view = board.TakeView(cell);
                    grid.Set(cell, BubbleType.None);
                    if (view != null)
                        view.StartFalling(new Vector2(Random.Range(-1.2f, 1.2f), Random.Range(0.5f, 2.5f)), Random.Range(-220f, 220f));
                }

                yield return new WaitForSeconds(0.07f);
            }

            yield return new WaitForSeconds(0.9f);

            bool newBest = score > GameSettings.HighScore;
            if (newBest)
                GameSettings.HighScore = score;
            ui.Hud.SetBest(GameSettings.HighScore);
            ui.ShowGameOver(score, GameSettings.HighScore, newBest);
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // ------------------------------------------------------------------ dev hooks (see DevHarness)

        public bool DevReady => state == State.Aiming && !paused;
        public bool DevLevelClear => state == State.LevelClear;
        public bool DevGameOver => state == State.GameOver;

        public string DevStatus =>
            $"state={state} level={level} score={score} shots={shotsSinceDrop}/{shotLimit} bubbles={grid.Count} drops={grid.CeilingDrops} streak={streak}";

        public void DevStartGame() => BeginNewGame();
        public void DevNextLevel() => NextLevel();

        public void DevSetPaused(bool value) => SetPaused(value);

        public void DevFireBest() => DevFire(DevChooseAngle());

        public void DevAim(float angle)
        {
            cannon.Aim(angle);
            RefreshGuide();
        }

        public void DevFire(float angle)
        {
            cannon.Aim(angle);
            StartCoroutine(ShotRoutine());
        }

        /// <summary>Greedy bot: tries every aim angle and picks the one that pops the biggest group.</summary>
        public float DevChooseAngle(bool avoidMatches = false)
        {
            BubbleType type = cannon.Loaded;
            float bestAngle = 90f;
            int bestScore = -1;

            for (float angle = GameConfig.MinAimAngle; angle <= GameConfig.MaxAimAngle; angle += 1.5f)
            {
                Vector2 dir = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
                ShotPathSimulator.Simulate(grid, cannon.Pivot + dir * 0.55f, dir, path);
                Cell cell = path.LandingCell;
                if (grid.IsOccupied(cell))
                    continue;

                grid.Set(cell, type);
                grid.FindCluster(cell, cluster);
                int size = cluster.Count;
                grid.Set(cell, BubbleType.None);

                int rating = size >= GameConfig.MinMatch ? (avoidMatches ? -100 : size * 10) : 0;
                rating += Random.Range(0, 5);
                if (rating > bestScore)
                {
                    bestScore = rating;
                    bestAngle = angle;
                }
            }

            return bestAngle;
        }
#endif

        // ------------------------------------------------------------------ helpers

        void AwardPoints(int basePoints, Vector2 at, Color color, float scale)
        {
            int multiplier = Mathf.Clamp(streak, 1, 4);
            int points = basePoints * multiplier;
            score += points;
            ui.Hud.SetScore(score);
            ui.Popup(at, multiplier > 1 ? $"+{points:N0}  x{multiplier}" : $"+{points:N0}", color, scale);
        }

        BubbleType PickType()
        {
            grid.TypesPresent(present);
            if (present.Count == 0)
                return (BubbleType)Random.Range(0, BubbleTypes.Count);
            return present[Random.Range(0, present.Count)];
        }

        // Once a colour is gone from the board it would be a dead shot, so swap it for one that remains.
        void RefreshQueue()
        {
            grid.TypesPresent(present);
            if (present.Count == 0)
                return;

            if (!present.Contains(cannon.Loaded))
                cannon.ReplaceLoaded(PickType());
            if (!present.Contains(cannon.Next))
                cannon.ReplaceNext(PickType());
        }

        void UpdateDanger()
        {
            int headroom = grid.RowsOfHeadroom();
            board.SetDanger(Mathf.Clamp01((3 - headroom) / 3f));

            if (headroom <= 1 && !dangerAnnounced)
            {
                dangerAnnounced = true;
                Say(DangerLines);
            }
            else if (headroom > 2)
            {
                dangerAnnounced = false;
            }
        }

        void Say(string[] lines, float seconds = 2.6f)
        {
            ui.Hud.Say(lines[Random.Range(0, lines.Length)], seconds);
        }
    }
}
