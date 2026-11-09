using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TourDeLaChance
{
    /// <summary>
    /// Les RÈGLES du shop (aucun affichage ici) : quels objets existent, lesquels sont
    /// possédés ou équipés, acheter, équiper, et sauvegarder. Comme Wallet, c'est une classe
    /// "static" : il n'y en a qu'une, accessible de partout (Shop.TryBuy(...)).
    ///
    /// Sauvegarde (PlayerPrefs) — on enregistre des IDENTIFIANTS, jamais des positions
    /// dans une liste, pour pouvoir réordonner ou ajouter des objets sans casser les sauvegardes :
    ///   shop_owned            = "couleur_rouge,chapeau_casquette" (les gratuits n'y sont pas)
    ///   shop_equipped_couleur = "couleur_bleu"
    ///   shop_equipped_chapeau = "chapeau_aucun"
    /// </summary>
    public static class Shop
    {
        public const string OwnedKey = "shop_owned";
        const string EquippedKeyPrefix = "shop_equipped_";

        /// Prévient l'interface (et le reste) qu'un achat ou un équipement a eu lieu.
        public static event Action Changed;

        static ShopCatalog catalog;

        /// Le catalogue, chargé une seule fois depuis Assets/Resources/ShopCatalog.asset.
        public static ShopCatalog Catalog
        {
            get
            {
                if (catalog == null)
                {
                    catalog = Resources.Load<ShopCatalog>("ShopCatalog");
                    if (catalog == null)
                        Debug.LogError("ShopCatalog introuvable : il doit être dans Assets/Resources/ShopCatalog.asset");
                }
                return catalog;
            }
        }

        // ---------- Lire le catalogue ----------

        /// Les objets d'une catégorie : gratuits d'abord, puis par prix croissant.
        public static List<ShopItem> ItemsIn(ShopCategory category)
        {
            if (Catalog == null) return new List<ShopItem>();
            return Catalog.Items
                .Where(item => item != null && item.Category == category)
                .OrderBy(item => item.Price)   // OrderBy garde l'ordre du catalogue à prix égal
                .ToList();
        }

        /// Retrouve un objet par son identifiant (null s'il n'existe pas ou plus).
        public static ShopItem Find(string id)
        {
            if (Catalog == null || string.IsNullOrEmpty(id)) return null;
            return Catalog.Items.FirstOrDefault(item => item != null && item.Id == id);
        }

        /// L'objet par défaut d'une catégorie = le premier objet gratuit (couleur_bleu, chapeau_aucun).
        public static ShopItem DefaultOf(ShopCategory category) =>
            ItemsIn(category).FirstOrDefault(item => item.IsFree);

        // ---------- Possédé / équipé ----------

        public static bool IsOwned(ShopItem item)
        {
            if (item == null) return false;
            return item.IsFree || ReadOwnedIds().Contains(item.Id);
        }

        /// L'objet équipé d'une catégorie. Si la sauvegarde contient un identifiant inconnu
        /// (objet supprimé, faute de frappe) ou un objet non possédé, on revient au défaut.
        public static ShopItem GetEquipped(ShopCategory category)
        {
            var fallback = DefaultOf(category);
            string savedId = PlayerPrefs.GetString(EquippedKey(category), fallback != null ? fallback.Id : "");
            var item = Find(savedId);
            if (item == null || item.Category != category || !IsOwned(item)) return fallback;
            return item;
        }

        public static bool IsEquipped(ShopItem item) =>
            item != null && GetEquipped(item.Category) == item;

        /// Combien de pièces manquent pour acheter cet objet (0 si on peut se l'offrir).
        public static int MissingCoins(ShopItem item) =>
            item == null ? 0 : Mathf.Max(0, item.Price - Wallet.Coins);

        public static bool CanBuy(ShopItem item) =>
            item != null && !IsOwned(item) && Wallet.Coins >= item.Price;

        // ---------- Actions ----------

        /// Achète un objet. Refuse (et ne change rien) s'il est déjà possédé ou trop cher.
        /// Acheter n'équipe PAS l'objet : c'est une action séparée.
        public static bool TryBuy(ShopItem item)
        {
            // Déjà possédé ? → refus. C'est ce qui protège du double-clic :
            // au 2e clic, l'objet est déjà à nous, donc on ne paie pas deux fois.
            if (item == null || IsOwned(item)) return false;

            // Wallet.TrySpend refuse si le solde est insuffisant : jamais sous zéro.
            if (!Wallet.TrySpend(item.Price)) return false;

            var owned = ReadOwnedIds();
            owned.Add(item.Id);
            PlayerPrefs.SetString(OwnedKey, string.Join(",", owned));
            PlayerPrefs.Save(); // sauvegarde tout de suite, comme Wallet.Add
            Changed?.Invoke();
            return true;
        }

        /// Équipe un objet possédé (remplace celui de la même catégorie). Gratuit et illimité.
        public static bool TryEquip(ShopItem item)
        {
            if (item == null || !IsOwned(item)) return false;
            PlayerPrefs.SetString(EquippedKey(item.Category), item.Id);
            PlayerPrefs.Save();
            Changed?.Invoke();
            return true;
        }

        // ---------- Outils internes ----------

        static string EquippedKey(ShopCategory category) =>
            EquippedKeyPrefix + category.ToString().ToLowerInvariant(); // "shop_equipped_couleur"

        /// Lit la liste des identifiants achetés. Les identifiants inconnus sont ignorés
        /// (ils ne correspondent à aucun objet, donc personne ne les demandera).
        static List<string> ReadOwnedIds()
        {
            string raw = PlayerPrefs.GetString(OwnedKey, "");
            return raw.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                      .Select(id => id.Trim())
                      .Distinct()
                      .ToList();
        }
    }
}
