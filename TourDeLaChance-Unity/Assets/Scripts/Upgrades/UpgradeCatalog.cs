using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TourDeLaChance
{
    /// <summary>
    /// Le catalogue des améliorations (spec v3 §18) : LA source unique des prix, des effets,
    /// des chances de rareté et de nombre de coffres. Le tirage ET le panneau Améliorations
    /// lisent ces mêmes nombres : impossible d'afficher un pourcentage différent de celui du
    /// code (règle d'or n°3). Rangé dans Assets/Resources/ pour être chargé par son nom.
    /// </summary>
    [CreateAssetMenu(fileName = "UpgradeCatalog", menuName = "Tour de la Chance/Catalogue des améliorations")]
    public class UpgradeCatalog : ScriptableObject
    {
        [Tooltip("Les 3 améliorations, dans l'ordre d'affichage : gains, coffres, pieges.")]
        [SerializeField] List<UpgradeDef> upgrades = new List<UpgradeDef>();
        [Tooltip("Les 5 raretés de coffre, de la plus simple (Bois) à la plus précieuse (Diamant).")]
        [SerializeField] List<ChestRarityDef> chestRarities = new List<ChestRarityDef>();
        [Tooltip("Nombre de coffres de la 1re colonne de poids de « Coffre en plus » (3).")]
        [SerializeField] int baseChestCount = 3;

        public IReadOnlyList<UpgradeDef> Upgrades => upgrades;
        public IReadOnlyList<ChestRarityDef> ChestRarities => chestRarities;
        public int BaseChestCount => baseChestCount;

        public UpgradeDef Get(string id) => upgrades.FirstOrDefault(u => u.id == id);

        /// Contrôle des données au chargement (spec §18). Une erreur ici est une erreur de
        /// DONNÉES, écrite en rouge dans la console. Renvoie true si tout va bien.
        public bool Validate()
        {
            bool ok = true;
            void Error(string message) { Debug.LogError("UpgradeCatalog : " + message); ok = false; }

            if (chestRarities.Count != 5) Error($"il faut 5 raretés de coffre (il y en a {chestRarities.Count}).");
            foreach (var id in new[] { Upgrades_.Gains, Upgrades_.Chests, Upgrades_.Traps })
                if (Get(id) == null) Error($"l'amélioration « {id} » manque.");

            foreach (var u in upgrades)
            {
                if (u.levels.Count < Upgrades_.MaxLevel + 1)
                    Error($"« {u.displayName} » a {u.levels.Count} niveaux au lieu de {Upgrades_.MaxLevel + 1} (0 à 5).");
                for (int i = 1; i < u.levels.Count; i++)
                {
                    // Prix strictement croissants : chaque niveau coûte plus que le précédent.
                    if (u.levels[i].price <= u.levels[i - 1].price)
                        Error($"« {u.displayName} » : le prix du niveau {i} ({u.levels[i].price}) n'est pas plus grand que celui du niveau {i - 1}.");
                }
                if (u.id == Upgrades_.Gains) continue; // la Bourse n'a pas de poids
                int columns = u.id == Upgrades_.Chests ? chestRarities.Count : 3;
                for (int i = 0; i < u.levels.Count; i++)
                {
                    var w = u.levels[i].weights;
                    if (w == null || w.Length != columns) { Error($"« {u.displayName} » niveau {i} : il faut {columns} poids."); continue; }
                    if (w.Sum() != 100) Error($"« {u.displayName} » niveau {i} : les poids font {w.Sum()} au lieu de 100.");
                    if (w.Any(x => x < 0)) Error($"« {u.displayName} » niveau {i} : un poids est négatif.");
                }
            }
            return ok;
        }
    }

    /// Les identifiants et constantes partagés (petit nom avec « _ » pour ne pas confondre
    /// avec la classe Upgrades, qui contient les règles).
    public static class Upgrades_
    {
        public const string Gains = "gains";
        public const string Chests = "coffres";
        public const string Traps = "pieges";
        public const int MaxLevel = 5;
    }
}
