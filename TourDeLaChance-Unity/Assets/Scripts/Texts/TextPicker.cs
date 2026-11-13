using System.Collections.Generic;
using UnityEngine;

namespace TourDeLaChance
{
    /// <summary>
    /// Choisit les phrases à afficher (spec v3 §11). Ce n'est PAS un MonoBehaviour : un petit
    /// outil d'affichage, partagé (static) par la bannière de montée et le HUD.
    ///
    /// - Bannière : nom du palier si on en change, sinon texte spécial de l'étage, sinon une
    ///   phrase de montée au hasard (jamais une des 3 dernières).
    /// - Piège : phrases spéciales prioritaires, sinon une phrase au hasard (jamais deux fois
    ///   de suite la même).
    /// Le hasard vient d'un générateur VISUEL séparé : il ne touche à rien d'autre.
    /// </summary>
    public static class TextPicker
    {
        static GameTexts texts;
        static readonly System.Random random = new System.Random();
        static readonly List<int> recentClimb = new List<int>();
        static int lastTrap = -1, lastCashOut = -1;

        /// Les textes (Assets/Data/Textes.asset), donnés par les écrans qui les affichent
        /// (HudController et ClimbOverlay ont un champ « Texts » dans l'Inspector).
        public static GameTexts Texts => texts;

        /// Branche la fiche des textes (appelé par les écrans au démarrage).
        public static void Use(GameTexts gameTexts)
        {
            if (gameTexts != null) texts = gameTexts;
        }

        /// Remplace {etage}, {cagnotte}, {gain}, {perte} par leurs valeurs.
        public static string Fill(string text, int floor, int pot, int gain, int loss)
        {
            if (string.IsNullOrEmpty(text)) return "";
            return text.Replace("{etage}", floor.ToString())
                       .Replace("{cagnotte}", pot.ToString())
                       .Replace("{gain}", gain.ToString())
                       .Replace("{perte}", loss.ToString());
        }

        /// La 2e ligne de la bannière de montée. tierName : non vide si on entre dans un palier.
        public static string ClimbLine(int floor, string tierName, int pot, int gain)
        {
            if (!string.IsNullOrEmpty(tierName)) return tierName;           // 1. nouveau palier
            var t = Texts;
            if (t == null) return "";
            // v6 (QA n°42) : après un trésor en diamants, le gain en pièces vaut 0. Une phrase
            // qui cite {gain} afficherait « +0 ! » : on saute donc ces phrases-là.
            bool noGain = gain <= 0;
            foreach (var special in t.floorTexts)                           // 2. texte de l'étage
                if (special.floor == floor && !(noGain && UsesGain(special.text))) return Fill(special.text, floor, pot, gain, 0);
            if (t.repeatEvery > 0 && floor >= t.repeatFromFloor && (floor - t.repeatFromFloor) % t.repeatEvery == 0
                && !(noGain && UsesGain(t.repeatText)))
                return Fill(t.repeatText, floor, pot, gain, 0);
            if (t.climbLines.Count == 0) return "";                         // 3. phrase au hasard
            int index = PickAvoiding(t.climbLines.Count, recentClimb, 3, i => !(noGain && UsesGain(t.climbLines[i])));
            return index < 0 ? "" : Fill(t.climbLines[index], floor, pot, gain, 0);
        }

        /// La 1re ligne du message de piège.
        public static string TrapLine(int floor, int loss)
        {
            var t = Texts;
            if (t == null) return "Piégé !";
            if (floor <= 1 && !string.IsNullOrEmpty(t.trapFloor1)) return Fill(t.trapFloor1, floor, 0, 0, loss);
            if (floor >= t.trapHighFloor && !string.IsNullOrEmpty(t.trapHigh)) return Fill(t.trapHigh, floor, 0, 0, loss);
            if (loss >= t.bigLoss && !string.IsNullOrEmpty(t.trapBigLoss)) return Fill(t.trapBigLoss, floor, 0, 0, loss);
            if (t.trapLines.Count == 0) return "Piégé !";
            lastTrap = PickDifferent(t.trapLines.Count, lastTrap);
            return Fill(t.trapLines[lastTrap], floor, 0, 0, loss);
        }

        /// La 2e ligne du message de piège : « Cagnotte perdue : X pièces. » ou « Rien de perdu ».
        public static string LossLine(int loss)
        {
            var t = Texts;
            if (t == null) return "";
            return loss > 0 ? Fill(t.lossLine, 0, 0, 0, loss) : t.noLossLine;
        }

        /// Le message d'encaissement (une phrase puis « Tu encaisses X pièces. »).
        public static string CashOutLines(int amount, out string second)
        {
            var t = Texts;
            second = t != null ? Fill(t.cashOutLine, 0, 0, amount, 0) : $"Tu encaisses {amount} pièces.";
            if (t == null || t.cashOutLines.Count == 0) return "Bien joué !";
            lastCashOut = PickDifferent(t.cashOutLines.Count, lastCashOut);
            return Fill(t.cashOutLines[lastCashOut], 0, 0, amount, 0);
        }

        /// Un indice au hasard, différent du précédent (sauf s'il n'y a qu'un choix).
        static int PickDifferent(int count, int last)
        {
            if (count <= 1) return 0;
            int i = random.Next(count - 1);
            return i >= last && last >= 0 ? i + 1 : i;
        }

        /// La phrase cite-t-elle le gain ({gain}) ?
        static bool UsesGain(string text) => !string.IsNullOrEmpty(text) && text.Contains("{gain}");

        /// Un indice au hasard qui n'est pas dans les "window" derniers, puis on le retient.
        /// usable (facultatif) écarte certaines phrases ; −1 si aucune ne convient.
        static int PickAvoiding(int count, List<int> recent, int window, System.Func<int, bool> usable = null)
        {
            var allowed = new List<int>();
            var fallback = new List<int>();
            for (int i = 0; i < count; i++)
            {
                if (usable != null && !usable(i)) continue;
                fallback.Add(i);
                if (!recent.Contains(i)) allowed.Add(i);
            }
            if (fallback.Count == 0) return -1;
            int pick = allowed.Count > 0 ? allowed[random.Next(allowed.Count)] : fallback[random.Next(fallback.Count)];
            recent.Add(pick);
            while (recent.Count > Mathf.Min(window, count - 1)) recent.RemoveAt(0);
            return pick;
        }
    }
}
