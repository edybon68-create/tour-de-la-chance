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
    }
}
