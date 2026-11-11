using UnityEngine;

namespace TourDeLaChance
{
    /// <summary>
    /// Le petit « fil » qui relie la partie au classement (spec v4 §12.5) : c'est de la
    /// LOGIQUE, sans affichage. Au démarrage, il initialise les statistiques (une seule fois,
    /// depuis une sauvegarde v3) et le service de classement. Ensuite il écoute :
    ///   - la fin de chaque partie (GameManager.RunEnded) : il ajoute l'encaissement aux
    ///     statistiques, puis l'envoie au classement ;
    ///   - un Secret obtenu dans une boîte (PlayerStats.SecretObtained).
    /// Le GameManager ne connaît pas le classement : il prévient seulement.
    /// </summary>
    public class LeaderboardRunner : MonoBehaviour
    {
        [SerializeField] GameManager game;

        void Awake() => PlayerStats.EnsureInitialized();

        void OnEnable()
        {
            if (game != null) game.RunEnded += OnRunEnded;
            PlayerStats.SecretObtained += OnSecretObtained;
        }

        void OnDisable()
        {
            if (game != null) game.RunEnded -= OnRunEnded;
            PlayerStats.SecretObtained -= OnSecretObtained;
        }

        // "async void" : la tâche tourne en arrière-plan, le jeu ne l'attend jamais.
        async void Start() => await Leaderboard.InitializeAsync();

        async void OnRunEnded(RunResult run)
        {
            if (!run.Lost) PlayerStats.AddCashedOut(run.CashedOut);
            await Leaderboard.SubmitRunAsync(run);
        }

        async void OnSecretObtained(ShopItem item) => await Leaderboard.SubmitSecretAsync(item);
    }
}
