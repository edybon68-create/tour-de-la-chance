using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace TourDeLaChance
{
    /// <summary>
    /// La cinématique du Secret (spec v3 §8.3), une classe d'AIDE utilisée par
    /// BoxOpeningController (ce n'est pas un MonoBehaviour). Elle dessine, par le code
    /// (Painter2D et éléments colorés, aucune image) :
    ///   0,0 s coupure noire → 0,5 s deux battements d'un point rose → 1,3 s fissures
    ///   arc-en-ciel → 2,2 s le noir éclate en 24 morceaux sur un fond arc-en-ciel →
    ///   (la carte qui tombe en tournant, le halo et les rayons restent au contrôleur) →
    ///   3,4 s impact et pluie de confettis → 4,7 s calme → 5,4 s fin.
    /// Tout est calculé à partir du temps écoulé depuis le début de la révélation ("since").
    /// Pur affichage : le résultat est déjà tiré et sauvegardé.
    /// </summary>
    public class SecretCinematic
    {
        // Les moments clés (secondes depuis le début de la révélation), tableau §8.3.
        public const float BeatStart = 0.5f;
        public const float CrackStart = 1.3f;
        public const float ShatterStart = 2.2f;
        public const float CardStart = 2.4f;
        public const float ImpactStart = 3.4f;
        public const float WordStart = 3.7f;
        public const float CalmStart = 4.7f;
        public const float Duration = 5.4f;

        const float Px = 1.5f; // tailles de la spec (référence 1280 × 720) → 1920 × 1080
        const int CrackCount = 8;
        const int ConfettiCount = 40;
        static readonly Color Pink = new Color(1f, 0.31f, 0.847f);   // #FF4FD8
        static readonly Color Night = new Color(0.043f, 0.031f, 0.071f); // #0B0812

        readonly VisualElement layer, background, black, dot, flash;
        readonly CracksElement cracks;
        readonly List<Shard> shards = new List<Shard>();
        readonly List<Confetto> confetti = new List<Confetto>();
        readonly System.Random fxRandom = new System.Random(); // hasard visuel seulement
        Vector2 size = new Vector2(1920f, 1080f);
        float lastSince;

        class Shard { public ShardElement e; public Vector2 pos, vel; public float angle, spin; }
        class Confetto { public VisualElement e; public float x, y, speed, sway, phase, angle, spin; }

        /// layerParent : l'élément plein écran où dessiner (au-dessus de la scène de la boîte).
        /// backgroundParent : l'élément où poser le fond arc-en-ciel (derrière la carte).
        public SecretCinematic(VisualElement layerParent, VisualElement backgroundParent)
        {
            background = Full(Color.white);
            background.style.opacity = 0f;
            backgroundParent.Insert(0, background);

            layer = Full(new Color(0, 0, 0, 0));
            layerParent.Add(layer);
            black = Full(Night);
            layer.Add(black);
            cracks = new CracksElement();
            cracks.style.position = Position.Absolute;
            cracks.style.left = 0; cracks.style.top = 0; cracks.style.right = 0; cracks.style.bottom = 0;
            layer.Add(cracks);
            dot = new VisualElement { pickingMode = PickingMode.Ignore };
            dot.style.position = Position.Absolute;
            layer.Add(dot);
            for (int i = 0; i < CrackCount * 3; i++)
            {
                var e = new ShardElement();
                layer.Add(e);
                shards.Add(new Shard { e = e });
            }
            for (int i = 0; i < ConfettiCount; i++)
            {
                var e = new VisualElement { pickingMode = PickingMode.Ignore };
                e.style.position = Position.Absolute;
                e.style.width = 10f * Px;
                e.style.height = 18f * Px;
                layer.Add(e);
                confetti.Add(new Confetto { e = e });
            }
            flash = Full(Color.white);
            flash.style.opacity = 0f;
            layer.Add(flash);
            Hide();
        }

        static VisualElement Full(Color color)
        {
            var e = new VisualElement { pickingMode = PickingMode.Ignore };
            e.style.position = Position.Absolute;
            e.style.left = 0; e.style.top = 0; e.style.right = 0; e.style.bottom = 0;
            e.style.backgroundColor = color;
            return e;
        }

        /// Prépare une nouvelle cinématique : fissures, morceaux et confettis tirés au hasard.
        public void Prepare()
        {
            float w = layer.parent != null ? layer.parent.resolvedStyle.width : float.NaN;
            float h = layer.parent != null ? layer.parent.resolvedStyle.height : float.NaN;
            if (!float.IsNaN(w) && w > 10f && !float.IsNaN(h) && h > 10f) size = new Vector2(w, h);
            Vector2 c = size * 0.5f;

            // 8 fissures : du centre jusqu'au bord, 3 segments brisés chacune.
            var lines = new Vector2[CrackCount][];
            var angles = new float[CrackCount];
            for (int i = 0; i < CrackCount; i++)
            {
                float a = (i + 0.5f) * Mathf.PI * 2f / CrackCount + Range(-0.18f, 0.18f);
                angles[i] = a;
                Vector2 dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                Vector2 end = c + dir * DistanceToEdge(c, dir);
                Vector2 normal = new Vector2(-dir.y, dir.x);
                float len = (end - c).magnitude;
                lines[i] = new[]
                {
                    c,
                    c + dir * len * 0.3f + normal * Range(-40f, 40f),
                    c + dir * len * 0.62f + normal * Range(-60f, 60f),
                    end
                };
            }
            cracks.Setup(lines, CrackCount);

            // 24 morceaux : chaque « part » entre deux fissures est coupée en 3 (près du
            // centre, au milieu, au bord). Le bord suit le contour de l'écran (coins compris).
            int s = 0;
            for (int i = 0; i < CrackCount; i++)
            {
                var a = lines[i];
                var b = lines[(i + 1) % CrackCount];
                AddShard(s++, new List<Vector2> { a[0], a[1], b[1] });
                AddShard(s++, new List<Vector2> { a[1], a[2], b[2], b[1] });
                var outer = new List<Vector2> { a[2], a[3] };
                outer.AddRange(CornersBetween(c, angles[i], angles[(i + 1) % CrackCount]));
                outer.Add(b[3]);
                outer.Add(b[2]);
                AddShard(s++, outer);
            }

            // Confettis : au-dessus de l'écran, chacun sa couleur, sa vitesse et son balancement.
            for (int i = 0; i < confetti.Count; i++)
            {
                var k = confetti[i];
                k.x = Range(0f, size.x);
                k.y = Range(-size.y * 0.6f, -20f);
                k.speed = Range(250f, 450f);
                k.sway = 60f * Px;
                k.phase = Range(0f, Mathf.PI * 2f);
                k.angle = Range(0f, 360f);
                k.spin = Range(-360f, 360f);
                k.e.style.backgroundColor = Rainbow(i / (float)confetti.Count, 0.75f);
            }
            lastSince = 0f;
        }

        void AddShard(int i, List<Vector2> polygon)
        {
            Vector2 centroid = Vector2.zero;
            foreach (var p in polygon) centroid += p;
            centroid /= polygon.Count;
            var local = new Vector2[polygon.Count];
            for (int j = 0; j < polygon.Count; j++) local[j] = polygon[j] - centroid;
            var sh = shards[i];
            sh.e.Setup(local, Night);
            sh.pos = centroid;
            Vector2 dir = (centroid - size * 0.5f).normalized;
            if (dir == Vector2.zero) dir = Vector2.up;
            sh.vel = dir * Range(600f, 1200f);
            sh.angle = 0f;
            sh.spin = Range(180f, 540f) * (fxRandom.Next(2) == 0 ? 1f : -1f);
        }

        /// Les coins de l'écran situés entre deux directions (dans le sens des angles croissants).
        List<Vector2> CornersBetween(Vector2 c, float from, float to)
        {
            if (to < from) to += Mathf.PI * 2f;
            var corners = new List<(float angle, Vector2 p)>();
            foreach (var p in new[] { Vector2.zero, new Vector2(size.x, 0f), size, new Vector2(0f, size.y) })
            {
                float a = Mathf.Atan2(p.y - c.y, p.x - c.x);
                while (a < from) a += Mathf.PI * 2f;
                if (a < to) corners.Add((a, p));
            }
            corners.Sort((x, y) => x.angle.CompareTo(y.angle));
            var result = new List<Vector2>();
            foreach (var corner in corners) result.Add(corner.p);
            return result;
        }

        float DistanceToEdge(Vector2 c, Vector2 dir)
        {
            float tx = dir.x > 0f ? (size.x - c.x) / dir.x : dir.x < 0f ? -c.x / dir.x : float.MaxValue;
            float ty = dir.y > 0f ? (size.y - c.y) / dir.y : dir.y < 0f ? -c.y / dir.y : float.MaxValue;
            return Mathf.Min(tx, ty);
        }

        public void Hide()
        {
            layer.style.display = DisplayStyle.None;
            background.style.display = DisplayStyle.None;
        }

        /// Place tout selon le temps écoulé depuis le début de la révélation. Renvoie la
        /// secousse de l'écran (px) à appliquer au contenu. skipped = animation passée :
        /// rien de tout cela (ni morceaux, ni confettis, ni fond arc-en-ciel).
        public Vector2 Render(float since, bool skipped)
        {
            float dt = Mathf.Max(0f, since - lastSince);
            lastSince = since;
            if (skipped || since < 0f || since >= Duration)
            {
                Hide();
                return Vector2.zero;
            }
            layer.style.display = DisplayStyle.Flex;
            Vector2 c = size * 0.5f;
            Vector2 shake = Vector2.zero;

            // --- Le noir (coupure), jusqu'à l'éclat ---
            black.style.display = since < ShatterStart ? DisplayStyle.Flex : DisplayStyle.None;

            // --- Le point de lumière rose : 2 battements, puis il grossit et blanchit ---
            float dotSize = 0f;
            Color dotColor = Pink;
            if (since >= BeatStart && since < CrackStart)
            {
                float b = (since - BeatStart) / 0.4f;          // 0 → 2 (deux battements)
                float k = Mathf.Repeat(b, 1f);
                dotSize = 12f * Px * Mathf.Lerp(1f, 2.2f, Ease.Sine(k));
                shake = 2f * Px * Ease.Sine(k) * new Vector2(Mathf.Sin(since * 90f), Mathf.Cos(since * 77f));
            }
            else if (since >= CrackStart && since < ShatterStart)
            {
                float k = (since - CrackStart) / (ShatterStart - CrackStart);
                dotSize = Mathf.Lerp(12f * Px, 40f * Px, k);
                dotColor = Color.Lerp(Pink, Color.white, k);
                float amp = 6f * Px * k;                       // l'écran tremble de plus en plus
                shake = amp * new Vector2(Mathf.Sin(since * 95f), Mathf.Cos(since * 83f));
            }
            dot.style.display = dotSize > 0f ? DisplayStyle.Flex : DisplayStyle.None;
            dot.style.width = dotSize;
            dot.style.height = dotSize;
            dot.style.left = c.x - dotSize * 0.5f;
            dot.style.top = c.y - dotSize * 0.5f;
            SetRadius(dot, dotSize * 0.5f);
            dot.style.backgroundColor = dotColor;

            // --- Les fissures : elles filent jusqu'aux bords en 0,6 s ---
            if (since >= CrackStart && since < ShatterStart)
            {
                cracks.style.display = DisplayStyle.Flex;
                cracks.SetProgress(Mathf.Clamp01((since - CrackStart) / 0.6f));
            }
            else cracks.style.display = DisplayStyle.None;

            // --- L'éclat : le noir part en 24 morceaux, fond arc-en-ciel et flash blanc ---
            bool shattered = since >= ShatterStart;
            background.style.display = shattered ? DisplayStyle.Flex : DisplayStyle.None;
            if (shattered)
            {
                Color bg = Color.HSVToRGB(Mathf.Repeat(since / 2f, 1f), 0.5f, 1f);
                background.style.backgroundColor = bg;
                background.style.opacity = 0.85f;
            }
            float flashK = (since - ShatterStart) / 0.3f;
            flash.style.opacity = flashK >= 0f && flashK < 1f ? 0.8f * (1f - flashK) : 0f;

            float shardAge = since - ShatterStart;
            foreach (var sh in shards)
            {
                bool alive = shardAge >= 0f && shardAge < 0.6f;
                sh.e.style.display = alive ? DisplayStyle.Flex : DisplayStyle.None;
                if (!alive) continue;
                sh.vel.y += 900f * dt;          // gravité 900 px/s²
                sh.pos += sh.vel * dt;
                sh.angle += sh.spin * dt;
                sh.e.style.left = sh.pos.x - sh.e.Half;
                sh.e.style.top = sh.pos.y - sh.e.Half;
                sh.e.style.rotate = new Rotate(sh.angle);
                sh.e.style.opacity = 1f - shardAge / 0.6f; // ils s'effacent en 0,6 s
            }

            // --- Impact : secousse ±14 px pendant 0,5 s, et pluie de confettis (2 s) ---
            if (since >= ImpactStart && since < ImpactStart + 0.5f)
            {
                float k = 1f - (since - ImpactStart) / 0.5f;
                shake = 14f * Px * k * new Vector2(Mathf.Sin(since * 157f), Mathf.Cos(since * 131f) * 0.6f);
            }
            float rainAge = since - ImpactStart;
            foreach (var k in confetti)
            {
                bool alive = rainAge >= 0f && rainAge < 2f;
                k.e.style.display = alive ? DisplayStyle.Flex : DisplayStyle.None;
                if (!alive) continue;
                k.y += k.speed * dt;
                float x = k.x + k.sway / (2f * Mathf.PI) * Mathf.Sin(2f * Mathf.PI * rainAge + k.phase);
                k.angle += k.spin * dt;
                k.e.style.left = x;
                k.e.style.top = k.y;
                k.e.style.rotate = new Rotate(k.angle);
                k.e.style.opacity = rainAge > 1.6f ? (2f - rainAge) / 0.4f : 1f; // « les confettis finissent »
            }
            return shake;
        }

        static void SetRadius(VisualElement e, float r)
        {
            e.style.borderTopLeftRadius = r; e.style.borderTopRightRadius = r;
            e.style.borderBottomLeftRadius = r; e.style.borderBottomRightRadius = r;
        }

        /// Une couleur de l'arc-en-ciel (saturation donnée, pas de néon à 100 %).
        public static Color Rainbow(float t, float saturation) => Color.HSVToRGB(Mathf.Repeat(t, 1f), saturation, 1f);

        /// Échelle horizontale de la carte qui tourne « comme une pièce » (3 demi-tours qui
        /// ralentissent, face visible à la fin) et sa chute (−700 px → 0, léger dépassement).
        public static void CardMotion(float since, out float scaleX, out float offsetY)
        {
            float k = Mathf.Clamp01((since - CardStart) / 1.0f);
            float angle = 3f * Mathf.PI * (1f - Ease.Out(k));
            scaleX = Mathf.Cos(angle);
            // ease-out avec dépassement : il descend un peu trop bas puis remonte.
            float fall = k < 0.75f ? Ease.Out(k / 0.75f) * 1.04f : Mathf.Lerp(1.04f, 1f, Ease.InOut((k - 0.75f) / 0.25f));
            offsetY = -700f * Px * (1f - fall);
        }

        float Range(float min, float max) => min + (float)fxRandom.NextDouble() * (max - min);

        /// Les 8 fissures, dessinées avec Painter2D jusqu'à une progression de 0 à 1.
        class CracksElement : VisualElement
        {
            Vector2[][] lines = new Vector2[0][];
            float progress;
            int count;

            public CracksElement()
            {
                pickingMode = PickingMode.Ignore;
                generateVisualContent += Draw;
            }

            public void Setup(Vector2[][] l, int n) { lines = l; count = n; MarkDirtyRepaint(); }
            public void SetProgress(float p) { progress = p; MarkDirtyRepaint(); }

            void Draw(MeshGenerationContext ctx)
            {
                var painter = ctx.painter2D;
                painter.lineWidth = 4f * Px;
                painter.lineJoin = LineJoin.Round;
                painter.lineCap = LineCap.Round;
                for (int i = 0; i < lines.Length; i++)
                {
                    var l = lines[i];
                    painter.strokeColor = Rainbow(i / (float)count, 0.75f);
                    painter.BeginPath();
                    painter.MoveTo(l[0]);
                    // On parcourt les 3 segments jusqu'à la progression voulue.
                    float total = progress * 3f;
                    for (int s = 0; s < 3; s++)
                    {
                        float k = Mathf.Clamp01(total - s);
                        if (k <= 0f) break;
                        painter.LineTo(Vector2.Lerp(l[s], l[s + 1], k));
                    }
                    painter.Stroke();
                }
            }
        }

        /// Un morceau de l'écran noir : un polygone dessiné dans un carré centré sur son
        /// centre de gravité (la rotation se fait donc autour de ce centre).
        class ShardElement : VisualElement
        {
            Vector2[] points = new Vector2[0];
            Color color;
            /// La moitié du côté du carré qui contient le morceau.
            public float Half { get; private set; }

            public ShardElement()
            {
                pickingMode = PickingMode.Ignore;
                style.position = Position.Absolute;
                generateVisualContent += Draw;
            }

            public void Setup(Vector2[] p, Color c)
            {
                points = p;
                color = c;
                Half = 1f;
                foreach (var q in p) Half = Mathf.Max(Half, Mathf.Abs(q.x), Mathf.Abs(q.y));
                style.width = 2f * Half;
                style.height = 2f * Half;
                MarkDirtyRepaint();
            }

            void Draw(MeshGenerationContext ctx)
            {
                if (points.Length < 3) return;
                var painter = ctx.painter2D;
                Vector2 o = new Vector2(Half, Half);
                painter.fillColor = color;
                painter.BeginPath();
                painter.MoveTo(points[0] + o);
                for (int i = 1; i < points.Length; i++) painter.LineTo(points[i] + o);
                painter.ClosePath();
                painter.Fill();
                // Un fin liseré clair sur les bords cassés : on voit bien les morceaux.
                painter.strokeColor = new Color(1f, 1f, 1f, 0.35f);
                painter.lineWidth = 2f;
                painter.Stroke();
            }
        }
    }
}
