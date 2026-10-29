namespace BustAKlud
{
    /// <summary>A tiny damped spring for juicy UI motion (hops, pops, score punches).</summary>
    public struct Spring
    {
        public float Value;
        public float Velocity;

        public Spring(float value)
        {
            Value = value;
            Velocity = 0f;
        }

        public void Step(float target, float dt, float stiffness = 320f, float damping = 14f)
        {
            Velocity += ((target - Value) * stiffness - Velocity * damping) * dt;
            Value += Velocity * dt;
        }
    }
}
