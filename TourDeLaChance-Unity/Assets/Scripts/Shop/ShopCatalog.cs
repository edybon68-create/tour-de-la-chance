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
        // v5 §4.5 : le plafond du bonus de tenue (v4 : 50 %) n'existe plus. Le garde-fou est
        // maintenant dans Validate : aucune rareté ne doit donner plus de 15 %.

        public IReadOnlyList<RarityDef> Rarities => rarities;
        public IReadOnlyList<LootBox> Boxes => boxes;
        public IReadOnlyList<ShopItem> FreeItems => freeItems;
        /// v5 §4.5 : bonus de tenue maximal d'UN objet (garde-fou de Validate).
        public const int MaxBonusPerItem = 15;

        /// Tous les objets : gratuits, puis ceux des boîtes.
        public IEnumerable<ShopItem> AllItems =>
            freeItems.Where(i => i != null).Concat(boxes.SelectMany(b => b.items).Where(i => i != null));

        /// La somme des poids (1000 = 100 %).
        public int TotalWeight => rarities.Sum(r => r.weight);

        public RarityDef GetRarity(string id) => rarities.FirstOrDefault(r => r.id == id);

        /// La place d'une rareté dans la liste (0 = Commun … 5 = Secret), -1 si inconnue.
        public int RarityRank(string id) => rarities.FindIndex(r => r.id == id);

        /// Contrôle des données (spec boites §4.3, v5 §12.2) : erreurs de DONNÉES (pas des cas
        /// de jeu), écrites en rouge dans la console. Renvoie true si tout va bien.
        ///   - les poids habituels font 1000 ;
        ///   - pour chaque boîte, avec SES poids (propres ou habituels) : les poids font 1000, une
        ///     rareté de poids > 0 a au moins un objet, une rareté de poids 0 n'en a AUCUN ;
        ///   - la revente reste sous le prix des boîtes payées en pièces ;
        ///   - v5 §4.5 : aucune rareté ne donne plus de 15 % de bonus de tenue (garde-fou).
        public bool Validate()
        {
            bool ok = true;
            void Error(string message) { Debug.LogError("Catalogue : " + message); ok = false; }

            if (TotalWeight != 1000) Error($"la somme des poids des raretés vaut {TotalWeight} au lieu de 1000.");
            foreach (var r in rarities)
                if (r.coinBonusPercent > MaxBonusPerItem)
                    Debug.LogWarning($"Catalogue : la rareté « {r.displayName} » donne +{r.coinBonusPercent} % de tenue (plus de {MaxBonusPerItem} %) : le bonus de tenue n'a plus de plafond (v5 §4.5), vérifie l'équilibre.");

            foreach (var box in boxes)
            {
                var weights = WeightsOf(box);
                if (weights.Length != rarities.Count)
                {
                    Error($"la boîte « {box.displayName} » a {weights.Length} poids au lieu de {rarities.Count} (un par rareté).");
                    continue;
                }
                if (weights.Sum() != 1000) Error($"les poids de la boîte « {box.displayName} » font {weights.Sum()} au lieu de 1000.");
                if (weights.Any(w => w < 0)) Error($"la boîte « {box.displayName} » a un poids négatif.");
                for (int i = 0; i < rarities.Count; i++)
                {
                    var r = rarities[i];
                    bool hasItem = box.items.Any(it => it != null && it.RarityId == r.id);
                    if (weights[i] > 0 && !hasItem) Error($"la boîte « {box.displayName} » n'a aucun objet de rareté « {r.displayName} » (poids {weights[i]}).");
                    if (weights[i] == 0 && hasItem) Error($"la boîte « {box.displayName} » contient un objet « {r.displayName} » alors que ce poids vaut 0 : il ne sortirait jamais.");
                }
                foreach (var item in box.items)
                {
                    if (item == null) Error($"case vide dans « {box.displayName} ».");
                    else if (GetRarity(item.RarityId) == null) Error($"« {item.Id} » a une rareté inconnue (« {item.RarityId} »).");
                }
                if (box.currency == BoxCurrency.Pieces)
                    foreach (var r in rarities)
                        if (r.sellValue >= box.price)
                            Error($"la revente d'un {r.displayName} ({r.sellValue}) doit rester sous le prix de « {box.displayName} ».");
            }
            return ok;
        }

        /// Les poids d'une boîte, un par rareté : les siens (Boîtes Pirates) ou les habituels.
        public int[] WeightsOf(LootBox box)
        {
            if (box != null && box.HasOwnWeights) return box.rarityWeights.ToArray();
            return rarities.Select(r => r.weight).ToArray();
        }
    }
}
