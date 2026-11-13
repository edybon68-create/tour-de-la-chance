using System;
using UnityEngine;

namespace TourDeLaChance
{
    /// <summary>
    /// Les RÈGLES de l'échange (spec v6 §5.4), sans aucun affichage : on DONNE des diamants et on
    /// REÇOIT des pièces dans le porte-monnaie (jamais dans la cagnotte). Classe "static", comme
    /// Wallet et Diamonds.
    ///
    /// - Un seul sens : il n'existe AUCUNE fonction pour obtenir des diamants contre des pièces.
    /// - Taux fixe (1 ◆ = 200 pièces), le même pour tous les lots, lu dans EconomyCatalog.
    /// - Aucun plafond (décision de l'auteur) : l'équilibre tient par le taux seul.
    /// - Le taux ne change pas avec la renaissance (elle double déjà pièces ET diamants gagnés).
    /// QUAND on peut échanger (Boutique ouverte, moment calme) est décidé par l'interface, qui
    /// ne s'ouvre qu'aux moments calmes (GameManager.CanOpenPanel).
    /// </summary>
    public static class CoinExchange
    {
        /// Prévient l'affichage qu'un échange vient d'avoir lieu (diamants donnés, pièces reçues).
        public static event Action<CoinPackDef> Exchanged;

        /// Le joueur a-t-il assez de diamants pour ce lot ?
        public static bool CanExchange(CoinPackDef pack) => pack != null && pack.diamonds > 0 && Diamonds.Count >= pack.diamonds;

        /// « Il te manque 2 ◆ » (0 si on peut).
        public static int MissingDiamonds(CoinPackDef pack) => pack == null ? 0 : Mathf.Max(0, pack.diamonds - Diamonds.Count);

        /// Échange, EN UNE FOIS et dans cet ordre : relire le solde, dépenser les diamants
        /// (Diamonds.TrySpend refuse si le solde ne suffit pas : jamais sous zéro), ajouter les
        /// pièces au porte-monnaie, compter la statistique, SAUVEGARDER, prévenir.
        /// Un double-clic ne fait qu'un échange s'il ne reste pas assez pour le second : au 2e
        /// appel, le solde est relu et TrySpend refuse.
        public static bool TryExchange(CoinPackDef pack)
        {
            if (!CanExchange(pack)) return false;
            if (!Diamonds.TrySpend(pack.diamonds)) return false;
            Wallet.Add(pack.coins);
            Diamonds.CountExchanged(pack.diamonds);
            PlayerPrefs.Save();
            Exchanged?.Invoke(pack);
            return true;
        }

        /// Retrouve un lot par son identifiant (outils de test).
        public static CoinPackDef Find(string id)
        {
            foreach (var p in EconomyCatalog.Instance.CoinPacks) if (p.id == id) return p;
            return null;
        }
    }
}
