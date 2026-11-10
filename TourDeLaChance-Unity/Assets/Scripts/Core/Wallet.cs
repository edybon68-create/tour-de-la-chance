using UnityEngine;

namespace TourDeLaChance
{
    /// <summary>
    /// Le porte-monnaie du joueur : les pièces encaissées, gardées pour toujours.
    /// "static" = il n'en existe qu'un seul, accessible de partout (Wallet.Coins).
    /// PlayerPrefs = petite sauvegarde intégrée à Unity, conservée même quand on quitte le jeu.
    /// </summary>
    public static class Wallet
    {
        const string SaveKey = "wallet_coins";

        public static int Coins => PlayerPrefs.GetInt(SaveKey, 0);

        /// Ajoute des pièces (encaissement, revente). Correctif B7 (spec v3 §13) : un montant
        /// NÉGATIF est refusé (rien ne change, un avertissement s'affiche dans la console) et
        /// 0 est ignoré sans rien sauvegarder. Renvoie true si on a vraiment crédité.
        /// Pour retirer des pièces, il n'existe que TrySpend (qui vérifie le solde).
        public static bool Add(int amount)
        {
            if (amount < 0)
            {
                Debug.LogWarning($"Wallet.Add({amount}) refusé : on n'ajoute jamais un montant négatif (utiliser TrySpend).");
                return false;
            }
            if (amount == 0) return false;
            PlayerPrefs.SetInt(SaveKey, Coins + amount);
            PlayerPrefs.Save(); // écrit tout de suite sur le disque
            return true;
        }

        /// Dépense des pièces (pour le shop). Renvoie true si c'est fait.
        /// Si le solde est insuffisant, on REFUSE et on ne change rien :
        /// le porte-monnaie ne peut jamais passer sous zéro.
        public static bool TrySpend(int amount)
        {
            if (amount < 0 || Coins < amount) return false;
            PlayerPrefs.SetInt(SaveKey, Coins - amount);
            PlayerPrefs.Save();
            return true;
        }
    }
}
