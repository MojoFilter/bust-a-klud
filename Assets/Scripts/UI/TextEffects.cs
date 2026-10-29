using UnityEngine;
using UnityEngine.UI;

namespace BustAKlud
{
    /// <summary>Bobs and tilts each letter of a Text along a travelling wave.</summary>
    public class WobbleText : BaseMeshEffect
    {
        public float Amplitude = 7f;     // pixels
        public float Speed = 3.2f;
        public float Frequency = 0.012f; // phase advance per pixel along the line
        public float Tilt = 4f;          // degrees

        void Update()
        {
            if (graphic != null)
                graphic.SetVerticesDirty();
        }

        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive() || vh.currentVertCount < 4)
                return;

            float time = Time.unscaledTime * Speed;
            int quads = vh.currentVertCount / 4;
            var vertex = new UIVertex();

            for (int q = 0; q < quads; q++)
            {
                Vector3 centre = Vector3.zero;
                for (int k = 0; k < 4; k++)
                {
                    vh.PopulateUIVertex(ref vertex, q * 4 + k);
                    centre += vertex.position;
                }

                centre *= 0.25f;
                float phase = time + centre.x * Frequency;
                Quaternion rotation = Quaternion.Euler(0f, 0f, Mathf.Cos(phase) * Tilt);
                Vector3 lift = new Vector3(0f, Mathf.Sin(phase) * Amplitude, 0f);

                for (int k = 0; k < 4; k++)
                {
                    int index = q * 4 + k;
                    vh.PopulateUIVertex(ref vertex, index);
                    vertex.position = centre + rotation * (vertex.position - centre) + lift;
                    vh.SetUIVertex(vertex, index);
                }
            }
        }
    }

    /// <summary>Tints a Text with a top-to-bottom colour gradient across its whole mesh.</summary>
    public class TextGradient : BaseMeshEffect
    {
        public Color Top = Color.white;
        public Color Bottom = Color.gray;

        public override void ModifyMesh(VertexHelper vh)
        {
            int count = vh.currentVertCount;
            if (!IsActive() || count == 0)
                return;

            var vertex = new UIVertex();
            float minY = float.MaxValue;
            float maxY = float.MinValue;
            for (int i = 0; i < count; i++)
            {
                vh.PopulateUIVertex(ref vertex, i);
                minY = Mathf.Min(minY, vertex.position.y);
                maxY = Mathf.Max(maxY, vertex.position.y);
            }

            for (int i = 0; i < count; i++)
            {
                vh.PopulateUIVertex(ref vertex, i);
                Color tint = Color.Lerp(Bottom, Top, Mathf.InverseLerp(minY, maxY, vertex.position.y));
                vertex.color = new Color32((byte)(tint.r * 255f), (byte)(tint.g * 255f), (byte)(tint.b * 255f), vertex.color.a);
                vh.SetUIVertex(vertex, i);
            }
        }
    }
}
