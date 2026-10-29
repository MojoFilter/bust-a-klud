using UnityEngine;

namespace BustAKlud
{
    /// <summary>
    /// A short-lived pooled sprite effect: it can scale, fade, drift under gravity, spin, and optionally play
    /// through a frame sequence. Everything is configured through <see cref="Play"/>.
    /// </summary>
    public class FxSprite : MonoBehaviour
    {
        static Material additiveMaterial;

        public bool Busy { get; private set; }

        SpriteRenderer sr;
        Material normalMaterial;
        Sprite[] frames;
        float age;
        float life;
        float scaleFrom;
        float scaleTo;
        float alphaFrom;
        float alphaTo;
        Vector2 velocity;
        float gravity;
        float drag;
        float spin;
        Color tint;

        public static Material AdditiveMaterial
        {
            get
            {
                if (additiveMaterial == null)
                {
                    var shader = Resources.Load<Shader>("Shaders/SpriteAdditive");
                    if (shader == null)
                        shader = Shader.Find("BustAKlud/SpriteAdditive");
                    additiveMaterial = new Material(shader);
                }

                return additiveMaterial;
            }
        }

        void Awake()
        {
            sr = gameObject.AddComponent<SpriteRenderer>();
            normalMaterial = sr.sharedMaterial;
            gameObject.SetActive(false);
        }

        public void Play(Sprite sprite, Sprite[] frameSequence, Vector2 position, float lifetime,
            float startScale, float endScale, float startAlpha, float endAlpha, Vector2 initialVelocity,
            float gravityPull, float dragFactor, float angularSpeed, float startRotation, Color color,
            bool additive, int sortingOrder)
        {
            frames = frameSequence;
            life = Mathf.Max(0.01f, lifetime);
            scaleFrom = startScale;
            scaleTo = endScale;
            alphaFrom = startAlpha;
            alphaTo = endAlpha;
            velocity = initialVelocity;
            gravity = gravityPull;
            drag = dragFactor;
            spin = angularSpeed;
            tint = color;
            age = 0f;

            sr.sprite = frames != null && frames.Length > 0 ? frames[0] : sprite;
            sr.sharedMaterial = additive ? AdditiveMaterial : normalMaterial;
            sr.sortingOrder = sortingOrder;
            transform.position = position;
            transform.rotation = Quaternion.Euler(0f, 0f, startRotation);
            Apply(0f);

            Busy = true;
            gameObject.SetActive(true);
        }

        void Update()
        {
            age += Time.deltaTime;
            if (age >= life)
            {
                Busy = false;
                gameObject.SetActive(false);
                return;
            }

            velocity.y -= gravity * Time.deltaTime;
            if (drag > 0f)
                velocity *= Mathf.Max(0f, 1f - drag * Time.deltaTime);
            transform.position += (Vector3)(velocity * Time.deltaTime);
            transform.Rotate(0f, 0f, spin * Time.deltaTime);
            Apply(age / life);
        }

        void Apply(float t)
        {
            float eased = 1f - (1f - t) * (1f - t);
            transform.localScale = Vector3.one * Mathf.Lerp(scaleFrom, scaleTo, eased);

            Color c = tint;
            c.a = tint.a * Mathf.Lerp(alphaFrom, alphaTo, t);
            sr.color = c;

            if (frames != null && frames.Length > 1)
                sr.sprite = frames[Mathf.Min(frames.Length - 1, (int)(t * frames.Length))];
        }
    }
}
