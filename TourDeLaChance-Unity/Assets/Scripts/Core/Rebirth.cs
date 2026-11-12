using System;
using UnityEngine;

namespace TourDeLaChance
{
    /// <summary>
    /// Les RÈGLES de la renaissance (spec v5 §13), sans aucun affichage. Classe "static",
    /// comme Upgrades. Tous les chiffres viennent du catalogue de l'économie (EconomyCatalog).
    ///
    /// Renaître (une seule fois en v5) : coûte 10 000 pièces du porte-monnaie, remet le
    /// porte-monnaie ENTIER à 0 et les 3 améliorations au niveau 0 ; garde objets, tenue,
    /// diamants, records, statistiques, classement, pseudo, réglages ; donne pour toujours
    /// ×2 pièces sur les trésors, ×2 diamants dans les trésors et débloque les Boîtes Animaux.
    /// Les multiplicateurs ne touchent JAMAIS les chances, ni la revente, ni les prix.
    ///
    /// QUAND on peut renaître (moment calme, sans cagnotte) est décidé par le GameManager
    /// (GameManager.CanRebirthNow) ; ici on ne vérifie que la sauvegarde.
    ///
    /// Sauvegarde : renaissance_niveau (0 = pas faite, 1 = faite ; ramené entre 0 et le
    /// maximum), stats_renaissances (nombre de renaissances faites).
    /// </summary>
    public static class Rebirth
    {
        public const string LevelKey = "renaissance_niveau";
        public const string StatsKey = "stats_renaissances";
        public static readonly string[] AllSaveKeys = { LevelKey, StatsKey };

        /// Prévient l'affichage qu'une renaissance vient d'être faite.
        public static event Action Changed;

        static EconomyCatalog Eco => EconomyCatalog.Instance;

        /// Niveau de renaissance (0 ou 1 en v5), toujours ramené dans les limites.
        public static int Level => Mathf.Clamp(PlayerPrefs.GetInt(LevelKey, 0), 0, Mathf.Max(0, Eco.MaxRebirths));

        public static bool IsDone => Level > 0;

        /// ×2 pièces sur les trésors après la renaissance (×1 avant).
        public static int CoinMultiplier => IsDone ? Eco.RebirthCoinMultiplier : 1;
        /// ×2 diamants dans les trésors après la renaissance (×1 avant).
        public static int DiamondMultiplier => IsDone ? Eco.RebirthDiamondMultiplier : 1;

        public static int Cost => Eco.RebirthCost;

        /// Reste-t-il une renaissance possible (v5 : une seule) ?
        public static bool Available => Level < Eco.MaxRebirths;

        /// « Il te manque X pièces » (0 si le porte-monnaie suffit).
        public static int MissingCoins => Mathf.Max(0, Cost - Wallet.Coins);

        /// La sauvegarde permet-elle de renaître (renaissance disponible ET assez de pièces) ?
        public static bool CanAfford => Available && Wallet.Coins >= Cost;

        /// Renaît, EN UNE FOIS et dans cet ordre (§13.2) : vérifier, mettre le porte-monnaie à
        /// 0, remettre les 3 améliorations à 0, niveau + 1, statistique + 1, SAUVEGARDER,
        /// prévenir. Refuse sans rien changer sinon (double-clic compris : au 2e appel, la
        /// renaissance n'est plus disponible).
        public static bool TryRebirth()
        {
            if (!CanAfford) return false;
            if (!Wallet.TrySpend(Wallet.Coins)) return false; // TOUTES les pièces, même au-delà du coût
            Upgrades.ResetAllLevels(save: false);
            PlayerPrefs.SetInt(LevelKey, Level + 1);
            PlayerPrefs.SetInt(StatsKey, Mathf.Max(0, PlayerPrefs.GetInt(StatsKey, 0)) + 1);
            PlayerPrefs.Save();
            Changed?.Invoke();
            Upgrades.NotifyChanged();
            return true;
        }

        /// OUTIL DE TEST : fixe le niveau de renaissance (0 pour l'annuler), sans rien d'autre.
        public static void DebugSetLevel(int level)
        {
            PlayerPrefs.SetInt(LevelKey, Mathf.Max(0, level));
            PlayerPrefs.Save();
            Changed?.Invoke();
        }
    }
}
