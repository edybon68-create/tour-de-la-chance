using System;
using UnityEngine;

namespace TourDeLaChance
{
    /// <summary>
    /// Une rareté de COFFRE (spec v3 §5.3) : Bois, Bronze, Argent, Or, Diamant.
    /// Rien à voir avec la rareté des objets des boîtes (RarityDef) !
    /// Le multiplicateur change le gain de l'étage ; les couleurs ne servent qu'à l'affichage.
    /// Rangée dans le catalogue des améliorations (UpgradeCatalog), seule source des chiffres.
    /// </summary>
    [Serializable]
    public class ChestRarityDef
    {
        [Tooltip("Identifiant : bois, bronze, argent, or, diamant.")]
        public string id = "bois";
        [Tooltip("Texte de l'étiquette d'étage : « Coffres d'or ».")]
        public string displayName = "Coffres en bois";
        [Tooltip("Nom court pour le panneau Améliorations : « Or ».")]
        public string shortName = "Bois";
        [Tooltip("Le gain de l'étage est multiplié par ce nombre (×1, ×1,5, ×2, ×3, ×5).")]
        [Min(0f)] public float multiplier = 1f;
        [Tooltip("v5 §10.2 : chance (pour mille) qu'un trésor de ces coffres soit en diamants " +
                 "à la place des pièces (Bois 10, Bronze 15, Argent 20, Or 30, Cristal 50).")]
        [Range(0, 1000)] public int diamondChancePermille;

        [Header("Affichage seulement")]
        [Tooltip("Couleur du Corps fermé (le code la remet à chaque nouvel étage).")]
        public Color bodyColor = new Color(0.549f, 0.349f, 0.2f);
        [Tooltip("Couleur des effets UI (étincelles, halo). Diamant : alternée avec la 2e couleur.")]
        public Color fxColor = Color.white;
        public Color fxColor2 = Color.white;
        [Tooltip("Petite image du coffre (étiquette d'étage, panneau Améliorations).")]
        public Sprite icon;
    }
}
