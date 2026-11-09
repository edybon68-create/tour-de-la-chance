using System.Collections.Generic;
using UnityEngine;

namespace TourDeLaChance
{
    /// <summary>
    /// Le catalogue : LA liste de tous les objets du shop, en un seul endroit.
    /// Le fichier ShopCatalog.asset est rangé dans un dossier "Resources" : Unity peut
    /// alors le charger par son nom depuis le code (Resources.Load), sans référence à brancher.
    /// </summary>
    [CreateAssetMenu(fileName = "ShopCatalog", menuName = "Tour de la Chance/Catalogue du shop")]
    public class ShopCatalog : ScriptableObject
    {
        [SerializeField] List<ShopItem> items = new List<ShopItem>();

        public IReadOnlyList<ShopItem> Items => items;
    }
}
