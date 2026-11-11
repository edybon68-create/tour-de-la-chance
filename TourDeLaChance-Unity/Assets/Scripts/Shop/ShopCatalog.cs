using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TourDeLaChance
{
    /// <summary>
    /// Le catalogue : LA source unique des raretés (et donc des chances), des boîtes et des
    /// objets. Le tirage ET l'interface lisent les mêmes nombres ici : impossible d'afficher
    /// un pourcentage différent de celui du code (règle d'or, spec boites §2.3).
    ///
    /// Le fichier ShopCatalog.asset est rangé dans un dossier "Resources" : Unity peut
    /// alors le charger par son nom depuis le code (Resources.Load), sans référence à brancher.
    /// </summary>
    [CreateAssetMenu(fileName = "ShopCatalog", menuName = "Tour de la Chance/Catalogue du shop")]
    public class ShopCatalog : ScriptableObject
    {
        [Tooltip("Les raretés, de la plus courante à la plus rare.")]
        [SerializeField] List<RarityDef> rarities = new List<RarityDef>();
        [SerializeField] List<LootBox> boxes = new List<LootBox>();
        [Tooltip("Objets gratuits, possédés d'office : 1 skin par défaut + 1 « Aucun » par emplacement.")]
        [SerializeField] List<ShopItem> freeItems = new List<ShopItem>();
        [Tooltip("v4 §9.1 : plafond du bonus de tenue (somme des 5 objets équipés), en %. " +
                 "Garde-fou pour les futures boîtes.")]
        [SerializeField, Min(0)] int outfitBonusCap = 50;

        public IReadOnlyList<RarityDef> Rarities => rarities;
        public IReadOnlyList<LootBox> Boxes => boxes;
        public IReadOnlyList<ShopItem> FreeItems => freeItems;
        public int OutfitBonusCap => outfitBonusCap;

        /// Tous les objets : gratuits, puis ceux des boîtes.
        public IEnumerable<ShopItem> AllItems =>
            freeItems.Where(i => i != null).Concat(boxes.SelectMany(b => b.items).Where(i => i != null));

        /// La somme des poids (1000 = 100 %).
        public int TotalWeight => rarities.Sum(r => r.weight);

        public RarityDef GetRarity(string id) => rarities.FirstOrDefault(r => r.id == id);

        /// La place d'une rareté dans la liste (0 = Commun … 5 = Secret), -1 si inconnue.
        public int RarityRank(string id) => rarities.FindIndex(r => r.id == id);

        /// Contrôle des données (spec §4.3) : chaque boîte doit avoir au moins un objet de
        /// chaque rareté, et la somme des poids doit faire 1000. C'est une erreur de DONNÉES
        /// (pas un cas de jeu) : on l'écrit en rouge dans la console. Renvoie true si tout va bien.
        public bool Validate()
        {
            bool ok = true;
            if (TotalWeight != 1000)
            {
                Debug.LogError($"Catalogue : la somme des poids des raretés vaut {TotalWeight} au lieu de 1000.");
                ok = false;
            }
            foreach (var box in boxes)
            {
                foreach (var r in rarities)
                {
                    if (!box.items.Any(i => i != null && i.RarityId == r.id))
                    {
                        Debug.LogError($"Catalogue : la boîte « {box.displayName} » n'a aucun objet de rareté « {r.displayName} ».");
                        ok = false;
                    }
                }
                foreach (var item in box.items)
                {
                    if (item == null) { Debug.LogError($"Catalogue : case vide dans « {box.displayName} »."); ok = false; }
                    else if (GetRarity(item.RarityId) == null)
                    {
                        Debug.LogError($"Catalogue : « {item.Id} » a une rareté inconnue (« {item.RarityId} »).");
                        ok = false;
                    }
                }
                foreach (var r in rarities)
                {
                    if (r.sellValue >= box.price)
                    {
                        Debug.LogError($"Catalogue : la revente d'un {r.displayName} ({r.sellValue}) doit rester sous le prix de « {box.displayName} ».");
                        ok = false;
                    }
                }
            }
            return ok;
        }
    }
}
