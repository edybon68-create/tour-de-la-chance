using System;
using System.Collections.Generic;

namespace TourDeLaChance
{
    // Les petites « boîtes de données » échangées entre le classement (logique), ses services
    // (local, en ligne) et la pop-up (affichage). Spec v4 §12.5.

    /// Les identifiants des 3 classements : les MÊMES en local et en ligne (§12.2).
    public static class LeaderboardIds
    {
        public const string Etage = "etage_max";
        public const string Pieces = "pieces_encaissees";
        public const string Secrets = "secrets_obtenus";
        public static readonly string[] All = { Etage, Pieces, Secrets };
    }

    /// Résultat d'un envoi des scores.
    public enum SubmitResult
    {
        Sent,      // enregistré (local) ou envoyé (en ligne)
        Queued,    // gardé pour plus tard (hors ligne, trop tôt, pas encore de pseudo)
        Rejected   // valeur invraisemblable : rien n'est enregistré (§12.10)
    }

    public enum NameStatus { Ok, Invalid, Offline }

    /// Résultat d'un changement de pseudo : Ok, Invalid (+ un message lisible), Offline.
    public class NameResult
    {
        public NameStatus Status;
        public string Message = "";
        public static NameResult Ok() => new NameResult { Status = NameStatus.Ok };
        public static NameResult Invalid(string message) => new NameResult { Status = NameStatus.Invalid, Message = message };
        public static NameResult Offline() => new NameResult { Status = NameStatus.Offline, Message = "Hors ligne : réessaie plus tard." };
    }

    /// Une partie terminée, telle que le classement local la garde (étage, pièces, date).
    [Serializable]
    public class RunRecord
    {
        public int etage;
        public int pieces;        // 0 pour une partie perdue
        public string date;       // "yyyy-MM-dd"
    }

    /// Ce qu'on envoie au service : les 3 totaux d'un coup, plus (pour le local) la partie qui
    /// vient de finir ou le Secret qui vient d'être obtenu. Les boîtes ouvertes servent
    /// seulement au contrôle de vraisemblance (on ne les classe pas).
    public class PlayerScores
    {
        public int etage;
        public int pieces;
        public int secrets;
        public int boites;
        public RunRecord partie;       // null si l'envoi ne suit pas une fin de partie
        public string nouveauSecret;   // identifiant du Secret obtenu, sinon null
        public string dateSecret;      // "yyyy-MM-dd"
    }

    /// Une ligne de classement.
    [Serializable]
    public class LeaderboardEntry
    {
        public int rank;              // 1 = premier
        public string displayName;    // pseudo (ou nom de l'objet pour les Secrets en local)
        public string suffix = "";    // en ligne : « #1234 » ajouté par le service
        public long score = -1;       // -1 = pas de score à afficher (onglet Secrets local)
        public string detail = "";    // date, ou autre précision
        public bool isMe;             // ma ligne (en ligne) / la plus récente (local)
    }

    /// Une page de classement : les lignes, la date de la lecture, et si elle vient du cache.
    public class LeaderboardPage
    {
        public List<LeaderboardEntry> entries = new List<LeaderboardEntry>();
        public DateTime updatedAt = DateTime.Now;
        public bool fromCache;
    }
}
