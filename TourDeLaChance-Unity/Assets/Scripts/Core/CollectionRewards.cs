using System;
using System.Collections.Generic;
using UnityEngine;

namespace TourDeLaChance
{
    /// <summary>
    /// La RÉCOMPENSE DE COLLECTION (spec v6 §6.3), sans affichage : quand le joueur possède pour
    /// la première fois TOUS les objets d'une boîte, il reçoit UNE fois des diamants
    /// (LootBox.completionReward : 5, 8 ou 10). Donnée automatiquement, sans « viens récupérer ».
    ///
    /// - Une collection complète ne redevient jamais incomplète (on ne vend jamais le dernier
    ///   exemplaire) : aucun retrait.
    /// - Ces diamants comptent dans stats_diamants_gagnes, mais ne sont NI doublés par la
    ///   renaissance, NI augmentés par le bonus de diamants (seuls les trésors le sont).
    /// - Sauvegarde : index_recompense_&lt;idBoîte&gt; = 1 une fois la récompense reçue.
    /// Quand vérifier : après l'écran de résultat d'une ouverture (« OK »), et au premier
    /// « Jouer » de la session pour les sauvegardes déjà complètes (rattrapage).
    /// </summary>
    public static class CollectionRewards
    {
        public const string KeyPrefix = "index_recompense_";

        /// Prévient l'affichage qu'une récompense vient d'être donnée (boîte, diamants).
        public static event Action<LootBox, int> Granted;

        public static string Key(LootBox box) => KeyPrefix + box.id;

        /// La récompense de cette boîte a-t-elle déjà été reçue ?
        public static bool Received(LootBox box) => box != null && PlayerPrefs.GetInt(Key(box), 0) == 1;

        /// Les diamants que donne la collection complète de cette boîte (0 = aucune récompense).
        public static int RewardOf(LootBox box) => box == null ? 0 : Mathf.Max(0, box.completionReward);

        /// Donne la récompense de CETTE boîte si elle est complète et pas encore reçue.
        /// Ajoutée tout de suite et sauvegardée. Renvoie les diamants donnés (0 sinon).
        public static int TryGrant(LootBox box)
        {
            int reward = RewardOf(box);
            if (reward <= 0 || Received(box) || !Shop.IsComplete(box)) return 0;
            PlayerPrefs.SetInt(Key(box), 1);          // marqué AVANT d'ajouter : jamais deux fois
            Diamonds.Add(reward, fromTreasure: true); // compté comme gagné ; Add sauvegarde
            Granted?.Invoke(box, reward);
            return reward;
        }

        /// Vérifie toutes les boîtes, dans l'ordre de la Boutique. Renvoie la liste des
        /// récompenses données (souvent vide).
        public static List<(LootBox box, int diamonds)> GrantAllPending()
        {
            var list = new List<(LootBox, int)>();
            foreach (var box in Shop.BoxesInShopOrder)
            {
                int n = TryGrant(box);
                if (n > 0) list.Add((box, n));
            }
            return list;
        }

        /// Toutes les clés de sauvegarde (remise à zéro après des tests).
        public static IEnumerable<string> AllSaveKeys()
        {
            foreach (var box in Shop.Boxes) yield return Key(box);
        }
    }
}
