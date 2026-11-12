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
    /// </summary>
    public class IconAutoBinder : AssetPostprocessor
    {
        public const string IconFolder = "Assets/Art/icones_detourees/";

        // 1. Avant l'import : toute image du dossier est réglée en Sprite (comme les autres icônes).
        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(IconFolder)) return;
            var importer = (TextureImporter)assetImporter;
            if (importer.textureType == TextureImporterType.Sprite) return;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
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
            if (catalog == null) return 0;
            var items = catalog.AllItems.Where(i => i != null).GroupBy(i => i.Id).ToDictionary(g => g.Key, g => g.First());
            int changed = 0;
            bool catalogDirty = false, ecoDirty = false;
            foreach (var path in paths)
            {
                string id = Path.GetFileNameWithoutExtension(path);
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite == null) continue;

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
