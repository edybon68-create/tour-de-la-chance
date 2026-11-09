using System;
using System.Collections.Generic;
using UnityEngine;

namespace TourDeLaChance
{
    /// <summary>
    /// Une boîte à tirage (spec boites §5.2) : on l'ouvre contre des pièces et elle donne
    /// UN objet de sa liste, tiré au hasard selon les raretés du catalogue.
    /// L'ordre de la liste donne les numéros de l'Index (n°01 à n°16).
    /// </summary>
    [Serializable]
    public class LootBox
    {
        [Tooltip("Identifiant unique (boite_skins_1). Ne jamais le changer une fois publié.")]
        public string id = "boite_xxx";
        public string displayName = "Boîte n°1";
        [Min(0)] public int price = 100;
        public Sprite icon;
        [Tooltip("Les objets de la boîte, dans l'ordre de l'Index (du Commun au Secret).")]
        public List<ShopItem> items = new List<ShopItem>();
    }
}
