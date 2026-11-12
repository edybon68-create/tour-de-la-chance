using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TourDeLaChance
{
    /// <summary>
    /// Le CLASSEMENT, partie logique (spec v4 §12.5 à §12.10). Classe "static" : une seule,
    /// accessible de partout. Elle :
    ///   - choisit le service (local en v4 ; en ligne seulement si TDLC_UGS est défini ET si
    ///     LeaderboardConfig.mode vaut EnLigne) ;
    ///   - décide QUAND envoyer (fin de partie, Secret obtenu ; en ligne : au plus un envoi
    ///     toutes les 30 s, sinon « en attente ») ;
    ///   - vérifie la VRAISEMBLANCE des scores avant tout envoi (anti-triche minimale, §12.10) ;
    ///   - filtre le PSEUDO (§12.7) et garde le CACHE de la dernière liste reçue (en ligne).
    /// La pop-up (LeaderboardPage) ne fait qu'afficher ce que cette classe lui donne.
    /// Un envoi ne bloque jamais le jeu ; une erreur écrit un avertissement, rien d'autre.
    /// </summary>
    public static class Leaderboard
    {
        // ---------- Clés de sauvegarde (§15) ----------
        public const string NameKey = "classement_pseudo";
        public const string PendingKey = "classement_en_attente";
        public const string LastSentKey = "classement_dernier_envoi";
        public const string CacheDateKey = "classement_cache_date";
        static string CacheKey(string boardId) => "classement_cache_" + ShortId(boardId);
        static string LastRankKey(string boardId) => "classement_dernier_rang_" + boardId;

        /// Toutes les clés « classement_* » (pour remettre à zéro après des tests).
        public static IEnumerable<string> AllSaveKeys
        {
            get
            {
                yield return NameKey; yield return PendingKey; yield return LastSentKey; yield return CacheDateKey;
                yield return LocalLeaderboardService.EtageKey;
                yield return LocalLeaderboardService.PiecesKey;
                yield return LocalLeaderboardService.SecretsKey;
                foreach (var id in LeaderboardIds.All) { yield return CacheKey(id); yield return LastRankKey(id); }
            }
        }

        // ---------- Règles chiffrées ----------
        public const int MaxFloor = 300;               // §12.10 : au-delà, c'est invraisemblable
        public const int MaxCoinsJump = 500000;        // v5 §18.6 : le ×2 de la renaissance double la plus grosse partie imaginable
        public const float MinSecondsBetweenSends = 30f; // en ligne seulement
        public const int NameMin = 3, NameMax = 16;
        public const string NameRefusedMessage = "Ce pseudo n'est pas autorisé, choisis-en un autre.";

        /// Prévient l'affichage qu'un score ou le pseudo a changé.
        public static event Action Changed;

        static ILeaderboardService service;
        static bool initialized;
        static float lastSendTime = -1000f;

        /// Le service choisi au démarrage (§12.5).
        public static ILeaderboardService Service
        {
            get
            {
                if (service == null)
                {
                    var config = Resources.Load<LeaderboardConfig>("LeaderboardConfig");
                    bool wantsOnline = config != null && config.mode == LeaderboardMode.EnLigne;
#if TDLC_UGS
                    if (wantsOnline) service = new UgsLeaderboardService();
#else
                    if (wantsOnline) Debug.LogWarning("Classement : mode « En ligne » demandé, mais le symbole TDLC_UGS n'est pas défini. Le classement reste local.");
#endif
                    if (service == null) service = new LocalLeaderboardService();
                }
                return service;
            }
        }

        /// Vrai si l'on a demandé le service EN LIGNE (connecté ou non).
        static bool OnlineMode => !(Service is LocalLeaderboardService);

        /// Connexion (en ligne) ou lecture (local), puis envoi de ce qui attendait.
        public static async Task InitializeAsync()
        {
            if (initialized) return;
            initialized = true;
            try
            {
                await Service.InitializeAsync();
                await TrySendPendingAsync();
            }
            catch (Exception e) { Debug.LogWarning("Classement : initialisation impossible (" + e.Message + ")."); }
        }

        // =====================================================================
        // Pseudo (§12.7)
        // =====================================================================

        public static string PlayerName => PlayerPrefs.GetString(NameKey, "");
        public static bool HasName => !string.IsNullOrEmpty(PlayerName);

        static readonly Dictionary<char, char> Decode = new Dictionary<char, char>
        {
            { '0', 'o' }, { '1', 'i' }, { '3', 'e' }, { '4', 'a' }, { '5', 's' }, { '7', 't' }, { '@', 'a' }, { '$', 's' }
        };

        static List<string> bannedWords;

        /// La liste des mots interdits (Assets/Resources/pseudos_interdits.txt, un mot par ligne,
        /// modifiable par l'auteur). Les lignes vides et celles qui commencent par # sont ignorées.
        static List<string> BannedWords
        {
            get
            {
                if (bannedWords == null)
                {
                    bannedWords = new List<string>();
                    var file = Resources.Load<TextAsset>("pseudos_interdits");
                    if (file == null) Debug.LogWarning("Classement : Assets/Resources/pseudos_interdits.txt introuvable (aucun mot filtré).");
                    else
                        foreach (var line in file.text.Split('\n'))
                        {
                            string w = Normalize(line.Trim());
                            if (w.Length >= 3 && !line.TrimStart().StartsWith("#")) bannedWords.Add(w);
                        }
                }
                return bannedWords;
            }
        }

        /// Un caractère permis dans un pseudo : lettre sans accent, chiffre, « _ » ou « - ».
        public static bool IsAllowedChar(char c) =>
            (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '_' || c == '-';

        /// Ce que le champ de texte garde de la frappe : les caractères permis, 16 au plus.
        /// (Le champ « refuse simplement » le reste : espaces, accents, ponctuation…)
        public static string CleanNameInput(string typed)
        {
            if (string.IsNullOrEmpty(typed)) return "";
            var sb = new StringBuilder();
            foreach (char c in typed)
                if (IsAllowedChar(c) && sb.Length < NameMax) sb.Append(c);
            return sb.ToString();
        }

        /// Minuscules + « décodage » des chiffres déguisés (0→o, 1→i, 3→e…).
        static string Normalize(string s)
        {
            var sb = new StringBuilder();
            foreach (char raw in s.ToLowerInvariant())
                sb.Append(Decode.TryGetValue(raw, out char d) ? d : raw);
            return sb.ToString();
        }

        /// Les règles du pseudo. Renvoie true si on peut le valider ; sinon « message » dit
        /// pourquoi (vide quand c'est seulement trop court : le bouton reste juste grisé).
        public static bool CheckName(string name, out string message)
        {
            message = "";
            if (string.IsNullOrEmpty(name) || name.Length < NameMin) { message = $"De {NameMin} à {NameMax} caractères."; return false; }
            if (name.Length > NameMax) { message = $"{NameMax} caractères au plus."; return false; }
            if (!name.All(IsAllowedChar)) { message = "Lettres sans accent, chiffres, « _ » et « - » seulement (pas d'espace)."; return false; }
            if (name.All(char.IsDigit)) { message = "Pas uniquement des chiffres."; return false; }
            string plain = Normalize(name);
            if (plain.Contains("www") || plain.Contains("http")) { message = NameRefusedMessage; return false; }
            foreach (var word in BannedWords)
                if (plain.Contains(word)) { message = NameRefusedMessage; return false; }
            return true;
        }

        static readonly string[] NameStarts = { "Cavalier", "Etoile", "Poney", "Licorne", "Comete", "Sauteur", "Champion", "Galop", "Tresor", "Fusee", "Criniere", "Lutin" };
        static readonly string[] NameEnds = { "Agile", "Filante", "Rigolo", "Magique", "Dore", "Rapide", "Malin", "Brillant", "Joyeux", "Fute", "Lunaire", "Royal" };
        static readonly System.Random nameRandom = new System.Random();

        /// Un pseudo amusant tiré au hasard (« CavalierAgile42 »), toujours valide.
        public static string RandomName()
        {
            for (int tries = 0; tries < 20; tries++)
            {
                string n = CleanNameInput(NameStarts[nameRandom.Next(NameStarts.Length)] +
                                          NameEnds[nameRandom.Next(NameEnds.Length)] + nameRandom.Next(1, 100));
                if (CheckName(n, out _)) return n;
            }
            return "Cavalier" + nameRandom.Next(10, 100);
        }

        /// Valide et enregistre le pseudo (après le filtre). Tant qu'aucun pseudo n'est choisi,
        /// rien n'est envoyé en ligne (§12.7).
        public static async Task<NameResult> SetNameAsync(string name)
        {
            if (!CheckName(name, out string message)) return NameResult.Invalid(message);
            NameResult result;
            try { result = await Service.SetPlayerNameAsync(name); }
            catch (Exception e) { Debug.LogWarning("Classement : pseudo non envoyé (" + e.Message + ")."); result = NameResult.Offline(); }
            if (result.Status != NameStatus.Ok) return result;
            PlayerPrefs.SetString(NameKey, name);
            PlayerPrefs.Save();
            Changed?.Invoke();
            await TrySendPendingAsync();
            return result;
        }

        /// « Quitter le classement » : pseudo et listes de parties effacés (en ligne : compte
        /// anonyme supprimé). Les pièces, objets et statistiques de jeu sont GARDÉS.
        public static async Task LeaveAsync()
        {
            try { await Service.DeleteMyDataAsync(); }
            catch (Exception e) { Debug.LogWarning("Classement : suppression en ligne impossible (" + e.Message + ")."); }
            foreach (var key in AllSaveKeys) PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();
            Changed?.Invoke();
        }

        // =====================================================================
        // Les scores et leur envoi (§12.8)
        // =====================================================================

        /// Les 3 totaux actuels (ceux qui seront envoyés en ligne).
        public static PlayerScores CurrentScores() => new PlayerScores
        {
            etage = Mathf.Max(0, PlayerPrefs.GetInt(GameManager.RecordKey, 0)),
            pieces = PlayerStats.CoinsCashed,
            secrets = PlayerStats.SecretsObtained,
            boites = PlayerStats.BoxesOpened
        };

        /// Fin d'une partie : on enregistre (local) ou on envoie (en ligne) les totaux.
        public static Task<SubmitResult> SubmitRunAsync(RunResult run)
        {
            var scores = CurrentScores();
            scores.partie = new RunRecord
            {
                etage = run.Floor,
                pieces = run.Lost ? 0 : run.CashedOut,
                date = LocalLeaderboardService.Today(),
                reprise = run.UsedRevive
            };
            return SubmitAsync(scores);
        }

        /// Un Secret vient d'être obtenu dans une boîte (onglet Secrets).
        public static Task<SubmitResult> SubmitSecretAsync(ShopItem item)
        {
            var scores = CurrentScores();
            scores.nouveauSecret = item != null ? item.Id : null;
            scores.dateSecret = LocalLeaderboardService.Today();
            return SubmitAsync(scores);
        }

        /// Contrôle de vraisemblance (§12.10), local ET en ligne, avant tout envoi.
        public static bool IsPlausible(PlayerScores s, out string reason)
        {
            reason = "";
            if (s.etage > MaxFloor) { reason = $"étage {s.etage} > {MaxFloor}"; return false; }
            if (s.secrets > s.boites) { reason = $"{s.secrets} Secrets pour {s.boites} boîtes ouvertes"; return false; }
            if (TryReadLastSent(out int e, out int p, out int sec))
            {
                if (s.etage < e || s.pieces < p || s.secrets < sec) { reason = "un total a diminué depuis le dernier envoi"; return false; }
                if (s.pieces - p > MaxCoinsJump) { reason = $"+{s.pieces - p} pièces depuis le dernier envoi (> {MaxCoinsJump})"; return false; }
            }
            return true;
        }

        public static async Task<SubmitResult> SubmitAsync(PlayerScores scores)
        {
            if (!IsPlausible(scores, out string reason))
            {
                Debug.LogWarning("Classement : envoi refusé, valeur invraisemblable (" + reason + "). Rien n'est enregistré.");
                return SubmitResult.Rejected;
            }
            if (OnlineMode)
            {
                // En ligne : pas de pseudo = rien n'est envoyé ; au plus un envoi toutes les 30 s.
                if (!HasName || Time.realtimeSinceStartup - lastSendTime < MinSecondsBetweenSends || !Service.IsOnline)
                {
                    MarkPending();
                    return SubmitResult.Queued;
                }
            }
            SubmitResult result;
            try { result = await Service.SubmitAsync(scores); }
            catch (Exception e)
            {
                Debug.LogWarning("Classement : envoi impossible (" + e.Message + "), il sera refait plus tard.");
                MarkPending();
                return SubmitResult.Queued;
            }
            if (result == SubmitResult.Sent)
            {
                lastSendTime = Time.realtimeSinceStartup;
                PlayerPrefs.SetString(LastSentKey, $"{scores.etage}|{scores.pieces}|{scores.secrets}");
                PlayerPrefs.SetInt(PendingKey, 0);
                PlayerPrefs.Save();
            }
            else if (result == SubmitResult.Queued) MarkPending();
            Changed?.Invoke();
            return result;
        }

        /// En ligne : s'il reste des totaux à envoyer, on envoie les derniers (une seule « file »
        /// suffit : les classements gardent le meilleur score). En local : rien à faire.
        public static async Task TrySendPendingAsync()
        {
            if (!OnlineMode || PlayerPrefs.GetInt(PendingKey, 0) != 1) return;
            await SubmitAsync(CurrentScores());
        }

        static void MarkPending()
        {
            PlayerPrefs.SetInt(PendingKey, 1);
            PlayerPrefs.Save();
        }

        static bool TryReadLastSent(out int etage, out int pieces, out int secrets)
        {
            etage = pieces = secrets = 0;
            var parts = PlayerPrefs.GetString(LastSentKey, "").Split('|');
            return parts.Length == 3
                && int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out etage)
                && int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out pieces)
                && int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out secrets);
        }

        // =====================================================================
        // Lecture pour l'affichage (avec le cache, en ligne)
        // =====================================================================

        /// Les lignes d'un classement. En ligne et sans Internet : la dernière liste reçue
        /// (cache, fromCache = vrai), jamais le local à la place.
        public static async Task<LeaderboardPage> GetPageAsync(string boardId, int count)
        {
            if (!OnlineMode) return await Service.GetTopAsync(boardId, count);
            try
            {
                if (!Service.IsOnline) throw new Exception("hors ligne");
                var page = await Service.GetTopAsync(boardId, count);
                SaveCache(boardId, page);
                return page;
            }
            catch (Exception e)
            {
                Debug.LogWarning("Classement : lecture en ligne impossible (" + e.Message + "), affichage du cache.");
                return LoadCache(boardId);
            }
        }

        [Serializable] class CachedPage { public List<LeaderboardEntry> entries = new List<LeaderboardEntry>(); }

        static void SaveCache(string boardId, LeaderboardPage page)
        {
            PlayerPrefs.SetString(CacheKey(boardId), JsonUtility.ToJson(new CachedPage { entries = page.entries }));
            PlayerPrefs.SetString(CacheDateKey, DateTime.Now.ToString("o", CultureInfo.InvariantCulture));
            PlayerPrefs.Save();
        }

        static LeaderboardPage LoadCache(string boardId)
        {
            var page = new LeaderboardPage { fromCache = true };
            var cached = JsonUtility.FromJson<CachedPage>(PlayerPrefs.GetString(CacheKey(boardId), "{}"));
            if (cached != null && cached.entries != null) page.entries = cached.entries;
            if (DateTime.TryParse(PlayerPrefs.GetString(CacheDateKey, ""), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var d))
                page.updatedAt = d;
            return page;
        }

        /// En ligne : combien de places gagnées depuis la dernière ouverture (0 sinon).
        /// Le rang est retenu pour la prochaine fois.
        public static int PlacesGained(string boardId, int newRank)
        {
            int old = PlayerPrefs.GetInt(LastRankKey(boardId), 0);
            PlayerPrefs.SetInt(LastRankKey(boardId), newRank);
            PlayerPrefs.Save();
            return old > 0 && newRank > 0 && newRank < old ? old - newRank : 0;
        }

        static string ShortId(string boardId) =>
            boardId == LeaderboardIds.Etage ? "etage" : boardId == LeaderboardIds.Pieces ? "pieces" : "secrets";

        // =====================================================================
        // Outils de test (depuis `unity command script eval`)
        // =====================================================================

        /// OUTIL DE TEST (critère 33) : essaie d'envoyer des totaux choisis, avec une fausse
        /// partie. Renvoie le résultat (Rejected attendu pour un étage de 500).
        public static SubmitResult DebugSubmit(int etage, int pieces, int secrets, int boites)
        {
            var scores = new PlayerScores
            {
                etage = etage, pieces = pieces, secrets = secrets, boites = boites,
                partie = new RunRecord { etage = etage, pieces = 0, date = LocalLeaderboardService.Today() }
            };
            return SubmitAsync(scores).Result; // le service local répond tout de suite
        }

        /// OUTIL DE TEST : ajoute une partie au classement local (comme une vraie fin de partie).
        public static SubmitResult DebugAddRun(int floor, int cashedOut) =>
            SubmitRunAsync(new RunResult { Floor = floor, CashedOut = cashedOut, Lost = cashedOut <= 0 }).Result;

        /// OUTIL DE TEST : oublie le service choisi (pour relire LeaderboardConfig).
        public static void DebugResetService() { service = null; initialized = false; bannedWords = null; }
    }
}
