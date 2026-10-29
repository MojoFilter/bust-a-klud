using UnityEngine;

namespace BustAKlud
{
    /// <summary>
    /// One bubble on screen. It sits at a logical <see cref="Home"/> plus a spring-driven offset, which gives
    /// the wobble when a neighbour is hit and the settle when a shot snaps into place.
    /// </summary>
    public class BubbleView : MonoBehaviour
    {
        const float Stiffness = 240f;
        const float Damping = 14f;
        const float PopInTime = 0.4f;
        const float FallGravity = 18f;
        const float KillY = -5f;

        public BubbleType Type { get; private set; }
        public Vector2 Home { get; private set; }
        public Vector2 DisplayPosition => Home + kick;

        SpriteRenderer sr;
        Vector2 kick;
        Vector2 kickVelocity;
        float phase;
        float spawnClock = PopInTime;
        float pulse;
        float scaleMultiplier = 1f;
        bool falling;
        Vector2 fallVelocity;
        float fallSpin;

        public static BubbleView Create(Transform parent, BubbleType type, Vector2 position)
        {
            var go = new GameObject("Bubble " + type);
            go.transform.SetParent(parent, false);
            var view = go.AddComponent<BubbleView>();
            view.Init(type, position);
            return view;
        }

        void Init(BubbleType type, Vector2 position)
        {
            sr = gameObject.AddComponent<SpriteRenderer>();
            sr.sortingOrder = GameConfig.OrderBubble;
            phase = Random.value * Mathf.PI * 2f;
            Home = position;
            transform.position = position;
            SetType(type);
        }

        public void SetType(BubbleType type)
        {
            Type = type;
            sr.sprite = GameAssets.Bubble(type);
        }

        public void SetSorting(int order)
        {
            sr.sortingOrder = order;
        }

        /// <summary>Hides or shows the sprite; the view keeps moving and animating either way.</summary>
        public void SetVisible(bool visible)
        {
            sr.enabled = visible;
        }

        public void SetScaleMultiplier(float multiplier)
        {
            scaleMultiplier = multiplier;
        }

        /// <summary>Moves the logical position; any existing offset keeps springing out on its own.</summary>
        public void SetHome(Vector2 home)
        {
            Home = home;
        }

        /// <summary>Jumps straight to a position with no spring.</summary>
        public void TeleportTo(Vector2 position)
        {
            Home = position;
            kick = Vector2.zero;
            kickVelocity = Vector2.zero;
            transform.position = position;
        }

        /// <summary>Keeps the bubble visually where it is while its home changes, so it springs there.</summary>
        public void SpringTo(Vector2 newHome)
        {
            Vector2 shown = DisplayPosition;
            Home = newHome;
            kick = shown - newHome;
        }

        public void Kick(Vector2 velocityImpulse)
        {
            kickVelocity += velocityImpulse;
        }

        public void Pulse(float amount = 0.25f)
        {
            pulse = amount;
        }

        public void PopIn(float delay)
        {
            spawnClock = -delay;
            ApplyScale();
        }

        public void StartFalling(Vector2 velocity, float spin)
        {
            falling = true;
            fallVelocity = velocity;
            fallSpin = spin;
            sr.sortingOrder = GameConfig.OrderFalling;
        }

        void Update()
        {
            float dt = Time.deltaTime;

            if (falling)
            {
                fallVelocity.y -= FallGravity * dt;
                transform.position += (Vector3)(fallVelocity * dt);
                transform.Rotate(0f, 0f, fallSpin * dt);
                if (transform.position.y < KillY)
                    Destroy(gameObject);
                return;
            }

            Vector2 accel = -Stiffness * kick - Damping * kickVelocity;
            kickVelocity += accel * dt;
            kick += kickVelocity * dt;
            transform.position = Home + kick;

            spawnClock += dt;
            pulse = Mathf.MoveTowards(pulse, 0f, dt * 2.5f);
            ApplyScale();
        }

        void ApplyScale()
        {
            float t = Mathf.Clamp01(spawnClock / PopInTime);
            float spawn = t <= 0f ? 0f : EaseOutBack(t);
            float breathe = 1f + 0.018f * Mathf.Sin(Time.time * 2.2f + phase);
            transform.localScale = Vector3.one * (GameConfig.ArtScale * spawn * breathe * (1f + pulse) * scaleMultiplier);
        }

        static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            float u = t - 1f;
            return 1f + c3 * u * u * u + c1 * u * u;
        }
    }
}
