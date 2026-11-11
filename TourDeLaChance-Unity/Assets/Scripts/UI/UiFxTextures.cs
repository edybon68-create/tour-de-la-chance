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
            // Corps des pop-ups (§5.6).
            ("popup_corps", "#3A3358", "#2A2440"),
            // Badge d'étage, un par palier de décor (§6.1).
            ("badge_palier1", "#7DB5FF", "#3B7DD8"),
            ("badge_palier2", "#FFAA8A", "#E0705A"),
            ("badge_palier3", "#A08CFF", "#6C4FE0"),
            ("badge_palier4", "#5A4AB0", "#2E2366"),
            // Ombres « il y a encore du contenu » en haut et en bas des listes (§5.7).
            ("fondu_haut", "#2A2440FF", "#2A244000"),
            ("fondu_bas", "#2A244000", "#2A2440FF"),
        };

#if UNITY_EDITOR
        /// OUTIL DE L'ÉDITEUR : enregistre tous les dégradés en PNG dans Assets/UI/Degrades/.
        public static string BakeAll()
        {
            const string folder = "Assets/UI/Degrades";
            System.IO.Directory.CreateDirectory(folder);
            foreach (var g in Gradients)
            {
                var tex = VerticalGradient(Hex(g.top), Hex(g.bottom));
                string path = $"{folder}/{g.file}.png";
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
            return $"{Gradients.Length} dégradés enregistrés dans {folder}";
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
