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
    /// perdus (même si le joueur tombe sur un piège plus haut). v6 : on peut les ÉCHANGER contre
    /// des pièces dans la Boutique (CoinExchange), jamais l'inverse ; aucun paiement réel n'en
    /// vend (règle d'or).
    ///
    /// Sauvegarde (PlayerPrefs) :
    ///   diamants               = le solde (jamais négatif)
    ///   stats_diamants_gagnes  = total gagné dans les trésors (ne baisse jamais) ; sert aussi
    ///                            à savoir si le compteur doit apparaître dans le HUD (§10.4)
    ///   stats_reprises         = reprises utilisées (§11)
    ///   diamants_eclats        = v6 §11.3 : les « éclats » (millièmes de diamant, 0 à 999) que
    ///                            le bonus de diamants met de côté
    ///   stats_echanges_diamants = v6 §5.4 : total des diamants échangés contre des pièces
    /// </summary>
    public static class Diamonds
    {
        public const string SaveKey = "diamants";
        public const string WonKey = "stats_diamants_gagnes";
        public const string RevivesKey = "stats_reprises";
        public const string ShardsKey = "diamants_eclats";
        public const string ExchangedKey = "stats_echanges_diamants";
        /// 1 000 millièmes = 1 diamant.
        public const int ShardsPerDiamond = 1000;

        /// Toutes les clés (pour remettre à zéro après des tests).
        public static readonly string[] AllSaveKeys = { SaveKey, WonKey, RevivesKey, ShardsKey, ExchangedKey };

        /// v6 §11.3 : les éclats mis de côté, en millièmes (0 à 999 ; une valeur abîmée est ramenée).
        public static int Shards => Mathf.Clamp(PlayerPrefs.GetInt(ShardsKey, 0), 0, ShardsPerDiamond - 1);

        /// Total des diamants échangés contre des pièces (statistique).
        public static int TotalExchanged => Mathf.Max(0, PlayerPrefs.GetInt(ExchangedKey, 0));

        /// Prévient l'affichage que le solde a changé.
        public static event Action Changed;

        /// Le solde (une valeur abîmée ou négative est lue comme 0).
        public static int Count => Mathf.Max(0, PlayerPrefs.GetInt(SaveKey, 0));

        /// Total des diamants gagnés dans les trésors depuis toujours.
        public static int TotalWon => Mathf.Max(0, PlayerPrefs.GetInt(WonKey, 0));

        /// §10.4 : le compteur n'apparaît dans le HUD qu'une fois un diamant gagné (un « 0 »
        /// permanent inviterait à en acheter).
        public static bool CounterUnlocked => TotalWon > 0;

        /// Ajoute des diamants. fromTreasure = vrai pour un diamant GAGNÉ en jouant (trésor, et en
        /// v6 palier de route ou collection complète) : compté aussi dans stats_diamants_gagnes.
        /// Faux pour un remboursement. Un montant négatif ou nul est refusé. Sauvegarde tout de suite.
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

        /// v6 §11.3 : paie un trésor en diamants AVEC le bonus de la tenue (objets Pirates).
        ///   millièmes = montant (base × renaissance) × (100 + bonus) × 10, plus les éclats gardés ;
        ///   on donne millièmes ÷ 1000 diamants (partie entière) et on garde le reste (0 à 999).
        /// Tout est déterministe (aucun hasard) : un trésor donne toujours AU MOINS son montant.
        /// fromBonus = combien de diamants viennent du bonus (pour « dont 1 grâce à ta tenue »).
        /// Comptés dans stats_diamants_gagnes, sauvegardés tout de suite.
        public static int AddWithBonus(int amount, int bonusPercent, out int fromBonus)
        {
            fromBonus = 0;
            if (amount <= 0) return 0;
            long thousandths = Shards + (long)amount * (100 + Mathf.Max(0, bonusPercent)) * 10;
            int given = (int)(thousandths / ShardsPerDiamond);
            int rest = (int)(thousandths % ShardsPerDiamond);
            fromBonus = Mathf.Max(0, given - amount);
            PlayerPrefs.SetInt(ShardsKey, rest);
            Add(given, fromTreasure: true); // Add sauvegarde (éclats compris) et prévient
            return given;
        }

        /// v6 §5.4 : la statistique des diamants échangés (appelée par CoinExchange, qui sauvegarde).
        public static void CountExchanged(int amount) =>
            PlayerPrefs.SetInt(ExchangedKey, TotalExchanged + Mathf.Max(0, amount));

        /// OUTIL DE TEST : fixe les éclats (en millièmes).
        public static void DebugSetShards(int thousandths)
        {
            PlayerPrefs.SetInt(ShardsKey, Mathf.Clamp(thousandths, 0, ShardsPerDiamond - 1));
            PlayerPrefs.Save();
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
