using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace TourDeLaChance.EditorTools
{
    /// <summary>
    /// OUTIL D'ÉDITEUR (il ne fait pas partie du jeu) : relie automatiquement les icônes à leur
    /// fiche, PAR IDENTIFIANT (spec v5 §17.3).
    ///
    /// Comment ça marche : dès qu'une image est déposée (ou remplacée) dans
    /// Assets/Art/icones_detourees/, Unity l'importe ; ce script est alors prévenu. Si le nom du
    /// fichier est l'identifiant d'un objet (« tricorne.png » → la fiche dont l'id est
    /// « tricorne »), d'une boîte (« boite_pirates_skins.png ») ou d'un pack de diamants, il
    /// règle l'image en Sprite et la branche dans la bonne case. Aucune ligne de code à
    /// changer quand le directeur artistique livre de nouvelles icônes : il suffit de les copier.
    ///
    /// Menu « Tour de la Chance › Brancher les icônes » : refait le branchement de toutes les
    /// icônes déjà présentes (utile après un changement de catalogue).
    /// Un objet SANS icône garde son substitut dessiné par l'interface.
    /// v6 : il branche aussi les images d'interface (UiImages : icone_route, drapeau_record,
    /// renaissance_heros…) et les images des lots d'échange (echange_sac…).
    /// </summary>
    public class IconAutoBinder : AssetPostprocessor
    {
        public const string IconFolder = "Assets/Art/icones_detourees/";

        // 1. Avant l'import : toute image du dossier est réglée en Sprite (comme les autres icônes).
        /// v6 (art bible §15.1.5) : les bulles des boutons rendues dans Blender.
        public const string BubbleFolder = "Assets/UI/Bulles/";

        void OnPreprocessTexture()
        {
            if (assetPath.StartsWith(BubbleFolder)) { PrepareBubble(); return; }
            if (!assetPath.StartsWith(IconFolder)) return;
            var importer = (TextureImporter)assetImporter;
            if (importer.textureType == TextureImporterType.Sprite) return;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            // v7 (QA point 10) : mip maps ACTIVÉES. Les icônes sont affichées bien plus petites
            // que leur image (1024 → 60 à 200 px) : sans mip maps, Unity saute des pixels et les
            // contours scintillent ou crénellent ; avec, il prend une version réduite propre.
            importer.mipmapEnabled = true;
            PrepareSpecial(importer, Path.GetFileNameWithoutExtension(assetPath));
        }

        /// v7 : réglages propres à certaines images du dossier (appelé aussi par ReimportAll).
        ///   - coffre_* : 1024 px au plus (au lieu de 512) : les icônes de coffres sont grandes ;
        ///   - ruban_palier : 9-slice, bords gauche et droit de 120 px (seul le milieu s'étire).
        public static void PrepareSpecial(TextureImporter importer, string id)
        {
            if (id.StartsWith("coffre_")) importer.maxTextureSize = Mathf.Max(importer.maxTextureSize, 1024);
            // Vector4 du bord d'un sprite : (gauche, BAS, droite, haut).
            if (id == "ruban_palier") importer.spriteBorder = new Vector4(120, 0, 120, 0);
        }

        /// v7 : passe TOUTES les icônes déjà importées en mip maps (et réglages spéciaux), puis
        /// les réimporte. Menu « Tour de la Chance › Icônes : mip maps ».
        [MenuItem("Tour de la Chance/Icônes : mip maps")]
        public static int ReimportAllWithMipmaps()
        {
            int n = 0;
            foreach (var path in Directory.GetFiles(IconFolder, "*.png").Select(p => p.Replace('\\', '/')))
            {
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) continue;
                bool before = importer.mipmapEnabled;
                int size = importer.maxTextureSize;
                var border = importer.spriteBorder;
                importer.mipmapEnabled = true;
                PrepareSpecial(importer, Path.GetFileNameWithoutExtension(path));
                if (before != importer.mipmapEnabled || size != importer.maxTextureSize || border != importer.spriteBorder)
                {
                    importer.SaveAndReimport();
                    n++;
                }
            }
            return n;
        }

        /// Une bulle : Sprite, transparence, bords nets (Clamp), mip maps (elle est affichée à la
        /// moitié de sa taille). Les bulles rectangulaires ont les bords du 9-slice : gauche 80,
        /// droite 80, haut 80, bas 100 (pixels de l'image) : seul le milieu s'étire en largeur.
        void PrepareBubble()
        {
            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            // Vector4 du bord d'un sprite : (gauche, BAS, droite, haut).
            importer.spriteBorder = Path.GetFileName(assetPath).StartsWith("bulle_rect_")
                ? new Vector4(80, 100, 80, 80) : Vector4.zero;
        }

        // 2. Après l'import : on branche les icônes qui viennent d'arriver.
        static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            var icons = imported.Where(p => p.StartsWith(IconFolder) && p.EndsWith(".png")).ToList();
            if (icons.Count == 0) return;
            // On attend la fin de l'import en cours pour toucher aux fiches (règle d'Unity).
            EditorApplication.delayCall += () => Bind(icons);
        }

        [MenuItem("Tour de la Chance/Brancher les icônes")]
        public static void BindAll()
        {
            var all = Directory.GetFiles(IconFolder, "*.png").Select(p => p.Replace('\\', '/')).ToList();
            int n = Bind(all);
            Debug.Log($"Icônes : {n} branchement(s) mis à jour.");
        }

        /// Branche une liste d'images (chemins d'assets). Renvoie le nombre de cases changées.
        public static int Bind(List<string> paths)
        {
            var catalog = Resources.Load<ShopCatalog>("ShopCatalog");
            var eco = Resources.Load<EconomyCatalog>("EconomyCatalog");
            var ui = Resources.Load<UiImages>("UiImages");
            if (catalog == null) return 0;
            var items = catalog.AllItems.Where(i => i != null).GroupBy(i => i.Id).ToDictionary(g => g.Key, g => g.First());
            int changed = 0;
            bool catalogDirty = false, ecoDirty = false;
            foreach (var path in paths)
            {
                string id = Path.GetFileNameWithoutExtension(path);
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite == null) continue;

                // v6 : une image d'interface (UiImages). Pas de « continue » : piece.png et
                // diamant.png peuvent aussi servir ailleurs.
                string field = UiImages.FieldFor(id);
                if (ui != null && field != null)
                {
                    var uso = new SerializedObject(ui);
                    var prop = uso.FindProperty(field);
                    if (prop != null && prop.objectReferenceValue != sprite)
                    {
                        prop.objectReferenceValue = sprite;
                        uso.ApplyModifiedPropertiesWithoutUndo();
                        EditorUtility.SetDirty(ui);
                        changed++;
                    }
                }

                // Un objet (skin ou accessoire) : case « icon » de sa fiche.
                if (items.TryGetValue(id, out var item))
                {
                    var so = new SerializedObject(item);
                    var prop = so.FindProperty("icon");
                    if (prop.objectReferenceValue != sprite)
                    {
                        prop.objectReferenceValue = sprite;
                        so.ApplyModifiedPropertiesWithoutUndo();
                        EditorUtility.SetDirty(item);
                        changed++;
                    }
                    continue;
                }
                // Une boîte : son image (Boutique, Index).
                var box = catalog.Boxes.FirstOrDefault(b => b.id == id);
                if (box != null)
                {
                    if (box.icon != sprite) { box.icon = sprite; catalogDirty = true; changed++; }
                    continue;
                }
                // Un pack de diamants (affichage seulement) : par identifiant.
                if (eco != null)
                {
                    var so = new SerializedObject(eco);
                    var packs = so.FindProperty("diamondPacks");
                    for (int i = 0; i < packs.arraySize; i++)
                    {
                        var p = packs.GetArrayElementAtIndex(i);
                        if (p.FindPropertyRelative("id").stringValue != id) continue;
                        var icon = p.FindPropertyRelative("icon");
                        if (icon.objectReferenceValue != sprite) { icon.objectReferenceValue = sprite; ecoDirty = true; changed++; }
                    }
                    // v6 : les lots d'échange (echange_poignee, echange_sac, echange_coffre).
                    var coinPacks = so.FindProperty("coinPacks");
                    for (int i = 0; coinPacks != null && i < coinPacks.arraySize; i++)
                    {
                        var p = coinPacks.GetArrayElementAtIndex(i);
                        if (p.FindPropertyRelative("id").stringValue != id) continue;
                        var icon = p.FindPropertyRelative("icon");
                        if (icon.objectReferenceValue != sprite) { icon.objectReferenceValue = sprite; ecoDirty = true; changed++; }
                    }
                    if (ecoDirty) so.ApplyModifiedPropertiesWithoutUndo();
                }
            }
            if (catalogDirty) EditorUtility.SetDirty(catalog);
            if (ecoDirty) EditorUtility.SetDirty(eco);
            if (changed > 0) AssetDatabase.SaveAssets();
            return changed;
        }
    }
}
