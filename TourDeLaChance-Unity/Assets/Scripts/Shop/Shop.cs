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
    /// RÈGLE D'OR : rien ici ne touche aux coffres ni au piège. v4 : la tenue équipée donne un
    /// bonus sur le MONTANT des trésors (OutfitBonusPercent), jamais sur les chances.
    ///
    /// v5 : une boîte peut avoir ses PROPRES taux (Boîtes Pirates), se payer en diamants, être
    /// verrouillée jusqu'à la renaissance (Boîtes Animaux) ; on porte AU PLUS 2 accessoires,
    /// sur deux emplacements différents (§4.4) ; le bonus de tenue compte 3 objets, sans
    /// plafond (§4.5). Clés de sauvegarde inchangées ; migration_v5_tenue = 1 quand la règle
    /// « 2 accessoires » a été appliquée une première fois à une sauvegarde v4.
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

        /// Le poids d'une rareté DANS CETTE BOÎTE (ses taux propres, ou les habituels).
        public static int WeightOf(LootBox box, RarityDef rarity)
        {
            if (Catalog == null || rarity == null) return 0;
            int index = Catalog.RarityRank(rarity.id);
            var weights = Catalog.WeightsOf(box);
            return index >= 0 && index < weights.Length ? weights[index] : 0;
        }

        /// La somme des poids d'une boîte (1000 si les données sont justes).
        public static int TotalWeightOf(LootBox box) => Catalog == null ? 0 : Catalog.WeightsOf(box).Sum();

        /// Chance (en %) de tirer une rareté à chaque ouverture DE CETTE BOÎTE :
        /// poids ÷ total × 100 (v5 §12.2 : les poids de la boîte).
        public static double RarityChance(LootBox box, RarityDef rarity)
        {
            int total = TotalWeightOf(box);
            return total > 0 ? 100.0 * WeightOf(box, rarity) / total : 0.0;
        }

        /// Les raretés qu'on peut obtenir dans cette boîte (poids > 0), de la plus courante à
        /// la plus rare : le tableau des taux n'affiche qu'elles (Pirates : 3 lignes).
        public static List<RarityDef> RaritiesIn(LootBox box) =>
            Catalog == null ? new List<RarityDef>() : Catalog.Rarities.Where(r => WeightOf(box, r) > 0).ToList();

        /// Chance (en %) de tirer CET objet : chance de sa rareté dans sa boîte ÷ nombre
        /// d'objets de cette rareté dans sa boîte (spec §5.1).
        public static double ItemChance(ShopItem item)
        {
            var box = BoxOf(item);
            var rarity = RarityOf(item);
            if (box == null || rarity == null) return 0.0;
            int sameRarity = ItemsOfRarity(box, rarity.id).Count;
            return sameRarity > 0 ? RarityChance(box, rarity) / sameRarity : 0.0;
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

        /// v5 §14.2 : la boîte est-elle encore verrouillée (Boîtes Animaux avant la renaissance) ?
        public static bool IsLocked(LootBox box) => box != null && box.requiresRebirth && !Rebirth.IsDone;

        /// Le solde dans la monnaie de la boîte (pièces ou diamants).
        public static int BalanceFor(LootBox box) =>
            box != null && box.currency == BoxCurrency.Diamants ? Diamonds.Count : Wallet.Coins;

        /// Combien manquent pour ouvrir cette boîte, dans SA monnaie (0 si on peut).
        public static int MissingAmount(LootBox box) => box == null ? 0 : Mathf.Max(0, box.price - BalanceFor(box));

        /// Ancien nom (v4) : combien de pièces / diamants manquent.
        public static int MissingCoins(LootBox box) => MissingAmount(box);

        /// Peut-on ouvrir cette boîte maintenant ? Débloquée ET solde suffisant.
        public static bool CanOpen(LootBox box) => box != null && !IsLocked(box) && BalanceFor(box) >= box.price;

        static bool Spend(LootBox box) =>
            box.currency == BoxCurrency.Diamants ? Diamonds.TrySpend(box.price) : Wallet.TrySpend(box.price);

        static void Refund(LootBox box)
        {
            if (box.currency == BoxCurrency.Diamants) Diamonds.Add(box.price, fromTreasure: false);
            else Wallet.Add(box.price);
        }

        /// LA fonction de tirage du jeu (spec §4.2, étapes 3 et 4). Elle ne touche à rien :
        /// ni au porte-monnaie, ni à la sauvegarde. On peut donc l'appeler 100 000 fois pour
        /// vérifier les taux (avec son propre générateur), sans rien abîmer.
        ///   1. Rareté : un entier au hasard entre 0 et (total − 1), comparé aux poids cumulés
        ///      (500, 750, 890, 960, 990, 1000 avec les poids de la spec).
        ///   2. Objet : au hasard, même chance pour chaque objet de cette rareté dans la boîte.
        public static ShopItem Draw(LootBox box, System.Random random)
        {
            if (box == null || Catalog == null) return null;
            // v5 §12.2 : on lit les poids DE LA BOÎTE (Pirates : 0 / 0 / 0 / 600 / 300 / 100).
            var weights = Catalog.WeightsOf(box);
            int total = weights.Sum();
            if (total <= 0) return null;
            int roll = random.Next(0, total); // 0 à 999 compris
            int cumulative = 0;
            for (int i = 0; i < weights.Length && i < Catalog.Rarities.Count; i++)
            {
                cumulative += weights[i];
                if (roll < cumulative)
                {
                    var candidates = ItemsOfRarity(box, Catalog.Rarities[i].id);
                    if (candidates.Count == 0) return null; // erreur de données (signalée par Validate)
                    return candidates[random.Next(candidates.Count)];
                }
            }
            return null;
        }

        /// Ouvre une boîte, en une fois et dans cet ordre (spec §4.2) : vérifier le déblocage et le solde,
        /// payer, tirer, ajouter l'exemplaire, SAUVEGARDER, puis renvoyer le résultat.
        /// L'animation ne commence qu'après : si le jeu est quitté pendant l'animation,
        /// l'objet est déjà dans la collection. Refuse (et ne change rien) si le solde ne
        /// suffit pas — c'est aussi ce qui protège du double-clic.
        public static bool TryOpenBox(LootBox box, out BoxResult result)
        {
            result = default;
            if (box == null || !CanOpen(box)) return false;

            // TrySpend (pièces ou diamants) refuse si le solde est insuffisant : jamais sous zéro.
            if (!Spend(box)) return false;

            var item = Draw(box, boxRandom);
            if (item == null)
            {
                // Erreur de données (déjà signalée par Validate) : on rend le prix.
                Refund(box);
                return false;
            }

            Collection.Add(item.Id, +1);
            // v4 §12.3 : les statistiques du classement (boîtes ouvertes, Secrets obtenus) sont
            // comptées APRÈS l'ajout à la collection, et sauvegardées avec elle juste en dessous.
            PlayerStats.OnBoxOpened(item, RarityOf(item));
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
        // Bonus de tenue (spec v4 §9, v5 §4.5)
        // =====================================================================

        /// Le bonus (en %) que donne CET objet quand il est équipé : celui de sa rareté
        /// (Commun 1 … Secret 15). 0 pour un objet gratuit (Bleu, Rien). Les doublons ne
        /// comptent pas : c'est l'objet équipé qui compte, une fois.
        public static int BonusPercent(ShopItem item)
        {
            if (item == null || item.IsFree) return 0;
            var rarity = RarityOf(item);
            return rarity != null ? Mathf.Max(0, rarity.coinBonusPercent) : 0;
        }

        /// Le bonus de la tenue SAUVEGARDÉE : skin + 2 accessoires au plus, SANS plafond (v5 :
        /// au plus 3 × 15 = 45 %). L'aperçu ne compte pas. RÈGLE D'OR : ce nombre ne touche que
        /// le montant des trésors, jamais le piège, le nombre de coffres, leur rareté ni les boîtes.
        public static int OutfitBonusPercent => OutfitBonusPercentWith(null);

        /// Le bonus qu'aurait la tenue si on équipait « tryItem » (calcul seulement, rien n'est
        /// sauvegardé) : sert à l'Inventaire, « Avec cet objet : +16 % ». Si 2 accessoires sont
        /// déjà portés sur d'autres emplacements, on suppose qu'il remplace « replace » (ou, à
        /// défaut, le moins rare, comme l'aperçu).
        public static int OutfitBonusPercentWith(ShopItem tryItem, ShopItem replace = null)
        {
            var skin = GetEquippedSkin();
            var accessories = OutfitAfter(tryItem, replace, out var newSkin);
            if (newSkin != null) skin = newSkin;
            int total = BonusPercent(skin);
            foreach (var acc in accessories) total += BonusPercent(acc);
            return Mathf.Max(0, total);
        }

        /// Les accessoires portés après l'équipement (simulé) de tryItem. Pour un skin, les
        /// accessoires ne changent pas et newSkin = tryItem.
        public static List<ShopItem> OutfitAfter(ShopItem tryItem, ShopItem replace, out ShopItem newSkin)
        {
            newSkin = null;
            var worn = EquippedAccessories();
            if (tryItem == null) return worn;
            if (tryItem.Category == ShopCategory.Skin) { newSkin = tryItem; return worn; }
            if (tryItem.IsFree) { worn.RemoveAll(a => a.Slot == tryItem.Slot); return worn; }
            if (worn.Contains(tryItem)) return worn;
            int sameSlot = worn.FindIndex(a => a.Slot == tryItem.Slot);
            if (sameSlot >= 0) { worn[sameSlot] = tryItem; return worn; }
            if (worn.Count < MaxAccessories) { worn.Add(tryItem); return worn; }
            var removed = replace != null && worn.Contains(replace) ? replace : LeastRare(worn);
            worn.Remove(removed);
            worn.Add(tryItem);
            return worn;
        }

        // =====================================================================
        // Équiper (v5 §4.4 : un skin et AU PLUS 2 accessoires, emplacements différents)
        // =====================================================================

        /// v5 §4.4 : nombre maximal d'accessoires portés en même temps.
        public const int MaxAccessories = 2;
        public const string MigrationKey = "migration_v5_tenue";

        /// Vrai si la migration « 2 accessoires » vient d'enlever des objets pendant cette
        /// session : l'Inventaire affiche alors une fois le petit message (toast).
        public static bool MigrationToastPending { get; set; }

        /// L'ordre de priorité à rareté égale (§4.4) : Tête, Dos, Visage, Cou.
        static readonly AccessorySlot[] SlotPriority = { AccessorySlot.Tete, AccessorySlot.Dos, AccessorySlot.Visage, AccessorySlot.Cou };

        /// L'objet gratuit d'une catégorie / d'un emplacement (couleur_bleu, chapeau_aucun…).
        public static ShopItem DefaultSkin() =>
            Catalog?.FreeItems.FirstOrDefault(i => i != null && i.Category == ShopCategory.Skin);

        public static ShopItem DefaultAccessory(AccessorySlot slot) =>
            Catalog?.FreeItems.FirstOrDefault(i => i != null && i.Category == ShopCategory.Accessoire && i.Slot == slot);

        public static ShopItem GetEquippedSkin() => ReadEquipped(SkinKey, DefaultSkin(),
            item => item.Category == ShopCategory.Skin);

        public static ShopItem GetEquippedAccessory(AccessorySlot slot) => ReadEquipped(AccessoryKey(slot),
            DefaultAccessory(slot), item => item.Category == ShopCategory.Accessoire && item.Slot == slot);

        /// Les accessoires VRAIMENT portés (sans les « Rien »), dans l'ordre Tête, Visage, Cou, Dos.
        public static List<ShopItem> EquippedAccessories()
        {
            var list = new List<ShopItem>();
            for (int s = 0; s < 4; s++)
            {
                var acc = GetEquippedAccessory((AccessorySlot)s);
                if (acc != null && !acc.IsFree) list.Add(acc);
            }
            return list;
        }

        /// L'objet équipé de la même place que cet objet (même catégorie, même emplacement).
        public static ShopItem GetEquippedLike(ShopItem item) =>
            item.Category == ShopCategory.Skin ? GetEquippedSkin() : GetEquippedAccessory(item.Slot);

        public static bool IsEquipped(ShopItem item) => item != null && GetEquippedLike(item) == item;

        /// Faut-il demander « lequel remplacer » pour équiper cet accessoire ? Oui si son
        /// emplacement est libre et que 2 accessoires sont déjà portés.
        public static bool NeedsReplaceChoice(ShopItem item)
        {
            if (item == null || item.Category != ShopCategory.Accessoire || item.IsFree || IsEquipped(item)) return false;
            var worn = EquippedAccessories();
            return worn.Count >= MaxAccessories && !worn.Any(a => a.Slot == item.Slot);
        }

        /// L'accessoire déjà porté que cet objet remplacerait automatiquement (même
        /// emplacement), sinon null. Sert à la mention « Remplace : Couronne ».
        public static ShopItem ReplacedBySameSlot(ShopItem item)
        {
            if (item == null || item.Category != ShopCategory.Accessoire || item.IsFree) return null;
            var current = GetEquippedAccessory(item.Slot);
            return current != null && !current.IsFree && current != item ? current : null;
        }

        /// Le moins rare d'une liste (à rareté égale : celui qui vient en DERNIER dans l'ordre
        /// Tête, Dos, Visage, Cou). Sert à l'aperçu et au bouton « Équiper » de l'ouverture.
        public static ShopItem LeastRare(List<ShopItem> items)
        {
            if (items == null || items.Count == 0) return null;
            return items.OrderBy(RarityRank).ThenByDescending(i => Array.IndexOf(SlotPriority, i.Slot)).First();
        }

        /// Équipe un objet possédé (§4.4). Skin : remplace le skin. Accessoire :
        ///   - son emplacement est occupé → il REMPLACE l'objet de cet emplacement ;
        ///   - emplacement libre et moins de 2 accessoires portés → il est AJOUTÉ ;
        ///   - emplacement libre et 2 portés → il remplace « replace » (un des 2 portés) ;
        ///     sans « replace » valide, on REFUSE sans rien changer (l'Inventaire demande lequel).
        /// Gratuit, illimité, sauvegardé tout de suite. Un objet « Rien » vide son emplacement.
        public static bool TryEquip(ShopItem item, ShopItem replace = null)
        {
            if (item == null || !IsOwned(item)) return false;
            if (item.Category == ShopCategory.Skin)
            {
                PlayerPrefs.SetString(SkinKey, item.Id);
            }
            else if (item.IsFree)
            {
                PlayerPrefs.SetString(AccessoryKey(item.Slot), item.Id);
            }
            else if (!IsEquipped(item))
            {
                var worn = EquippedAccessories();
                bool slotTaken = worn.Any(a => a.Slot == item.Slot);
                if (!slotTaken && worn.Count >= MaxAccessories)
                {
                    // Il faut libérer une place : « replace » doit être un des accessoires portés.
                    if (replace == null || !worn.Contains(replace)) return false;
                    var empty = DefaultAccessory(replace.Slot);
                    PlayerPrefs.SetString(AccessoryKey(replace.Slot), empty != null ? empty.Id : "");
                }
                PlayerPrefs.SetString(AccessoryKey(item.Slot), item.Id);
            }
            PlayerPrefs.Save();
            Changed?.Invoke();
            return true;
        }

        /// « Retirer » un accessoire porté : son emplacement redevient vide (objet « Rien »).
        public static bool Unequip(AccessorySlot slot)
        {
            var current = GetEquippedAccessory(slot);
            if (current == null || current.IsFree) return false;
            var empty = DefaultAccessory(slot);
            PlayerPrefs.SetString(AccessoryKey(slot), empty != null ? empty.Id : "");
            PlayerPrefs.Save();
            Changed?.Invoke();
            return true;
        }

        /// v5 §4.4 : la règle « au plus 2 accessoires » est vérifiée à la lecture. Si la
        /// sauvegarde en porte plus (sauvegarde v4), on garde les 2 PLUS RARES (à rareté égale :
        /// Tête, Dos, Visage, Cou) et on remet les autres emplacements à « Rien ». Les objets
        /// restent possédés. Appelée au démarrage (PanelsController) ; renvoie true si elle a
        /// changé quelque chose.
        public static bool EnforceMaxAccessories()
        {
            if (Catalog == null) return false;
            var worn = EquippedAccessories();
            bool changed = false;
            if (worn.Count > MaxAccessories)
            {
                var keep = worn.OrderByDescending(RarityRank)
                               .ThenBy(i => Array.IndexOf(SlotPriority, i.Slot))
                               .Take(MaxAccessories).ToList();
                foreach (var acc in worn)
                {
                    if (keep.Contains(acc)) continue;
                    var empty = DefaultAccessory(acc.Slot);
                    PlayerPrefs.SetString(AccessoryKey(acc.Slot), empty != null ? empty.Id : "");
                }
                MigrationToastPending = true;
                changed = true;
                Debug.Log($"Tenue v5 : {worn.Count} accessoires équipés → on garde {string.Join(" et ", keep.Select(k => k.DisplayName))}.");
            }
            if (PlayerPrefs.GetInt(MigrationKey, 0) != 1) { PlayerPrefs.SetInt(MigrationKey, 1); changed = true; }
            if (changed) PlayerPrefs.Save();
            return changed;
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
            "shop_equipped_chapeau", "shop_equipped_visage", "shop_equipped_cou", "shop_equipped_dos", MigrationKey
        };

        /// Prévient l'interface que la sauvegarde a changé « de l'extérieur » (tests).
        public static void NotifyChanged() => Changed?.Invoke();
    }
}
