using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace TourDeLaChance
{
    /// <summary>
    /// Le panneau « Améliorations » (spec v3 §5.7, v4 §10.6) : une carte par amélioration, avec
    /// son niveau (10 pastilles), l'effet actuel → l'effet suivant, et le bouton « Améliorer ».
    /// Ce n'est pas un MonoBehaviour : un simple objet créé et piloté par PanelsController.
    ///
    /// Il AFFICHE et TRANSMET : l'achat est décidé par Upgrades.TryBuy, et TOUS les chiffres
    /// (prix, multiplicateurs, chances, risque) sont lus dans UpgradeCatalog via Upgrades :
    /// changer un poids dans le catalogue change le pourcentage affiché, sans toucher à l'UI.
    /// </summary>
    public class UpgradesPage : IDisposable
    {
        readonly ScrollView list;
        readonly MonoBehaviour host; // pour lancer les petites animations (coroutines)

        /// Un niveau vient d'être acheté (id, niveau atteint) : saut du cheval, son…
        public event Action<string, int> Bought;

        // Animation d'achat en cours : pendant 0,3 s, le bouton de la carte est inactif.
        float buyLockedUntil = -1f;
        string flashId;
        int popLevel = -1;

        static readonly Color CardColor = new Color(0.227f, 0.227f, 0.314f);  // #3A3A50
        static readonly Color FlashColor = new Color(0.353f, 0.29f, 0.125f);  // #5A4A20

        public UpgradesPage(ScrollView page, MonoBehaviour coroutineHost)
        {
            list = page;
            host = coroutineHost;
        }

        public void Dispose() { }

        public void Open()
        {
            Refresh();
            list.scrollOffset = Vector2.zero;
        }

        public void Refresh()
        {
            list.Clear();
            var catalog = Upgrades.Catalog;
            if (catalog == null) return;
            foreach (var def in catalog.Upgrades) list.Add(Card(def));
            ButtonStyler.Apply(list);
        }

        VisualElement Card(UpgradeDef def)
        {
            int level = Upgrades.Level(def.id);
            bool max = level >= Upgrades_.MaxLevel;

            var card = new VisualElement();
            card.AddToClassList("up-card");

            // 1. Icône, nom, phrase
            var top = new VisualElement { pickingMode = PickingMode.Ignore };
            top.AddToClassList("up-top");
            var icon = new VisualElement { pickingMode = PickingMode.Ignore };
            icon.AddToClassList("up-icon");
            if (def.icon != null) icon.style.backgroundImage = new StyleBackground(def.icon);
            top.Add(icon);
            var titles = new VisualElement { pickingMode = PickingMode.Ignore };
            titles.AddToClassList("up-title-col");
            titles.Add(ShopUi.NewLabel(def.displayName, "up-name"));
            titles.Add(ShopUi.NewLabel(def.description, "up-desc"));
            top.Add(titles);
            card.Add(top);

            // 2. Niveau : « Niveau 7 / 10 » + 10 pastilles
            var levelRow = new VisualElement { pickingMode = PickingMode.Ignore };
            levelRow.AddToClassList("up-level-row");
            levelRow.Add(ShopUi.NewLabel($"Niveau {level} / {Upgrades_.MaxLevel}", "up-level"));
            var dots = new List<VisualElement>();
            for (int i = 1; i <= Upgrades_.MaxLevel; i++)
            {
                var dot = new VisualElement { pickingMode = PickingMode.Ignore };
                dot.AddToClassList("up-dot");
                if (i <= level) dot.AddToClassList("up-dot--full");
                levelRow.Add(dot);
                dots.Add(dot);
            }
            card.Add(levelRow);

            // 3. Effet actuel → effet suivant
            switch (def.id)
            {
                case Upgrades_.Gains: AddGainsEffect(card, level, max); break;
                case Upgrades_.Chests: AddChestsEffect(card, level, max); break;
                case Upgrades_.Traps: AddTrapsEffect(card, level, max); break;
            }

            // 4. Bouton « Améliorer — 900 », ou bandeau « Niveau max ! »
            var buyRow = new VisualElement();
            buyRow.AddToClassList("up-buy-row");
            if (max)
            {
                buyRow.Add(ShopUi.NewLabel("Niveau max !", "up-max"));
            }
            else
            {
                string id = def.id;
                var buy = new Button(() => OnBuy(id)) { text = $"Améliorer — {Upgrades.NextPrice(id)}" };
                buy.AddToClassList("btn");
                buy.AddToClassList("btn--vert");
                buy.AddToClassList("up-buy");
                buy.AddToClassList(UiSounds.SilentClass); // le son d'amélioration suffit
                buy.name = "buy-" + id;
                bool canBuy = Upgrades.CanBuy(id) && Time.time >= buyLockedUntil;
                buy.SetEnabled(canBuy);
                buyRow.Add(buy);
                int missing = Upgrades.MissingCoins(id);
                if (missing > 0) buyRow.Add(ShopUi.NewLabel($"Il te manque {missing} pièces", "up-missing"));
            }
            card.Add(buyRow);

            // 5. Rappel en petit
            card.Add(ShopUi.NewLabel("S'applique à partir du prochain étage.", "up-note"));

            // Animation de l'achat qui vient d'avoir lieu sur CETTE carte.
            if (flashId == def.id && host != null)
            {
                flashId = null;
                host.StartCoroutine(FlashCard(card));
                if (popLevel >= 1 && popLevel <= dots.Count) host.StartCoroutine(PopDot(dots[popLevel - 1]));
            }
            return card;
        }

        // ---------- Les trois façons d'afficher l'effet ----------

        static void AddGainsEffect(VisualElement card, int level, bool max)
        {
            string now = $"Maintenant : trésors ×{ShopUi.Number(Upgrades.GainMultiplier(level))}";
            string text = max ? now : $"{now} → Prochain : ×{ShopUi.Number(Upgrades.GainMultiplier(level + 1))}";
            var label = ShopUi.NewLabel(text, "up-effect");
            label.AddToClassList("up-effect--strong");
            card.Add(label);
            card.Add(ShopUi.NewLabel(Upgrades.GainText(max ? level : level + 1), "up-effect"));
        }

        static void AddChestsEffect(VisualElement card, int level, bool max)
        {
            int[] now = Upgrades.ChestWeights(level);
            int[] next = Upgrades.ChestWeights(max ? level : level + 1);
            var table = new VisualElement { pickingMode = PickingMode.Ignore };
            table.AddToClassList("up-table");
            table.Add(Row(null, "Coffres", "Maintenant", max ? "" : "Prochain", header: true));
            for (int i = 0; i < Upgrades.RarityCount; i++)
            {
                int a = i < now.Length ? now[i] : 0, b = i < next.Length ? next[i] : 0;
                if (a == 0 && b == 0) continue; // rareté pas encore possible : on ne la montre pas
                var rarity = Upgrades.Rarity(i);
                table.Add(Row(rarity.icon, $"{rarity.shortName} ×{ShopUi.Number(rarity.multiplier)}",
                              $"{a} %", max ? "" : $"{b} %", header: false));
            }
            card.Add(table);
            string avg = $"Gain moyen ×{ShopUi.Number(Round2(Upgrades.AverageChestMultiplier(level)))}";
            if (!max) avg += $" → ×{ShopUi.Number(Round2(Upgrades.AverageChestMultiplier(level + 1)))}";
            var label = ShopUi.NewLabel(avg, "up-effect");
            label.AddToClassList("up-effect--strong");
            card.Add(label);
        }

        static void AddTrapsEffect(VisualElement card, int level, bool max)
        {
            int[] now = Upgrades.CountWeights(level);
            int[] next = Upgrades.CountWeights(max ? level : level + 1);
            int baseCount = Upgrades.Catalog != null ? Upgrades.Catalog.BaseChestCount : 3;
            for (int i = 0; i < now.Length || i < next.Length; i++)
            {
                int a = i < now.Length ? now[i] : 0, b = i < next.Length ? next[i] : 0;
                if (a == 0 && b == 0) continue;
                string text = max ? $"{baseCount + i} coffres : {a} % des étages"
                                  : $"{baseCount + i} coffres : {a} % → {b} % des étages";
                card.Add(ShopUi.NewLabel(text, "up-effect"));
            }
            string risk = $"Risque moyen : {Mathf.RoundToInt(Upgrades.AverageRiskPercent(level))} %";
            if (!max) risk += $" → {Mathf.RoundToInt(Upgrades.AverageRiskPercent(level + 1))} %";
            var label = ShopUi.NewLabel(risk, "up-effect");
            label.AddToClassList("up-effect--strong");
            card.Add(label);
            card.Add(ShopUi.NewLabel("Toujours un seul coffre piégé.", "up-effect"));
        }

        /// Arrondi à 2 décimales « comme à l'école » (1,125 → 1,13), pas « au pair le plus proche ».
        static float Round2(float v) => (float)(Math.Round(v * 100.0, MidpointRounding.AwayFromZero) / 100.0);

        static VisualElement Row(Sprite icon, string name, string now, string next, bool header)
        {
            var row = new VisualElement { pickingMode = PickingMode.Ignore };
            row.AddToClassList("up-row");
            if (header) row.AddToClassList("up-row--header");
            var iconBox = new VisualElement { pickingMode = PickingMode.Ignore };
            iconBox.AddToClassList("up-cell-icon");
            if (icon != null) iconBox.style.backgroundImage = new StyleBackground(icon);
            row.Add(iconBox);
            var nameLabel = ShopUi.NewLabel(name, "up-cell");
            nameLabel.AddToClassList("up-cell--name");
            row.Add(nameLabel);
            row.Add(ShopUi.NewLabel(now, "up-cell"));
            row.Add(ShopUi.NewLabel(next, "up-cell"));
            return row;
        }

        // ---------- Achat ----------

        /// Clic sur « Améliorer » : la règle est dans Upgrades.TryBuy (qui relit le niveau et le
        /// solde à chaque clic). Le bouton est inactif pendant les 0,3 s de l'animation.
        void OnBuy(string id)
        {
            if (Time.time < buyLockedUntil) return;
            if (!Upgrades.TryBuy(id)) return; // Upgrades.Changed → PanelsController → Refresh
            buyLockedUntil = Time.time + 0.3f;
            flashId = id;
            popLevel = Upgrades.Level(id);
            Bought?.Invoke(id, popLevel);
            Refresh();
            if (host != null) host.StartCoroutine(UnlockLater());
        }

        IEnumerator UnlockLater()
        {
            yield return new WaitForSeconds(0.3f);
            Refresh(); // le bouton redevient actif (s'il reste assez de pièces)
        }

        /// La carte fait un flash or : #3A3A50 → #5A4A20 → #3A3A50 en 0,4 s.
        static IEnumerator FlashCard(VisualElement card)
        {
            for (float t = 0f; t < 0.4f; t += Time.deltaTime)
            {
                card.style.backgroundColor = Color.Lerp(CardColor, FlashColor, Ease.Sine(t / 0.4f));
                yield return null;
            }
            card.style.backgroundColor = StyleKeyword.Null;
        }

        /// La pastille du nouveau niveau se remplit avec un « pop » (0 → 1,3 → 1 en 0,25 s).
        static IEnumerator PopDot(VisualElement dot)
        {
            for (float t = 0f; t < 0.25f; t += Time.deltaTime)
            {
                float s = Ease.Pop(t / 0.25f, 0f, 1.3f);
                dot.style.scale = new Scale(new Vector3(s, s, 1f));
                yield return null;
            }
            dot.style.scale = StyleKeyword.Null;
        }

        /// OUTIL DE TEST : simule un clic sur « Améliorer ».
        public void DebugClickBuy(string id) => OnBuy(id);
    }
}
