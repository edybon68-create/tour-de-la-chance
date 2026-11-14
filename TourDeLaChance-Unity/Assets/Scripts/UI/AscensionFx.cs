using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace TourDeLaChance
{
    /// <summary>
    /// v7 (art bible §17.2) : les petits EFFETS DESSINÉS PAR LE CODE de L'Ascension festive.
    /// Ce sont de simples éléments d'interface (pas des MonoBehaviour) : TrophyRoadPage les crée,
    /// les pose dans ses calques et les fait bouger dans son Tick. Aucun ne reçoit de clic
    /// (pickingMode Ignore) et aucun ne contient de règle de jeu : c'est du décor.
    ///   - Glow : une lueur ronde (le disque flou UiFxTextures.Halo, teinté) ;
    ///   - Sparkle4 : une étincelle à 4 branches ;
    ///   - Cloud : un nuage (l'image rendue dans Blender si elle existe, sinon des boules
    ///     dessinées) ;
    ///   - Beam : le faisceau du projecteur (trapèze qui s'éclaire vers le bas) ;
    ///   - Stripes : les rayures diagonales qui « coulent » dans la barre de progression ;
    ///   - Confetti : une petite pluie de confettis (physique très simple).
    /// </summary>
    public static class AscensionFx
    {
        static readonly Color Ink = new Color(0.067f, 0.051f, 0.122f, 1f); // #110D1F

        public static Color Hex(string hex) => ColorUtility.TryParseHtmlString(hex, out var c) ? c : Color.magenta;

        /// Une lueur ronde (ou en ellipse si w ≠ h), centrée sur (cx, cy) dans son parent.
        public static VisualElement Glow(Color color, float w, float h, float cx, float cy)
        {
            var e = new VisualElement { pickingMode = PickingMode.Ignore };
            e.style.position = Position.Absolute;
            e.style.width = w; e.style.height = h;
            e.style.left = cx - w * 0.5f; e.style.top = cy - h * 0.5f;
            e.style.backgroundImage = new StyleBackground(UiFxTextures.Halo);
            e.style.unityBackgroundImageTintColor = color;
            return e;
        }

        /// Des rayons qui tournent (RaysElement), centrés sur (cx, cy), de diamètre d.
        public static RaysElement Rays(int count, Color color, float d, float cx, float cy)
        {
            var rays = new RaysElement();
            rays.style.position = Position.Absolute;
            rays.style.width = d; rays.style.height = d;
            rays.style.left = cx - d * 0.5f; rays.style.top = cy - d * 0.5f;
            rays.Configure(count, color);
            return rays;
        }

        // =====================================================================
        // L'étincelle à 4 branches (deux losanges fins croisés, cœur blanc)
        // =====================================================================
        public class Sparkle4 : VisualElement
        {
            Color color;
            public Sparkle4(Color color, float size)
            {
                this.color = color;
                pickingMode = PickingMode.Ignore;
                style.position = Position.Absolute;
                style.width = size; style.height = size;
                generateVisualContent += Draw;
            }

            void Draw(MeshGenerationContext ctx)
            {
                var r = contentRect;
                var c = r.center;
                float R = r.width * 0.5f, t = r.width * 0.12f; // longueur d'une branche, demi-épaisseur
                var p = ctx.painter2D;
                p.fillColor = color;
                // Losange vertical, puis losange horizontal.
                p.BeginPath();
                p.MoveTo(c + new Vector2(0, -R)); p.LineTo(c + new Vector2(t, 0)); p.LineTo(c + new Vector2(0, R)); p.LineTo(c + new Vector2(-t, 0)); p.ClosePath();
                p.Fill();
                p.BeginPath();
                p.MoveTo(c + new Vector2(-R, 0)); p.LineTo(c + new Vector2(0, t)); p.LineTo(c + new Vector2(R, 0)); p.LineTo(c + new Vector2(0, -t)); p.ClosePath();
                p.Fill();
                p.fillColor = Color.white; // le cœur blanc
                p.BeginPath(); p.Arc(c, t * 1.1f, 0f, 360f); p.Fill();
            }
        }

        // =====================================================================
        // Le nuage : image Blender (blanche, teintée par le code) ou boules dessinées
        // =====================================================================
        public class Cloud : VisualElement
        {
            readonly Color tint;
            readonly bool drawn;
            // Les boules du nuage dessiné : (x, y, rayon) en fraction de la largeur.
            static readonly Vector3[] Balls =
            {
                new Vector3(0.20f, 0.62f, 0.17f), new Vector3(0.38f, 0.42f, 0.22f), new Vector3(0.60f, 0.38f, 0.24f),
                new Vector3(0.80f, 0.60f, 0.16f), new Vector3(0.50f, 0.66f, 0.18f)
            };

            public Cloud(Sprite sprite, float width, Color tint, float opacity)
            {
                this.tint = tint;
                pickingMode = PickingMode.Ignore;
                style.position = Position.Absolute;
                style.width = width; style.height = width * 0.5f;
                style.opacity = opacity;
                if (sprite != null)
                {
                    style.backgroundImage = new StyleBackground(sprite);
                    style.backgroundSize = new BackgroundSize(BackgroundSizeType.Contain); // « scale-to-fit »
                    style.unityBackgroundImageTintColor = tint;
                }
                else
                {
                    drawn = true;
                    generateVisualContent += Draw;
                }
            }

            /// Repli tant que les nuages Blender ne sont pas livrés : des boules lisses, contour
            /// d'encre fin, ventre un peu plus sombre (comme le rendu prévu au §17.2.11).
            void Draw(MeshGenerationContext ctx)
            {
                if (!drawn) return;
                float w = contentRect.width;
                var p = ctx.painter2D;
                // 1. Le contour : les mêmes boules, un peu plus grosses, à l'encre.
                p.fillColor = new Color(Ink.r, Ink.g, Ink.b, 0.9f);
                foreach (var b in Balls) { p.BeginPath(); p.Arc(new Vector2(b.x * w, b.y * w * 0.5f), b.z * w + 3f, 0f, 360f); p.Fill(); }
                // 2. Le ventre gris-lavande, puis le blanc teinté par-dessus (décalé vers le haut).
                p.fillColor = Hex("#D8D2E6") * tint;
                foreach (var b in Balls) { p.BeginPath(); p.Arc(new Vector2(b.x * w, b.y * w * 0.5f), b.z * w, 0f, 360f); p.Fill(); }
                p.fillColor = tint;
                foreach (var b in Balls) { p.BeginPath(); p.Arc(new Vector2(b.x * w, b.y * w * 0.5f - b.z * w * 0.18f), b.z * w * 0.86f, 0f, 360f); p.Fill(); }
            }
        }

        // =====================================================================
        // Le faisceau du projecteur : haut étroit (40 %), bas large, 0 % → 55 % d'opacité
        // =====================================================================
        public class Beam : VisualElement
        {
            public Beam()
            {
                pickingMode = PickingMode.Ignore;
                style.position = Position.Absolute;
                generateVisualContent += Draw;
            }

            void Draw(MeshGenerationContext ctx)
            {
                var r = contentRect;
                float w = r.width, h = r.height;
                var warm = Hex("#FFF6C8");
                var top = new Color(warm.r, warm.g, warm.b, 0f);
                var bottom = new Color(warm.r, warm.g, warm.b, 0.75f); // v7 : 0,55 se voyait à peine sur le ciel de jour
                var clear = new Color(warm.r, warm.g, warm.b, 0f);
                // 3 bandes verticales : bord gauche fondu, centre, bord droit fondu (20 %).
                float tl = w * 0.30f, tr = w * 0.70f;            // haut : 40 % de la largeur
                float tli = Mathf.Lerp(tl, tr, 0.2f), tri = Mathf.Lerp(tl, tr, 0.8f);
                float bl = 0f, br = w, bli = w * 0.2f, bri = w * 0.8f;
                var mesh = ctx.Allocate(8, 18);
                mesh.SetNextVertex(V(tl, 0, clear)); mesh.SetNextVertex(V(tli, 0, top)); mesh.SetNextVertex(V(tri, 0, top)); mesh.SetNextVertex(V(tr, 0, clear));
                mesh.SetNextVertex(V(bl, h, clear)); mesh.SetNextVertex(V(bli, h, bottom)); mesh.SetNextVertex(V(bri, h, bottom)); mesh.SetNextVertex(V(br, h, clear));
                for (int i = 0; i < 3; i++)
                {
                    ushort a = (ushort)i, b = (ushort)(i + 1), c = (ushort)(i + 5), d = (ushort)(i + 4);
                    mesh.SetNextIndex(a); mesh.SetNextIndex(b); mesh.SetNextIndex(c);
                    mesh.SetNextIndex(a); mesh.SetNextIndex(c); mesh.SetNextIndex(d);
                }
            }

            static Vertex V(float x, float y, Color c) => new Vertex { position = new Vector3(x, y, Vertex.nearZ), tint = c };
        }

        // =====================================================================
        // Les rayures de la barre : bandes blanches à 18 %, 10 px, pas de 28 px, à 45°
        // =====================================================================
        public class Stripes : VisualElement
        {
            float offset;
            public Stripes()
            {
                pickingMode = PickingMode.Ignore;
                style.position = Position.Absolute;
                style.left = 0; style.right = 0; style.top = 0; style.bottom = 0;
                generateVisualContent += Draw;
            }

            /// Fait glisser les rayures vers le haut (0 → 28 px, en boucle).
            public void SetOffset(float y) { offset = Mathf.Repeat(y, 28f); MarkDirtyRepaint(); }

            void Draw(MeshGenerationContext ctx)
            {
                var r = contentRect;
                if (r.height <= 0f) return;
                var p = ctx.painter2D;
                p.fillColor = new Color(1f, 1f, 1f, 0.18f);
                float w = r.width;
                // Une bande = un parallélogramme qui monte de gauche à droite (45°).
                for (float y = -w - 28f + (28f - offset); y < r.height + 28f; y += 28f)
                {
                    p.BeginPath();
                    p.MoveTo(new Vector2(0, y + w)); p.LineTo(new Vector2(w, y)); p.LineTo(new Vector2(w, y + 10f)); p.LineTo(new Vector2(0, y + w + 10f));
                    p.ClosePath();
                    p.Fill();
                }
            }
        }

        // =====================================================================
        // Les confettis (§17.2.10) : rectangles 10 × 16 et ronds Ø 10, sans contour
        // =====================================================================
        public class Confetti
        {
            static readonly string[] Colors = { "#FF5FA2", "#FFD54A", "#3BC8FF", "#6BE34A", "#A35CE0", "#FFFFFF" };
            const float Life = 1.2f, Gravity = 1400f;

            struct Piece { public VisualElement e; public Vector2 pos, vel; public float rot, spin, age; }
            readonly List<Piece> pieces = new List<Piece>();
            readonly System.Random random = new System.Random();

            /// Fait jaillir n confettis vers le haut depuis (x, y) dans le calque donné.
            public void Burst(VisualElement layer, Vector2 at, int n)
            {
                for (int i = 0; i < n; i++)
                {
                    var e = new VisualElement { pickingMode = PickingMode.Ignore };
                    e.style.position = Position.Absolute;
                    bool round = random.NextDouble() < 0.4;
                    e.style.width = 10; e.style.height = round ? 10 : 16;
                    float radius = round ? 5f : 3f;
                    e.style.borderTopLeftRadius = radius; e.style.borderTopRightRadius = radius;
                    e.style.borderBottomLeftRadius = radius; e.style.borderBottomRightRadius = radius;
                    e.style.backgroundColor = Hex(Colors[random.Next(Colors.Length)]);
                    layer.Add(e);
                    // Cône de ±60° vers le haut, 380 à 620 px/s.
                    float angle = (float)(random.NextDouble() * 120.0 - 60.0) * Mathf.Deg2Rad;
                    float speed = 380f + 240f * (float)random.NextDouble();
                    var vel = new Vector2(Mathf.Sin(angle), -Mathf.Cos(angle)) * speed;
                    float spin = (180f + 360f * (float)random.NextDouble()) * (random.NextDouble() < 0.5 ? -1f : 1f);
                    pieces.Add(new Piece { e = e, pos = at, vel = vel, rot = (float)random.NextDouble() * 360f, spin = spin });
                }
            }

            public int Count => pieces.Count;

            public void Tick(float dt)
            {
                for (int i = pieces.Count - 1; i >= 0; i--)
                {
                    var p = pieces[i];
                    p.age += dt;
                    if (p.age >= Life || p.e.panel == null) { p.e.RemoveFromHierarchy(); pieces.RemoveAt(i); continue; }
                    p.vel.y += Gravity * dt;
                    p.pos += p.vel * dt;
                    p.rot += p.spin * dt;
                    p.e.style.left = p.pos.x - 5f;
                    p.e.style.top = p.pos.y - 8f;
                    p.e.style.rotate = new Rotate(p.rot);
                    p.e.style.opacity = p.age < Life - 0.3f ? 1f : (Life - p.age) / 0.3f;
                    pieces[i] = p;
                }
            }

            public void Clear()
            {
                foreach (var p in pieces) p.e.RemoveFromHierarchy();
                pieces.Clear();
            }
        }
    }
}
