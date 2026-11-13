using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace TourDeLaChance
{
    /// <summary>
    /// La pop-up « Index » v5 (spec v5 §8) : toute la collection, pour DÉCOUVRIR et CONSULTER.
    /// v6 : l'Index montre ce qu'on a DÉJÀ eu : un objet non obtenu (non Secret) garde son nom
    /// mais son visuel est remplacé par le pictogramme « pas encore vu » (§6.1) ; les raretés vont
    /// de Commun à Secret partout (§5.3) ; chaque boîte de l'onglet Tous dit sa récompense de
    /// collection (§6.3).
    /// Trois onglets : Tous (une section par boîte), Par rareté (Commun en haut), Par catégorie
    /// (v6.5 §3 : seulement Skins et Accessoires, sans le détail tête, cou, dos…). Tout est visible en couleurs, SAUF les Secrets non
    /// obtenus (« ? » et « ??? ») ; un objet non obtenu porte le ruban « Pas encore ». Chaque
    /// carte montre la chance par objet, le bonus de tenue et la mini-image de sa boîte.
    /// Un clic ouvre la FICHE AGRANDIE (ItemDetailView) avec l'objet sur le cheval vierge.
    /// Plus de vitrine ni d'aperçu sur le cheval du joueur (c'est le rôle de l'Inventaire).
    ///
    /// Ce n'est pas un MonoBehaviour : PanelsController le crée et l'avance. Aucune règle ici.
    /// </summary>
    public class IndexPage : IDisposable
    {
        public enum Tab { Tous, ParRarete, ParCategorie }

        /// Le dernier onglet choisi, retenu pendant la session (jamais sauvegardé).
        static Tab lastTab = Tab.Tous;

        readonly VisualElement page, tabsRow, barFill;
        readonly Label progress;
        readonly ScrollView grid;
        readonly ItemDetailView detail;
        readonly Dictionary<Tab, Button> tabButtons = new Dictionary<Tab, Button>();
        readonly List<ShopItem> order = new List<ShopItem>();       // l'ordre de l'onglet (flèches)
        readonly Dictionary<ShopItem, VisualElement> cards = new Dictionary<ShopItem, VisualElement>();

        /// « Voir dans l'inventaire » / « Voir la boîte » (fiche agrandie).
        public event Action<ShopItem> ShowInInventoryRequested;
        public event Action<LootBox> ShowBoxRequested;
        /// v6 §6.1 : « Voir dans la Boutique » (fiche d'un objet non obtenu).
        public event Action<ShopItem> ShowInShopRequested;

        public Tab CurrentTab => lastTab;
        public ItemDetailView Detail => detail;
        /// Les titres des sections affichées (pour les tests).
        public List<string> SectionTitles => grid.Query<Label>(className: "idx-section-title").ToList().Select(l => l.text).ToList();
        public string ProgressText => progress.text;

        public IndexPage(VisualElement page, ShowcaseHorse horse)
        {
            this.page = page;
            page.Clear();
            var top = new VisualElement();
            top.AddToClassList("idx-top");
            tabsRow = new VisualElement();
            tabsRow.AddToClassList("v5-tabs");
            foreach (var (tab, text) in new[] { (Tab.Tous, "Tous"), (Tab.ParRarete, "Par rareté"), (Tab.ParCategorie, "Par catégorie") })
            {
                var t = tab;
                var b = new Button(() => { lastTab = t; Refresh(); grid.scrollOffset = Vector2.zero; }) { text = text };
                b.AddToClassList("btn"); b.AddToClassList("btn--sombre"); b.AddToClassList("v5-tab"); b.AddToClassList(UiSounds.TabClass);
                tabsRow.Add(b);
                tabButtons[tab] = b;
            }
            top.Add(tabsRow);
            var progressBox = new VisualElement { pickingMode = PickingMode.Ignore };
            progressBox.AddToClassList("idx-progress");
            progress = ShopUi.NewLabel("", "progress-text");
            progressBox.Add(progress);
            var bar = new VisualElement { pickingMode = PickingMode.Ignore };
            bar.AddToClassList("progress-bar");
            barFill = new VisualElement { pickingMode = PickingMode.Ignore };
            barFill.AddToClassList("progress-fill");
            bar.Add(barFill);
            progressBox.Add(bar);
            top.Add(progressBox);
            page.Add(top);

            grid = new ScrollView();
            grid.AddToClassList("shop-scroll");
            grid.AddToClassList("idx-scroll");
            page.Add(grid);
            PanelsController.SetupScroll(grid);

            detail = new ItemDetailView(page, horse, hideUnowned: true);
            detail.ShowInInventoryRequested += item => ShowInInventoryRequested?.Invoke(item);
            detail.ShowInShopRequested += item => ShowInShopRequested?.Invoke(item);
            detail.ShowBoxRequested += box => ShowBoxRequested?.Invoke(box);
            ButtonStyler.Apply(page);
        }

        public void Dispose() { }

        public void Open()
        {
            detail.Close();
            Refresh();
            grid.scrollOffset = Vector2.zero;
        }

        public bool HandleEscape() => detail.HandleEscape();
        public void Tick(float dt) => detail.Tick(dt);

        public void Refresh()
        {
            foreach (var pair in tabButtons) ShopUi.SetClass(pair.Value, "subtab--active", pair.Key == lastTab);

            // « Collection : 41 / 88 » : tous les objets de toutes les boîtes (sans les gratuits).
            var all = Shop.Boxes.SelectMany(b => b.items).Where(i => i != null).ToList();
            int found = all.Count(i => Shop.CountOf(i) >= 1);
            progress.text = $"Collection : {found} / {all.Count}";
            barFill.style.width = Length.Percent(all.Count > 0 ? 100f * found / all.Count : 0f);

            grid.Clear();
            order.Clear();
            cards.Clear();
            switch (lastTab)
            {
                case Tab.Tous:
                    foreach (var box in Shop.BoxesInShopOrder) // v6 (QA n°43) : l'ordre de la Boutique
                        Section(BoxTitle(box), box.items.Where(i => i != null).ToList(), null, box);
                    break;
                case Tab.ParRarete:
                    if (Shop.Catalog == null) break;
                    foreach (var rarity in Shop.Catalog.Rarities) // v6 §5.3 : Commun en haut, Secret en bas
                    {
                        var items = Shop.BoxesInShopOrder.SelectMany(b => b.items).Where(i => i != null && i.RarityId == rarity.id).ToList();
                        if (items.Count == 0) continue;
                        Section(rarity.displayName, items, rarity);
                    }
                    break;
                case Tab.ParCategorie:
                    var boxOrder = Shop.BoxesInShopOrder.ToList();
                    IEnumerable<ShopItem> Sorted(Func<ShopItem, bool> filter) => Shop.Boxes.SelectMany(b => b.items)
                        .Where(i => i != null && filter(i))
                        .OrderBy(Shop.RarityRank)   // v6 §5.3 : du plus commun au plus rare
                        .ThenBy(i => boxOrder.IndexOf(Shop.BoxOf(i)))
                        .ThenBy(Shop.NumberOf);
                    // v6.5 §3 : deux catégories seulement. Tous les accessoires sont ensemble,
                    // quel que soit leur emplacement (tête, cou, dos…), triés par rareté.
                    Section("Skins", Sorted(i => i.Category == ShopCategory.Skin).ToList(), null);
                    Section("Accessoires", Sorted(i => i.Category == ShopCategory.Accessoire).ToList(), null);
                    break;
            }
            ButtonStyler.Apply(grid);
        }

        /// Titre d'une section « par boîte » : nom, prix (pièces ou ◆), cadenas si verrouillée.
        static string BoxTitle(LootBox box)
        {
            string price = box.currency == BoxCurrency.Diamants ? $"{box.price} {ShopUi.Diamant}" : $"{ShopUi.Thousands(box.price)} pièces";
            // (Pas d'émoji cadenas : la police n'en a pas ; le cadenas est dit en toutes lettres.)
            return $"{box.displayName}  ·  {price}" + (Shop.IsLocked(box) ? "  ·  verrouillée (après la renaissance)" : "");
        }

        void Section(string title, List<ShopItem> items, RarityDef rarity, LootBox box = null)
        {
            var head = new VisualElement { pickingMode = PickingMode.Ignore };
            head.AddToClassList("idx-section-head");
            var t = ShopUi.NewLabel(title, "idx-section-title");
            // v6 (QA n°40) : sur le fond blanc, un titre sans rareté s'écrit en encre foncée ; un
            // titre de rareté garde sa couleur, avec un contour noir pour rester lisible.
            if (rarity != null) { ShopUi.ColorText(t, rarity); t.AddToClassList("idx-section-title--rarete"); }
            head.Add(t);
            int found = items.Count(i => Shop.CountOf(i) >= 1);
            head.Add(ShopUi.NewLabel($"{found} / {items.Count}", "idx-section-count"));
            // v6 §6.3 : la récompense de collection de la boîte (onglet Tous seulement).
            string reward = RewardLine(box);
            if (reward.Length > 0) head.Add(ShopUi.NewLabel(reward, "idx-section-recompense"));
            grid.Add(head);

            var cells = new VisualElement { pickingMode = PickingMode.Ignore };
            cells.AddToClassList("grid");
            cells.AddToClassList("idx-grid");
            foreach (var item in items)
            {
                int position = order.Count;
                order.Add(item);
                VisualElement card = null;
                card = ShopUi.CollectionTile(item, () => detail.Open(new List<ShopItem>(order), position, card), hideUnowned: true);
                cards[item] = card;
                cells.Add(card);
            }
            grid.Add(cells);
        }

        /// « Tout collectionner : 5 ◆ », ou « ✓ Récompense reçue » une fois donnée ("" sans récompense).
        public static string RewardLine(LootBox box)
        {
            int reward = CollectionRewards.RewardOf(box);
            if (box == null || reward <= 0) return "";
            return CollectionRewards.Received(box) ? "✓ Récompense reçue" : $"Tout collectionner : {reward} {ShopUi.Diamant}";
        }

        /// OUTIL DE TEST : choisit l'onglet (0 Tous, 1 Par rareté, 2 Par catégorie).
        public void DebugTab(int tab) { lastTab = (Tab)tab; Refresh(); }
        /// OUTIL DE TEST : ouvre la fiche agrandie d'un objet (comme un clic sur sa carte).
        public void DebugOpen(string itemId)
        {
            int i = order.FindIndex(it => it.Id == itemId);
            if (i >= 0) detail.Open(new List<ShopItem>(order), i, cards[order[i]]);
        }
    }
}
