using System.Threading.Tasks;

namespace TourDeLaChance
{
    /// <summary>
    /// Ce que sait faire un « service de classement » (spec v4 §12.5). L'écran ne connaît QUE
    /// cette interface : le jour où le service en ligne est branché, l'écran ne change presque
    /// pas. Deux versions : LocalLeaderboardService (actif en v4) et UgsLeaderboardService
    /// (préparé, compilé seulement avec le symbole TDLC_UGS).
    ///
    /// "Task" = une tâche qui peut prendre du temps (Internet) ; on l'attend avec "await"
    /// sans bloquer le jeu. Le service local répond tout de suite.
    /// </summary>
    public interface ILeaderboardService
    {
        bool IsOnline { get; }                    // vrai seulement pour le service en ligne connecté
        string ModeText { get; }                  // « Classement local » / « En ligne » / « Hors ligne »
        Task<bool> InitializeAsync();             // connexion (en ligne) ou lecture (local)
        Task<SubmitResult> SubmitAsync(PlayerScores scores);           // les 3 totaux d'un coup
        Task<LeaderboardPage> GetTopAsync(string boardId, int count);  // les meilleures lignes
        Task<LeaderboardEntry> GetMyEntryAsync(string boardId);        // ma ligne, null si aucune
        Task<NameResult> SetPlayerNameAsync(string name);              // pseudo (déjà filtré)
        Task DeleteMyDataAsync();                 // « Quitter le classement »
    }
}
