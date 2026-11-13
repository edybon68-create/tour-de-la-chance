using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace TourDeLaChance
{
    /// <summary>
    /// « MES SKINS » : les tenues sauvegardées (spec v6 §7.2), côté RÈGLES (aucun affichage).
    /// 5 emplacements ; une tenue = un nom + 1 skin + au plus 2 accessoires. Gratuit, sans limite
    /// de temps, sauvegardé tout de suite. Mettre une tenue passe par Shop.TryEquipOutfit.
    ///
    /// Sauvegarde : tenues_sauvegardees = du JSON (texte structuré) avec les 5 emplacements
    /// {nom, skin, acc1, acc2} ; un nom vide = emplacement libre ; "" = pas d'objet.
    /// Un objet qui n'est plus possédé n'est PAS retiré de la tenue : s'il revient, la tenue
    /// redevient complète.
    /// </summary>
    public static class SavedOutfits
    {
        public const string SaveKey = "tenues_sauvegardees";
        public const int SlotCount = 5;
        public const int MaxNameLength = 16;
        public static readonly string[] AllSaveKeys = { SaveKey };

        /// Prévient l'affichage qu'une tenue a été sauvée, renommée, remplacée ou supprimée.
        public static event Action Changed;

        /// Une tenue rangée. [Serializable] : JsonUtility sait l'écrire et la relire.
        [Serializable]
        public class Entry
        {
            public string nom = "";
            public string skin = "";
            public string acc1 = "";
            public string acc2 = "";
            public bool IsEmpty => string.IsNullOrEmpty(nom);
        }

        [Serializable]
        class Data { public List<Entry> tenues = new List<Entry>(); }

        /// Relit les 5 emplacements (une sauvegarde abîmée donne 5 emplacements vides).
        public static List<Entry> All()
        {
            Data data = null;
            string json = PlayerPrefs.GetString(SaveKey, "");
            if (!string.IsNullOrEmpty(json))
            {
                try { data = JsonUtility.FromJson<Data>(json); }
                catch (Exception) { data = null; }
            }
            var list = data?.tenues ?? new List<Entry>();
            while (list.Count < SlotCount) list.Add(new Entry());
            if (list.Count > SlotCount) list.RemoveRange(SlotCount, list.Count - SlotCount);
            for (int i = 0; i < list.Count; i++) if (list[i] == null) list[i] = new Entry();
            return list;
        }

        public static Entry Get(int slot) => slot >= 0 && slot < SlotCount ? All()[slot] : new Entry();

        static void Write(List<Entry> list)
        {
            PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(new Data { tenues = list }));
            PlayerPrefs.Save();
            Changed?.Invoke();
        }

        // ---------- Le nom ----------

        /// Le nom par défaut d'un emplacement : « Tenue 1 » … « Tenue 5 ».
        public static string DefaultName(int slot) => $"Tenue {slot + 1}";

        /// Caractères permis : lettres (accents compris), chiffres, espace, « - », « ' », « ! ».
        public static bool IsAllowedChar(char c) => char.IsLetterOrDigit(c) || c == ' ' || c == '-' || c == '\'' || c == '!';

        /// Nettoie une saisie : enlève les caractères refusés, coupe à 16 caractères. (L'interface
        /// l'appelle à chaque frappe : un 17e caractère est refusé.)
        public static string Filter(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            var sb = new StringBuilder();
            foreach (char c in text) if (IsAllowedChar(c)) sb.Append(c);
            string s = sb.ToString();
            return s.Length > MaxNameLength ? s.Substring(0, MaxNameLength) : s;
        }

        /// Le nom final : filtré, espaces du début et de la fin enlevés ; vide → « Tenue N ».
        public static string CleanName(string text, int slot)
        {
            string s = Filter(text).Trim();
            return s.Length == 0 ? DefaultName(slot) : s;
        }

        // ---------- Les actions ----------

        /// Range la tenue ACTUELLE dans un emplacement (nouvelle tenue ou « Remplacer » : le nom
        /// donné est gardé tel quel s'il n'est pas vide).
        public static void SaveCurrent(int slot, string name)
        {
            if (slot < 0 || slot >= SlotCount) return;
            var list = All();
            var worn = Shop.EquippedAccessories();
            list[slot] = new Entry
            {
                nom = CleanName(name, slot),
                skin = Shop.GetEquippedSkin()?.Id ?? "",
                acc1 = worn.Count > 0 ? worn[0].Id : "",
                acc2 = worn.Count > 1 ? worn[1].Id : ""
            };
            Write(list);
        }

        /// « Remplacer par ma tenue » : le contenu change, le nom est GARDÉ.
        public static void ReplaceWithCurrent(int slot)
        {
            var e = Get(slot);
            if (e.IsEmpty) return;
            SaveCurrent(slot, e.nom);
        }

        public static void Rename(int slot, string name)
        {
            var list = All();
            if (slot < 0 || slot >= SlotCount || list[slot].IsEmpty) return;
            list[slot].nom = CleanName(name, slot);
            Write(list);
        }

        public static void Delete(int slot)
        {
            var list = All();
            if (slot < 0 || slot >= SlotCount) return;
            list[slot] = new Entry();
            Write(list);
        }

        /// « Mettre » : équipe la tenue en une fois (Shop.TryEquipOutfit). La tenue rangée n'est
        /// PAS modifiée. Renvoie le nombre d'objets laissés de côté (non possédés), −1 si vide.
        public static int Wear(int slot)
        {
            var e = Get(slot);
            if (e.IsEmpty) return -1;
            return Shop.TryEquipOutfit(Shop.Find(e.skin), Shop.Find(e.acc1), Shop.Find(e.acc2));
        }

        // ---------- Ce que l'affichage lit ----------

        /// Les objets d'une tenue (null = aucun objet à cette place ; un identifiant inconnu donne
        /// aussi null, mais il compte comme « manquant »).
        public static (ShopItem skin, ShopItem acc1, ShopItem acc2) Items(Entry e) =>
            (Shop.Find(e.skin), Shop.Find(e.acc1), Shop.Find(e.acc2));

        /// Combien d'objets de la tenue ne sont plus possédés (ou n'existent plus).
        public static int MissingCount(Entry e)
        {
            int n = 0;
            foreach (var id in new[] { e.skin, e.acc1, e.acc2 })
            {
                if (string.IsNullOrEmpty(id)) continue;
                var item = Shop.Find(id);
                if (item == null || !Shop.IsOwned(item)) n++;
            }
            return n;
        }

        static IEnumerable<ShopItem> Accessories(Entry e) =>
            new[] { Shop.Find(e.acc1), Shop.Find(e.acc2) }.Where(a => a != null);

        public static int CoinBonus(Entry e) => Shop.OutfitCoinBonus(Shop.Find(e.skin), Accessories(e));
        public static int DiamondBonus(Entry e) => Shop.OutfitDiamondBonus(Shop.Find(e.skin), Accessories(e));

        /// Est-ce EXACTEMENT la tenue portée ? (badge « Portée ✓ », bouton « Mettre » grisé)
        public static bool IsWorn(Entry e)
        {
            if (e.IsEmpty) return false;
            var skin = Shop.Find(e.skin);
            if (skin == null || !Shop.IsOwned(skin)) return false;
            return Shop.IsWearing(skin, Accessories(e).Where(Shop.IsOwned));
        }

        /// OUTIL DE TEST : écrit directement une tenue (pour simuler un objet manquant).
        public static void DebugSet(int slot, string name, string skin, string acc1, string acc2)
        {
            var list = All();
            list[slot] = new Entry { nom = name, skin = skin ?? "", acc1 = acc1 ?? "", acc2 = acc2 ?? "" };
            Write(list);
        }
    }
}
