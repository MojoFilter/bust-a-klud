using UnityEngine;

namespace BustAKlud
{
    /// <summary>
    /// The cauldron cannon: aims, recoils when it fires, shows what is loaded by lighting its skull in that
    /// bubble's colour, and keeps the next bubble waiting beside the base.
    /// </summary>
    public class Cannon : MonoBehaviour
    {
        const float BodyScale = 0.85f;
        const float BaseScale = 1.2f;
        const float BaseDrop = 0.19f;          // base centre below the pivot: low enough that the base's peak clears the skull
        const float MouthDistance = 0.60f;
        const float LoadedDistance = 0.52f;
        const float LoadDuration = 0.26f;      // how long a bubble flies into the barrel before it is hidden
        const float RecoilStiffness = 420f;
        const float RecoilDamping = 20f;
        static readonly Vector2 NextBallOffset = new Vector2(-1.05f, -0.1f);
        static readonly Vector3 SkullCentre = new Vector3(0.17f, -0.02f, 0f);   // in the body's local space

        // The skull's glow colour for each bubble type (the ghost bubble is black, so it glows pale instead).
        static readonly Color[] SkullColors =
        {
            new Color(1.00f, 0.60f, 0.15f),   // bat
            new Color(0.45f, 1.00f, 0.25f),   // pumpkin
            new Color(0.35f, 0.55f, 1.00f),   // skull
            new Color(0.80f, 0.95f, 1.00f),   // ghost
            new Color(1.00f, 0.25f, 0.22f)    // spider
        };

        Transform bubbleRoot;
        Transform body;
        SpriteRenderer skullRenderer;
        SpriteRenderer glowRenderer;
        BubbleView loadedView;
        BubbleView nextView;
        float recoil;
        float recoilVelocity;
        float loadHideTimer;
        Color skullColor = Color.white;
        Color skullTarget = Color.white;
        float skullPulse;

        public float Angle { get; private set; } = 90f;
        public BubbleType Loaded { get; private set; } = BubbleType.None;
        public BubbleType Next { get; private set; } = BubbleType.None;

        public Vector2 Pivot => transform.position;
        public Vector2 Direction => new Vector2(Mathf.Cos(Angle * Mathf.Deg2Rad), Mathf.Sin(Angle * Mathf.Deg2Rad));
        public Vector2 Muzzle => Pivot + Direction * (MouthDistance + 0.1f);
        public Vector2 ShotOrigin => Pivot + Direction * LoadedDistance;
        Vector2 NextBallPosition => Pivot + NextBallOffset;

        // If the skull overlay couldn't be built, the loaded bubble is drawn in the barrel instead.
        bool ShowLoadedBubble => skullRenderer == null;

        public void Init(Transform projectileRoot)
        {
            bubbleRoot = projectileRoot;
            transform.position = GameConfig.CannonPivot;

            var bodyGo = new GameObject("CannonBody");
            bodyGo.transform.SetParent(transform, false);
            bodyGo.transform.localScale = Vector3.one * BodyScale;
            var bodyRenderer = bodyGo.AddComponent<SpriteRenderer>();
            bodyRenderer.sprite = GameAssets.CannonBody;
            bodyRenderer.sortingOrder = GameConfig.OrderCannon;
            body = bodyGo.transform;

            Sprite skull = GameAssets.CannonSkull;
            if (skull != null)
            {
                var skullGo = new GameObject("CannonSkull");
                skullGo.transform.SetParent(body, false);
                skullRenderer = skullGo.AddComponent<SpriteRenderer>();
                skullRenderer.sprite = skull;
                skullRenderer.sortingOrder = GameConfig.OrderCannon + 2;   // above the base, so tilting never clips it
                // Pivoted like the body, so it scales about the same point.

                var glowGo = new GameObject("SkullGlow");
                glowGo.transform.SetParent(body, false);
                glowGo.transform.localPosition = SkullCentre;
                glowRenderer = glowGo.AddComponent<SpriteRenderer>();
                glowRenderer.sprite = ProceduralSprites.Glow;
                glowRenderer.sharedMaterial = FxSprite.AdditiveMaterial;
                glowRenderer.sortingOrder = GameConfig.OrderCannon + 3;
            }

            // Drawn in front of the barrel: the bulb is bigger than the base, so behind it the base was hidden.
            // It cradles the bottom of the bulb while the barrel swings above it.
            var baseGo = new GameObject("CannonBase");
            baseGo.transform.SetParent(transform, false);
            baseGo.transform.localPosition = new Vector3(0f, -BaseDrop, 0f);
            baseGo.transform.localScale = Vector3.one * BaseScale;
            var baseRenderer = baseGo.AddComponent<SpriteRenderer>();
            baseRenderer.sprite = GameAssets.CannonBase;
            baseRenderer.sortingOrder = GameConfig.OrderCannon + 1;

            ApplyPose();
            ApplySkull(0f);
        }

        public void Aim(float angle)
        {
            Angle = Mathf.Clamp(angle, GameConfig.MinAimAngle, GameConfig.MaxAimAngle);
        }

        public void AimAt(Vector2 worldPoint)
        {
            Vector2 d = worldPoint - Pivot;
            if (d.sqrMagnitude < 0.01f)
                return;
            Aim(Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
        }

        public void Rotate(float degrees)
        {
            Aim(Angle + degrees);
        }

        public bool HitsNextBall(Vector2 worldPoint)
        {
            return nextView != null && Vector2.Distance(worldPoint, NextBallPosition) < 0.42f;
        }

        public void Load(BubbleType loaded, BubbleType next)
        {
            Clear();
            Loaded = loaded;
            Next = next;

            loadedView = BubbleView.Create(bubbleRoot, loaded, ShotOrigin);
            loadedView.SetSorting(GameConfig.OrderCannon - 2);
            loadedView.SetVisible(ShowLoadedBubble);
            loadedView.PopIn(0.1f);

            nextView = BubbleView.Create(bubbleRoot, next, NextBallPosition);
            nextView.SetSorting(GameConfig.OrderCannon - 2);
            nextView.SetScaleMultiplier(0.8f);
            nextView.PopIn(0.25f);

            SetSkullTarget(true);
        }

        public void Clear()
        {
            if (loadedView != null)
                Destroy(loadedView.gameObject);
            if (nextView != null)
                Destroy(nextView.gameObject);
            loadedView = null;
            nextView = null;
            Loaded = BubbleType.None;
            Next = BubbleType.None;
            loadHideTimer = 0f;
            SetSkullTarget(false);
        }

        /// <summary>Hands the loaded bubble over to be flown; the waiting bubble hops into the barrel and a new one queues.</summary>
        public BubbleView Fire(BubbleType newNext)
        {
            BubbleView flying = loadedView;
            flying.SetVisible(true);
            flying.SetSorting(GameConfig.OrderFlying);
            flying.TeleportTo(ShotOrigin);

            loadedView = nextView;
            Loaded = Next;
            loadedView.SetScaleMultiplier(1f);
            loadedView.SpringTo(ShotOrigin);
            loadHideTimer = ShowLoadedBubble ? 0f : LoadDuration;

            Next = newNext;
            nextView = BubbleView.Create(bubbleRoot, newNext, NextBallPosition);
            nextView.SetSorting(GameConfig.OrderCannon - 2);
            nextView.SetScaleMultiplier(0.8f);
            nextView.PopIn(0.12f);

            recoilVelocity = 7f;
            SetSkullTarget(true);
            return flying;
        }

        public void Swap()
        {
            if (loadedView == null || nextView == null)
                return;

            (loadedView, nextView) = (nextView, loadedView);
            (Loaded, Next) = (Next, Loaded);

            loadedView.SetScaleMultiplier(1f);
            nextView.SetScaleMultiplier(0.8f);

            if (!ShowLoadedBubble)
            {
                // The old loaded bubble pops out of the barrel to the waiting spot; the waiting one hops in.
                nextView.SpringTo(NextBallPosition);
                nextView.SetVisible(true);
                loadedView.SpringTo(ShotOrigin);
                loadHideTimer = LoadDuration;
            }

            loadedView.Pulse(0.2f);
            nextView.Pulse(0.2f);
            SetSkullTarget(true);
        }

        public void ReplaceLoaded(BubbleType type)
        {
            Loaded = type;
            if (loadedView != null)
            {
                loadedView.SetType(type);
                loadedView.Pulse(0.3f);
            }

            SetSkullTarget(true);
        }

        public void ReplaceNext(BubbleType type)
        {
            Next = type;
            if (nextView != null)
            {
                nextView.SetType(type);
                nextView.Pulse(0.3f);
            }
        }

        void SetSkullTarget(bool pulse)
        {
            skullTarget = Loaded == BubbleType.None ? Color.white : SkullColors[(int)Loaded];
            if (pulse)
                skullPulse = 1f;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            recoilVelocity += (-RecoilStiffness * recoil - RecoilDamping * recoilVelocity) * dt;
            recoil += recoilVelocity * dt;
            ApplyPose();
            ApplySkull(dt);

            if (loadHideTimer > 0f)
            {
                loadHideTimer -= dt;
                if (loadHideTimer <= 0f && loadedView != null)
                    loadedView.SetVisible(ShowLoadedBubble);
            }

            if (loadedView != null)
                loadedView.SetHome(ShotOrigin + Direction * (-recoil * 0.03f));
            if (nextView != null)
                nextView.SetHome(NextBallPosition + Vector2.up * (Mathf.Sin(Time.time * 2f) * 0.025f));
        }

        void ApplyPose()
        {
            if (body == null)
                return;
            body.localRotation = Quaternion.Euler(0f, 0f, Angle);
            body.localPosition = (Vector3)(-Direction * (recoil * 0.03f));
        }

        // Eases the skull to the loaded colour; a fresh load pops the skull and flares its glow.
        void ApplySkull(float dt)
        {
            if (skullRenderer == null)
                return;

            skullColor = Color.Lerp(skullColor, skullTarget, dt <= 0f ? 1f : 1f - Mathf.Exp(-16f * dt));
            skullPulse = Mathf.MoveTowards(skullPulse, 0f, dt * 3f);

            skullRenderer.color = skullColor;
            skullRenderer.transform.localScale = Vector3.one * (1f + 0.18f * skullPulse);

            bool loaded = Loaded != BubbleType.None;
            float breathe = 0.3f + 0.1f * Mathf.Sin(Time.time * 3f);
            Color glow = skullColor;
            glow.a = loaded ? breathe + 0.55f * skullPulse : 0f;
            glowRenderer.color = glow;
            glowRenderer.transform.localScale = Vector3.one * (0.62f + 0.25f * skullPulse);
        }
    }
}
