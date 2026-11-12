using System.Collections.Generic;
using UnityEngine;

namespace TourDeLaChance
{
    /// <summary>
    /// Les MOTIFS calculés des skins Pirates / Animaux (catalogue v5 §1 et §3, rendus « C » et
    /// « T » en attendant les images du directeur artistique). Pur affichage.
    ///
    /// Chaque motif est une image 256 × 256 fabriquée UNE fois (puis gardée en mémoire), posée
    /// sur les UV du cheval : U fait le tour du corps (0 → 1, couture à l'arrière), V monte du
    /// socle (0) aux oreilles (1). Le tour du corps mesure ≈ 3,1 m et la hauteur ≈ 2 m : les
    /// tailles sont données en mètres et converties, pour que les taches restent rondes.
    /// Le motif « boucle » autour du corps (aucune couture visible).
    /// Certains motifs ont aussi une image de LUMIÈRE (émission) : veines, taches lumineuses…
    /// Le hasard (place des taches) est un générateur VISUEL tiré de l'identifiant de l'objet :
    /// le même skin a toujours le même dessin. Il n'a rien à voir avec les règles du jeu.
    /// </summary>
    public static class SkinPatterns
    {
        public const int Size = 256;
        const float Around = 3.1f, Height = 2f; // tour et hauteur du corps, en mètres

        public class Result
        {
            public Texture2D baseMap;      // les couleurs
            public Texture2D emissionMap;  // ce qui brille (null = rien)
            public List<Vector4> spots;    // taches lumineuses (u, v, rayon, phase) pour les effets animés
        }

        static readonly Dictionary<string, Result> cache = new Dictionary<string, Result>();

        /// Le motif d'un skin (fabriqué au premier appel). null si le skin n'a pas de motif.
        public static Result For(ShopItem item)
        {
            if (item == null || item.Pattern == SkinPattern.Aucun) return null;
            if (cache.TryGetValue(item.Id, out var r) && r.baseMap != null) return r;
            r = Build(item);
            cache[item.Id] = r;
            return r;
        }

        // ---------- Petits outils ----------

        static Texture2D NewTexture(string name) => new Texture2D(Size, Size, TextureFormat.RGBA32, true)
        {
            name = name, wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear, hideFlags = HideFlags.DontSave
        };

        /// Distance en MÈTRES entre deux points (u, v), en tenant compte du tour complet en U.
        public static float Dist(float u1, float v1, float u2, float v2)
        {
            float du = Mathf.Abs(u1 - u2);
            du = Mathf.Min(du, 1f - du) * Around;
            float dv = (v1 - v2) * Height;
            return Mathf.Sqrt(du * du + dv * dv);
        }

        /// Un bruit doux qui boucle en U (mélange de deux bruits de Perlin décalés d'un tour).
        static float Noise(float u, float v, float freqU, float freqV, float seed)
        {
            float a = Mathf.PerlinNoise(u * freqU + seed, v * freqV + seed * 0.7f);
            float b = Mathf.PerlinNoise((u - 1f) * freqU + seed, v * freqV + seed * 0.7f);
            return Mathf.Lerp(a, b, u);
        }

        static float Frac(float x) => x - Mathf.Floor(x);

        /// Passage doux de 0 à 1 quand x va de a à b (comme « smoothstep » des shaders).
        /// Attention : Mathf.SmoothStep(a, b, t) d'Unity fait autre chose (il va de a à b).
        static float Smooth(float a, float b, float x)
        {
            float t = Mathf.Clamp01((x - a) / (b - a));
            return t * t * (3f - 2f * t);
        }

        // =====================================================================
        // Fabrication
        // =====================================================================

        static Result Build(ShopItem item)
        {
            var rnd = new System.Random(item.Id.GetHashCode());
            float seed = (item.Id.GetHashCode() & 0xFFF) * 0.13f;
            Color c1 = item.Color, c2 = item.SecondaryColor, c3 = item.AccentColor, glow = item.GlowColor;
            c1.a = 1f;
            var px = new Color[Size * Size];
            Color[] em = null;
            var result = new Result();
            int count = item.PatternCount;

            // Points semés au hasard (u, v, rayon en m) pour les motifs à taches.
            List<Vector3> Scatter(int n, float rMin, float rMax, float vMin = 0f, float vMax = 1f)
            {
                var list = new List<Vector3>();
                for (int i = 0; i < n; i++)
                    list.Add(new Vector3((float)rnd.NextDouble(), Mathf.Lerp(vMin, vMax, (float)rnd.NextDouble()), Mathf.Lerp(rMin, rMax, (float)rnd.NextDouble())));
                return list;
            }

            List<Vector3> spots = null, cells = null;
            switch (item.Pattern)
            {
                case SkinPattern.Taches: spots = Scatter(70, 0.02f, 0.07f); break;
                case SkinPattern.GrandesTaches: spots = Scatter(14, 0.14f, 0.28f); break;
                case SkinPattern.Rosettes: spots = Scatter(60, 0.04f, 0.07f); break;
                case SkinPattern.Alveoles: spots = Scatter(260, 0.012f, 0.022f); break;
                case SkinPattern.Reseau: cells = Scatter(90, 0f, 0f); break;
                case SkinPattern.Points:
                {
                    // 7 points placés (pas un motif répété), dont un sur le front (devant = u 0,5).
                    spots = new List<Vector3> { new Vector3(0.5f, 0.9f, 0.08f) };
                    spots.AddRange(Scatter(Mathf.Max(1, (count > 0 ? count : 7) - 1), 0.08f, 0.11f, 0.12f, 0.75f));
                    break;
                }
                case SkinPattern.Bois: spots = Scatter(10, 0.02f, 0.035f, 0.1f, 0.85f); break;   // coquillages
                case SkinPattern.Ventouses:
                {
                    // 3 rangées de ventouses qui descendent le long de l'encolure (devant).
                    spots = new List<Vector3>();
                    for (int row = 0; row < 3; row++)
                        for (float v = 0.3f; v < 0.74f; v += 0.07f)
                            spots.Add(new Vector3(0.44f + 0.06f * row, v + 0.02f * row, 0.035f));
                    // ~20 taches lumineuses (jamais sur les yeux, v ≈ 0,83 devant).
                    result.spots = new List<Vector4>();
                    foreach (var s in Scatter(22, 0.02f, 0.04f, 0.08f, 0.95f))
                        if (Dist(s.x, s.y, 0.5f, 0.84f) > 0.25f) result.spots.Add(new Vector4(s.x, s.y, s.z, s.y));
                    break;
                }
            }

            for (int y = 0; y < Size; y++)
            {
                float v = (y + 0.5f) / Size;
                for (int x = 0; x < Size; x++)
                {
                    float u = (x + 0.5f) / Size;
                    Color c = c1;
                    Color e = Color.black;
                    switch (item.Pattern)
                    {
                        case SkinPattern.Bandes:
                        {
                            int n = count > 0 ? count : 8;
                            c = Mathf.FloorToInt(v * n) % 2 == 0 ? c1 : c2;
                            break;
                        }
                        case SkinPattern.BandesOndulees:
                        {
                            int n = count > 0 ? count : 12;
                            float s = v * n + 0.18f * Mathf.Sin(u * Mathf.PI * 2f * 3f + v * 5f) + 0.35f * (Noise(u, v, 6f, 6f, seed) - 0.5f);
                            float f = Frac(s);
                            // Rayures plus fines aux bouts (« en pointe ») : épaisseur qui varie avec u.
                            float w = 0.4f + 0.12f * Mathf.Sin(u * Mathf.PI * 2f * 5f + seed);
                            c = f < w ? c2 : c1;
                            break;
                        }
                        case SkinPattern.Taches:
                        case SkinPattern.GrandesTaches:
                        {
                            float wobble = item.Pattern == SkinPattern.GrandesTaches ? 0.09f * (Noise(u, v, 10f, 10f, seed) - 0.5f) * 2f : 0f;
                            foreach (var s in spots) if (Dist(u, v, s.x, s.y) < s.z + wobble) { c = c2; break; }
                            break;
                        }
                        case SkinPattern.Points:
                            foreach (var s in spots) if (Dist(u, v, s.x, s.y) < s.z) { c = c2; break; }
                            break;
                        case SkinPattern.Rosettes:
                            foreach (var s in spots)
                            {
                                float d = Dist(u, v, s.x, s.y);
                                if (d < s.z * 0.55f) { c = c3; break; }           // centre ocre
                                if (d < s.z) { c = c2; break; }                   // anneau brun
                            }
                            break;
                        case SkinPattern.Reseau:
                        {
                            float d1 = 99f, d2 = 99f;
                            foreach (var p in cells)
                            {
                                float d = Dist(u, v, p.x, p.y);
                                if (d < d1) { d2 = d1; d1 = d; } else if (d < d2) d2 = d;
                            }
                            c = d2 - d1 < 0.03f ? c1 : c2; // lignes claires entre les taches brunes
                            break;
                        }
                        case SkinPattern.Ecailles:
                        case SkinPattern.Plumes:
                        {
                            bool plumes = item.Pattern == SkinPattern.Plumes;
                            // Écailles ≈ 12 cm, plumes ≈ 17 cm de haut (lisibles depuis la caméra du jeu).
                            float rows = plumes ? 12f : 16f, cols = plumes ? 18f : 26f;
                            float ry = v * rows;
                            int row = Mathf.FloorToInt(ry);
                            float cx = u * cols + (row % 2 == 0 ? 0f : 0.5f);
                            float fx = Frac(cx) - 0.5f, fy = Frac(ry);
                            // Une écaille = un arrondi en bas ; le liseré clair dessine son bord.
                            float r = Mathf.Sqrt(fx * fx * (plumes ? 1.6f : 1f) + fy * fy);
                            Color baseCol = Color.Lerp(c2, c1, v); // dégradé du socle vers les oreilles
                            if (plumes) baseCol = Color.Lerp(c1, c2, v);
                            // Dedans : la couleur (un peu plus claire en haut) ; un fin liseré au bord ;
                            // dehors : l'écaille voisine, un peu plus sombre (elle passe dessous).
                            bool rim = r > 0.42f && r < 0.5f;
                            c = r <= 0.42f ? Color.Lerp(baseCol * 0.92f, baseCol, fy)
                              : rim ? Color.Lerp(baseCol, c3, 0.6f) : baseCol * 0.82f;
                            if (plumes && rim) e = c3; // bords de plumes qui luisent (Paon)
                            break;
                        }
                        case SkinPattern.Bois:
                        {
                            float grain = Noise(u, v, 70f, 3f, seed);
                            c = Color.Lerp(c1, c2, Smooth(0.55f, 0.75f, grain));
                            c = Color.Lerp(c, c1 * 1.08f, 0.25f * Noise(u, v, 8f, 8f, seed + 3f));
                            foreach (var s in spots) if (Dist(u, v, s.x, s.y) < s.z) { c = c3; break; }
                            break;
                        }
                        case SkinPattern.Alveoles:
                        {
                            c = Color.Lerp(c1, c1 * 0.92f, Noise(u, v, 12f, 12f, seed));
                            foreach (var s in spots)
                            {
                                float d = Dist(u, v, s.x, s.y);
                                if (d < s.z) { c = d > s.z * 0.55f ? c2 : Color.Lerp(c1, c2, 0.4f); break; }
                            }
                            break;
                        }
                        case SkinPattern.Patine:
                        {
                            float n = Noise(u, v, 9f, 7f, seed) * 0.7f + Noise(u, v, 25f, 20f, seed + 5f) * 0.3f;
                            c = n > 0.52f ? c2 : Color.Lerp(c1, c1 * 1.15f, n);
                            break;
                        }
                        case SkinPattern.Parchemin:
                        {
                            float n = Noise(u, v, 10f, 10f, seed);
                            c = Color.Lerp(c1, c3, 0.35f * Smooth(0.5f, 0.8f, n));
                            // Le chemin en pointillés (une courbe qui fait le tour) et deux croix « X ».
                            float pathV = 0.45f + 0.18f * Mathf.Sin(u * Mathf.PI * 2f * 2f + 1f);
                            if (Mathf.Abs(v - pathV) * Height < 0.012f && Frac(u * 60f) < 0.5f) c = c2;
                            foreach (var cross in new[] { new Vector2(0.62f, 0.3f), new Vector2(0.2f, 0.65f) })
                            {
                                float du = (u - cross.x) * Around, dv = (v - cross.y) * Height;
                                if (Mathf.Abs(du) < 0.06f && Mathf.Abs(dv) < 0.06f && (Mathf.Abs(du - dv) < 0.012f || Mathf.Abs(du + dv) < 0.012f)) c = c2;
                            }
                            break;
                        }
                        case SkinPattern.Panda:
                        {
                            // Taches « en larme » autour des yeux (plus longues vers le bas), oreilles et
                            // bas du corps noirs. Les yeux restent visibles (ils sont devant la tache).
                            float lv = v < 0.82f ? 0.82f + (v - 0.82f) * 0.55f : v; // étire la tache vers le bas
                            bool dark = v < 0.2f || v > 0.93f
                                     || Dist(u, lv, 0.45f, 0.82f) < 0.12f || Dist(u, lv, 0.55f, 0.82f) < 0.12f;
                            c = dark ? c2 : c1;
                            break;
                        }
                        case SkinPattern.Degrade:
                            c = Color.Lerp(c2, c1, Smooth(0f, 1f, v));
                            c = Color.Lerp(c, c * 0.93f, 0.5f * Noise(u, v, 14f, 14f, seed));
                            if (glow.a > 0f && item.EmissionStrength > 0f) e = Color.black; // les effets animés dessinent la lumière
                            break;
                        case SkinPattern.Ventouses:
                        {
                            c = Color.Lerp(c1, c2, Smooth(0.75f, 0.98f, v));
                            foreach (var s in spots)
                            {
                                float d = Dist(u, v, s.x, s.y);
                                if (d < s.z) { c = d > s.z * 0.7f ? c3 * 0.75f : c3; break; }
                            }
                            break;
                        }
                        case SkinPattern.LignesAbyssales:
                        {
                            c = c1;
                            // Lignes de méduse : des courbes qui ondulent autour du corps.
                            for (int k = 0; k < 5; k++)
                            {
                                float lv = 0.15f + 0.17f * k + 0.04f * Mathf.Sin(u * Mathf.PI * 2f * (3 + k) + k);
                                if (Mathf.Abs(v - lv) * Height < 0.012f) { e = k % 2 == 0 ? c2 : c3; c = Color.Lerp(c1, e, 0.5f); }
                            }
                            break;
                        }
                        case SkinPattern.Marbre:
                        {
                            float n = Noise(u, v, 4f, 3f, seed) + 0.5f * Noise(u, v, 12f, 9f, seed + 2f);
                            float s = Mathf.Abs(Mathf.Sin((v * 6f + n * 3f) * Mathf.PI));
                            c = Color.Lerp(c1, c2, Smooth(0.3f, 0.9f, s));
                            break;
                        }
                        case SkinPattern.Veines:
                        {
                            c = Color.Lerp(c1, c1 * 0.85f, Noise(u, v, 10f, 10f, seed));
                            float n = Noise(u, v, 7f, 9f, seed + 1f);
                            if (Mathf.Abs(n - 0.5f) < 0.018f) { c = Color.Lerp(c1, c2, 0.6f); e = c2; }
                            break;
                        }
                    }
                    c.a = 1f;
                    px[y * Size + x] = c;
                    if (e != Color.black)
                    {
                        if (em == null) em = new Color[Size * Size];
                        em[y * Size + x] = e;
                    }
                }
            }

            result.baseMap = NewTexture("Motif " + item.Id);
            result.baseMap.SetPixels(px);
            result.baseMap.Apply(true);
            if (em != null)
            {
                result.emissionMap = NewTexture("Lumière " + item.Id);
                result.emissionMap.SetPixels(em);
                result.emissionMap.Apply(true);
            }
            return result;
        }
    }
}
