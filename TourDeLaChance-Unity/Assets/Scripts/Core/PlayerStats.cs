using System;
using UnityEngine;

namespace TourDeLaChance
{
    /// <summary>
    /// Les STATISTIQUES du joueur (spec v4 §12.3), comptées dès la v4 pour qu'au passage en
    /// ligne chacun arrive avec son vrai historique. Classe "static" (comme Wallet) : aucune
    /// interface ici, seulement des nombres sauvegardés dans les PlayerPrefs.
    ///
    ///   stats_pieces_encaissees : total de tous les « Encaisser » (ne fait que monter ;
    ///                             les reventes de doublons ne comptent pas)
    ///   stats_secrets_obtenus   : Secrets obtenus en ouvrant des boîtes, doublons compris
    ///   stats_boites_ouvertes   : boîtes ouvertes (sert au contrôle de vraisemblance)
    ///   stats_init_v4           : 1 quand l'initialisation depuis une sauvegarde v3 est faite
    ///
    /// Initialisation (une seule fois) : on ne connaît pas l'historique d'une sauvegarde v3,
    /// on part donc des seules valeurs sûres : le porte-monnaie, et les exemplaires de la
    /// collection (Secrets / tous).
    /// </summary>
    public static class PlayerStats
    {
        public const string CoinsKey = "stats_pieces_encaissees";
        public const string SecretsKey = "stats_secrets_obtenus";
        public const string BoxesKey = "stats_boites_ouvertes";
        public const string InitKey = "stats_init_v4";

        /// Toutes les clés (pour remettre à zéro après des tests).
        public static readonly string[] AllSaveKeys = { CoinsKey, SecretsKey, BoxesKey, InitKey };

        /// Un Secret vient d'être obtenu dans une boîte (le classement local le note avec sa date).
        public static event Action<ShopItem> SecretObtained;
        /// Une statistique a changé (pour rafraîchir l'affichage du classement).
        public static event Action Changed;

        const string SecretRarityId = "secret";

        public static int CoinsCashed => Mathf.Max(0, PlayerPrefs.GetInt(CoinsKey, 0));
        public static int SecretsObtained => Mathf.Max(0, PlayerPrefs.GetInt(SecretsKey, 0));
        public static int BoxesOpened => Mathf.Max(0, PlayerPrefs.GetInt(BoxesKey, 0));
        public static bool IsInitialized => PlayerPrefs.GetInt(InitKey, 0) == 1;

        /// À appeler au démarrage du jeu (LeaderboardRunner). Ne fait rien la 2e fois.
        /// Sans catalogue (erreur déjà affichée par Shop), on ne fait rien et on réessaiera.
        public static void EnsureInitialized()
        {
            if (IsInitialized || Shop.Catalog == null) return;
            int secrets = 0, boxes = 0;
            foreach (var pair in Collection.All())
            {
                var item = Shop.Find(pair.Key);
                if (item == null || item.IsFree) continue;
                boxes += pair.Value;
                if (item.RarityId == SecretRarityId) secrets += pair.Value;
            }
            PlayerPrefs.SetInt(CoinsKey, Wallet.Coins);
            PlayerPrefs.SetInt(SecretsKey, secrets);
            PlayerPrefs.SetInt(BoxesKey, boxes);
            PlayerPrefs.SetInt(InitKey, 1);
            PlayerPrefs.Save();
            Debug.Log($"Statistiques v4 initialisées : {Wallet.Coins} pièces encaissées, {secrets} Secret(s), {boxes} boîte(s).");
        }

        /// Un encaissement : on ajoute le montant au total (jamais un montant négatif).
        public static void AddCashedOut(int amount)
        {
            EnsureInitialized();
            if (amount <= 0) return;
            PlayerPrefs.SetInt(CoinsKey, CoinsCashed + amount);
            PlayerPrefs.Save();
            Changed?.Invoke();
        }

        /// Une boîte vient d'être ouverte (appelé par Shop.TryOpenBox APRÈS l'ajout à la
        /// collection ; Shop sauvegarde juste après). Si l'initialisation n'avait pas encore eu
        /// lieu, elle compte déjà cette boîte (elle lit la collection) : on n'ajoute rien de plus.
        public static void OnBoxOpened(ShopItem item, RarityDef rarity)
        {
            if (!IsInitialized)
            {
                EnsureInitialized();
            }
            else
            {
                PlayerPrefs.SetInt(BoxesKey, BoxesOpened + 1);
                if (rarity != null && rarity.id == SecretRarityId)
                    PlayerPrefs.SetInt(SecretsKey, SecretsObtained + 1);
            }
            if (rarity != null && rarity.id == SecretRarityId) SecretObtained?.Invoke(item);
            Changed?.Invoke();
        }

        /// OUTIL DE TEST : fixe les statistiques (et marque l'initialisation comme faite).
        public static void DebugSet(int coins, int secrets, int boxes)
        {
            PlayerPrefs.SetInt(CoinsKey, Mathf.Max(0, coins));
            PlayerPrefs.SetInt(SecretsKey, Mathf.Max(0, secrets));
            PlayerPrefs.SetInt(BoxesKey, Mathf.Max(0, boxes));
            PlayerPrefs.SetInt(InitKey, 1);
            PlayerPrefs.Save();
            Changed?.Invoke();
        }
    }
}
