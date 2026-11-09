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

        public static void Add(int amount)
        {
            PlayerPrefs.SetInt(SaveKey, Coins + amount);
            PlayerPrefs.Save(); // écrit tout de suite sur le disque
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
