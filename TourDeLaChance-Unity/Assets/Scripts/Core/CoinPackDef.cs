using System;
using UnityEngine;

namespace TourDeLaChance
{
    /// <summary>
    /// Un lot de l'ÉCHANGE (spec v6 §5.4) : on DONNE des diamants et on REÇOIT des pièces dans
    /// le porte-monnaie. Un seul sens : on n'achète JAMAIS de diamants (ni avec des pièces, ni
    /// avec de l'argent réel). v7 §1 : les gros lots donnent un peu plus de pièces par diamant
    /// (bonus plafonné à +40 %, jamais un taux qui baisse) : EconomyCatalog.Validate le vérifie.
    /// C'est une simple donnée (rangée dans EconomyCatalog) : la règle est dans CoinExchange.
    /// </summary>
    [Serializable]
    public class CoinPackDef
    {
        [Tooltip("Identifiant interne (echange_poignee, echange_pochette, echange_sac…). Sert aussi de nom d'image.")]
        public string id = "echange_poignee";
        [Tooltip("Nom court (Poignée, Sac…) : pas affiché sur la carte, utilisé dans la confirmation.")]
        public string displayName = "Poignée";
        [Tooltip("Diamants donnés par le joueur.")]
        [Min(1)] public int diamonds = 1;
        [Tooltip("Pièces reçues dans le porte-monnaie (jamais dans la cagnotte).")]
        [Min(1)] public int coins = 200;
        [Tooltip("Image du lot (DA). Vide = une pile de pièces dessinée par l'interface.")]
        public Sprite icon;
    }
}
