using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace TourDeLaChance
{
    /// <summary>
    /// La pop-up « Route des trophées » (spec v6 §8, art bible §15.5) : la tour comme une route
    /// qu'on fait défiler, de l'étage 1 (en bas) à max(30, record + 5) en haut.
    ///   - En-tête fixe « Tes coffres en ce moment » (valable à TOUS les étages : le nombre de
    ///     coffres, leur rareté, le risque et la chance de diamants dépendent des améliorations,
    ///     pas de l'étage), ton record et tes trophées.
    ///   - Une dalle par étage : « Trésor : +70 à +350 », « Cagnotte : ≈ 280 », « Y arriver : 8,8 % ».
    ///   - Des bandeaux quand quelque chose change (décor, diamants par trésor), les paliers et
    ///     leur récompense (« Reçu ✓ » ou à gagner), la marque « Toi » et le drapeau du record.
    /// Tous les nombres viennent de TrophyRoad (qui utilise les fonctions du jeu) : aucune règle
    /// ici. Ce n'est pas un MonoBehaviour : PanelsController le crée et l'avance (Tick).
    /// </summary>
    public class TrophyRoadPage : IDisposable
    {
        const float RowHeight = 96f;

        readonly VisualElement page;
        readonly GameManager game;
        ScrollView scroll;
        Button goButton;
        VisualElement youRow;
        bool scrollPending;

        // Les couleurs du ciel par palier de décor (jour, coucher, crépuscule, nuit), art bible §15.5.
        static readonly Color[] Sky =
        {
            new Color(0.43f, 0.76f, 1f), new Color(1f, 0.6f, 0.48f), new Color(0.56f, 0.48f, 1f), new Color(0.13f, 0.1f, 0.3f)
        };

        /// Les textes de la page (pour les tests).
        public string Text => string.Join(" | ", page.Query<Label>().ToList().ConvertAll(l => l.text));
        /// L'étage marqué « Toi » (pour les tests).
        public int YouFloor { get; private set; }
        public float ScrollY => scroll != null ? scroll.scrollOffset.y : 0f;

        public TrophyRoadPage(VisualElement page, GameManager game)
        {
            this.page = page;
            this.game = game;
        }

        public void Dispose() { }

        public void Open()
        {
            Refresh();
            scrollPending = true;
        }

        /// L'étage où se trouve le joueur : pendant une partie, l'étage en cours ; après une
        /// partie (Perdu, Encaissé), l'étage atteint à la dernière partie ; sinon le record.
        int CurrentFloor(out bool inRun)
        {
            inRun = game.State == GameState.Choosing || game.State == GameState.Revealing || game.State == GameState.Climbing;
            if (inRun) return Mathf.Max(1, game.Floor);
            if (game.State == GameState.Lost || game.State == GameState.CashedOut) return Mathf.Max(1, game.LastRunFloor > 0 ? game.LastRunFloor : game.Floor);
            return Mathf.Max(1, game.Record);
        }

        public void Refresh()
        {
            page.Clear();
            int record = game.Record;
            int you = CurrentFloor(out bool inRun);
            YouFloor = you;
            int top = TrophyRoad.TopFloorShown(Mathf.Max(record, you));

            page.Add(Header(record, top));

            var frame = new VisualElement();
            frame.AddToClassList("route-fenetre");
            scroll = new ScrollView();
            scroll.AddToClassList("shop-scroll");
            scroll.AddToClassList("route-scroll");
            PanelsController.SetupScroll(scroll);
            scroll.verticalScroller.valueChanged += _ => UpdateGoButton();
            frame.Add(scroll);

            // De HAUT en BAS : l'étage le plus haut d'abord, l'étage 1 tout en bas.
            youRow = null;
            for (int f = top; f >= 1; f--)
            {
                var row = Row(TrophyRoad.RowFor(f), f == you, inRun, f == record && record > 0, f > record);
                if (f == you) youRow = row;
                scroll.Add(row);
                // Un bandeau SOUS l'étage où quelque chose commence (on lit de haut en bas).
                string change = ChangeBefore(f);
                if (change.Length > 0) scroll.Add(Banner(change, GameManager.TierForFloor(f)));
            }

            goButton = new Button(() => { scrollPending = true; }) { text = "Aller à mon étage" };
            goButton.AddToClassList("btn"); goButton.AddToClassList("btn--bleu"); goButton.AddToClassList("route-aller");
            goButton.style.display = DisplayStyle.None;
            frame.Add(goButton);
            page.Add(frame);
            ButtonStyler.Apply(page);
        }

        /// Ce qui change en ARRIVANT à cet étage (bandeau entre f − 1 et f), lu dans les données.
        static string ChangeBefore(int floor)
        {
            if (floor <= 1) return "";
            var parts = new List<string>();
            int tier = GameManager.TierForFloor(floor);
            if (tier != GameManager.TierForFloor(floor - 1))
                parts.Add(tier == 2 ? "Coucher de soleil" : tier == 3 ? "Crépuscule" : "Dans les étoiles");
            var eco = EconomyCatalog.Instance;
            int now = eco.DiamondsForFloor(floor), before = eco.DiamondsForFloor(floor - 1);
            if (now != before) parts.Add($"Trésors en diamants : {now * Rebirth.DiamondMultiplier} {ShopUi.Diamant}");
            return string.Join("  ·  ", parts);
        }

        VisualElement Header(int record, int top)
        {
            var head = new VisualElement();
            head.AddToClassList("route-entete");
            var left = new VisualElement { pickingMode = PickingMode.Ignore };
            left.AddToClassList("route-entete__gauche");
            left.Add(ShopUi.NewLabel("Tes coffres en ce moment (à tous les étages)", "route-entete__titre"));

            int chests = Upgrades.Level(Upgrades_.Chests), traps = Upgrades.Level(Upgrades_.Traps), gains = Upgrades.Level(Upgrades_.Gains);
            left.Add(ShopUi.NewLabel($"{TrophyRoad.ChestCountText(traps)} · 1 piège · Risque : {ShopUi.Number(Upgrades.AverageRiskPercent(traps))} %", "route-entete__ligne"));

            var w = Upgrades.ChestWeights(chests);
            var rarities = new List<string>();
            var diamonds = new List<string>();
            for (int i = 0; i < w.Length && i < Upgrades.RarityCount; i++)
            {
                if (w[i] <= 0) continue;
                var r = Upgrades.Rarity(i);
                rarities.Add($"{r.shortName} {w[i]} %");
                diamonds.Add($"{r.shortName} {ShopUi.Number(r.diamondChancePermille / 10f)} %");
            }
            left.Add(ShopUi.NewLabel(string.Join(" · ", rarities), "route-entete__ligne"));
            left.Add(ShopUi.NewLabel($"Chance de diamants : {string.Join(" · ", diamonds)}", "route-entete__ligne"));

            var mult = new List<string>();
            float bourse = (Upgrades.GainMultiplier(gains) - 1f) * 100f;
            if (bourse > 0f) mult.Add($"Bourse +{Mathf.RoundToInt(bourse)} %");
            if (Shop.OutfitBonusPercent > 0) mult.Add($"Tenue +{Shop.OutfitBonusPercent} %");
            if (Rebirth.CoinMultiplier > 1) mult.Add($"Renaissance ×{Rebirth.CoinMultiplier}");
            if (Shop.DiamondBonusPercent > 0) mult.Add($"{ShopUi.Diamant} +{Shop.DiamondBonusPercent} %");
            if (mult.Count > 0) left.Add(ShopUi.NewLabel(string.Join(" · ", mult), "route-entete__ligne"));
            head.Add(left);

            var right = new VisualElement { pickingMode = PickingMode.Ignore };
            right.AddToClassList("route-entete__droite");
            var rec = Pill("route-pilule--record", record > 0 ? $"Ton record : étage {record}" : "Pas encore de record");
            var flag = new VisualElement { pickingMode = PickingMode.Ignore };
            flag.AddToClassList("route-pilule__drapeau");
            if (UiImages.Drapeau != null) { flag.style.backgroundImage = new StyleBackground(UiImages.Drapeau); rec.Insert(0, flag); }
            right.Add(rec);
            var milestones = TrophyRoad.MilestonesUpTo(top);
            int got = milestones.FindAll(TrophyRoad.IsReceived).Count;
            right.Add(Pill("route-pilule--trophees", $"Trophées : {got} / {milestones.Count}"));
            int next = TrophyRoad.NextMilestoneAbove(TrophyRoad.HighestReceived);
            if (next > 0) right.Add(Pill("route-pilule--trophees", $"Prochain : étage {next} (+{TrophyRoad.RewardAt(next)} {ShopUi.Diamant})"));
            head.Add(right);
            return head;
        }

        static VisualElement Pill(string cls, string text)
        {
            var p = new VisualElement { pickingMode = PickingMode.Ignore };
            p.AddToClassList("route-pilule");
            p.AddToClassList(cls);
            p.Add(ShopUi.NewLabel(text, "route-pilule__texte"));
            return p;
        }

        VisualElement Banner(string text, int tier)
        {
            var b = new VisualElement { pickingMode = PickingMode.Ignore };
            b.AddToClassList("route-bandeau");
            b.style.backgroundColor = SkyColor(tier, 0.55f);
            b.Add(ShopUi.NewLabel(text, "route-bandeau__texte"));
            return b;
        }

        static Color SkyColor(int tier, float alpha)
        {
            var c = Sky[Mathf.Clamp(tier, 1, 4) - 1];
            c.a = alpha;
            return c;
        }

        VisualElement Row(TrophyRoad.Row data, bool you, bool inRun, bool isRecord, bool unexplored)
        {
            int f = data.Floor;
            var row = new VisualElement { pickingMode = PickingMode.Ignore };
            row.AddToClassList("route-dalle");
            row.style.backgroundColor = SkyColor(GameManager.TierForFloor(f), 0.75f);

            // Le chemin : un trait vertical doré jusqu'à l'étage atteint, blanc au-delà.
            var path = new VisualElement { pickingMode = PickingMode.Ignore };
            path.style.position = Position.Absolute;
            path.style.left = 16f + 96f + 32f - 13f;
            path.style.top = 0; path.style.bottom = 0;
            path.style.width = 26f;
            path.style.backgroundColor = unexplored ? new Color(1f, 1f, 1f, 0.35f) : new Color(1f, 0.78f, 0.23f);
            row.Add(path);

            bool milestone = data.MilestoneReward > 0;
            var node = new VisualElement { pickingMode = PickingMode.Ignore };
            node.AddToClassList("route-noeud");
            if (milestone) node.AddToClassList("route-noeud--palier");
            if (!unexplored) node.AddToClassList("route-noeud--passe");
            node.Add(ShopUi.NewLabel(f.ToString(), "route-noeud__num"));
            row.Add(node);

            var card = new VisualElement { pickingMode = PickingMode.Ignore };
            card.AddToClassList("route-carte");
            if (unexplored) card.AddToClassList("route-carte--sombre"); // « pas encore exploré », mais lisible
            var col = new VisualElement { pickingMode = PickingMode.Ignore };
            col.AddToClassList("route-carte__col");
            string gain = data.GainMin == data.GainMax ? $"+{ShopUi.Thousands(data.GainMin)}" : $"+{ShopUi.Thousands(data.GainMin)} à +{ShopUi.Thousands(data.GainMax)}";
            col.Add(ShopUi.NewLabel($"Étage {f}  ·  Trésor : {gain}", "route-carte__ligne"));
            var small = new List<string>();
            if (f > 1) small.Add($"Cagnotte : ≈ {ShopUi.Thousands(data.PotAverage)}");
            if (f > 1) small.Add($"Y arriver : {ReachText(data.ReachChance)}");
            small.Add($"Trésor en diamants : {data.DiamondsPerTreasure} {ShopUi.Diamant}");
            col.Add(ShopUi.NewLabel(string.Join("  ·  ", small), "route-carte__petit"));
            card.Add(col);

            if (milestone)
            {
                bool got = TrophyRoad.IsReceived(f);
                var t = new VisualElement { pickingMode = PickingMode.Ignore };
                t.AddToClassList("route-trophee");
                if (!got) t.AddToClassList("route-trophee--verrou");
                t.Add(ShopUi.NewLabel(got ? $"Trophée +{data.MilestoneReward} {ShopUi.Diamant} · Reçu ✓" : $"Trophée : +{data.MilestoneReward} {ShopUi.Diamant}", "route-trophee__texte"));
                card.Add(t);
            }
            if (isRecord) card.Add(Mark("Record", UiImages.Drapeau));
            if (you) card.Add(Mark(inRun ? "Toi" : "Dernière partie", Shop.GetEquippedSkin()?.Icon));
            row.Add(card);
            return row;
        }

        static VisualElement Mark(string text, Sprite icon)
        {
            var m = new VisualElement { pickingMode = PickingMode.Ignore };
            m.AddToClassList("route-marque");
            if (icon != null)
            {
                var img = new VisualElement { pickingMode = PickingMode.Ignore };
                img.AddToClassList("route-marque__img");
                img.style.backgroundImage = new StyleBackground(icon);
                m.Add(img);
            }
            m.Add(ShopUi.NewLabel(text, "route-marque__texte"));
            return m;
        }

        /// « 8,8 % », ou « moins de 0,1 % » pour les très petites chances.
        static string ReachText(double chance)
        {
            double pct = chance * 100.0;
            return pct < 0.1 ? "moins de 0,1 %" : ShopUi.Percent(Math.Round(pct, 1)); // « 8,8 % » (§8.3)
        }

        /// À chaque image : défiler (une fois la mise en page faite) pour que la dalle « Toi » soit
        /// au TIERS BAS de la zone visible.
        public void Tick(float dt)
        {
            if (!scrollPending || scroll == null || youRow == null) return;
            float y = youRow.layout.y, view = scroll.contentViewport.layout.height, content = scroll.contentContainer.layout.height;
            if (float.IsNaN(y) || float.IsNaN(view) || view <= 0f || content <= 0f) return;
            scrollPending = false;
            float target = y + RowHeight * 0.5f - view * (2f / 3f);
            scroll.scrollOffset = new Vector2(0f, Mathf.Clamp(target, 0f, Mathf.Max(0f, content - view)));
            UpdateGoButton();
        }

        /// Le bouton « Aller à mon étage » apparaît quand la dalle « Toi » sort de la vue.
        void UpdateGoButton()
        {
            if (goButton == null || youRow == null || scroll == null) return;
            float y = youRow.layout.y - scroll.scrollOffset.y, view = scroll.contentViewport.layout.height;
            bool far = !float.IsNaN(y) && (y < -RowHeight || y > view);
            goButton.style.display = far ? DisplayStyle.Flex : DisplayStyle.None;
        }

        /// OUTIL DE TEST : fait défiler la route.
        public void DebugScrollTo(float y) { if (scroll != null) scroll.scrollOffset = new Vector2(0f, y); }
    }
}
