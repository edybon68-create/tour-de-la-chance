using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TourDeLaChance
{
    /// <summary>
    /// Les RÈGLES des boîtes et des cosmétiques (aucun affichage ici, spec boites §4 et §7.9) :
    /// ouvrir une boîte (payer, tirer, ajouter, sauvegarder), vendre des doublons, équiper
    /// un skin et un accessoire par emplacement. Comme Wallet, c'est une classe "static" :
    /// il n'y en a qu'une, accessible de partout (Shop.TryOpenBox(...)).
    ///
    /// Ce que l'interface affiche (« Il te manque 10 pièces », la chance par objet, le total
    /// « Vendre tous les doublons ») est CALCULÉ ici, pour qu'elle n'ait rien à décider.
    ///
    /// Sauvegarde (PlayerPrefs) :
    ///   collection_counts      → voir Collection
    ///   shop_equipped_couleur  = skin équipé       (nom de clé v1 conservé)
    ///   shop_equipped_chapeau  = accessoire Tête   (nom de clé v1 conservé)
    ///   shop_equipped_visage / shop_equipped_cou / shop_equipped_dos
    ///
    /// RÈGLE D'OR : rien ici ne touche aux coffres, au piège ni aux gains.
    /// </summary>
    public static class Shop
    {
        /// Ancienne clé v1 (liste des objets achetés), lue une fois par la migration.
        public const string OldOwnedKey = "shop_owned";
        public const string SkinKey = "shop_equipped_couleur";

        /// Prévient l'interface (et le reste) qu'une ouverture, une vente ou un équipement a eu lieu.
        public static event Action Changed;

        // Le générateur de hasard des boîtes : créé UNE fois, séparé de celui du piège et de
        // celui des effets visuels (spec §4.2). Chaque ouverture est indépendante.
        static readonly System.Random boxRandom = new System.Random();

        static ShopCatalog catalog;

        /// Le catalogue, chargé une seule fois depuis Assets/Resources/ShopCatalog.asset.
        /// Au chargement, on vérifie les données (erreur rouge dans la console si besoin).
        public static ShopCatalog Catalog
        {
            get
            {
                if (catalog == null)
                {
                    catalog = Resources.Load<ShopCatalog>("ShopCatalog");
                    if (catalog == null)
                        Debug.LogError("ShopCatalog introuvable : il doit être dans Assets/Resources/ShopCatalog.asset");
                    else
                        catalog.Validate();
                }
                return catalog;
            }
        }

        // =====================================================================
        // Lire le catalogue
        // =====================================================================

        public static IReadOnlyList<LootBox> Boxes =>
            Catalog != null ? Catalog.Boxes : (IReadOnlyList<LootBox>)Array.Empty<LootBox>();

        public static LootBox FindBox(string id) => Boxes.FirstOrDefault(b => b.id == id);

        /// Retrouve un objet par son identifiant (null s'il n'existe pas ou plus).
        public static ShopItem Find(string id)
        {
            if (Catalog == null || string.IsNullOrEmpty(id)) return null;
            return Catalog.AllItems.FirstOrDefault(item => item.Id == id);
        }

        public static RarityDef RarityOf(ShopItem item) =>
            item == null || Catalog == null ? null : Catalog.GetRarity(item.RarityId);

        /// Rang de rareté : 0 = Commun … 5 = Secret ; -1 pour un objet gratuit.
        public static int RarityRank(ShopItem item) =>
            item == null || Catalog == null ? -1 : Catalog.RarityRank(item.RarityId);

        /// La boîte qui contient cet objet (null pour un objet gratuit).
        public static LootBox BoxOf(ShopItem item) => Boxes.FirstOrDefault(b => b.items.Contains(item));

        /// Le numéro de l'objet dans sa boîte (1 à 16), 0 s'il n'est dans aucune boîte.
        public static int NumberOf(ShopItem item)
        {
            var box = BoxOf(item);
            return box == null ? 0 : box.items.IndexOf(item) + 1;
        }

        /// Les objets d'une rareté dans une boîte.
        public static List<ShopItem> ItemsOfRarity(LootBox box, string rarityId) =>
            box.items.Where(i => i != null && i.RarityId == rarityId).ToList();

        /// Chance (en %) de tirer une rareté à chaque ouverture : poids ÷ total × 100.
        public static double RarityChance(RarityDef rarity)
        {
            int total = Catalog != null ? Catalog.TotalWeight : 0;
            return total > 0 ? 100.0 * rarity.weight / total : 0.0;
        }

        /// Chance (en %) de tirer CET objet : chance de sa rareté ÷ nombre d'objets de cette
        /// rareté dans sa boîte (spec §5.1).
        public static double ItemChance(ShopItem item)
        {
            var box = BoxOf(item);
            var rarity = RarityOf(item);
            if (box == null || rarity == null) return 0.0;
            int sameRarity = ItemsOfRarity(box, rarity.id).Count;
            return sameRarity > 0 ? RarityChance(rarity) / sameRarity : 0.0;
        }

        // =====================================================================
        // Collection : possédé, doublons, progression
        // =====================================================================

        public static int CountOf(ShopItem item) => item == null ? 0 : Collection.CountOf(item.Id);

        /// Possédé = au moins 1 exemplaire, ou objet gratuit.
        public static bool IsOwned(ShopItem item) => item != null && (item.IsFree || CountOf(item) >= 1);

        /// Nombre d'objets différents trouvés dans une boîte (« 9 / 16 trouvés »).
        public static int FoundCount(LootBox box) => box.items.Count(i => i != null && CountOf(i) >= 1);

        public static bool IsComplete(LootBox box) => FoundCount(box) >= box.items.Count;

        /// Valeur de revente d'un doublon de cet objet (0 pour un objet gratuit).
        public static int SellValue(ShopItem item)
        {
            if (item == null || item.IsFree) return 0;
            var rarity = RarityOf(item);
            return rarity != null ? rarity.sellValue : 0;
        }

        /// On ne vend que les exemplaires EN TROP : il faut en avoir au moins 2.
        public static bool CanSellDuplicate(ShopItem item) => item != null && !item.IsFree && CountOf(item) >= 2;

        /// Total de « Vendre tous les doublons » : pour chaque objet, (exemplaires − 1) × valeur.
        public static int DuplicatesTotal()
        {
            int total = 0;
            foreach (var pair in Collection.All())
            {
                var item = Find(pair.Key);
                if (item != null && pair.Value > 1) total += (pair.Value - 1) * SellValue(item);
            }
            return total;
        }

        // =====================================================================
        // Ouvrir une boîte
        // =====================================================================

        /// Combien de pièces manquent pour ouvrir cette boîte (0 si on peut).
        public static int MissingCoins(LootBox box) => box == null ? 0 : Mathf.Max(0, box.price - Wallet.Coins);

        public static bool CanOpen(LootBox box) => box != null && Wallet.Coins >= box.price;

        /// LA fonction de tirage du jeu (spec §4.2, étapes 3 et 4). Elle ne touche à rien :
        /// ni au porte-monnaie, ni à la sauvegarde. On peut donc l'appeler 100 000 fois pour
        /// vérifier les taux (avec son propre générateur), sans rien abîmer.
        ///   1. Rareté : un entier au hasard entre 0 et (total − 1), comparé aux poids cumulés
        ///      (500, 750, 890, 960, 990, 1000 avec les poids de la spec).
        ///   2. Objet : au hasard, même chance pour chaque objet de cette rareté dans la boîte.
        public static ShopItem Draw(LootBox box, System.Random random)
        {
            if (box == null || Catalog == null) return null;
            int roll = random.Next(0, Catalog.TotalWeight); // 0 à 999 compris
            int cumulative = 0;
            foreach (var rarity in Catalog.Rarities)
            {
                cumulative += rarity.weight;
                if (roll < cumulative)
                {
                    var candidates = ItemsOfRarity(box, rarity.id);
                    if (candidates.Count == 0) return null; // erreur de données (signalée par Validate)
                    return candidates[random.Next(candidates.Count)];
                }
            }
            return null;
        }

        /// Ouvre une boîte, en une fois et dans cet ordre (spec §4.2) : vérifier le solde,
        /// payer, tirer, ajouter l'exemplaire, SAUVEGARDER, puis renvoyer le résultat.
        /// L'animation ne commence qu'après : si le jeu est quitté pendant l'animation,
        /// l'objet est déjà dans la collection. Refuse (et ne change rien) si le solde ne
        /// suffit pas — c'est aussi ce qui protège du double-clic.
        public static bool TryOpenBox(LootBox box, out BoxResult result)
        {
            result = default;
            if (box == null || !CanOpen(box)) return false;

            // Wallet.TrySpend refuse si le solde est insuffisant : jamais sous zéro.
            if (!Wallet.TrySpend(box.price)) return false;

            var item = Draw(box, boxRandom);
            if (item == null)
            {
                // Erreur de données (déjà signalée par Validate) : on rend les pièces.
                Wallet.Add(box.price);
                return false;
            }

            Collection.Add(item.Id, +1);
            PlayerPrefs.Save(); // tout est écrit sur le disque AVANT l'animation

            int count = CountOf(item);
            result = new BoxResult
            {
                Box = box,
                Item = item,
                Rarity = RarityOf(item),
                IsNew = count == 1,
                Count = count
            };
            Changed?.Invoke();
            return true;
        }

        // =====================================================================
        // Vendre
        // =====================================================================

        /// Vend 1 doublon : exemplaires − 1, porte-monnaie + valeur de la rareté.
        /// Refusé s'il ne reste qu'un exemplaire (on ne perd jamais un objet).
        public static bool TrySellDuplicate(ShopItem item)
        {
            if (!CanSellDuplicate(item)) return false;
            Collection.Add(item.Id, -1);
            Wallet.Add(SellValue(item)); // Wallet.Add fait aussi PlayerPrefs.Save()
            Changed?.Invoke();
            return true;
        }

        /// Ramène chaque objet à 1 exemplaire et crédite la somme totale. Renvoie cette somme.
        public static int SellAllDuplicates()
        {
            int total = DuplicatesTotal();
            if (total <= 0) return 0;
            var counts = Collection.All();
            foreach (var id in counts.Keys.ToList())
                if (counts[id] > 1) counts[id] = 1;
            Collection.Write(counts);
            Wallet.Add(total);
            Changed?.Invoke();
            return total;
        }

        // =====================================================================
        // Équiper
        // =====================================================================

        /// L'objet gratuit d'une catégorie / d'un emplacement (couleur_bleu, chapeau_aucun…).
        public static ShopItem DefaultSkin() =>
            Catalog?.FreeItems.FirstOrDefault(i => i != null && i.Category == ShopCategory.Skin);

        public static ShopItem DefaultAccessory(AccessorySlot slot) =>
            Catalog?.FreeItems.FirstOrDefault(i => i != null && i.Category == ShopCategory.Accessoire && i.Slot == slot);

        public static ShopItem GetEquippedSkin() => ReadEquipped(SkinKey, DefaultSkin(),
            item => item.Category == ShopCategory.Skin);

        public static ShopItem GetEquippedAccessory(AccessorySlot slot) => ReadEquipped(AccessoryKey(slot),
            DefaultAccessory(slot), item => item.Category == ShopCategory.Accessoire && item.Slot == slot);

        /// L'objet équipé de la même place que cet objet (même catégorie, même emplacement).
        public static ShopItem GetEquippedLike(ShopItem item) =>
            item.Category == ShopCategory.Skin ? GetEquippedSkin() : GetEquippedAccessory(item.Slot);

        public static bool IsEquipped(ShopItem item) => item != null && GetEquippedLike(item) == item;

        /// Équipe un objet possédé : il remplace celui de la même catégorie (skin) ou du même
        /// emplacement (accessoire) seulement. Gratuit, illimité, sauvegardé tout de suite.
        public static bool TryEquip(ShopItem item)
        {
            if (item == null || !IsOwned(item)) return false;
            string key = item.Category == ShopCategory.Skin ? SkinKey : AccessoryKey(item.Slot);
            PlayerPrefs.SetString(key, item.Id);
            PlayerPrefs.Save();
            Changed?.Invoke();
            return true;
        }

        /// Clé de sauvegarde d'un emplacement. Tête garde le nom de clé v1 (« chapeau »).
        public static string AccessoryKey(AccessorySlot slot) => slot switch
        {
            AccessorySlot.Tete => "shop_equipped_chapeau",
            AccessorySlot.Visage => "shop_equipped_visage",
            AccessorySlot.Cou => "shop_equipped_cou",
            _ => "shop_equipped_dos"
        };

        /// Lit un objet équipé. Identifiant inconnu, objet non possédé ou mauvais
        /// emplacement → on revient à l'objet gratuit (spec §4.6).
        static ShopItem ReadEquipped(string key, ShopItem fallback, Func<ShopItem, bool> fits)
        {
            string savedId = PlayerPrefs.GetString(key, fallback != null ? fallback.Id : "");
            var item = Find(savedId);
            if (item == null || !fits(item) || !IsOwned(item)) return fallback;
            return item;
        }

        /// Toutes les clés de sauvegarde du shop (pour remettre à zéro après des tests).
        public static readonly string[] AllSaveKeys =
        {
            "wallet_coins", Collection.CountsKey, OldOwnedKey, SkinKey,
            "shop_equipped_chapeau", "shop_equipped_visage", "shop_equipped_cou", "shop_equipped_dos"
        };

        /// Prévient l'interface que la sauvegarde a changé « de l'extérieur » (tests).
        public static void NotifyChanged() => Changed?.Invoke();
    }
}
