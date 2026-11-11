using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;

namespace TourDeLaChance
{
    /// <summary>
    /// La pop-up « Classement » (spec v4 §12.6, « LeaderboardPopup » : le nom LeaderboardPage
    /// désigne déjà les données d'une page de classement). Simple objet piloté par PanelsController :
    /// elle AFFICHE ce que lui donne la logique (Leaderboard, PlayerStats) et TRANSMET les clics.
    /// Elle ne sait pas si le classement est local ou en ligne : elle lit seulement des
    /// LeaderboardPage (lignes + date + « vient du cache »).
    ///
    /// 3 onglets (Étage · Pièces · Secrets ; le dernier choisi est retenu pendant la session),
    /// un en-tête avec tes totaux, la liste (qui défile), puis Actualiser, Changer de pseudo et
    /// « Quitter le classement » (avec confirmation). Pendant le chargement : 5 lignes
    /// « fantômes » grises qui pulsent, jamais de faux noms.
    /// </summary>
    public class LeaderboardPopup : IDisposable
    {
        const float RefreshCooldown = 10f;

        readonly Button tabEtage, tabPieces, tabSecrets, refreshButton, renameButton, leaveButton, yesButton, noButton;
        readonly Label info, header, banner, mode;
        readonly ScrollView list;
        readonly VisualElement confirm;

        /// « Changer de pseudo » : PanelsController montre la page du pseudo.
        public event Action RenameRequested;

        static string currentBoard = LeaderboardIds.Etage; // retenu pendant la session
        float refreshAllowedAt;
        int loadToken;            // pour ignorer une réponse arrivée après un changement d'onglet
        IVisualElementScheduledItem ghostPulse;

        static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

        public LeaderboardPopup(VisualElement page)
        {
            tabEtage = page.Q<Button>("lb-tab-etage");
            tabPieces = page.Q<Button>("lb-tab-pieces");
            tabSecrets = page.Q<Button>("lb-tab-secrets");
            info = page.Q<Label>("lb-info");
            header = page.Q<Label>("lb-header");
            banner = page.Q<Label>("lb-banner");
            list = page.Q<ScrollView>("lb-list");
            mode = page.Q<Label>("lb-mode");
            refreshButton = page.Q<Button>("lb-refresh");
            renameButton = page.Q<Button>("lb-rename");
            leaveButton = page.Q<Button>("lb-leave");
            confirm = page.Q<VisualElement>("lb-confirm");
            yesButton = page.Q<Button>("lb-confirm-yes");
            noButton = page.Q<Button>("lb-confirm-no");

            tabEtage.clicked += OnTabEtage;
            tabPieces.clicked += OnTabPieces;
            tabSecrets.clicked += OnTabSecrets;
            refreshButton.clicked += OnRefresh;
            renameButton.clicked += OnRename;
            leaveButton.clicked += OnLeave;
            yesButton.clicked += OnLeaveConfirmed;
            noButton.clicked += OnLeaveCancelled;
        }

        public void Dispose()
        {
            tabEtage.clicked -= OnTabEtage;
            tabPieces.clicked -= OnTabPieces;
            tabSecrets.clicked -= OnTabSecrets;
            refreshButton.clicked -= OnRefresh;
            renameButton.clicked -= OnRename;
            leaveButton.clicked -= OnLeave;
            yesButton.clicked -= OnLeaveConfirmed;
            noButton.clicked -= OnLeaveCancelled;
            ghostPulse?.Pause();
        }

        void OnTabEtage() => ShowBoard(LeaderboardIds.Etage);
        void OnTabPieces() => ShowBoard(LeaderboardIds.Pieces);
        void OnTabSecrets() => ShowBoard(LeaderboardIds.Secrets);
        void OnRename() => RenameRequested?.Invoke();

        public async void Open()
        {
            confirm.style.display = DisplayStyle.None;
            await Leaderboard.TrySendPendingAsync(); // en ligne : ce qui attendait part maintenant
            Refresh();
        }

        void ShowBoard(string boardId)
        {
            currentBoard = boardId;
            Refresh();
        }

        void OnRefresh()
        {
            if (Time.unscaledTime < refreshAllowedAt) return;
            refreshAllowedAt = Time.unscaledTime + RefreshCooldown;
            Refresh();
            // « Actualiser » reste inactif 10 s (sert surtout en ligne : les services limitent les appels).
            refreshButton.SetEnabled(false);
            refreshButton.schedule.Execute(() => refreshButton.SetEnabled(true)).StartingIn((long)(RefreshCooldown * 1000));
        }

        /// Redessine l'onglet courant : en-tête tout de suite, lignes quand elles arrivent.
        public async void Refresh()
        {
            ShopUi.SetClass(tabEtage, "subtab--active", currentBoard == LeaderboardIds.Etage);
            ShopUi.SetClass(tabPieces, "subtab--active", currentBoard == LeaderboardIds.Pieces);
            ShopUi.SetClass(tabSecrets, "subtab--active", currentBoard == LeaderboardIds.Secrets);

            var scores = Leaderboard.CurrentScores();
            header.text = currentBoard == LeaderboardIds.Etage ? (scores.etage > 0 ? $"Ton record : étage {scores.etage}" : "Ton record : pas encore d'étage")
                        : currentBoard == LeaderboardIds.Pieces ? $"Total encaissé : {Thousands(scores.pieces)}"
                        : $"Secrets obtenus : {scores.secrets}";
            bool online = Leaderboard.Service.IsOnline;
            info.text = Leaderboard.Service is LocalLeaderboardService
                ? "Classement local : tes meilleures parties sur cet ordinateur. Le classement entre tous les joueurs arrivera avec la version en ligne."
                : online ? "Classement en ligne : tous les joueurs." : "Hors ligne : ton score sera envoyé plus tard.";
            banner.style.display = DisplayStyle.None;

            int token = ++loadToken;
            ShowGhosts();
            int count = Leaderboard.Service is LocalLeaderboardService ? LocalLeaderboardService.MaxRuns : 50;
            LeaderboardPage page;
            try { page = await Leaderboard.GetPageAsync(currentBoard, count); }
            catch (Exception e) { Debug.LogWarning("Classement : lecture impossible (" + e.Message + ")."); page = null; }
            if (token != loadToken) return; // l'onglet a changé entre-temps
            StopGhosts();
            Fill(page, scores);
        }

        void Fill(LeaderboardPage page, PlayerScores scores)
        {
            list.Clear();
            bool local = Leaderboard.Service is LocalLeaderboardService;
            mode.text = local ? Leaderboard.Service.ModeText
                      : page != null && page.fromCache ? "Liste du " + page.updatedAt.ToString("d MMM, HH 'h' mm", CultureInfo.GetCultureInfo("fr-FR"))
                      : "Mis à jour il y a " + Math.Max(0, (int)(DateTime.Now - (page?.updatedAt ?? DateTime.Now)).TotalMinutes) + " min";

            if (page == null || page.entries.Count == 0)
            {
                string empty = currentBoard == LeaderboardIds.Secrets
                    ? "Aucun Secret obtenu dans une boîte pour l'instant."
                    : currentBoard == LeaderboardIds.Pieces ? "Aucun encaissement pour l'instant : encaisse une partie pour entrer ici !"
                    : "Aucune partie pour l'instant : joue une partie pour entrer au classement !";
                list.Add(ShopUi.NewLabel(empty, "lb-empty"));
            }
            else
            {
                foreach (var e in page.entries) list.Add(Row(e, local));
            }
            // Les Secrets trouvés avant la v4 sont comptés dans le total, sans date connue.
            if (local && currentBoard == LeaderboardIds.Secrets && page != null && scores.secrets > page.entries.Count)
                list.Add(ShopUi.NewLabel($"{scores.secrets - page.entries.Count} Secret(s) obtenu(s) avant la version 4 : comptés dans le total, sans date.", "lb-empty"));

            // En ligne : « Tu as gagné 12 places ! » si le rang s'est amélioré depuis la dernière fois.
            if (!local && page != null && !page.fromCache)
            {
                var me = page.entries.Find(x => x.isMe);
                int gained = me != null ? Leaderboard.PlacesGained(currentBoard, me.rank) : 0;
                if (gained > 0)
                {
                    banner.text = $"Tu as gagné {gained} place{(gained > 1 ? "s" : "")} !";
                    banner.style.display = DisplayStyle.Flex;
                    if (SoundManager.Instance != null) SoundManager.Instance.Play(Sfx.Record);
                }
            }
            list.scrollOffset = Vector2.zero;
        }

        VisualElement Row(LeaderboardEntry e, bool local)
        {
            var row = new VisualElement { pickingMode = PickingMode.Ignore };
            row.AddToClassList("lb-row");
            if (e.isMe) row.AddToClassList("lb-row--me");

            // Rang : 1, 2, 3 dans une médaille ronde ; ensuite le chiffre seul.
            var rank = ShopUi.NewLabel(e.rank.ToString(Invariant), "lb-rank");
            if (e.rank >= 1 && e.rank <= 3)
            {
                rank.AddToClassList("lb-medal");
                rank.AddToClassList("lb-medal--" + e.rank);
            }
            row.Add(rank);

            var names = new VisualElement { pickingMode = PickingMode.Ignore };
            names.AddToClassList("lb-names");
            string name = e.displayName + (string.IsNullOrEmpty(e.suffix) ? "" : " " + e.suffix);
            if (!local && e.isMe) name += " (toi)";
            names.Add(ShopUi.NewLabel(name, "lb-name"));
            if (!string.IsNullOrEmpty(e.detail)) names.Add(ShopUi.NewLabel(e.detail, "lb-detail"));
            row.Add(names);

            if (e.score >= 0)
            {
                string score = currentBoard == LeaderboardIds.Etage ? "étage " + e.score : Thousands(e.score);
                row.Add(ShopUi.NewLabel(score, "lb-score"));
            }
            return row;
        }

        /// 5 lignes « fantômes » grises qui pulsent (opacité 0,3 ↔ 0,6) pendant le chargement.
        void ShowGhosts()
        {
            list.Clear();
            for (int i = 0; i < 5; i++)
            {
                var ghost = new VisualElement { pickingMode = PickingMode.Ignore };
                ghost.AddToClassList("lb-row");
                ghost.AddToClassList("lb-row--ghost");
                list.Add(ghost);
            }
            float start = Time.unscaledTime;
            ghostPulse?.Pause();
            ghostPulse = list.schedule.Execute(() =>
            {
                float o = 0.45f + 0.15f * Mathf.Sin((Time.unscaledTime - start) * Mathf.PI * 2f);
                foreach (var child in list.Children()) child.style.opacity = o;
            }).Every(50);
        }

        void StopGhosts() => ghostPulse?.Pause();

        void OnLeave() => confirm.style.display = DisplayStyle.Flex;
        void OnLeaveCancelled() => confirm.style.display = DisplayStyle.None;

        async void OnLeaveConfirmed()
        {
            confirm.style.display = DisplayStyle.None;
            await Leaderboard.LeaveAsync();
            Refresh();
        }

        /// 48310 → « 48 310 » (espace simple entre les milliers, lisible par toutes les polices).
        static string Thousands(long n) => n.ToString("#,0", Invariant).Replace(",", " ");

        // ---------- Outils de test ----------
        public void DebugShowBoard(string boardId) => ShowBoard(boardId);
        public string DebugHeader => header.text;
        public string DebugInfo => info.text;
        public string DebugRows
        {
            get
            {
                var sb = new System.Text.StringBuilder();
                foreach (var row in list.Children())
                {
                    foreach (var l in row.Query<Label>().ToList()) sb.Append(l.text).Append(" | ");
                    if (row.ClassListContains("lb-row--me")) sb.Append("[surlignée]");
                    sb.Append("\n");
                }
                return sb.ToString();
            }
        }
        public void DebugLeave() => OnLeaveConfirmed();
    }
}
