#if TDLC_UGS
// Ce fichier n'est compilé QUE si le symbole TDLC_UGS est défini (Project Settings › Player ›
// Scripting Define Symbols), c'est-à-dire quand les paquets Unity Gaming Services sont
// installés : com.unity.services.authentication et com.unity.services.leaderboards.
// Sans ce symbole, Unity ignore tout ce qui est entre #if et #endif : le projet compile
// sans ces paquets (spec v4 §12.5, critère 34).
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Leaderboards;
using UnityEngine;

namespace TourDeLaChance
{
    /// <summary>
    /// Le classement EN LIGNE (préparé, désactivé en v4 ; spec v4 §12.9). Connexion anonyme
    /// (aucune adresse e-mail, aucun mot de passe), un score par classement, lecture du top 50
    /// et de ma ligne. Ce qui est envoyé : le pseudo filtré, les 3 totaux et l'identifiant
    /// anonyme créé par le service. Rien d'autre.
    /// Étape 2 recommandée (§12.10) : faire passer SubmitAsync par le script Cloud Code
    /// « soumettre_scores » au lieu d'écrire directement les scores.
    /// </summary>
    public class UgsLeaderboardService : ILeaderboardService
    {
        bool connected;

        public bool IsOnline => connected;
        public string ModeText => connected ? "En ligne" : "Hors ligne";

        public async Task<bool> InitializeAsync()
        {
            try
            {
                if (UnityServices.State != ServicesInitializationState.Initialized)
                    await UnityServices.InitializeAsync();
                if (!AuthenticationService.Instance.IsSignedIn)
                    await AuthenticationService.Instance.SignInAnonymouslyAsync();
                connected = true;
            }
            catch (Exception e)
            {
                Debug.LogWarning("Classement en ligne : connexion impossible (" + e.Message + ").");
                connected = false;
            }
            return connected;
        }

        public async Task<SubmitResult> SubmitAsync(PlayerScores scores)
        {
            if (!connected) return SubmitResult.Queued;
            var lb = LeaderboardsService.Instance;
            await lb.AddPlayerScoreAsync(LeaderboardIds.Etage, scores.etage);
            await lb.AddPlayerScoreAsync(LeaderboardIds.Pieces, scores.pieces);
            await lb.AddPlayerScoreAsync(LeaderboardIds.Secrets, scores.secrets);
            return SubmitResult.Sent;
        }

        public async Task<LeaderboardPage> GetTopAsync(string boardId, int count)
        {
            var response = await LeaderboardsService.Instance.GetScoresAsync(boardId, new GetScoresOptions { Limit = count });
            string myId = AuthenticationService.Instance.PlayerId;
            var page = new LeaderboardPage { updatedAt = DateTime.Now, fromCache = false };
            foreach (var e in response.Results) page.entries.Add(ToEntry(e, myId));
            return page;
        }

        public async Task<LeaderboardEntry> GetMyEntryAsync(string boardId)
        {
            try
            {
                var e = await LeaderboardsService.Instance.GetPlayerScoreAsync(boardId);
                return ToEntry(e, AuthenticationService.Instance.PlayerId);
            }
            catch (Exception) { return null; } // pas encore de score dans ce classement
        }

        public async Task<NameResult> SetPlayerNameAsync(string name)
        {
            if (!connected) return NameResult.Offline();
            await AuthenticationService.Instance.UpdatePlayerNameAsync(name); // le service ajoute « #1234 »
            return NameResult.Ok();
        }

        public async Task DeleteMyDataAsync()
        {
            if (!connected) return;
            await AuthenticationService.Instance.DeleteAccountAsync(); // supprime le compte anonyme
            connected = false;
        }

        /// Une ligne du service → notre LeaderboardEntry (rang 0 côté service → rang affiché 1).
        static LeaderboardEntry ToEntry(Unity.Services.Leaderboards.Models.LeaderboardEntry e, string myId)
        {
            string name = e.PlayerName ?? "";
            string suffix = "";
            int hash = name.LastIndexOf('#');
            if (hash > 0) { suffix = name.Substring(hash); name = name.Substring(0, hash); }
            return new LeaderboardEntry
            {
                rank = e.Rank + 1,
                displayName = name,
                suffix = suffix,
                score = (long)Math.Round(e.Score),
                isMe = e.PlayerId == myId
            };
        }
    }
}
#endif
