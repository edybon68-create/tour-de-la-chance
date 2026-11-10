using System;
using UnityEngine;

namespace TourDeLaChance
{
    /// <summary>
    /// Un niveau d'une amélioration (spec v3 §5.2 à §5.4) : son prix et son effet.
    /// Selon l'amélioration, l'effet est une VALEUR (Bourse : ×1,2) ou un TABLEAU DE POIDS
    /// sur 100 (Coffres précieux : chances de chaque rareté ; Coffre en plus : chances
    /// d'avoir 3, 4 ou 5 coffres). Le niveau 0 (pas acheté) a un prix de 0.
    /// </summary>
    [Serializable]
    public class UpgradeLevel
    {
        [Tooltip("Prix de CE niveau (en pièces du porte-monnaie). 0 pour le niveau 0.")]
        [Min(0)] public int price;
        [Tooltip("Bourse magique : le multiplicateur de gain (1, 1,2, 1,4…).")]
        public float value = 1f;
        [Tooltip("Coffres précieux : poids Bois, Bronze, Argent, Or, Diamant. " +
                 "Coffre en plus : poids de 3, 4, 5 coffres. La somme doit faire 100.")]
        public int[] weights = new int[0];
        [Tooltip("Texte court de l'effet (Bourse : « Trésors +20 % »).")]
        public string effectText = "";
    }
}
