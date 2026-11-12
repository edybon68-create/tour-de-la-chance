using System;
using UnityEngine;

namespace TourDeLaChance
{
    /// <summary>
    /// Un pack de diamants AFFICHÉ dans l'onglet « Diamants » de la Boutique (spec v5 §15.5).
    /// ATTENTION : c'est une simple donnée d'affichage. Il n'y a AUCUN prix, aucun identifiant
    /// de magasin (store), aucun paiement : les cartes sont toujours grisées « Bientôt
    /// disponible ». Activer un jour des paiements demande une décision de l'auteur (§16).
    /// </summary>
    [Serializable]
    public class DiamondPackDef
    {
        [Tooltip("Identifiant interne (pack_diamants_1…). Ce n'est PAS un identifiant de magasin.")]
        public string id = "pack_diamants_1";
        public string displayName = "Poignée de diamants";
        [Min(1)] public int amount = 10;
        [Tooltip("Image du pack (lot d'images v5). Vide = un diamant dessiné par l'interface.")]
        public Sprite icon;
    }
}
