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

        static Texture2D stripes, dots, galaxy;

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

        /// Galaxie : texture noire avec 7 petits points blancs placés par un générateur à
        /// GRAINE FIXE (toujours le même ciel). Elle sert de carte d'émission : seules les
        /// étoiles brillent, le fond garde la couleur du matériau.
        public static Texture2D GalaxyStars()
        {
            if (galaxy == null)
            {
                galaxy = NewTexture("Galaxie");
                var black = new Color(0f, 0f, 0f, 1f);
                for (int y = 0; y < Size; y++)
                    for (int x = 0; x < Size; x++)
                        galaxy.SetPixel(x, y, black);

                var random = new System.Random(2024); // graine fixe = même ciel à chaque lancement
                for (int s = 0; s < 7; s++)
                {
                    float cx = random.Next(4, Size - 4), cy = random.Next(4, Size - 4);
                    // Une tuile mesure ~0,8 m de large : 1 pixel ≈ 1,2 cm. Rayon 1 à 2,2 px → étoiles de 2 à 5 cm.
                    float r = 1f + (float)random.NextDouble() * 1.2f;
                    for (int y = (int)(cy - 4); y <= (int)(cy + 4); y++)
                        for (int x = (int)(cx - 4); x <= (int)(cx + 4); x++)
                        {
                            float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy));
                            float k = Mathf.Clamp01(r - d + 0.5f);
                            if (k > 0f) galaxy.SetPixel(x, y, Color.Lerp(black, Color.white, Mathf.Max(k, galaxy.GetPixel(x, y).r)));
                        }
                }
                galaxy.Apply(true);
            }
            return galaxy;
        }

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
