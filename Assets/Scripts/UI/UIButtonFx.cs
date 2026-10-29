using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BustAKlud
{
    /// <summary>Hover/press feedback for buttons: colour shift, a springy scale and a tick sound.</summary>
    public class UIButtonFx : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        public Graphic Target;
        public Color Normal = Color.white;
        public Color Hover = Color.white;
        public Color Pressed = Color.gray;

        bool hovering;
        bool pressing;
        float scale = 1f;
        float scaleVelocity;

        void OnEnable()
        {
            hovering = false;
            pressing = false;
            scale = 1f;
            scaleVelocity = 0f;
            transform.localScale = Vector3.one;
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            float target = pressing ? 0.94f : hovering ? 1.06f : 1f;
            scaleVelocity += ((target - scale) * 520f - scaleVelocity * 24f) * dt;
            scale += scaleVelocity * dt;
            transform.localScale = Vector3.one * scale;

            if (Target != null)
            {
                Color goal = pressing ? Pressed : hovering ? Hover : Normal;
                Target.color = Color.Lerp(Target.color, goal, 1f - Mathf.Exp(-18f * dt));
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            hovering = true;
            if (AudioManager.Instance != null)
                AudioManager.Instance.Play(Sfx.Hover);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            hovering = false;
            pressing = false;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            pressing = true;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            pressing = false;
        }
    }
}
