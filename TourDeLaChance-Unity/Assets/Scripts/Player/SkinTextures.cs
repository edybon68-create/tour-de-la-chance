using UnityEngine;

namespace TourDeLaChance
{
    /// <summary>
    /// Les petites images (textures) des skins à motif, FABRIQUÉES PAR LE CODE au démarrage
    /// (spec boites §5.3, art-bible §4.1) : pas de fichier dessiné, seulement des aplats nets
    /// de la palette. Chaque texture fait 64 × 64 pixels et se répète sur le corps (tiling).
    ///
    /// On y range aussi la fabrication des coordonnées de texture (UV) « cylindriques » du
    /// corps : le modèle personnage.fbx n'en a pas, et sans elles un motif ne peut pas
    /// s'afficher (voir AddCylindricalUVs).
    /// </summary>
    public static class SkinTextures
    {
        const int Size = 64;

        static Texture2D stripes, dots, rainbow;

        /// Arc-en-ciel v3 (spec v3 §8.1) : 4 × 64 pixels, dégradé vertical qui fait UN TOUR
        /// COMPLET du cercle des couleurs (saturation 0,75, luminosité 1). Il se répète sans
        /// couture (le haut rejoint le bas : rouge → … → rouge).
        public static Texture2D RainbowGradient()
        {
            if (rainbow == null)
            {
                rainbow = new Texture2D(4, 64, TextureFormat.RGBA32, false)
                {
                    name = "ArcEnCiel",
                    wrapMode = TextureWrapMode.Repeat,
                    filterMode = FilterMode.Bilinear,
                    hideFlags = HideFlags.DontSave
                };
                for (int y = 0; y < 64; y++)
                {
                    Color c = Color.HSVToRGB(y / 64f, 0.75f, 1f);
                    for (int x = 0; x < 4; x++) rainbow.SetPixel(x, y, c);
                }
                rainbow.Apply();
            }
            return rainbow;
        }

        /// Rayures : 2 bandes par tuile (haut = couleur 1, bas = couleur 2). Répétée 4 fois
        /// en hauteur, cela fait les 8 bandes du sucre d'orge.
        /// Les couleurs sont dans la texture elle-même (la couleur de base du matériau reste blanche).
        public static Texture2D Stripes(Color a, Color b)
        {
            if (stripes == null)
            {
                stripes = NewTexture("Rayures");
                for (int y = 0; y < Size; y++)
                    for (int x = 0; x < Size; x++)
                        stripes.SetPixel(x, y, y < Size / 2 ? b : a);
                stripes.Apply(true); // true = calcule les mipmaps (pas de scintillement de loin)
            }
            return stripes;
        }

        /// Pois en quinconce : un pois au centre de la tuile + un quart de pois dans chaque coin.
        /// Quand la tuile se répète, les quarts de coins forment des pois entiers décalés.
        public static Texture2D Dots(Color background, Color dot)
        {
            if (dots == null)
            {
                dots = NewTexture("Pois");
                float radius = Size * 0.2f; // diamètre = 40 % de la tuile
                Vector2[] centers =
                {
                    new Vector2(Size * 0.5f, Size * 0.5f),
                    new Vector2(0, 0), new Vector2(Size, 0), new Vector2(0, Size), new Vector2(Size, Size)
                };
                for (int y = 0; y < Size; y++)
                    for (int x = 0; x < Size; x++)
                    {
                        var p = new Vector2(x + 0.5f, y + 0.5f);
                        // Bord adouci sur un pixel : 1 dedans, 0 dehors.
                        float inside = 0f;
                        foreach (var c in centers)
                            inside = Mathf.Max(inside, Mathf.Clamp01(radius - Vector2.Distance(p, c) + 0.5f));
                        dots.SetPixel(x, y, Color.Lerp(background, dot, inside));
                    }
                dots.Apply(true);
            }
            return dots;
        }

        // (v4 : la Galaxie n'est plus une texture fixe ici, voir GalaxyTexture.)

        static Texture2D NewTexture(string name) => new Texture2D(Size, Size, TextureFormat.RGBA32, true)
        {
            name = name,
            wrapMode = TextureWrapMode.Repeat,     // la tuile se répète
            filterMode = FilterMode.Bilinear,      // bords nets mais sans escalier
            hideFlags = HideFlags.DontSave
        };

        /// <summary>
        /// Ajoute des UV « cylindriques » à une COPIE du maillage du corps (art-bible §6.2) :
        /// U fait le tour du corps (0 → 1), V monte de la base (0) au sommet (1).
        /// La couture (là où U repasse de 1 à 0) est placée à l'ARRIÈRE (+z), que la caméra
        /// ne voit jamais. Si le maillage a déjà des UV, on le garde tel quel.
        /// </summary>
        public static Mesh AddCylindricalUVs(Mesh source)
        {
            if (source == null || (source.uv != null && source.uv.Length == source.vertexCount)) return source;

            var mesh = Object.Instantiate(source);
            mesh.name = source.name + " (UV cylindriques)";
            var vertices = mesh.vertices;
            var uvs = new Vector2[vertices.Length];
            Bounds b = source.bounds;
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 v = vertices[i] - b.center;
                // atan2(x, -z) vaut 0 devant (−z) et ±π derrière (+z) : la couture est derrière.
                float u = Mathf.Atan2(v.x, -v.z) / (2f * Mathf.PI) + 0.5f;
                float h = Mathf.InverseLerp(b.min.y, b.max.y, vertices[i].y);
                uvs[i] = new Vector2(u, h);
            }
            mesh.uv = uvs;
            return mesh;
        }
    }
}
