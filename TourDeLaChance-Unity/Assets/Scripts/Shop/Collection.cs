using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace TourDeLaChance
{
    /// <summary>
    /// La collection du joueur : COMBIEN d'exemplaires il possède de chaque objet
    /// (0 = pas encore obtenu). Aucun affichage, aucune règle d'achat ici : seulement
    /// lire, écrire et sauvegarder ces nombres (spec boites §9).
    ///
    /// Sauvegarde (PlayerPrefs) — on enregistre des IDENTIFIANTS, jamais des positions :
    ///   collection_counts = "couleur_rouge:3,chapeau_casquette:1"
    /// Seulement les objets à 1 exemplaire ou plus, jamais les objets gratuits.
    ///
    /// Migration v1 : l'ancienne sauvegarde (shop_owned = "couleur_or,chapeau_couronne")
    /// est convertie une fois, au premier chargement : chaque objet acheté = 1 exemplaire.
    ///
    /// v3 (correctifs B5 et B6, spec v3 §13) :
    ///   - la migration n'a lieu QUE si le catalogue est chargé (sinon on ne touche à rien et
    ///     on réessaie au prochain accès) ;
    ///   - un identifiant INCONNU du catalogue est ignoré pour le jeu (ni affiché, ni compté,
    ///     ni vendable) mais CONSERVÉ tel quel dans la sauvegarde, recopié à la fin du texte.
    /// </summary>
    public static class Collection
    {
        public const string CountsKey = "collection_counts";

        // Le dernier texte lu dans la sauvegarde, et sa version « décodée ».
        // On relit PlayerPrefs à chaque fois (c'est très rapide) : si le texte n'a pas
        // changé, on réutilise le décodage. Ainsi la sauvegarde reste la seule vérité.
        static string cachedRaw;
        static Dictionary<string, int> cachedCounts = new Dictionary<string, int>();

        /// Nombre d'exemplaires d'un objet (0 si jamais obtenu ou inconnu).
        public static int CountOf(string id)
        {
            if (string.IsNullOrEmpty(id)) return 0;
            return Read().TryGetValue(id, out int n) ? n : 0;
        }

        /// Une copie de toute la collection (identifiant → nombre).
        public static Dictionary<string, int> All() => new Dictionary<string, int>(Read());

        /// Ajoute (ou retire, si delta est négatif) des exemplaires. Ne sauvegarde PAS sur
        /// le disque : c'est l'appelant (Shop) qui fait PlayerPrefs.Save() à la fin de son
        /// action, pour que tout soit écrit en une fois.
        public static void Add(string id, int delta)
        {
            var counts = All();
            counts.TryGetValue(id, out int n);
            n = Mathf.Max(0, n + delta);
            if (n > 0) counts[id] = n;
            else counts.Remove(id);
            Write(counts);
        }

        /// Remplace toute la collection (utilisé par « Vendre tous les doublons »).
        public static void Write(Dictionary<string, int> counts)
        {
            // Sans catalogue, on ne sait pas quels identifiants sont connus : on n'écrit rien
            // (on risquerait d'effacer la collection). Shop.Catalog a déjà affiché l'erreur.
            if (Shop.Catalog == null) return;

            var parts = counts
                .Where(pair => pair.Value > 0 && IsKnownBoxItem(pair.Key))
                .Select(pair => pair.Key + ":" + pair.Value.ToString(CultureInfo.InvariantCulture))
                .ToList();
            // B6 : les morceaux inconnus de l'ancienne sauvegarde sont recopiés à la fin, tels quels.
            parts.AddRange(UnknownParts(PlayerPrefs.GetString(CountsKey, "")));
            PlayerPrefs.SetString(CountsKey, string.Join(",", parts));
            cachedRaw = null; // le prochain Read() décodera le nouveau texte
        }

        /// Les morceaux « id:nombre » de la sauvegarde dont l'identifiant n'est pas dans le
        /// catalogue (objet retiré un temps, faute de frappe corrigée plus tard…).
        static IEnumerable<string> UnknownParts(string raw)
        {
            foreach (string part in raw.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                int colon = part.LastIndexOf(':');
                if (colon <= 0) continue;
                string id = part.Substring(0, colon).Trim();
                if (!IsKnownBoxItem(id) && Shop.Find(id) == null) yield return part.Trim();
            }
        }

        /// Lit (et décode) la collection, en faisant la migration v1 si besoin.
        static Dictionary<string, int> Read()
        {
            MigrateFromV1IfNeeded();
            string raw = PlayerPrefs.GetString(CountsKey, "");
            if (raw == cachedRaw) return cachedCounts;

            var counts = new Dictionary<string, int>();
            foreach (string part in raw.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                // Chaque morceau ressemble à "couleur_rouge:3".
                int colon = part.LastIndexOf(':');
                if (colon <= 0) continue;                       // morceau illisible : ignoré
                string id = part.Substring(0, colon).Trim();
                string number = part.Substring(colon + 1).Trim();
                if (!int.TryParse(number, NumberStyles.Integer, CultureInfo.InvariantCulture, out int n))
                    continue;                                   // nombre illisible : ignoré
                if (n <= 0) continue;                           // nul ou négatif : non obtenu
                if (!IsKnownBoxItem(id)) continue;              // identifiant inconnu : ignoré
                counts[id] = n;                                 // (doublon de clé : le dernier gagne)
            }
            cachedRaw = raw;
            cachedCounts = counts;
            return counts;
        }

        /// Migration depuis la v1 (spec §9.2) : seulement si collection_counts n'existe pas.
        static void MigrateFromV1IfNeeded()
        {
            if (PlayerPrefs.HasKey(CountsKey)) return;
            // B5 : sans catalogue, tous les objets paraîtraient « inconnus » : on ne migre pas,
            // on n'écrit rien, on ne supprime rien. On réessaiera au prochain accès.
            if (Shop.Catalog == null) return;

            string owned = PlayerPrefs.GetString(Shop.OldOwnedKey, "");
            var counts = new Dictionary<string, int>();
            foreach (string part in owned.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string id = part.Trim();
                if (IsKnownBoxItem(id)) counts[id] = 1; // chaque objet acheté = 1 exemplaire
            }
            PlayerPrefs.SetString(CountsKey, "");   // crée la clé (même vide) : migration faite
            Write(counts);
            PlayerPrefs.DeleteKey(Shop.OldOwnedKey);
            PlayerPrefs.Save();
            if (owned.Length > 0)
                Debug.Log($"Sauvegarde v1 convertie : « {owned} » → « {PlayerPrefs.GetString(CountsKey)} ».");
        }

        /// Un identifiant d'objet de boîte qui existe dans le catalogue (pas un gratuit).
        static bool IsKnownBoxItem(string id)
        {
            var item = Shop.Find(id);
            return item != null && !item.IsFree;
        }
    }
}
