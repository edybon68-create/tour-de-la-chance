using UnityEngine;

namespace TourDeLaChance
{
    /// <summary>
    /// Les images des effets de révélation, DESSINÉES PAR LE CODE (art-bible §10.3) :
    /// un halo (disque flou) et une étincelle (étoile à 4 branches). Elles sont blanches :
    /// l'interface les teinte avec la couleur de la rareté.
    /// Les rayons, eux, sont dessinés par RaysElement (leur nombre change selon la rareté).
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

        static Texture2D NewTexture(string name, int size) => new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = name,
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            hideFlags = HideFlags.DontSave
        };
    }
}
