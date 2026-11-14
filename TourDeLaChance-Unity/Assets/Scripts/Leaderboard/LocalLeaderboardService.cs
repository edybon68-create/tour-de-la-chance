using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace TourDeLaChance
{
    /// <summary>
    /// Le classement LOCAL (spec v4 §12.4) : « tes meilleures parties sur cet ordinateur ».
    /// Il n'y a qu'un joueur par ordinateur : on n'invente AUCUN faux joueur. Chaque onglet
    /// montre ce qui est vrai : tes 10 meilleures parties (Étage), tes 10 meilleurs
    /// encaissements (Pièces), et chaque Secret obtenu (Secrets).
    ///
    /// Sauvegarde (PlayerPrefs), dans l'ORDRE où les lignes ont été ajoutées (la dernière est
    /// la plus récente, surlignée à l'écran) :
    ///   classement_local_etage   = "6|2026-11-11;4|2026-11-11"
    ///   classement_local_pieces  = "120|2026-11-11"
    ///   classement_local_secrets = "matiere_galaxie|2026-11-11"
    /// v5 §19 : un 3e champ facultatif « |r » marque une partie qui a utilisé une reprise
    /// ("6|2026-11-11|r") ; les anciennes lignes restent lisibles.
    /// Il répond tout de suite (Task.FromResult) : pas d'Internet.
    /// </summary>
    public class LocalLeaderboardService : ILeaderboardService
    {
        public const string EtageKey = "classement_local_etage";
        public const string PiecesKey = "classement_local_pieces";
        public const string SecretsKey = "classement_local_secrets";
        public const int MaxRuns = 10;
        /// v5 : le marqueur ajouté après la date d'une partie qui a utilisé une reprise.
        public const string ReviveMark = "|r";

        static readonly CultureInfo French = CultureInfo.GetCultureInfo("fr-FR");

        public bool IsOnline => false;
        public string ModeText => "Classement local";

        public Task<bool> InitializeAsync() => Task.FromResult(true);

        /// Enregistre tout de suite la partie (et/ou le Secret). La vraisemblance a déjà été
        /// vérifiée par Leaderboard avant d'arriver ici.
        public Task<SubmitResult> SubmitAsync(PlayerScores scores)
        {
            if (scores.partie != null)
            {
                // Une partie perdue entre au classement Étage (on a bien atteint cet étage)
                // mais pas au classement Pièces (rien d'encaissé).
                // v5 : la date garde le marqueur « |r » d'une partie avec reprise.
                // v6 §9.4 : « |r3 » = 3 reprises (« |r » seul, ancien format = 1 reprise).
                int n = Math.Max(scores.partie.reprises, scores.partie.reprise ? 1 : 0);
                string date = scores.partie.date + (n > 0 ? ReviveMark + n.ToString(CultureInfo.InvariantCulture) : "");
                if (scores.partie.etage >= 1) AddRun(EtageKey, scores.partie.etage, date);
                if (scores.partie.pieces > 0) AddRun(PiecesKey, scores.partie.pieces, date);
            }
            if (!string.IsNullOrEmpty(scores.nouveauSecret))
            {
                var list = Read(SecretsKey);
                list.Add((scores.nouveauSecret, scores.dateSecret ?? Today()));
                Write(SecretsKey, list);
            }
            PlayerPrefs.Save();
            return Task.FromResult(SubmitResult.Sent);
        }

        /// QA v7 p2 n°3 : rattrape le record d'étage quand il n'est pas dans la liste locale
        /// (record atteint avant que le classement existe, ou partie jamais enregistrée) : la
        /// meilleure ligne correspond alors toujours à « Ton record ». Renvoie vrai si ajouté.
        public static bool CatchUpRecord(int record)
        {
            if (record < 1) return false;
            var list = Read(EtageKey);
            foreach (var e in list) if (ScoreOf(e) >= record) return false;
            AddRun(EtageKey, record, Today());
            PlayerPrefs.Save();
            return true;
        }

        /// Ajoute une partie et ne garde que les 10 meilleures (en cas d'égalité, on retire la
        /// plus ancienne). L'ordre d'ajout est conservé : la dernière ligne est la plus récente.
        static void AddRun(string key, int score, string date)
        {
            var list = Read(key);
            list.Add((score.ToString(CultureInfo.InvariantCulture), date));
            while (list.Count > MaxRuns)
            {
                int worst = 0;
                for (int i = 1; i < list.Count; i++)
                    if (ScoreOf(list[i]) < ScoreOf(list[worst])) worst = i; // « < » : la plus ancienne gagne l'égalité
                list.RemoveAt(worst);
            }
            Write(key, list);
        }

        public Task<LeaderboardPage> GetTopAsync(string boardId, int count)
        {
            var page = new LeaderboardPage { updatedAt = DateTime.Now, fromCache = false };
            string name = Leaderboard.PlayerName;
            if (string.IsNullOrEmpty(name)) name = "Toi";

            if (boardId == LeaderboardIds.Secrets)
            {
                // Rang = ordre d'obtention ; le « nom » est celui de l'objet.
                var list = Read(SecretsKey);
                for (int i = 0; i < list.Count && i < count; i++)
                {
                    var item = Shop.Find(list[i].value);
                    page.entries.Add(new LeaderboardEntry
                    {
                        rank = i + 1,
                        displayName = item != null ? item.DisplayName : list[i].value,
                        score = -1,
                        detail = PrettyDate(list[i].date),
                        isMe = i == list.Count - 1
                    });
                }
                return Task.FromResult(page);
            }

            var runs = Read(boardId == LeaderboardIds.Pieces ? PiecesKey : EtageKey);
            int latest = runs.Count - 1;
            // Tri du meilleur au moins bon ; à égalité, la plus ancienne d'abord.
            var order = Enumerable.Range(0, runs.Count)
                .OrderByDescending(i => ScoreOf(runs[i])).ThenBy(i => i).Take(count).ToList();
            for (int r = 0; r < order.Count; r++)
            {
                int i = order[r];
                page.entries.Add(new LeaderboardEntry
                {
                    rank = r + 1,
                    displayName = name,
                    score = ScoreOf(runs[i]),
                    detail = PrettyDate(runs[i].date),
                    isMe = i == latest
                });
            }
            return Task.FromResult(page);
        }

        public async Task<LeaderboardEntry> GetMyEntryAsync(string boardId)
        {
            var page = await GetTopAsync(boardId, 1);
            return page.entries.Count > 0 ? page.entries[0] : null;
        }

        /// En local, le pseudo est simplement gardé par Leaderboard : rien à envoyer.
        public Task<NameResult> SetPlayerNameAsync(string name) => Task.FromResult(NameResult.Ok());

        /// « Quitter le classement » en local : on efface les listes de parties.
        /// (Le pseudo est effacé par Leaderboard ; les statistiques de jeu sont gardées.)
        public Task DeleteMyDataAsync()
        {
            PlayerPrefs.DeleteKey(EtageKey);
            PlayerPrefs.DeleteKey(PiecesKey);
            PlayerPrefs.DeleteKey(SecretsKey);
            PlayerPrefs.Save();
            return Task.CompletedTask;
        }

        // ---------- Lecture / écriture du texte sauvegardé ----------

        static List<(string value, string date)> Read(string key)
        {
            var list = new List<(string, string)>();
            foreach (var part in PlayerPrefs.GetString(key, "").Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                int bar = part.IndexOf('|');
                if (bar <= 0) continue; // morceau illisible : ignoré
                list.Add((part.Substring(0, bar).Trim(), part.Substring(bar + 1).Trim()));
            }
            return list;
        }

        static void Write(string key, List<(string value, string date)> list) =>
            PlayerPrefs.SetString(key, string.Join(";", list.Select(e => e.value + "|" + e.date)));

        static long ScoreOf((string value, string date) e) =>
            long.TryParse(e.value, NumberStyles.Integer, CultureInfo.InvariantCulture, out long n) ? n : 0;

        public static string Today() => DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        /// « 2026-11-11 » → « 11 nov. » (à la française).
        public static string PrettyDate(string date)
        {
            // v5 : « 2026-11-11|r » → « 11 nov. · ◆ » ; v6 : « 2026-11-11|r3 » → « 11 nov. · ◆×3 ».
            int mark = date != null ? date.IndexOf(ReviveMark, StringComparison.Ordinal) : -1;
            if (mark > 0)
            {
                string tail = date.Substring(mark + ReviveMark.Length);
                int n = int.TryParse(tail, NumberStyles.Integer, CultureInfo.InvariantCulture, out int v) ? Math.Max(1, v) : 1;
                return PrettyDate(date.Substring(0, mark)) + " · " + ShopUi.Diamant + (n > 1 ? "×" + n : "");
            }
            if (DateTime.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
                return d.ToString("d MMM", French);
            return date ?? "";
        }
    }
}
