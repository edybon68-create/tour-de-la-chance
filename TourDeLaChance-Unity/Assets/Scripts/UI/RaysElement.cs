using UnityEngine;
using UnityEngine.UIElements;

namespace TourDeLaChance
{
    /// <summary>
    /// Un élément d'interface qui dessine N rayons partant du centre (art-bible §10.3) :
    /// des triangles fins (~10° d'ouverture) dont le bout extérieur s'efface.
    /// On le fait tourner avec style.rotate ; ses couleurs viennent de la rareté
    /// (le Secret alterne plusieurs couleurs).
    /// Le dessin utilise Painter2D (l'outil de dessin de UI Toolkit), appelé par Unity
    /// chaque fois que l'élément doit être redessiné (generateVisualContent).
    /// </summary>
    public class RaysElement : VisualElement
    {
        int count = 8;
        Color[] colors = { Color.white };

        public RaysElement()
        {
            pickingMode = PickingMode.Ignore;
            generateVisualContent += Draw;
        }

        /// Change le nombre de rayons et leurs couleurs (une couleur par rayon, en boucle).
        public void Configure(int rayCount, params Color[] rayColors)
        {
            count = Mathf.Max(0, rayCount);
            colors = rayColors != null && rayColors.Length > 0 ? rayColors : new[] { Color.white };
            MarkDirtyRepaint(); // demande à Unity de redessiner
        }

        void Draw(MeshGenerationContext context)
        {
            if (count <= 0) return;
            var painter = context.painter2D;
            Vector2 center = contentRect.center;
            float radius = Mathf.Min(contentRect.width, contentRect.height) * 0.5f;
            float halfAngle = 5f * Mathf.Deg2Rad; // 10° d'ouverture
            for (int i = 0; i < count; i++)
            {
                float angle = i * Mathf.PI * 2f / count;
                Color c = colors[i % colors.Length];
                // Le bout extérieur s'efface : on dessine 6 segments de plus en plus transparents
                // (en partant d'un peu loin du centre, caché derrière la carte).
                const int parts = 6;
                for (int part = 0; part < parts; part++)
                {
                    float r0 = radius * (0.15f + 0.85f * part / parts), r1 = radius * (0.15f + 0.85f * (part + 1) / parts);
                    var fill = c;
                    fill.a *= 1f - (part + 0.5f) / parts;
                    painter.fillColor = fill;
                    painter.BeginPath();
                    painter.MoveTo(center + Polar(angle - halfAngle, r0));
                    painter.LineTo(center + Polar(angle - halfAngle, r1));
                    painter.LineTo(center + Polar(angle + halfAngle, r1));
                    painter.LineTo(center + Polar(angle + halfAngle, r0));
                    painter.ClosePath();
                    painter.Fill();
                }
            }
        }

        static Vector2 Polar(float angle, float r) => new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * r;
    }
}
