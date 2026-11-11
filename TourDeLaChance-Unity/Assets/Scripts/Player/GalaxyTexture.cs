using System.Collections.Generic;
using UnityEngine;

namespace TourDeLaChance
{
    /// <summary>
    /// La texture ANIMÉE du skin Galaxie (spec v4 §7), calculée par le code : une nébuleuse
    /// violet et bleu qui tourbillonne lentement, et des étoiles qui scintillent à leur place.
    /// Ce n'est pas un MonoBehaviour : PlayerAvatar en crée une et appelle Step() 20 fois par
    /// seconde, seulement quand la Galaxie est affichée.
    ///
    /// Pourquoi recalculer sur le processeur ? Le shader URP Lit n'a qu'un seul réglage de
    /// répétition pour toutes ses cartes : impossible de faire glisser la nébuleuse en gardant
    /// les étoiles fixes avec le matériau seul. Une tuile de 128 × 128 se recalcule très vite.
    ///
    /// Ingrédients (calculés une fois) :
    ///   - couche A « les nuages » : bruit de valeur PÉRIODIQUE (la tuile se répète sans
    ///     couture), 4 octaves (4, 8, 16, 32 cases), graine 2024 ;
    ///   - couche B « les filaments » : même bruit, 3 octaves (6, 12, 24), transformé en
    ///     crêtes f = (1 − |2n − 1|)³ (de fines veines brillantes), graine 4048 ;
    ///   - 78 étoiles fixes (graine 7) : 60 petites, 14 moyennes, 4 grosses en croix.
    /// Animation : A glisse de (0,03 ; 0,025) tuile/s, B de (−0,045 ; 0,04) (sens contraire),
    /// densité d = clamp01(0,7 A + 0,6 B − 0,15) → couleur de la palette, puis étoiles.
    /// </summary>
    public class GalaxyTexture
    {
        public const int Size = 128;
        const int N = Size * Size;

        public Texture2D BaseMap { get; private set; }
        public Texture2D EmissionMap { get; private set; }

        /// Durée du dernier recalcul en millisecondes (et la plus longue), pour vérifier < 1 ms.
        public double LastMs { get; private set; }
        public double MaxMs { get; private set; }
        /// La plus forte valeur écrite dans la carte d'émission (0 à 1) pour les tests.
        public float MaxEmissionTexel { get; private set; }

        static readonly Vector2 SpeedA = new Vector2(0.03f, 0.025f);
        static readonly Vector2 SpeedB = new Vector2(-0.045f, 0.04f);

        readonly float[] layerA = new float[N];
        readonly float[] layerB = new float[N];
        readonly Color32[] baseLut = new Color32[256];      // densité → couleur de la nébuleuse
        readonly Color32[] emissionLut = new Color32[256];  // densité → nébuleuse × 0,3 × d
        readonly Color32[] basePixels = new Color32[N];
        readonly Color32[] emissionPixels = new Color32[N];
        readonly List<Star> stars = new List<Star>();
        readonly System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();

        Vector2 offsetA, offsetB; // en tuiles (0 à 1)
        float time;

        class Star
        {
            public Color32 color;
            public float period, phase;
            public bool big;
            public int[] disc; public float[] discWeight;     // le rond de l'étoile
            public int[] cross; public float[] crossWeight;   // la croix à 4 branches (grosses)
        }

        // Palette de l'icône (spec §7.3) : 7 couleurs le long de la densité.
        static readonly (float d, Color c)[] Palette =
        {
            (0.00f, Hex(0x070514)), (0.30f, Hex(0x1C1450)), (0.50f, Hex(0x3A1F8F)), (0.68f, Hex(0x6A2FD0)),
            (0.82f, Hex(0x3D6BFF)), (0.92f, Hex(0xC54BE8)), (1.00f, Hex(0xF2D6FF))
        };

        public GalaxyTexture()
        {
            BaseMap = NewTexture("Galaxie (couleur)");
            EmissionMap = NewTexture("Galaxie (émission)");
            BuildNoise(layerA, 2024, new[] { 4, 8, 16, 32 }, new[] { 1f, 0.5f, 0.25f, 0.125f }, ridged: false);
            BuildNoise(layerB, 4048, new[] { 6, 12, 24 }, new[] { 1f, 0.5f, 0.25f }, ridged: true);
            for (int i = 0; i < 256; i++)
            {
                float d = i / 255f;
                Color c = PaletteAt(d);
                baseLut[i] = c;
                float k = 0.3f * d; // seules les zones claires luisent un peu
                emissionLut[i] = new Color(c.r * k, c.g * k, c.b * k, 1f);
            }
            BuildStars();
            Step(0f, 1f);
        }

        public void Dispose()
        {
            if (BaseMap != null) Object.Destroy(BaseMap);
            if (EmissionMap != null) Object.Destroy(EmissionMap);
        }

        static Texture2D NewTexture(string name) => new Texture2D(Size, Size, TextureFormat.RGBA32, false)
        {
            name = name,
            wrapMode = TextureWrapMode.Repeat,  // la tuile se répète sur le corps
            filterMode = FilterMode.Bilinear,
            hideFlags = HideFlags.DontSave
        };

        /// Fait avancer l'animation de "dt" secondes (speed = 3 pendant le saut de joie) et
        /// réécrit les deux textures.
        public void Step(float dt, float speed)
        {
            watch.Restart();
            time += dt;
            offsetA += SpeedA * (dt * speed);
            offsetB += SpeedB * (dt * speed);
            offsetA.x = Mathf.Repeat(offsetA.x, 1f); offsetA.y = Mathf.Repeat(offsetA.y, 1f);
            offsetB.x = Mathf.Repeat(offsetB.x, 1f); offsetB.y = Mathf.Repeat(offsetB.y, 1f);

            // 1. La nébuleuse. Le décalage est le même pour tous les pixels : les 4 poids de
            //    l'interpolation bilinéaire (lecture « entre » les pixels, mouvement fluide)
            //    sont donc calculés une seule fois par image.
            Shift(offsetA, out int ax, out int ay, out float aw00, out float aw10, out float aw01, out float aw11);
            Shift(offsetB, out int bx, out int by, out float bw00, out float bw10, out float bw01, out float bw11);
            const int mask = Size - 1; // 127 : « & mask » = modulo 128 (la tuile boucle)
            for (int y = 0; y < Size; y++)
            {
                int ra0 = ((y + ay) & mask) * Size, ra1 = ((y + ay + 1) & mask) * Size;
                int rb0 = ((y + by) & mask) * Size, rb1 = ((y + by + 1) & mask) * Size;
                int row = y * Size;
                for (int x = 0; x < Size; x++)
                {
                    int xa0 = (x + ax) & mask, xa1 = (x + ax + 1) & mask;
                    int xb0 = (x + bx) & mask, xb1 = (x + bx + 1) & mask;
                    float a = layerA[ra0 + xa0] * aw00 + layerA[ra0 + xa1] * aw10 + layerA[ra1 + xa0] * aw01 + layerA[ra1 + xa1] * aw11;
                    float b = layerB[rb0 + xb0] * bw00 + layerB[rb0 + xb1] * bw10 + layerB[rb1 + xb0] * bw01 + layerB[rb1 + xb1] * bw11;
                    float d = 0.7f * a + 0.6f * b - 0.15f;
                    int i = d <= 0f ? 0 : d >= 1f ? 255 : (int)(d * 255f);
                    basePixels[row + x] = baseLut[i];
                    emissionPixels[row + x] = emissionLut[i];
                }
            }

            // 2. Les étoiles par-dessus, à leur place fixe, avec leur éclat du moment.
            byte maxEmission = 0;
            foreach (var s in stars)
            {
                float wave = 0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * (time / s.period + s.phase));
                float glow = 0.25f + 0.75f * wave * wave * wave * wave; // pointes brèves et franches
                Paint(s, s.disc, s.discWeight, glow);
                if (s.big && glow > 0.6f) Paint(s, s.cross, s.crossWeight, glow);
            }
            for (int i = 0; i < N; i++)
            {
                var e = emissionPixels[i];
                byte m = e.r > e.g ? (e.r > e.b ? e.r : e.b) : (e.g > e.b ? e.g : e.b);
                if (m > maxEmission) maxEmission = m;
            }
            MaxEmissionTexel = maxEmission / 255f;

            BaseMap.SetPixels32(basePixels);
            BaseMap.Apply(false);
            EmissionMap.SetPixels32(emissionPixels);
            EmissionMap.Apply(false);
            watch.Stop();
            LastMs = watch.Elapsed.TotalMilliseconds;
            if (LastMs > MaxMs) MaxMs = LastMs;
        }

        /// Une étoile se mélange à la couleur (blanc chaud, bleuté ou rosé) selon son éclat,
        /// et s'AJOUTE à l'émission (texel plafonné à 1 : jamais au-dessus du maximum).
        void Paint(Star s, int[] pixels, float[] weights, float glow)
        {
            for (int k = 0; k < pixels.Length; k++)
            {
                int p = pixels[k];
                float w = weights[k] * glow;
                basePixels[p] = Color32.Lerp(basePixels[p], s.color, w);
                var e = emissionPixels[p];
                emissionPixels[p] = new Color32(Add(e.r, s.color.r, w), Add(e.g, s.color.g, w), Add(e.b, s.color.b, w), 255);
            }
        }

        static byte Add(byte a, byte b, float w) => (byte)Mathf.Min(255, a + b * w);

        /// Décalage en tuiles → décalage entier en pixels + 4 poids bilinéaires.
        static void Shift(Vector2 offset, out int ix, out int iy, out float w00, out float w10, out float w01, out float w11)
        {
            float px = offset.x * Size, py = offset.y * Size;
            ix = Mathf.FloorToInt(px); iy = Mathf.FloorToInt(py);
            float fx = px - ix, fy = py - iy;
            w00 = (1 - fx) * (1 - fy); w10 = fx * (1 - fy); w01 = (1 - fx) * fy; w11 = fx * fy;
        }

        // =====================================================================
        // Calculs faits une seule fois
        // =====================================================================

        /// Bruit de valeur périodique : une grille de nombres au hasard (cells × cells) qui
        /// BOUCLE sur elle-même, lissée entre les cases. Plusieurs octaves (grilles de plus en
        /// plus fines, de moins en moins fortes) sont additionnées, puis ramenées entre 0 et 1.
        static void BuildNoise(float[] target, int seed, int[] cellsPerTile, float[] weights, bool ridged)
        {
            var random = new System.Random(seed);
            for (int o = 0; o < cellsPerTile.Length; o++)
            {
                int cells = cellsPerTile[o];
                var grid = new float[cells * cells];
                for (int i = 0; i < grid.Length; i++) grid[i] = (float)random.NextDouble();
                for (int y = 0; y < Size; y++)
                    for (int x = 0; x < Size; x++)
                    {
                        float gx = (float)x * cells / Size, gy = (float)y * cells / Size;
                        int x0 = (int)gx, y0 = (int)gy;
                        int x1 = (x0 + 1) % cells, y1 = (y0 + 1) % cells; // la grille boucle
                        float tx = Smooth(gx - x0), ty = Smooth(gy - y0);
                        float top = Mathf.Lerp(grid[y0 * cells + x0], grid[y0 * cells + x1], tx);
                        float bottom = Mathf.Lerp(grid[y1 * cells + x0], grid[y1 * cells + x1], tx);
                        target[y * Size + x] += weights[o] * Mathf.Lerp(top, bottom, ty);
                    }
            }
            // Ramené entre 0 et 1 (le plus petit devient 0, le plus grand 1).
            float min = float.MaxValue, max = float.MinValue;
            foreach (var v in target) { if (v < min) min = v; if (v > max) max = v; }
            for (int i = 0; i < N; i++)
            {
                float n = (target[i] - min) / Mathf.Max(0.0001f, max - min);
                if (ridged) { float r = 1f - Mathf.Abs(2f * n - 1f); n = r * r * r; }
                target[i] = n;
            }
        }

        static float Smooth(float t) => t * t * (3f - 2f * t);

        void BuildStars()
        {
            var random = new System.Random(7);
            float R(float a, float b) => a + (float)random.NextDouble() * (b - a);
            void Add(int count, float rMin, float rMax, bool big)
            {
                for (int i = 0; i < count; i++)
                {
                    var s = new Star
                    {
                        big = big,
                        period = R(1.5f, 3.5f),
                        phase = R(0f, 1f)
                    };
                    double c = random.NextDouble();
                    s.color = c < 0.7 ? Hex(0xFFF1D6) : c < 0.9 ? Hex(0xBFD4FF) : Hex(0xFFC2F0);
                    float cx = R(0f, Size), cy = R(0f, Size), r = R(rMin, rMax);
                    BuildDisc(s, cx, cy, r);
                    if (big) BuildCross(s, cx, cy);
                    stars.Add(s);
                }
            }
            Add(60, 0.7f, 1.2f, false);
            Add(14, 1.5f, 2f, false);
            Add(4, 2.5f, 2.5f, true);
        }

        static void BuildDisc(Star s, float cx, float cy, float r)
        {
            var px = new List<int>(); var w = new List<float>();
            int reach = Mathf.CeilToInt(r + 1f);
            for (int dy = -reach; dy <= reach; dy++)
                for (int dx = -reach; dx <= reach; dx++)
                {
                    int x = Mathf.FloorToInt(cx) + dx, y = Mathf.FloorToInt(cy) + dy;
                    float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy));
                    float k = Mathf.Clamp01(r - dist + 0.5f); // bord adouci sur un pixel
                    if (k <= 0f) continue;
                    px.Add(Wrap(y) * Size + Wrap(x)); w.Add(k);
                }
            s.disc = px.ToArray(); s.discWeight = w.ToArray();
        }

        /// La croix des grosses étoiles : 4 branches de 6 px, 1 px de large, qui s'estompent.
        static void BuildCross(Star s, float cx, float cy)
        {
            var px = new List<int>(); var w = new List<float>();
            int x0 = Mathf.FloorToInt(cx), y0 = Mathf.FloorToInt(cy);
            for (int k = 3; k <= 8; k++) // 6 px à partir du bord du rond (rayon 2,5)
            {
                float fade = 1f - (k - 3) / 6f;
                px.Add(Wrap(y0) * Size + Wrap(x0 + k)); w.Add(fade);
                px.Add(Wrap(y0) * Size + Wrap(x0 - k)); w.Add(fade);
                px.Add(Wrap(y0 + k) * Size + Wrap(x0)); w.Add(fade);
                px.Add(Wrap(y0 - k) * Size + Wrap(x0)); w.Add(fade);
            }
            s.cross = px.ToArray(); s.crossWeight = w.ToArray();
        }

        static int Wrap(int v) => ((v % Size) + Size) % Size;

        static Color PaletteAt(float d)
        {
            for (int i = 1; i < Palette.Length; i++)
                if (d <= Palette[i].d)
                    return Color.Lerp(Palette[i - 1].c, Palette[i].c, Mathf.InverseLerp(Palette[i - 1].d, Palette[i].d, d));
            return Palette[Palette.Length - 1].c;
        }

        static Color Hex(int rgb) => new Color(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f, 1f);
    }
}
