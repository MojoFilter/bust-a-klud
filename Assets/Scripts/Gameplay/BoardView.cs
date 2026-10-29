using System.Collections;
using UnityEngine;

namespace BustAKlud
{
    /// <summary>
    /// Everything visual about the playfield: the board art and frame, the descending ceiling, the lose line,
    /// and the bubble views that mirror the logical <see cref="HexGrid"/>.
    /// </summary>
    public class BoardView : MonoBehaviour
    {
        const float RumbleTime = 0.45f;
        const float DropTime = 0.55f;

        HexGrid grid;
        FxManager fx;
        readonly BubbleView[,] views = new BubbleView[GameConfig.StorageRows, GameConfig.Columns];

        Transform bubbleRoot;
        Transform ceilingRoot;
        SpriteRenderer ceilingFill;
        SpriteRenderer dangerLine;
        SpriteRenderer[] fogLayers;
        float displayDrops;
        float danger;

        public Transform BubbleRoot => bubbleRoot;
        public float DisplayDrops => displayDrops;

        public void Init(HexGrid logicalGrid, FxManager effects)
        {
            grid = logicalGrid;
            fx = effects;

            bubbleRoot = new GameObject("Bubbles").transform;
            bubbleRoot.SetParent(transform, false);

            BuildArt();
            BuildCeiling();
            BuildDangerLine();
            BuildFog();
            ApplyDrops();
        }

        void BuildArt()
        {
            var art = NewSprite("BoardArt", GameAssets.Board, GameConfig.OrderBoardArt);
            art.transform.localPosition = Vector3.zero;
            ArtGlows.Attach(art.gameObject, GameConfig.OrderBoardArt + 2);

            // A touch of shade so the bright bubbles stay readable over the busy painting.
            var shade = NewSprite("BoardShade", ProceduralSprites.White, GameConfig.OrderBoardArt + 1);
            shade.color = new Color(0.05f, 0.02f, 0.14f, 0.22f);
            shade.transform.localScale = new Vector3(GameConfig.BoardWidth, GameConfig.BoardHeight, 1f);

            var frame = NewSprite("BoardFrame", ProceduralSprites.PanelOutline, GameConfig.OrderCeiling + 1);
            frame.drawMode = SpriteDrawMode.Sliced;
            frame.size = new Vector2(GameConfig.BoardWidth + 0.06f, GameConfig.BoardHeight + 0.06f);
            frame.color = new Color(1f, 0.55f, 0.15f, 0.75f);
        }

        void BuildCeiling()
        {
            ceilingRoot = new GameObject("Ceiling").transform;
            ceilingRoot.SetParent(transform, false);

            var edge = NewSprite("CeilingEdge", ProceduralSprites.CeilingEdge, GameConfig.OrderCeiling, ceilingRoot);
            edge.transform.localPosition = Vector3.zero;

            ceilingFill = NewSprite("CeilingFill", ProceduralSprites.Stone, GameConfig.OrderCeiling - 1, ceilingRoot);
            ceilingFill.drawMode = SpriteDrawMode.Tiled;
            ceilingFill.tileMode = SpriteTileMode.Continuous;
        }

        void BuildDangerLine()
        {
            dangerLine = NewSprite("DangerLine", ProceduralSprites.Dash, GameConfig.OrderCeiling + 2);
            dangerLine.drawMode = SpriteDrawMode.Tiled;
            dangerLine.tileMode = SpriteTileMode.Continuous;
            dangerLine.size = new Vector2(GameConfig.BoardWidth - 0.1f, 0.08f);
            dangerLine.transform.localPosition = new Vector3(0f, GameConfig.DangerLineY, 0f);
            dangerLine.color = new Color(1f, 0.2f, 0.25f, 0.25f);
        }

        void BuildFog()
        {
            fogLayers = new SpriteRenderer[2];
            for (int i = 0; i < fogLayers.Length; i++)
            {
                var fog = NewSprite("Fog" + i, ProceduralSprites.Fog, GameConfig.OrderBoardFog + i);
                fog.color = new Color(1f, 1f, 1f, 0.16f);
                fog.transform.localScale = new Vector3(3.6f + i, 1.6f + i * 0.4f, 1f);
                fogLayers[i] = fog;
            }
        }

        SpriteRenderer NewSprite(string objectName, Sprite sprite, int order, Transform parent = null)
        {
            var go = new GameObject(objectName);
            go.transform.SetParent(parent != null ? parent : transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
            return sr;
        }

        void Update()
        {
            float t = Time.time;
            for (int i = 0; i < fogLayers.Length; i++)
            {
                float speed = 0.11f + i * 0.07f;
                float x = Mathf.Sin(t * speed + i * 2.1f) * 1.4f;
                float y = -1.5f + i * 2.6f + Mathf.Sin(t * 0.17f + i) * 0.3f;
                fogLayers[i].transform.localPosition = new Vector3(x, y, 0f);
            }

            float pulse = 0.5f + 0.5f * Mathf.Sin(t * 6f);
            float alpha = Mathf.Lerp(0.22f, 0.35f, danger) + danger * 0.45f * pulse;
            dangerLine.color = new Color(1f, 0.2f, 0.25f, alpha);
        }

        /// <summary>0 = safe, 1 = about to lose; drives how loudly the lose line pulses.</summary>
        public void SetDanger(float amount)
        {
            danger = Mathf.Clamp01(amount);
        }

        public void Clear()
        {
            for (int r = 0; r < GameConfig.StorageRows; r++)
                for (int c = 0; c < GameConfig.Columns; c++)
                    views[r, c] = null;

            for (int i = bubbleRoot.childCount - 1; i >= 0; i--)
                Destroy(bubbleRoot.GetChild(i).gameObject);

            displayDrops = 0f;
            danger = 0f;
            ApplyDrops();
        }

        /// <summary>Creates a view for every bubble in the grid, popping them in row by row.</summary>
        public void Rebuild(bool animate)
        {
            Clear();
            displayDrops = grid.CeilingDrops;

            for (int r = 0; r < GameConfig.StorageRows; r++)
            {
                for (int c = 0; c < HexGrid.ColumnsInRow(r); c++)
                {
                    BubbleType type = grid.Get(new Cell(r, c));
                    if (type == BubbleType.None)
                        continue;

                    var view = BubbleView.Create(bubbleRoot, type, HexGrid.CellCenter(r, c, displayDrops));
                    if (animate)
                        view.PopIn(0.15f + r * 0.07f + c * 0.012f);
                    views[r, c] = view;
                }
            }

            ApplyDrops();
        }

        public BubbleView ViewAt(Cell cell) => HexGrid.IsValid(cell.Row, cell.Col) ? views[cell.Row, cell.Col] : null;

        /// <summary>Removes and returns the view in a cell; the caller decides what happens to it.</summary>
        public BubbleView TakeView(Cell cell)
        {
            BubbleView view = ViewAt(cell);
            if (view != null)
                views[cell.Row, cell.Col] = null;
            return view;
        }

        /// <summary>Seats a shot bubble in its cell; it springs from where it stopped.</summary>
        public void Adopt(Cell cell, BubbleView view)
        {
            views[cell.Row, cell.Col] = view;
            view.SetSorting(GameConfig.OrderBubble);
            view.SpringTo(HexGrid.CellCenter(cell.Row, cell.Col, displayDrops));
        }

        /// <summary>Shoves nearby bubbles away from a point; they spring back.</summary>
        public void Ripple(Vector2 origin, float strength, float radius = 1.9f)
        {
            for (int r = 0; r < GameConfig.StorageRows; r++)
            {
                for (int c = 0; c < GameConfig.Columns; c++)
                {
                    BubbleView view = views[r, c];
                    if (view == null)
                        continue;

                    Vector2 away = view.Home - origin;
                    float distance = away.magnitude;
                    if (distance < 0.05f || distance > radius)
                        continue;

                    float falloff = 1f - distance / radius;
                    view.Kick(away / distance * (strength * falloff * falloff));
                }
            }
        }

        public void PulseAll(float amount)
        {
            for (int r = 0; r < GameConfig.StorageRows; r++)
                for (int c = 0; c < GameConfig.Columns; c++)
                    if (views[r, c] != null)
                        views[r, c].Pulse(amount);
        }

        /// <summary>Plays the ceiling's warning rumble and slide; the logical grid has already advanced.</summary>
        public IEnumerator DropCeiling()
        {
            float from = displayDrops;
            float to = grid.CeilingDrops;
            Vector3 restPosition = ceilingRoot.localPosition;
            float dustClock = 0f;

            for (float t = 0f; t < RumbleTime; t += Time.deltaTime)
            {
                float intensity = t / RumbleTime;
                float jitter = (Random.value - 0.5f) * 0.05f * (0.4f + intensity);
                ceilingRoot.localPosition = restPosition + new Vector3(jitter, 0f, 0f);

                dustClock += Time.deltaTime;
                if (dustClock > 0.07f)
                {
                    dustClock = 0f;
                    Vector2 spot = new Vector2(Random.Range(GameConfig.BoardLeft + 0.2f, GameConfig.BoardRight - 0.2f),
                        GameConfig.CeilingBaseY - from * GameConfig.RowHeight - 0.15f);
                    fx.Dust(spot, 1, new Color(0.7f, 0.6f, 0.85f));
                }

                yield return null;
            }

            CameraRig.Instance.Shake(0.09f, 0.45f);
            fx.Dust(new Vector2(0f, grid.CeilingY), 10, new Color(0.75f, 0.65f, 0.9f));

            for (float t = 0f; t < DropTime; t += Time.deltaTime)
            {
                float k = Mathf.Clamp01(t / DropTime);
                float eased = 1f + 1.2f * Mathf.Pow(k - 1f, 3f) + 0.2f * Mathf.Pow(k - 1f, 2f);
                displayDrops = Mathf.LerpUnclamped(from, to, eased);
                ApplyDrops();
                yield return null;
            }

            displayDrops = to;
            ceilingRoot.localPosition = restPosition;
            ApplyDrops();
            PulseAll(0.12f);
        }

        void ApplyDrops()
        {
            if (grid == null)
                return;

            for (int r = 0; r < GameConfig.StorageRows; r++)
            {
                for (int c = 0; c < GameConfig.Columns; c++)
                {
                    BubbleView view = views[r, c];
                    if (view != null)
                        view.SetHome(HexGrid.CellCenter(r, c, displayDrops));
                }
            }

            float ceilingBottom = GameConfig.CeilingBaseY - displayDrops * GameConfig.RowHeight;
            Vector3 root = ceilingRoot.localPosition;
            ceilingRoot.localPosition = new Vector3(root.x, ceilingBottom, 0f);

            // The slab is 0.1 tall above the root; the stone fill spans from there up to the top of the board.
            float fillBottom = ceilingBottom + GameConfig.CeilingThickness;
            float fillHeight = GameConfig.BoardTop - fillBottom;
            ceilingFill.enabled = fillHeight > 0.01f;
            if (ceilingFill.enabled)
            {
                ceilingFill.size = new Vector2(GameConfig.BoardWidth, fillHeight);
                ceilingFill.transform.position = new Vector3(0f, fillBottom + fillHeight * 0.5f, 0f);
            }
        }
    }
}
