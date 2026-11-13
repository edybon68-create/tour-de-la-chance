using UnityEngine;
using UnityEngine.UIElements;

namespace TourDeLaChance
{
    /// <summary>
    /// Un élément d'interface qui dessine N rayons partant du centre (art-bible §10.3) :
    /// des triangles fins (~10° d'ouverture) dont le bout extérieur s'efface.
    /// On le fait tourner avec style.rotate ; ses couleurs viennent de la rareté
    /// (le Secret alterne plusieurs couleurs).
    /// Le dessin est un petit maillage fait main (sommets colorés), fabriqué par Unity
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
            Vector2 center = contentRect.center;
            float radius = Mathf.Min(contentRect.width, contentRect.height) * 0.5f;
            float halfAngle = 5f * Mathf.Deg2Rad; // 10° d'ouverture
            // v6 (QA n°50) : avant, chaque rayon était fait de 6 morceaux de transparences
            // différentes : on voyait des « marches » (des blocs carrés). Maintenant on fabrique
            // nous-mêmes un petit maillage dont CHAQUE SOMMET a sa transparence : la carte
            // graphique fait un dégradé parfaitement lisse entre eux.
            const int steps = RaySteps;
            int vertsPerRay = 2 * (steps + 1);
            int indicesPerRay = 6 * steps;
            var mesh = context.Allocate(vertsPerRay * count, indicesPerRay * count);
            for (int i = 0; i < count; i++)
            {
                float angle = i * Mathf.PI * 2f / count;
                Color c = colors[i % colors.Length];
                int baseVertex = i * vertsPerRay;
                for (int k = 0; k <= steps; k++)
                {
                    float u = (float)k / steps;                 // 0 au départ du rayon, 1 au bout
                    float r = radius * (0.15f + 0.85f * u);     // départ un peu loin du centre (derrière la carte)
                    var tint = c;
                    tint.a *= (1f - u) * (1f - u);              // le bout s'efface en douceur
                    mesh.SetNextVertex(new Vertex { position = Point(center + Polar(angle - halfAngle, r)), tint = tint });
                    mesh.SetNextVertex(new Vertex { position = Point(center + Polar(angle + halfAngle, r)), tint = tint });
                }
                for (int k = 0; k < steps; k++)
                {
                    // Deux triangles par tranche (sommets a, b au début, c, d à la fin).
                    ushort a = (ushort)(baseVertex + 2 * k), b = (ushort)(a + 1), cc = (ushort)(a + 2), d = (ushort)(a + 3);
                    mesh.SetNextIndex(a); mesh.SetNextIndex(cc); mesh.SetNextIndex(b);
                    mesh.SetNextIndex(b); mesh.SetNextIndex(cc); mesh.SetNextIndex(d);
                }
            }
        }

        // Nombre de tranches par rayon : assez pour que la courbe de transparence soit douce.
        const int RaySteps = 8;

        static Vector3 Point(Vector2 p) => new Vector3(p.x, p.y, Vertex.nearZ);

        static Vector2 Polar(float angle, float r) => new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * r;
    }
}
