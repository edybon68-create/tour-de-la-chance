using System;
using UnityEngine;

namespace TourDeLaChance
{
    /// <summary>
    /// Les RÈGLES des améliorations (spec v3 §5 et §18), sans aucun affichage. Comme Wallet et
    /// Shop, c'est une classe "static" : il n'y en a qu'une, accessible de partout.
    ///
    /// Elle sait : lire les niveaux (sauvegardés), le prix du niveau suivant, acheter, et
    /// TIRER le nombre de coffres et la rareté d'un étage. Elle calcule aussi ce que le panneau
    /// affiche (« Il te manque X pièces », les chances, le risque moyen) : l'interface n'a
    /// rien à décider. Tous les chiffres viennent du catalogue (UpgradeCatalog).
    ///
    /// Sauvegarde (PlayerPrefs) : amelioration_gains, amelioration_coffres, amelioration_pieges
    /// (niveau 0 à 5, 0 par défaut). Une valeur illisible ou hors limites est ramenée entre 0 et 5.
    ///
    /// RÈGLE D'OR : il y a TOUJOURS exactement 1 coffre piégé par étage ; les améliorations ne
    /// changent que le gain, la rareté et le nombre de coffres. Elles ne s'appliquent qu'au
    /// PROCHAIN étage préparé (c'est le GameManager qui lit les niveaux à ce moment-là).
    /// </summary>
    public static class Upgrades
    {
        /// Prévient l'affichage qu'un niveau vient d'être acheté.
        public static event Action Changed;

        static UpgradeCatalog catalog;

        /// Le catalogue, chargé une seule fois depuis Assets/Resources/UpgradeCatalog.asset.
        public static UpgradeCatalog Catalog
        {
            get
            {
                if (catalog == null)
                {
                    catalog = Resources.Load<UpgradeCatalog>("UpgradeCatalog");
                    if (catalog == null) Debug.LogError("UpgradeCatalog introuvable : il doit être dans Assets/Resources/UpgradeCatalog.asset");
                    else catalog.Validate();
                }
                return catalog;
            }
        }

        public static string SaveKey(string id) => "amelioration_" + id;

        /// Toutes les clés de sauvegarde v3 (pour remettre à zéro après des tests).
        public static readonly string[] AllSaveKeys =
            { "amelioration_gains", "amelioration_coffres", "amelioration_pieges", GameManager.RecordKey };

        // =====================================================================
        // Niveaux et achat
        // =====================================================================

        /// Niveau actuel (0 à 5) : toujours ramené dans les limites, même si la sauvegarde est abîmée.
        public static int Level(string id) => Mathf.Clamp(PlayerPrefs.GetInt(SaveKey(id), 0), 0, Upgrades_.MaxLevel);

        public static bool IsMax(string id) => Level(id) >= Upgrades_.MaxLevel;

        /// Prix du niveau SUIVANT (0 si déjà au maximum ou si le catalogue manque).
        public static int NextPrice(string id)
        {
            var def = Catalog != null ? Catalog.Get(id) : null;
            int next = Level(id) + 1;
            if (def == null || next > Upgrades_.MaxLevel || next >= def.levels.Count) return 0;
            return def.levels[next].price;
        }

        public static bool CanBuy(string id) => !IsMax(id) && NextPrice(id) > 0 && Wallet.Coins >= NextPrice(id);

        /// « Il te manque X pièces » (0 si on peut acheter).
        public static int MissingCoins(string id) => IsMax(id) ? 0 : Mathf.Max(0, NextPrice(id) - Wallet.Coins);

        /// Achète le niveau suivant, en une seule action et dans cet ordre (spec §5.1) :
        /// vérifier le niveau, vérifier le solde, payer, niveau + 1, SAUVEGARDER, prévenir.
        /// Refuse sans rien changer sinon. Le niveau et le solde sont RELUS à chaque appel :
        /// un double-clic n'achète donc qu'un niveau s'il n'y a pas assez pour le suivant.
        public static bool TryBuy(string id)
        {
            if (Catalog == null || Catalog.Get(id) == null) return false;
            int level = Level(id);
            if (level >= Upgrades_.MaxLevel) return false;
            int price = NextPrice(id);
            if (price <= 0 || Wallet.Coins < price) return false;
            if (!Wallet.TrySpend(price)) return false;   // jamais sous zéro
            PlayerPrefs.SetInt(SaveKey(id), level + 1);
            PlayerPrefs.Save();                          // écrit tout de suite sur le disque
            Changed?.Invoke();
            return true;
        }

        // =====================================================================
        // Effets (lus dans le catalogue)
        // =====================================================================

        static UpgradeLevel LevelData(string id, int level)
        {
            var def = Catalog != null ? Catalog.Get(id) : null;
            if (def == null || def.levels.Count == 0) return null;
            return def.levels[Mathf.Clamp(level, 0, def.levels.Count - 1)];
        }

        /// Multiplicateur de la Bourse magique à un niveau (×1 si absent).
        public static float GainMultiplier(int level) => LevelData(Upgrades_.Gains, level)?.value ?? 1f;

        /// Texte de l'effet de la Bourse à un niveau (« Trésors +20 % »).
        public static string GainText(int level) => LevelData(Upgrades_.Gains, level)?.effectText ?? "";

        /// Poids (sur 100) des 5 raretés de coffre à un niveau de Coffres précieux.
        public static int[] ChestWeights(int level) => LevelData(Upgrades_.Chests, level)?.weights ?? new[] { 100, 0, 0, 0, 0 };

        /// Poids (sur 100) de 3, 4 et 5 coffres à un niveau de Coffre en plus.
        public static int[] CountWeights(int level) => LevelData(Upgrades_.Traps, level)?.weights ?? new[] { 100, 0, 0 };

        public static int RarityCount => Catalog != null ? Catalog.ChestRarities.Count : 0;

        public static ChestRarityDef Rarity(int rank)
        {
            if (Catalog == null || Catalog.ChestRarities.Count == 0) return null;
            return Catalog.ChestRarities[Mathf.Clamp(rank, 0, Catalog.ChestRarities.Count - 1)];
        }

        /// Gain moyen des coffres à un niveau : somme des poids × multiplicateurs ÷ 100.
        public static float AverageChestMultiplier(int level)
        {
            var w = ChestWeights(level);
            float sum = 0f;
            for (int i = 0; i < w.Length && i < RarityCount; i++) sum += w[i] * Rarity(i).multiplier;
            return sum / 100f;
        }

        /// Risque moyen par étage (en %) : Σ poids × (1 ÷ nombre de coffres).
        public static float AverageRiskPercent(int level)
        {
            var w = CountWeights(level);
            float risk = 0f;
            for (int i = 0; i < w.Length; i++) risk += w[i] / (float)(BaseCount + i);
            return risk; // déjà en % puisque les poids sont sur 100
        }

        static int BaseCount => Catalog != null ? Catalog.BaseChestCount : 3;

        // =====================================================================
        // Tirages (spec §5.5) : ils ne touchent à rien, on peut les appeler 100 000 fois
        // =====================================================================

        /// Tire un nombre de coffres (3, 4 ou 5) selon le niveau de Coffre en plus.
        public static int DrawChestCount(int level, System.Random random) => BaseCount + DrawIndex(CountWeights(level), random);

        /// Tire une rareté de coffre (0 = Bois … 4 = Diamant) selon le niveau de Coffres précieux.
        /// Un entier entre 0 et 99 comparé aux poids cumulés, comme le tirage des boîtes.
        public static int DrawChestRarity(int level, System.Random random) => DrawIndex(ChestWeights(level), random);

        static int DrawIndex(int[] weights, System.Random random)
        {
            int roll = random.Next(0, 100); // 0 à 99 compris
            int cumulative = 0;
            for (int i = 0; i < weights.Length; i++)
            {
                cumulative += weights[i];
                if (roll < cumulative) return i;
            }
            return 0; // poids incomplets (erreur signalée par Validate) : on retombe sur le premier
        }

        /// Gain d'un trésor (spec §5.5) : arrondi(base × étage × Bourse × rareté du coffre).
        /// Exemple : étage 3, Bourse niveau 1, bronze → 10 × 3 × 1,2 × 1,5 = 54.
        public static int FloorGain(int coinsPerFloor, int floor, int gainsLevel, int rarityRank)
        {
            var rarity = Rarity(rarityRank);
            float m = GainMultiplier(gainsLevel) * (rarity != null ? rarity.multiplier : 1f);
            return Mathf.RoundToInt(coinsPerFloor * floor * m);
        }

        // =====================================================================
        // Outils de test (depuis `unity command script eval`)
        // =====================================================================

        /// OUTIL DE TEST : fixe les trois niveaux (et sauvegarde). Ne pas utiliser dans le jeu.
        public static void DebugSetLevels(int gains, int chests, int traps)
        {
            PlayerPrefs.SetInt(SaveKey(Upgrades_.Gains), Mathf.Clamp(gains, 0, Upgrades_.MaxLevel));
            PlayerPrefs.SetInt(SaveKey(Upgrades_.Chests), Mathf.Clamp(chests, 0, Upgrades_.MaxLevel));
            PlayerPrefs.SetInt(SaveKey(Upgrades_.Traps), Mathf.Clamp(traps, 0, Upgrades_.MaxLevel));
            PlayerPrefs.Save();
            Changed?.Invoke();
        }

        /// OUTIL DE TEST (critères 12 et 13) : simule n étages préparés avec un générateur à
        /// part, SANS toucher à la sauvegarde ni à la partie. Renvoie, pour chaque nombre de
        /// coffres (3, 4, 5), combien d'étages l'ont eu ; pour chaque rareté, combien de fois
        /// elle est sortie ; et pour chaque place, combien de fois elle était piégée (par nombre
        /// de coffres). Le piège est tiré par la MÊME fonction que le jeu (ChestTrial.DrawTrap).
        public static string DebugSimulate(int floors, int chestsLevel, int trapsLevel, int seed)
        {
            var random = new System.Random(seed);
            var counts = new int[6];
            var rarities = new int[RarityCount];
            var traps = new int[6, 5];
            int wrongTrapCount = 0;
            for (int i = 0; i < floors; i++)
            {
                int n = DrawChestCount(trapsLevel, random);
                int r = DrawChestRarity(chestsLevel, random);
                int trap = ChestTrial.DrawTrap(n, random);
                counts[n]++;
                rarities[r]++;
                traps[n, trap]++;
                if (trap < 0 || trap >= n) wrongTrapCount++; // jamais : exactement 1 piège parmi les présents
            }
            var sb = new System.Text.StringBuilder();
            sb.Append($"{floors} étages (Coffres précieux {chestsLevel}, Coffre en plus {trapsLevel}) : ");
            for (int n = 3; n <= 5; n++) sb.Append($"{n} coffres {100.0 * counts[n] / floors:0.00} % ; ");
            for (int r = 0; r < rarities.Length; r++) sb.Append($"{Rarity(r).shortName} {100.0 * rarities[r] / floors:0.00} % ; ");
            for (int n = 3; n <= 5; n++)
            {
                if (counts[n] == 0) continue;
                sb.Append($"| piège sur {n} coffres :");
                for (int p = 0; p < n; p++) sb.Append($" n°{p + 1} {100.0 * traps[n, p] / counts[n]:0.00} %");
            }
            sb.Append($" | pièges hors des coffres présents : {wrongTrapCount}");
            return sb.ToString();
        }
    }
}
