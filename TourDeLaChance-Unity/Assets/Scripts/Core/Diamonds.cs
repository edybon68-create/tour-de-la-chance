using System;
using UnityEngine;

namespace TourDeLaChance
{
    /// <summary>
    /// Le compteur de DIAMANTS (spec v5 §10), la 2e monnaie, rare. Comme Wallet, c'est une
    /// classe "static" : il n'y en a qu'une, accessible de partout (Diamonds.Count).
    ///
    /// Les diamants ne vont JAMAIS dans la cagnotte : un trésor en diamants les ajoute ici
    /// directement, et ils sont sauvegardés tout de suite. Ils ne peuvent donc jamais être
    /// perdus (même si le joueur tombe sur un piège plus haut). On ne les convertit jamais en
    /// pièces, et aucun paiement réel n'en vend (règle d'or v5).
    ///
    /// Sauvegarde (PlayerPrefs) :
    ///   diamants               = le solde (jamais négatif)
    ///   stats_diamants_gagnes  = total gagné dans les trésors (ne baisse jamais) ; sert aussi
    ///                            à savoir si le compteur doit apparaître dans le HUD (§10.4)
    ///   stats_reprises         = reprises utilisées (§11)
    /// </summary>
    public static class Diamonds
    {
        public const string SaveKey = "diamants";
        public const string WonKey = "stats_diamants_gagnes";
        public const string RevivesKey = "stats_reprises";

        /// Toutes les clés (pour remettre à zéro après des tests).
        public static readonly string[] AllSaveKeys = { SaveKey, WonKey, RevivesKey };

        /// Prévient l'affichage que le solde a changé.
        public static event Action Changed;

        /// Le solde (une valeur abîmée ou négative est lue comme 0).
        public static int Count => Mathf.Max(0, PlayerPrefs.GetInt(SaveKey, 0));

        /// Total des diamants gagnés dans les trésors depuis toujours.
        public static int TotalWon => Mathf.Max(0, PlayerPrefs.GetInt(WonKey, 0));

        /// §10.4 : le compteur n'apparaît dans le HUD qu'une fois un diamant gagné (un « 0 »
        /// permanent inviterait à en acheter).
        public static bool CounterUnlocked => TotalWon > 0;

        /// Ajoute des diamants. fromTreasure = vrai pour un gain de trésor (compté aussi dans
        /// stats_diamants_gagnes). Un montant négatif ou nul est refusé. Sauvegarde tout de suite.
        public static bool Add(int amount, bool fromTreasure)
        {
            if (amount < 0)
            {
                Debug.LogWarning($"Diamonds.Add({amount}) refusé : on n'ajoute jamais un montant négatif (utiliser TrySpend).");
                return false;
            }
            if (amount == 0) return false;
            PlayerPrefs.SetInt(SaveKey, Count + amount);
            if (fromTreasure) PlayerPrefs.SetInt(WonKey, TotalWon + amount);
            PlayerPrefs.Save();
            Changed?.Invoke();
            return true;
        }

        /// Dépense des diamants (reprise, Boîte Pirates). Refuse sans rien changer si le solde
        /// ne suffit pas : jamais sous zéro. C'est aussi ce qui protège du double-clic.
        public static bool TrySpend(int amount)
        {
            if (amount < 0 || Count < amount) return false;
            PlayerPrefs.SetInt(SaveKey, Count - amount);
            PlayerPrefs.Save();
            Changed?.Invoke();
            return true;
        }

        /// Une reprise a été utilisée (statistique).
        public static void CountRevive()
        {
            PlayerPrefs.SetInt(RevivesKey, Mathf.Max(0, PlayerPrefs.GetInt(RevivesKey, 0)) + 1);
            PlayerPrefs.Save();
        }

        /// OUTIL DE TEST : fixe le solde (et marque le compteur comme débloqué si > 0).
        public static void DebugSet(int count)
        {
            PlayerPrefs.SetInt(SaveKey, Mathf.Max(0, count));
            if (count > 0 && TotalWon == 0) PlayerPrefs.SetInt(WonKey, count);
            PlayerPrefs.Save();
            Changed?.Invoke();
        }

        /// Prévient l'affichage que la sauvegarde a changé « de l'extérieur » (tests).
        public static void NotifyChanged() => Changed?.Invoke();
    }
}
