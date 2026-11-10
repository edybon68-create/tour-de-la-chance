using System;
using System.Collections.Generic;
using UnityEngine;

namespace TourDeLaChance
{
    /// <summary>
    /// Une amélioration (spec v3 §5.1) : son nom, sa phrase, son icône et ses 6 niveaux
    /// (0 = pas achetée, puis 1 à 5). Rangée dans UpgradeCatalog.
    /// </summary>
    [Serializable]
    public class UpgradeDef
    {
        [Tooltip("gains, coffres ou pieges. Sert aussi de clé de sauvegarde : amelioration_<id>.")]
        public string id = "gains";
        public string displayName = "Bourse magique";
        [Tooltip("Phrase courte sous le nom, dans le panneau.")]
        public string description = "";
        public Sprite icon;
        [Tooltip("Niveaux 0 à 5, dans l'ordre (6 lignes).")]
        public List<UpgradeLevel> levels = new List<UpgradeLevel>();
    }
}
