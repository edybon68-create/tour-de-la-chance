using UnityEngine;

namespace TourDeLaChance
{
    /// Local (v4) ou en ligne (plus tard, spec v4 §12.11).
    public enum LeaderboardMode { Local, EnLigne }

    /// <summary>
    /// Le réglage qui choisit le service de classement (spec v4 §12.5). Rangé dans
    /// Assets/Resources/LeaderboardConfig.asset pour être chargé par son nom.
    /// « En ligne » n'est utilisé que si le symbole TDLC_UGS est défini (paquets Unity Gaming
    /// Services installés) ; sinon le jeu reste en local, sans erreur.
    /// </summary>
    [CreateAssetMenu(fileName = "LeaderboardConfig", menuName = "Tour de la Chance/Réglage du classement")]
    public class LeaderboardConfig : ScriptableObject
    {
        [Tooltip("Local : les meilleures parties de cet ordinateur. En ligne : tous les joueurs (TDLC_UGS requis).")]
        public LeaderboardMode mode = LeaderboardMode.Local;
    }
}
