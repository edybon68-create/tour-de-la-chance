using System;
using UnityEngine;

namespace TourDeLaChance
{
    /// <summary>
    /// Un lot de l'ÉCHANGE (spec v6 §5.4) : on DONNE des diamants et on REÇOIT des pièces dans
    /// le porte-monnaie. Un seul sens : on n'achète JAMAIS de diamants (ni avec des pièces, ni
    /// avec de l'argent réel). Le taux est le même pour tous les lots (1 ◆ = 200 pièces) :
    /// EconomyCatalog.Validate le vérifie.
    /// C'est une simple donnée (rangée dans EconomyCatalog) : la règle est dans CoinExchange.
    /// </summary>
    [Serializable]
    public class CoinPackDef
    {
        [Tooltip("Identifiant interne (echange_poignee, echange_sac, echange_coffre). Sert aussi de nom d'image.")]
        public string id = "echange_poignee";
        public string displayName = "Poignée de pièces";
        [Tooltip("Diamants donnés par le joueur.")]
        [Min(1)] public int diamonds = 1;
        [Tooltip("Pièces reçues dans le porte-monnaie (jamais dans la cagnotte).")]
        [Min(1)] public int coins = 200;
        [Tooltip("Image du lot (DA). Vide = une pile de pièces dessinée par l'interface.")]
        public Sprite icon;
    }
}
