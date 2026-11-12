using UnityEngine;

namespace TourDeLaChance
{
    /// <summary>
    /// Les images des effets de révélation, DESSINÉES PAR LE CODE (art-bible §10.3) :
    /// un halo (disque flou) et une étincelle (étoile à 4 branches). Elles sont blanches :
    /// l'interface les teinte avec la couleur de la rareté.
    /// Les rayons, eux, sont dessinés par RaysElement (leur nombre change selon la rareté).
    ///
    /// v4 (spec §5.3, §5.6, §6.1) : les DÉGRADÉS des boutons, des pop-ups et du badge d'étage
    /// sont aussi fabriqués ici (VerticalGradient, 1 × 32 pixels, du « clair » en haut à la
    /// « base » en bas). Comme une feuille USS ne peut pas lire une texture créée pendant le
    /// jeu, BakeAll() (outil de l'éditeur) les enregistre une fois en PNG dans
    /// Assets/UI/Degrades/ ; les USS y renvoient avec background-image. Pour changer une
    /// couleur : modifier la liste Gradients ci-dessous, puis relancer BakeAll().
    /// </summary>
    public static class UiFxTextures
    {
        static Texture2D halo, sparkle;

        /// Disque flou 128 × 128 : opacité 1 au centre, 0 au bord, décroissance (1 − r)².
        public static Texture2D Halo
        {
            get
            {
                if (halo == null)
                {
                    const int size = 128;
                    halo = NewTexture("Halo", size);
                    for (int y = 0; y < size; y++)
                        for (int x = 0; x < size; x++)
                        {
                            float dx = (x + 0.5f) / size * 2f - 1f, dy = (y + 0.5f) / size * 2f - 1f;
                            float r = Mathf.Clamp01(Mathf.Sqrt(dx * dx + dy * dy));
                            float a = (1f - r) * (1f - r);
                            halo.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                        }
                    halo.Apply();
                }
                return halo;
            }
        }

        /// Étoile à 4 branches aux côtés creusés, 32 × 32 (même forme que les scintillements
        /// du ciel). Astuce : |x|^0,5 + |y|^0,5 < 1 donne exactement cette forme « en croix douce ».
        public static Texture2D Sparkle
        {
            get
            {
                if (sparkle == null)
                {
                    const int size = 32;
                    sparkle = NewTexture("Etincelle", size);
                    for (int y = 0; y < size; y++)
                        for (int x = 0; x < size; x++)
                        {
                            float dx = Mathf.Abs((x + 0.5f) / size * 2f - 1f), dy = Mathf.Abs((y + 0.5f) / size * 2f - 1f);
                            float d = Mathf.Sqrt(dx) + Mathf.Sqrt(dy);
                            float a = Mathf.Clamp01((1f - d) * 6f); // bord adouci
                            sparkle.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                        }
                    sparkle.Apply();
                }
                return sparkle;
            }
        }

        /// Un dégradé vertical 1 × height : "top" en haut, "bottom" en bas.
        public static Texture2D VerticalGradient(Color top, Color bottom, int height = 32)
        {
            var t = new Texture2D(1, height, TextureFormat.RGBA32, false)
            {
                name = "Degrade",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.DontSave
            };
            // Dans une texture, la ligne y = 0 est EN BAS.
            for (int y = 0; y < height; y++) t.SetPixel(0, y, Color.Lerp(bottom, top, y / (height - 1f)));
            t.Apply();
            return t;
        }

        static Color Hex(string hex) => ColorUtility.TryParseHtmlString(hex, out var c) ? c : Color.magenta;

        /// Les dégradés de l'interface v4 : (nom du fichier, haut, bas).
        public static readonly (string file, string top, string bottom)[] Gradients =
        {
            // Familles de boutons (spec §5.4) : « clair » en haut → « base » en bas.
            ("bouton_vert", "#5AD68C", "#2E9E5B"),
            ("bouton_violet", "#A08CFF", "#6C4FE0"),
            ("bouton_or", "#FFE58A", "#FFC93C"),
            ("bouton_bleu", "#7DB5FF", "#3B7DD8"),
            ("bouton_rouge", "#FF8F92", "#F0484E"),
            ("bouton_sombre", "#5A4F88", "#3A3358"),
            // Corps des pop-ups : v5.1, BLANC CASSÉ (art bible §12.18), à peine plus foncé en bas.
            ("popup_corps", "#FFFDF8", "#F1ECE2"),
            // Badge d'étage, un par palier de décor (§6.1).
            ("badge_palier1", "#7DB5FF", "#3B7DD8"),
            ("badge_palier2", "#FFAA8A", "#E0705A"),
            ("badge_palier3", "#A08CFF", "#6C4FE0"),
            ("badge_palier4", "#5A4AB0", "#2E2366"),
            // Ombres « il y a encore du contenu » en haut et en bas des listes (§5.7).
            // v5.1 : de la couleur du fond blanc cassé (sinon une bande sombre salirait la liste).
            ("fondu_haut", "#F8F4EDFF", "#F8F4ED00"),
            ("fondu_bas", "#F4F0E800", "#F4F0E8FF"),
        };

        // =====================================================================
        // v5.1 (art bible §12.18) : les fonds des CARTES et le motif en filigrane des pop-ups
        // =====================================================================

        /// Les fonds de cartes : (nom du fichier, couleur en haut à gauche, couleur en bas à
        /// droite). Un dégradé en diagonale + une lueur blanche douce (le « rayonnement »).
        public static readonly (string file, string from, string to)[] CardGradients =
        {
            ("carte_rose", "#FFC94A", "#FF4F8B"),     // boîtes classiques (pièces)
            ("carte_bleue", "#62E6FF", "#3B5BDB"),    // boîtes Pirates, amélioration « pièges », « Tu gardes »
            ("carte_verte", "#D2F55A", "#1FAE6A"),    // boîtes Animaux
            ("carte_violette", "#D2B4FF", "#6C4FE0"), // packs de diamants, amélioration « coffres »
            ("carte_or", "#FFE66B", "#FF9F1C"),       // amélioration « gains », « Tu gagnes »
            ("carte_rouge", "#FFA08A", "#E0404A"),    // « Tu perds »
        };

        /// Un fond de carte size × size : dégradé en diagonale de "from" (haut gauche) à "to"
        /// (bas droite), éclairci par une lueur blanche ronde centrée un peu au-dessus du milieu.
        public static Texture2D CardGradient(Color from, Color to, int size = 256)
        {
            var t = NewTexture("Carte", size);
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float u = (x + 0.5f) / size, v = 1f - (y + 0.5f) / size; // v = 0 en HAUT
                    var c = Color.Lerp(from, to, Mathf.Clamp01((u + v) * 0.5f));
                    // La lueur : (1 − d)² autour de (0,5 ; 0,42), jusqu'à 35 % de blanc.
                    float dx = u - 0.5f, dy = v - 0.42f;
                    float d = Mathf.Clamp01(Mathf.Sqrt(dx * dx + dy * dy) / 0.6f);
                    c = Color.Lerp(c, Color.white, 0.35f * (1f - d) * (1f - d));
                    c.a = 1f;
                    t.SetPixel(x, y, c);
                }
            t.Apply();
            return t;
        }

        /// Le motif répété en filigrane du fond des pop-ups (192 × 192, répété par le USS) :
        /// une étoile, une pièce et un diamant, en encre, sur fond transparent. Le USS le pose
        /// à ~6 % d'opacité. Chaque pixel est échantillonné 4 × 4 fois : des bords lisses.
        public static Texture2D PatternTile(int size = 192)
        {
            var t = NewTexture("Motif", size);
            var ink = Hex("#110D1F");
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    int hits = 0;
                    for (int sy = 0; sy < 4; sy++)
                        for (int sx = 0; sx < 4; sx++)
                        {
                            // Coordonnées en pixels, y = 0 en HAUT (plus simple pour dessiner).
                            var p = new Vector2(x + (sx + 0.5f) / 4f, size - (y + (sy + 0.5f) / 4f));
                            if (InPattern(p, size)) hits++;
                        }
                    t.SetPixel(x, y, new Color(ink.r, ink.g, ink.b, hits / 16f));
                }
            t.Apply();
            return t;
        }

        /// Le point p fait-il partie d'une des 4 formes du motif ? (Positions choisies pour que
        /// le motif se raccorde sans couture quand il est répété.)
        static bool InPattern(Vector2 p, int size)
        {
            float s = size / 192f;
            return InStar(p, new Vector2(48, 48) * s, 26 * s, 11 * s)
                || InCoin(p, new Vector2(144, 60) * s, 20 * s)
                || InDiamond(p, new Vector2(56, 146) * s, 22 * s)
                || InStar(p, new Vector2(146, 150) * s, 14 * s, 6 * s);
        }

        /// Étoile à 5 branches (rayons extérieur R et intérieur r), pointe vers le haut.
        static bool InStar(Vector2 p, Vector2 c, float R, float r)
        {
            var pts = new Vector2[10];
            for (int i = 0; i < 10; i++)
            {
                float a = -Mathf.PI / 2f + i * Mathf.PI / 5f;
                float rr = i % 2 == 0 ? R : r;
                pts[i] = c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * rr;
            }
            return InPolygon(p, pts);
        }

        /// Une pièce : un anneau épais et un petit rond au centre.
        static bool InCoin(Vector2 p, Vector2 c, float R)
        {
            float d = Vector2.Distance(p, c);
            return (d <= R && d >= R * 0.72f) || d <= R * 0.4f;
        }

        /// Un diamant taillé : le haut plat (trapèze) et la pointe en bas (triangle).
        static bool InDiamond(Vector2 p, Vector2 c, float R)
        {
            var pts = new[]
            {
                c + new Vector2(-R * 0.55f, -R * 0.7f), c + new Vector2(R * 0.55f, -R * 0.7f),
                c + new Vector2(R, -R * 0.2f), c + new Vector2(0f, R), c + new Vector2(-R, -R * 0.2f)
            };
            return InPolygon(p, pts);
        }

        /// Test « point dans un polygone » (règle du nombre de croisements).
        static bool InPolygon(Vector2 p, Vector2[] pts)
        {
            bool inside = false;
            for (int i = 0, j = pts.Length - 1; i < pts.Length; j = i++)
            {
                if ((pts[i].y > p.y) != (pts[j].y > p.y) &&
                    p.x < (pts[j].x - pts[i].x) * (p.y - pts[i].y) / (pts[j].y - pts[i].y) + pts[i].x)
                    inside = !inside;
            }
            return inside;
        }

#if UNITY_EDITOR
        /// OUTIL DE L'ÉDITEUR : enregistre tous les dégradés en PNG dans Assets/UI/Degrades/.
        public static string BakeAll()
        {
            const string folder = "Assets/UI/Degrades";
            System.IO.Directory.CreateDirectory(folder);
            foreach (var g in Gradients) Save(VerticalGradient(Hex(g.top), Hex(g.bottom)), $"{folder}/{g.file}.png");
            // v5.1 : les fonds de cartes et le motif en filigrane.
            foreach (var g in CardGradients) Save(CardGradient(Hex(g.from), Hex(g.to)), $"{folder}/{g.file}.png");
            Save(PatternTile(), $"{folder}/motif_popup.png");
            return $"{Gradients.Length + CardGradients.Length + 1} images enregistrées dans {folder}";
        }

        /// Enregistre une texture en PNG et règle son import (pas de compression ni de mipmaps).
        static void Save(Texture2D tex, string path)
        {
            System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            UnityEditor.AssetDatabase.ImportAsset(path);
            if (UnityEditor.AssetImporter.GetAtPath(path) is UnityEditor.TextureImporter imp)
            {
                imp.textureType = UnityEditor.TextureImporterType.Default;
                imp.mipmapEnabled = false;
                imp.wrapMode = TextureWrapMode.Clamp;
                imp.filterMode = FilterMode.Bilinear;
                imp.alphaIsTransparency = true;
                imp.npotScale = UnityEditor.TextureImporterNPOTScale.None;
                imp.textureCompression = UnityEditor.TextureImporterCompression.Uncompressed;
                imp.SaveAndReimport();
            }
        }
#endif

        static Texture2D NewTexture(string name, int size) => new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = name,
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            hideFlags = HideFlags.DontSave
        };
    }
}
