using System;
using System.Collections.Generic;
using UnityEngine;

namespace TourDeLaChance
{
    /// <summary>
    /// La ROUTE DES TROPHÉES (spec v6 §8), côté RÈGLES (aucun affichage) :
    ///   1. les PALIERS : étages 5, 10, 15, 20, 25, 30 puis tous les 10 ; les atteindre pour la
    ///      première fois donne des diamants (1, 2, 3, 5, 5, 10, puis 10), une seule fois pour
    ///      toujours, sauvegardés tout de suite et jamais perdus ;
    ///   2. le CALCUL des lignes de la route (gain du trésor, cagnotte moyenne, chance d'arriver
    ///      jusque-là…), avec LES MÊMES fonctions que le jeu (Upgrades.FloorGain,
    ///      AverageRiskPercent, AverageChestMultiplier, EconomyCatalog) : rien n'est écrit en dur.
    ///
    /// Honnêteté (§8.2) : le nombre de coffres, leur rareté, le risque et la chance de diamants
    /// ne dépendent PAS de l'étage mais des améliorations ; la route le montre tel quel.
    /// Les diamants des paliers ne sont ni doublés par la renaissance, ni augmentés par le bonus
    /// de diamants, mais comptent dans stats_diamants_gagnes.
    ///
    /// Sauvegarde : route_palier_recu = le plus haut étage-palier déjà récompensé (0, 5, 10…).
    /// </summary>
    public static class TrophyRoad
    {
        public const string ReceivedKey = "route_palier_recu";
        public static readonly string[] AllSaveKeys = { ReceivedKey };

        /// Prévient l'affichage du RATTRAPAGE d'une sauvegarde v5 (diamants donnés, record).
        public static event Action<int, int> CatchUpGranted;

        static EconomyCatalog Eco => EconomyCatalog.Instance;

        /// Le plus haut palier déjà récompensé (0 si aucun).
        public static int HighestReceived => Mathf.Max(0, PlayerPrefs.GetInt(ReceivedKey, 0));

        // =====================================================================
        // 1. Les paliers
        // =====================================================================

        /// Les diamants d'un palier à cet étage (0 si ce n'est pas un palier).
        public static int RewardAt(int floor)
        {
            var list = Eco.TrophyMilestones;
            foreach (var m in list) if (m.floor == floor) return m.diamonds;
            int last = list.Count > 0 ? list[list.Count - 1].floor : 0;
            int every = Mathf.Max(1, Eco.TrophyRepeatEvery);
            if (floor > last && (floor - last) % every == 0) return Eco.TrophyRepeatDiamonds;
            return 0;
        }

        public static bool IsMilestone(int floor) => floor >= 1 && RewardAt(floor) > 0;

        /// Le palier a-t-il déjà été récompensé ? (le record ne revient jamais en arrière)
        public static bool IsReceived(int floor) => IsMilestone(floor) && floor <= HighestReceived;

        /// Le prochain palier strictement au-dessus de cet étage.
        public static int NextMilestoneAbove(int floor)
        {
            for (int f = Mathf.Max(1, floor + 1); f < floor + 1000; f++) if (IsMilestone(f)) return f;
            return 0;
        }

        /// Donne TOUS les paliers pas encore reçus jusqu'à cet étage compris (en jeu il n'y en a
        /// qu'un à la fois ; pour le rattrapage, plusieurs d'un coup). Sauvegardé tout de suite.
        /// Renvoie le total de diamants donnés (0 si rien) et le plus haut palier donné.
        public static int GrantUpTo(int floor, out int highestGranted)
        {
            highestGranted = 0;
            int total = 0;
            for (int f = HighestReceived + 1; f <= floor; f++)
            {
                int r = RewardAt(f);
                if (r <= 0) continue;
                total += r;
                highestGranted = f;
            }
            if (highestGranted == 0) return 0;
            PlayerPrefs.SetInt(ReceivedKey, highestGranted); // marqué avant d'ajouter
            if (total > 0) Diamonds.Add(total, fromTreasure: true);
            else PlayerPrefs.Save();
            return total;
        }

        /// v6 §8.4 : au premier « Jouer » en v6, les paliers déjà dépassés par le record sont
        /// donnés d'un coup (une seule fois : ensuite ils sont marqués reçus).
        public static int CatchUp(int record)
        {
            int total = GrantUpTo(record, out _);
            if (total > 0) CatchUpGranted?.Invoke(total, record);
            return total;
        }

        /// Les diamants des paliers jusqu'à un étage (pour le texte « Trophées : 2 / 6 »).
        public static List<int> MilestonesUpTo(int maxFloor)
        {
            var list = new List<int>();
            for (int f = 1; f <= maxFloor; f++) if (IsMilestone(f)) list.Add(f);
            return list;
        }

        // =====================================================================
        // 2. Les lignes de la route (calculées avec les fonctions du jeu)
        // =====================================================================

        /// Une dalle de la route.
        public struct Row
        {
            public int Floor;
            public int GainMin;        // trésor en coffre de bois
            public int GainMax;        // trésor de la meilleure rareté possible au niveau actuel
            public int PotAverage;     // ≈ cagnotte en arrivant (rareté moyenne), 0 à l'étage 1
            public double ReachChance; // chance d'arriver jusque-là (0 à 1), 1 à l'étage 1
            public int DiamondsPerTreasure; // base × renaissance (sans le bonus de tenue)
            public int MilestoneReward;     // 0 si ce n'est pas un palier
        }

        const int CoinsPerFloor = 10; // la même base que le GameManager (champ coinsPerFloor = 10)

        /// La meilleure rareté de coffre possible à ce niveau de Coffres précieux (poids > 0).
        public static int BestRarityRank(int chestsLevel)
        {
            var w = Upgrades.ChestWeights(chestsLevel);
            int best = 0;
            for (int i = 0; i < w.Length; i++) if (w[i] > 0) best = i;
            return best;
        }

        /// Calcule la dalle d'un étage avec les niveaux, la tenue et la renaissance ACTUELS.
        public static Row RowFor(int floor)
        {
            int gains = Upgrades.Level(Upgrades_.Gains);
            int chests = Upgrades.Level(Upgrades_.Chests);
            int traps = Upgrades.Level(Upgrades_.Traps);
            int outfit = Shop.OutfitBonusPercent;
            int rebirth = Rebirth.CoinMultiplier;
            var row = new Row { Floor = floor };
            row.GainMin = Upgrades.FloorGain(CoinsPerFloor, floor, gains, 0, outfit, rebirth);
            row.GainMax = Upgrades.FloorGain(CoinsPerFloor, floor, gains, BestRarityRank(chests), outfit, rebirth);
            row.PotAverage = AveragePotBefore(floor, gains, chests, outfit, rebirth);
            float safe = 1f - Upgrades.AverageRiskPercent(traps) / 100f;
            row.ReachChance = Math.Pow(Mathf.Clamp01(safe), Mathf.Max(0, floor - 1));
            row.DiamondsPerTreasure = Eco.DiamondsForFloor(floor) * Rebirth.DiamondMultiplier;
            row.MilestoneReward = RewardAt(floor);
            return row;
        }

        /// Somme des gains MOYENS (rareté moyenne du niveau) des étages 1 à floor − 1.
        /// Même formule que FloorGain, mais avec le multiplicateur moyen des coffres.
        static int AveragePotBefore(int floor, int gains, int chests, int outfit, int rebirth)
        {
            double bourse = Upgrades.GainMultiplier(gains) - 1.0;
            double m = (1.0 + bourse + Mathf.Max(0, outfit) / 100.0) * Upgrades.AverageChestMultiplier(chests) * Mathf.Max(1, rebirth);
            double sum = 0;
            for (int f = 1; f < floor; f++) sum += CoinsPerFloor * f * m;
            return (int)Math.Round(sum);
        }

        /// Le dernier étage affiché : max(30, record + 5), arrondi au palier de 10 supérieur
        /// au-dessus de 30 (record 34 → 40).
        public static int TopFloorShown(int record)
        {
            int top = Mathf.Max(30, record + 5);
            if (top > 30) top = Mathf.CeilToInt(top / 10f) * 10;
            return top;
        }

        /// Les nombres de coffres possibles au niveau actuel de Coffre en plus : « 5 coffres »,
        /// « 4 ou 5 coffres », « 3, 4 ou 5 coffres ».
        public static string ChestCountText(int trapsLevel)
        {
            var w = Upgrades.CountWeights(trapsLevel);
            int baseCount = Upgrades.Catalog != null ? Upgrades.Catalog.BaseChestCount : 3;
            var counts = new List<string>();
            for (int i = 0; i < w.Length; i++) if (w[i] > 0) counts.Add((baseCount + i).ToString());
            if (counts.Count == 0) return $"{baseCount} coffres";
            if (counts.Count == 1) return $"{counts[0]} coffres";
            return string.Join(", ", counts.GetRange(0, counts.Count - 1)) + " ou " + counts[counts.Count - 1] + " coffres";
        }
    }
}
