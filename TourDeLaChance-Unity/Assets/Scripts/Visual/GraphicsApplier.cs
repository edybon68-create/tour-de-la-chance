using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace TourDeLaChance
{
    /// <summary>
    /// Applique le NIVEAU GRAPHIQUE (spec v6 §12) au rendu, tout de suite, sans redémarrer.
    /// Un seul exemplaire dans la scène. Il écoute GraphicsLevel.Changed.
    ///
    /// Ce qu'il change :
    ///   1. le NIVEAU DE QUALITÉ du projet (Project Settings › Quality : Faible, Moyen, Élevé),
    ///      chacun branché sur sa copie de l'asset URP (PC_Faible, PC_Moyen, PC_Eleve) qui règle
    ///      l'échelle de rendu (0,75 + FSR en Faible), le MSAA et les ombres ;
    ///   2. le BLOOM du volume de la scène : coupé en Faible, haute qualité en Élevé. On travaille
    ///      sur une COPIE du profil (volume.profile en crée une) : le fichier .asset ne change pas ;
    ///   3. les IMAGES PAR SECONDE : 30 en Faible, synchro écran sinon.
    /// L'interface (UI Toolkit) n'est pas touchée : elle reste nette à toutes les échelles.
    /// </summary>
    public class GraphicsApplier : MonoBehaviour
    {
        [Tooltip("Le volume global de la scène (Bloom, Tonemapping, Vignette). Vide = cherché tout seul.")]
        [SerializeField] Volume volume;

        Bloom bloom;
        bool bloomWasActive = true;

        /// Le niveau de qualité URP effectivement actif (pour les tests).
        public string ActiveQualityName => QualitySettings.names[QualitySettings.GetQualityLevel()];

        void OnEnable()
        {
            if (volume == null) volume = FindFirstObjectByType<Volume>();
            if (volume != null && volume.profile != null && volume.profile.TryGet(out bloom))
                bloomWasActive = bloom.active;
            GraphicsLevel.Changed += Apply;
            Apply();
        }

        void OnDisable() => GraphicsLevel.Changed -= Apply;

        void Apply()
        {
            int level = GraphicsLevel.Current;

            // 1. Le niveau de qualité qui porte le même nom (on le cherche par son nom : l'ordre
            //    de la liste peut changer sans casser le jeu). true = appliquer aussi les réglages
            //    « coûteux » (anticrénelage…) tout de suite.
            int index = FindQualityLevel(GraphicsLevel.Names[level]);
            if (index >= 0 && index != QualitySettings.GetQualityLevel()) QualitySettings.SetQualityLevel(index, true);

            // 2. Le Bloom (sur la copie du profil, jamais sur le fichier).
            if (bloom != null)
            {
                bloom.active = bloomWasActive && level != GraphicsLevel.Low;
                bloom.highQualityFiltering.overrideState = true;
                bloom.highQualityFiltering.value = level == GraphicsLevel.High;
            }

            // 3. Images par seconde.
            if (level == GraphicsLevel.Low)
            {
                QualitySettings.vSyncCount = 0;
                Application.targetFrameRate = 30;
            }
            else
            {
                QualitySettings.vSyncCount = 1;           // synchro écran (60, 120… selon l'écran)
                Application.targetFrameRate = -1;         // pas de limite en plus
            }
        }

        static int FindQualityLevel(string name)
        {
            var names = QualitySettings.names;
            for (int i = 0; i < names.Length; i++) if (names[i] == name) return i;
            return -1;
        }
    }
}
