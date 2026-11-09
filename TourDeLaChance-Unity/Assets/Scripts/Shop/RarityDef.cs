using System;
using UnityEngine;

namespace TourDeLaChance
{
    /// <summary>
    /// Une rareté (spec boites §5.1) : son nom, sa couleur, son POIDS dans le tirage et la
    /// valeur de revente d'un doublon. Rangée dans le catalogue (ShopCatalog).
    /// Le poids est LA source des chances : le tirage ET l'affichage lisent ce même nombre.
    /// </summary>
    [Serializable]
    public class RarityDef
    {
        [Tooltip("Identifiant (commun, peu_commun…). Sert aussi de classe USS : rarete-commun.")]
        public string id = "commun";
        public string displayName = "Commun";
        [Tooltip("Couleur de base : liserés, pastilles, halos.")]
        public Color color = Color.gray;
        [Tooltip("Couleur des petits textes sur panneau sombre (plus claire pour Rare et Super rare).")]
        public Color textColor = Color.gray;
        [Tooltip("Poids dans le tirage, sur le total de toutes les raretés (1000 = 100 %).")]
        [Min(0)] public int weight = 500;
        [Tooltip("Pièces gagnées en vendant un doublon. Toujours moins que le prix d'une boîte.")]
        [Min(0)] public int sellValue = 10;
    }
}
