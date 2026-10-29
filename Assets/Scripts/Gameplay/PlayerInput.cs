using UnityEngine;
using UnityEngine.EventSystems;

namespace BustAKlud
{
    public struct PlayerCommands
    {
        public bool HasAimPoint;
        public Vector2 AimPoint;
        public float Rotate;          // +1 = counter-clockwise (towards the left wall)
        public bool Fire;
        public bool FireByPointer;    // a click/tap rather than a key, so it may hit the "next" bubble to swap
        public bool Swap;
        public bool Pause;
    }

    /// <summary>
    /// Collects mouse, keyboard and touch into one command per frame. Touch aims while held and fires on
    /// release; the mouse aims on move and fires on click.
    /// </summary>
    public static class PlayerInput
    {
        static Vector3 lastMouse;

        public static PlayerCommands Poll(Camera cam)
        {
            var cmd = new PlayerCommands();

            if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A))
                cmd.Rotate += 1f;
            if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D))
                cmd.Rotate -= 1f;

            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) ||
                Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
                cmd.Fire = true;

            if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow) ||
                Input.GetKeyDown(KeyCode.Tab) || Input.GetKeyDown(KeyCode.LeftShift))
                cmd.Swap = true;

            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P))
                cmd.Pause = true;

            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                if (!IsOverUi(touch.fingerId))
                {
                    cmd.HasAimPoint = true;
                    cmd.AimPoint = cam.ScreenToWorldPoint(touch.position);
                    if (touch.phase == TouchPhase.Ended)
                    {
                        cmd.Fire = true;
                        cmd.FireByPointer = true;
                    }
                }

                return cmd;
            }

            Vector3 mouse = Input.mousePosition;
            bool moved = (mouse - lastMouse).sqrMagnitude > 0.25f;
            lastMouse = mouse;
            bool overUi = IsOverUi(-1);

            if ((moved || Input.GetMouseButtonDown(0)) && !overUi)
            {
                cmd.HasAimPoint = true;
                cmd.AimPoint = cam.ScreenToWorldPoint(mouse);
            }

            if (Input.GetMouseButtonDown(0) && !overUi)
            {
                cmd.Fire = true;
                cmd.FireByPointer = true;
            }

            if (Input.GetMouseButtonDown(1) && !overUi)
                cmd.Swap = true;

            return cmd;
        }

        static bool IsOverUi(int pointerId)
        {
            EventSystem system = EventSystem.current;
            if (system == null)
                return false;
            return pointerId < 0 ? system.IsPointerOverGameObject() : system.IsPointerOverGameObject(pointerId);
        }
    }
}
