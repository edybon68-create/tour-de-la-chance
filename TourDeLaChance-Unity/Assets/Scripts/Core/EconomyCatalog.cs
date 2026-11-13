using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace TourDeLaChance
{
    /// <summary>
    /// Le catalogue de l'ÉCONOMIE v5 (spec v5 §20) : LA source unique des chiffres des diamants
    /// (combien selon l'étage), de la reprise (coût, nombre par partie) et de la renaissance
    /// (coût, multiplicateurs, nombre maximal), et des packs de diamants affichés (désactivés).
    /// La logique ET l'interface lisent ces mêmes nombres : impossible d'afficher autre chose
    /// que ce que fait le code. Rangé dans Assets/Resources/ pour être chargé par son nom.
    ///
    /// (Les CHANCES de diamants, elles, dépendent de la rareté des coffres : elles sont dans
    /// le catalogue des améliorations, champ diamondChancePermille de chaque rareté.)
    /// </summary>
    [CreateAssetMenu(fileName = "EconomyCatalog", menuName = "Tour de la Chance/Catalogue de l'économie")]
    public class EconomyCatalog : ScriptableObject
    {
        /// Une tranche d'étages et le nombre de diamants d'un trésor en diamants.
        [Serializable]
        public class DiamondTier
        {
            [Tooltip("Premier étage de la tranche (1, 6, 10…).")]
            [Min(1)] public int fromFloor = 1;
            [Tooltip("Diamants d'un trésor en diamants à partir de cet étage (avant la renaissance).")]
            [Min(1)] public int amount = 1;
        }

        [Header("Diamants dans les trésors (§10.2)")]
        [Tooltip("Tranches d'étages, dans l'ordre croissant : 1 → 1 diamant, 6 → 2, 10 → 3.")]
        [SerializeField] List<DiamondTier> diamondTiers = new List<DiamondTier>();

        /// v6 §8.4 : un palier de la route des trophées (étage atteint → diamants offerts).
        [Serializable]
        public class TrophyMilestone
        {
            [Min(1)] public int floor = 5;
            [Min(0)] public int diamonds = 1;
        }

        [Header("Reprise après un piège (v6 §9 : coût qui double, 5 au plus)")]
        [Tooltip("Coût de la 1re reprise d'une partie (1 diamant).")]
        [FormerlySerializedAs("reviveCost")]
        [SerializeField, Min(1)] int reviveBaseCost = 1;
        [Tooltip("Multiplicateur du coût à chaque reprise suivante (×2 : 1, 2, 4, 8, 16).")]
        [SerializeField, Min(1)] int reviveCostGrowth = 2;
        [SerializeField, Min(0)] int maxRevivesPerRun = 5;

        [Header("Bonus de diamants des objets Pirates (v6 §11)")]
        [Tooltip("Plafond du bonus de diamants de la tenue, en % (50).")]
        [SerializeField, Min(0)] int diamondBonusCap = 50;

        [Header("Échange diamants → pièces (v6 §5.4)")]
        [Tooltip("Les lots, dans l'ordre d'affichage. Tous au même taux (1 ◆ = 200 pièces).")]
        [SerializeField] List<CoinPackDef> coinPacks = new List<CoinPackDef>();

        [Header("L'Ascension, ex-route des trophées (v6 §8.4)")]
        [Tooltip("Les paliers, dans l'ordre croissant : 5 → 1 ◆, 10 → 2, 15 → 3, 20 → 5, 25 → 5, 30 → 10.")]
        [SerializeField] List<TrophyMilestone> trophyMilestones = new List<TrophyMilestone>();
        [Tooltip("Au-delà du dernier palier : un palier tous les N étages (10)…")]
        [SerializeField, Min(1)] int trophyRepeatEvery = 10;
        [Tooltip("…qui donne ce nombre de diamants (10).")]
        [SerializeField, Min(0)] int trophyRepeatDiamonds = 10;

        [Header("Renaissance (§13)")]
        [SerializeField, Min(1)] int rebirthCost = 10000;
        [SerializeField, Min(1)] int rebirthCoinMultiplier = 2;
        [SerializeField, Min(1)] int rebirthDiamondMultiplier = 2;
        [Tooltip("v5 : une seule renaissance possible.")]
        [SerializeField, Min(0)] int maxRebirths = 1;

        [Header("Onglet « Diamants » de la Boutique (§15.5) : AFFICHAGE SEULEMENT")]
        [Tooltip("Vrai = l'onglet est montré (packs grisés « Bientôt disponible », sans prix). " +
                 "Faux = l'onglet disparaît entièrement. Aucun paiement n'existe dans le jeu.")]
        [SerializeField] bool showDiamondPacks = true;
        [SerializeField] List<DiamondPackDef> diamondPacks = new List<DiamondPackDef>();

        public IReadOnlyList<DiamondTier> DiamondTiers => diamondTiers;
        public int ReviveBaseCost => reviveBaseCost;
        public int ReviveCostGrowth => reviveCostGrowth;
        public int MaxRevivesPerRun => maxRevivesPerRun;
        public int DiamondBonusCap => diamondBonusCap;
        public IReadOnlyList<CoinPackDef> CoinPacks => coinPacks;
        public IReadOnlyList<TrophyMilestone> TrophyMilestones => trophyMilestones;
        public int TrophyRepeatEvery => trophyRepeatEvery;
        public int TrophyRepeatDiamonds => trophyRepeatDiamonds;

        /// v6 §9.4 : le coût de la reprise n° k (k = 1 à 5) = base × croissance^(k − 1).
        /// 1, 2, 4, 8, 16 avec les valeurs de la spec. 0 si k sort des limites.
        public int ReviveCostFor(int k)
        {
            if (k < 1 || k > maxRevivesPerRun) return 0;
            long cost = reviveBaseCost;
            for (int i = 1; i < k; i++) cost *= reviveCostGrowth;
            return (int)Math.Min(cost, int.MaxValue);
        }
        public int RebirthCost => rebirthCost;
        public int RebirthCoinMultiplier => rebirthCoinMultiplier;
        public int RebirthDiamondMultiplier => rebirthDiamondMultiplier;
        public int MaxRebirths => maxRebirths;
        public bool ShowDiamondPacks => showDiamondPacks;
        public IReadOnlyList<DiamondPackDef> DiamondPacks => diamondPacks;

        /// Diamants d'un trésor en diamants à cet étage, AVANT la renaissance (1, 2 ou 3).
        public int DiamondsForFloor(int floor)
        {
            int amount = diamondTiers.Count > 0 ? diamondTiers[0].amount : 1;
            foreach (var tier in diamondTiers)
                if (floor >= tier.fromFloor) amount = tier.amount;
            return amount;
        }

        // =====================================================================
        // Chargement (une seule fois) et contrôle des données
        // =====================================================================

        static EconomyCatalog instance;

        /// Le catalogue, chargé depuis Assets/Resources/EconomyCatalog.asset. S'il manque, on
        /// crée en mémoire un catalogue aux valeurs de la spec (et on le signale en rouge).
        public static EconomyCatalog Instance
        {
            get
            {
                if (instance != null) return instance;
                instance = Resources.Load<EconomyCatalog>("EconomyCatalog");
                if (instance == null)
                {
                    Debug.LogError("EconomyCatalog introuvable : il doit être dans Assets/Resources/EconomyCatalog.asset (valeurs par défaut utilisées).");
                    instance = CreateInstance<EconomyCatalog>();
                    instance.diamondTiers = new List<DiamondTier>
                    {
                        new DiamondTier { fromFloor = 1, amount = 1 },
                        new DiamondTier { fromFloor = 6, amount = 2 },
                        new DiamondTier { fromFloor = 10, amount = 3 }
                    };
                }
                instance.Validate();
                return instance;
            }
        }

        /// Vérifie les données (erreurs de DONNÉES en rouge dans la console).
        public bool Validate()
        {
            bool ok = true;
            void Error(string message) { Debug.LogError("EconomyCatalog : " + message); ok = false; }
            if (diamondTiers.Count == 0) Error("il faut au moins une tranche d'étages pour les diamants.");
            for (int i = 1; i < diamondTiers.Count; i++)
                if (diamondTiers[i].fromFloor <= diamondTiers[i - 1].fromFloor)
                    Error("les tranches d'étages des diamants doivent être dans l'ordre croissant.");
            if (diamondTiers.Count > 0 && diamondTiers[0].fromFloor != 1) Error("la première tranche doit commencer à l'étage 1.");
            // v6 §5.4 : tous les lots d'échange au MÊME taux (pas de « bonus de gros »).
            // On compare en croisant les produits (pièces × diamants) pour éviter les virgules.
            for (int i = 1; i < coinPacks.Count; i++)
                if ((long)coinPacks[i].coins * coinPacks[0].diamonds != (long)coinPacks[0].coins * coinPacks[i].diamonds)
                    Error($"le lot d'échange « {coinPacks[i].displayName} » n'a pas le même taux que « {coinPacks[0].displayName} ».");
            for (int i = 1; i < trophyMilestones.Count; i++)
                if (trophyMilestones[i].floor <= trophyMilestones[i - 1].floor)
                    Error("les paliers de L'Ascension doivent être dans l'ordre croissant.");
            return ok;
        }
    }
}
