using System;
using UnityEngine;

namespace TourDeLaChance
{
    /// <summary>
    /// Le NIVEAU GRAPHIQUE choisi dans les Réglages (spec v6 §12) : 0 Faible, 1 Moyen (défaut),
    /// 2 Élevé. Classe "static" toute simple : elle garde le choix (PlayerPrefs) et prévient
    /// ceux qui doivent s'adapter (événement Changed).
    ///
    /// Qui lit ce niveau ?
    ///   - GraphicsApplier (dans la scène) : niveau de qualité URP (échelle de rendu, MSAA,
    ///     ombres), Bloom, images par seconde ;
    ///   - nos effets (CoinBurst, DiamondBurst, TrapCloud…) : MOITIÉ des particules en Faible ;
    ///   - AccessoryRig : ressorts figés en Faible ;
    ///   - les caméras de vitrine : taille de leur image (RenderTexture).
    /// RÈGLE D'OR : rien du JEU ne change (chances, gains, durées du suspense).
    ///
    /// Sauvegarde : reglages_graphismes = 0, 1 ou 2 (une valeur abîmée est lue comme 1 = Moyen).
    /// </summary>
    public static class GraphicsLevel
    {
        public const string SaveKey = "reglages_graphismes";
        public const int Low = 0, Medium = 1, High = 2;
        public static readonly string[] AllSaveKeys = { SaveKey };
        public static readonly string[] Names = { "Faible", "Moyen", "Élevé" };

        /// Prévient que le niveau vient de changer.
        public static event Action Changed;

        /// Le niveau actuel (0, 1 ou 2).
        public static int Current
        {
            get
            {
                int v = PlayerPrefs.GetInt(SaveKey, Medium);
                return v < Low || v > High ? Medium : v;
            }
        }

        public static string CurrentName => Names[Current];

        /// Choisit un niveau : sauvegardé tout de suite, appliqué tout de suite (par les abonnés).
        public static void Set(int level)
        {
            level = Mathf.Clamp(level, Low, High);
            PlayerPrefs.SetInt(SaveKey, level);
            PlayerPrefs.Save();
            Changed?.Invoke();
        }

        /// Particules : en Faible, la MOITIÉ (arrondie au-dessus, au moins 1) ; sinon tout.
        public static int Particles(int count)
        {
            if (count <= 0) return 0;
            return Current == Low ? Mathf.Max(1, (count + 1) / 2) : count;
        }

        /// Ressorts des accessoires (antennes, plumes, pans) : figés en Faible.
        public static bool SpringsEnabled => Current != Low;

        /// Taille de l'image des vitrines (cheval vierge, vitrine de l'Inventaire), à partir de la
        /// taille « Moyen » (512 × 680) : 384 × 510 en Faible, 768 × 1 020 en Élevé.
        public static Vector2Int ShowcaseSize(int mediumWidth, int mediumHeight)
        {
            float k = Current == Low ? 0.75f : Current == High ? 1.5f : 1f;
            return new Vector2Int(Mathf.RoundToInt(mediumWidth * k), Mathf.RoundToInt(mediumHeight * k));
        }
    }
}
