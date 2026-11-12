using System;
using System.Collections.Generic;
using UnityEngine;

namespace TourDeLaChance
{
    /// <summary>
    /// Une boîte à tirage (spec boites §5.2) : on l'ouvre contre des pièces (ou, v5, des
    /// diamants) et elle donne UN objet de sa liste, tiré au hasard selon les raretés.
    /// L'ordre de la liste donne les numéros de l'Index (n°01 à n°16).
    ///
    /// v5 : monnaie (Pièces / Diamants), déblocage par la renaissance, taux PROPRES facultatifs
    /// (rarityWeights), modèle 3D d'ouverture, famille (touche thématique des Secrets).
    /// </summary>
    [Serializable]
    public class LootBox
    {
        [Tooltip("Identifiant unique (boite_skins_1). Ne jamais le changer une fois publié.")]
        public string id = "boite_xxx";
        public string displayName = "Boîte n°1";
        [Tooltip("Prix, dans la monnaie de la boîte (250 pièces, 5 diamants…).")]
        [Min(0)] public int price = 100;
        public Sprite icon;
        [Tooltip("Les objets de la boîte, dans l'ordre de l'Index (du Commun au Secret).")]
        public List<ShopItem> items = new List<ShopItem>();

        [Header("v5")]
        [Tooltip("La monnaie de la boîte (§12.3). Diamants : Boîtes Pirates.")]
        public BoxCurrency currency = BoxCurrency.Pieces;
        [Tooltip("§14.2 : la boîte ne s'ouvre qu'après la première renaissance (Boîtes Animaux).")]
        public bool requiresRebirth;
        [Tooltip("§12.2 : taux PROPRES à la boîte, un poids par rareté du catalogue, dans l'ordre " +
                 "(Commun … Secret), total 1000. VIDE = les taux habituels du catalogue.")]
        public List<int> rarityWeights = new List<int>();
        [Tooltip("§6.2 : le modèle 3D de l'ouverture (Corps, Couvercle, Ruban, PointSortie…). " +
                 "Vide = la boîte générique du studio.")]
        public GameObject openingPrefab;
        [Tooltip("Teinte du Corps du modèle d'ouverture (blanc = couleurs du modèle). Sert aux " +
                 "boîtes qui réutilisent provisoirement un autre modèle (Pirates, Animaux).")]
        public Color openingTint = Color.white;
        [Tooltip("§6.6 : la touche thématique après un Secret (vague, empreintes…).")]
        public BoxFamily family = BoxFamily.Classique;

        /// La boîte a-t-elle ses propres taux ?
        public bool HasOwnWeights => rarityWeights != null && rarityWeights.Count > 0;
    }
}
