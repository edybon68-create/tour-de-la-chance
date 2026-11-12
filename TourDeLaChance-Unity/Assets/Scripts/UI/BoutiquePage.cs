using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace TourDeLaChance
{
    /// <summary>
    /// La pop-up « Boutique » v5 (spec v5 §15). Trois onglets : Boîtes (n°1 et Animaux, en grille
    /// de 2 colonnes, sans défilement), Pirates (payées en diamants) et Diamants (packs AFFICHÉS
    /// mais DÉSACTIVÉS « Bientôt disponible », sans prix : aucun paiement n'existe dans le jeu ;
    /// l'onglet disparaît si showDiamondPacks est faux). Chaque carte compacte montre l'image,
    /// le prix, la collection, les TAUX toujours visibles (barre segmentée + pourcentages, lus
    /// dans les données de la boîte), « Ouvrir » et « Contenu et taux » (sous-page avec le tableau
    /// complet et la grille des objets ; un clic ouvre la fiche agrandie).
    /// Avant la renaissance, les Boîtes Animaux ont un voile et un cadenas (contenu consultable).
    ///
    /// Ce n'est pas un MonoBehaviour : PanelsController le crée. Il AFFICHE et TRANSMET les
    /// clics ; ouvrir une boîte est décidé par la classe Shop.
    /// </summary>
    public class BoutiquePage : IDisposable
    {
        public enum Tab { Boites, Pirates, Diamants }

        /// Le dernier onglet choisi, retenu pendant la session (jamais sauvegardé).
        static Tab lastTab = Tab.Boites;

        readonly VisualElement page, tabsRow, balanceRow, body;
        readonly ItemDetailView detail;
        readonly Dictionary<Tab, Button> tabButtons = new Dictionary<Tab, Button>();
        LootBox contentBox; // la boîte de la sous-page « Contenu et taux » (null = les onglets)

        /// Clic sur « Ouvrir » : PanelsController transmet à Shop.TryOpenBox.
        public event Action<LootBox> OpenRequested;

        public Tab CurrentTab => lastTab;
        public LootBox ContentBox => contentBox;
        public ItemDetailView Detail => detail;

        public BoutiquePage(VisualElement page, ShowcaseHorse horse)
        {
            this.page = page;
            page.Clear();
            var top = new VisualElement();
            top.AddToClassList("btq-top");
            tabsRow = new VisualElement();
            tabsRow.AddToClassList("v5-tabs");
            top.Add(tabsRow);
            balanceRow = new VisualElement { pickingMode = PickingMode.Ignore };
            balanceRow.AddToClassList("btq-balance");
            top.Add(balanceRow);
            page.Add(top);
            body = new VisualElement();
            body.AddToClassList("btq-body");
            page.Add(body);
            detail = new ItemDetailView(page, horse);
            detail.ShowBoxRequested += box => Open(box);
            detail.ShowInInventoryRequested += _ => { }; // depuis la Boutique : rien de plus
        }

        public void Dispose() { }

        /// Ouvre la Boutique (sur l'onglet retenu) ou directement sur « Contenu et taux » d'une boîte.
        public void Open(LootBox content = null)
        {
            detail.Close();
            contentBox = content;
            Refresh();
        }

        public bool HandleEscape()
        {
            if (detail.HandleEscape()) return true;
            if (contentBox != null) { contentBox = null; Refresh(); return true; }
            return false;
        }

        public void Tick(float dt) => detail.Tick(dt);

        public void Refresh()
        {
            BuildTabs();
            BuildBalance();
            body.Clear();
            if (contentBox != null) BuildContentPage(contentBox);
            else
            {
                switch (lastTab)
                {
                    case Tab.Boites: BuildBoxGrid(Shop.Boxes.Where(b => b.currency == BoxCurrency.Pieces)); break;
                    case Tab.Pirates:
                        BuildBoxGrid(Shop.Boxes.Where(b => b.currency == BoxCurrency.Diamants));
                        var note = new VisualElement { pickingMode = PickingMode.Ignore };
                        note.AddToClassList("btq-note");
                        note.Add(ShopUi.NewLabel("D'où viennent les diamants ?", "btq-note__titre"));
                        note.Add(ShopUi.NewLabel(DiamondOriginText(), "btq-note__texte"));
                        body.Add(note);
                        break;
                    case Tab.Diamants: BuildPacks(); break;
                }
            }
            ButtonStyler.Apply(page);
        }

        /// « Parfois, un trésor contient des diamants… (1 % à 5 %) » : chiffres lus dans le
        /// catalogue des raretés de coffres.
        static string DiamondOriginText()
        {
            var r = Upgrades.Catalog != null ? Upgrades.Catalog.ChestRarities : null;
            string range = r != null && r.Count > 0
                ? $" ({ShopUi.Number(r.Min(x => x.diamondChancePermille) / 10f)} % à {ShopUi.Number(r.Max(x => x.diamondChancePermille) / 10f)} %)" : "";
            return "Parfois, un trésor contient des diamants à la place des pièces : plus les coffres sont précieux, plus c'est fréquent" + range + ".";
        }

        void BuildTabs()
        {
            tabsRow.Clear();
            tabButtons.Clear();
            bool packs = EconomyCatalog.Instance.ShowDiamondPacks;
            if (!packs && lastTab == Tab.Diamants) lastTab = Tab.Boites;
            AddTab(Tab.Boites, "Boîtes", "coin coin--small");
            AddTab(Tab.Pirates, "Pirates", "diamant diamant--petit");
            if (packs) AddTab(Tab.Diamants, "Diamants · Bientôt", "diamant diamant--petit");
            tabsRow.style.display = contentBox != null ? DisplayStyle.None : DisplayStyle.Flex;
        }

        void AddTab(Tab tab, string text, string iconClasses)
        {
            var b = new Button(() => { lastTab = tab; contentBox = null; Refresh(); });
            b.AddToClassList("btn"); b.AddToClassList("btn--sombre"); b.AddToClassList("v5-tab"); b.AddToClassList(UiSounds.TabClass);
            var icon = new VisualElement { pickingMode = PickingMode.Ignore };
            foreach (var c in iconClasses.Split(' ')) icon.AddToClassList(c);
            b.Add(icon);
            b.Add(ShopUi.NewLabel(text, "v5-tab__texte"));
            ShopUi.SetClass(b, "subtab--active", tab == lastTab && contentBox == null);
            tabsRow.Add(b);
            tabButtons[tab] = b;
        }

        /// Le solde sous les onglets : pièces, et diamants dans l'onglet Pirates (ou une boîte en diamants).
        void BuildBalance()
        {
            balanceRow.Clear();
            balanceRow.Add(ShopUi.NewLabel(ShopUi.Thousands(Wallet.Coins), "btq-balance__texte"));
            balanceRow.Add(Icon("coin coin--small"));
            bool diamonds = lastTab != Tab.Boites || (contentBox != null && contentBox.currency == BoxCurrency.Diamants);
            if (diamonds)
            {
                balanceRow.Add(ShopUi.NewLabel($"Tu as {Diamonds.Count}", "btq-balance__texte btq-balance__texte--diamants"));
                balanceRow.Add(Icon("diamant diamant--petit"));
            }
        }

        static VisualElement Icon(string classes)
        {
            var e = new VisualElement { pickingMode = PickingMode.Ignore };
            foreach (var c in classes.Split(' ')) e.AddToClassList(c);
            return e;
        }

        // =====================================================================
        // Les cartes compactes (476 × 300)
        // =====================================================================

        void BuildBoxGrid(IEnumerable<LootBox> boxes)
        {
            var grid = new VisualElement { pickingMode = PickingMode.Ignore };
            grid.AddToClassList("btq-grid");
            foreach (var box in boxes) grid.Add(CompactCard(box));
            body.Add(grid);
        }

        VisualElement CompactCard(LootBox box)
        {
            var card = new VisualElement();
            card.AddToClassList("btq-card");
            bool locked = Shop.IsLocked(box);

            // 1. Image + nom + prix
            var top = new VisualElement { pickingMode = PickingMode.Ignore };
            top.AddToClassList("btq-card-top");
            top.Add(BoxImage(box, "btq-card-image"));
            var titleCol = new VisualElement { pickingMode = PickingMode.Ignore };
            titleCol.AddToClassList("btq-card-titles");
            titleCol.Add(ShopUi.NewLabel(box.displayName, "btq-card-name"));
            titleCol.Add(PriceRow(box));
            // 2. Progression
            int found = Shop.FoundCount(box), total = box.items.Count;
            titleCol.Add(ShopUi.NewLabel($"Collection : {found} / {total}", "progress-text"));
            titleCol.Add(ProgressBar(found, total));
            top.Add(titleCol);
            card.Add(top);

            // 3. Les taux, toujours visibles (barre segmentée + pourcentages colorés).
            card.Add(RatesBar(box));

            // 4. Boutons
            var buttons = new VisualElement();
            buttons.AddToClassList("btq-card-buttons");
            if (!locked)
            {
                var open = new Button(() => OpenRequested?.Invoke(box)) { text = OpenText(box) };
                open.AddToClassList("btn"); open.AddToClassList("btn--vert"); open.AddToClassList("btq-open");
                open.AddToClassList(UiSounds.SilentClass); // le son de paiement suffit
                bool canOpen = Shop.CanOpen(box);
                open.SetEnabled(canOpen);
                buttons.Add(open);
            }
            var content = new Button(() => Open(box)) { text = "Contenu et taux" };
            content.AddToClassList("btn"); content.AddToClassList("btn--sombre"); content.AddToClassList("btq-content-btn");
            buttons.Add(content);
            card.Add(buttons);
            if (!locked && !Shop.CanOpen(box)) card.Add(ShopUi.NewLabel(MissingText(box), "missing btq-missing"));

            // 5. Collection complète / verrou des Boîtes Animaux.
            if (Shop.IsComplete(box)) card.Add(ShopUi.NewLabel("Collection complète !", "complete-banner btq-complete"));
            if (locked)
            {
                var veil = new VisualElement { pickingMode = PickingMode.Ignore };
                veil.AddToClassList("btq-lock");
                veil.Add(Lock());
                veil.Add(ShopUi.NewLabel("Débloquée après ta première renaissance", "btq-lock__texte"));
                card.Add(veil);
                content.BringToFront(); // « Contenu et taux » reste cliquable au-dessus du voile
            }
            return card;
        }

        /// Le cadenas, dessiné en USS (anse + corps), aucune image.
        public static VisualElement Lock()
        {
            var l = new VisualElement { pickingMode = PickingMode.Ignore };
            l.AddToClassList("cadenas");
            var anse = new VisualElement { pickingMode = PickingMode.Ignore };
            anse.AddToClassList("cadenas__anse");
            var corps = new VisualElement { pickingMode = PickingMode.Ignore };
            corps.AddToClassList("cadenas__corps");
            l.Add(anse);
            l.Add(corps);
            return l;
        }

        static string OpenText(LootBox box) =>
            box.currency == BoxCurrency.Diamants ? $"Ouvrir — {box.price} ◆" : $"Ouvrir — {ShopUi.Thousands(box.price)}";

        static string MissingText(LootBox box) =>
            box.currency == BoxCurrency.Diamants ? $"Il te manque {Shop.MissingAmount(box)} ◆" : $"Il te manque {ShopUi.Thousands(Shop.MissingAmount(box))} pièces";

        static VisualElement PriceRow(LootBox box)
        {
            var row = new VisualElement { pickingMode = PickingMode.Ignore };
            row.AddToClassList("price-row");
            row.Add(ShopUi.NewLabel(ShopUi.Thousands(box.price), "price-text"));
            row.Add(Icon(box.currency == BoxCurrency.Diamants ? "diamant diamant--prix" : "coin coin--small"));
            return row;
        }

        static VisualElement ProgressBar(int found, int total)
        {
            var bar = new VisualElement { pickingMode = PickingMode.Ignore };
            bar.AddToClassList("progress-bar");
            var fill = new VisualElement { pickingMode = PickingMode.Ignore };
            fill.AddToClassList("progress-fill");
            fill.style.width = Length.Percent(total > 0 ? 100f * found / total : 0f);
            bar.Add(fill);
            return bar;
        }

        /// L'image d'une boîte, ou (images de la partie 2 pas encore là) une boîte dessinée
        /// de la couleur de son modèle, avec un ruban.
        static VisualElement BoxImage(LootBox box, string className)
        {
            var image = new VisualElement { pickingMode = PickingMode.Ignore };
            image.AddToClassList(className);
            if (box.icon != null) { image.style.backgroundImage = new StyleBackground(box.icon); return image; }
            var drawn = new VisualElement { pickingMode = PickingMode.Ignore };
            drawn.AddToClassList("boite-dessin");
            drawn.style.backgroundColor = box.openingTint;
            var ribbon = new VisualElement { pickingMode = PickingMode.Ignore };
            ribbon.AddToClassList("boite-dessin__ruban");
            drawn.Add(ribbon);
            var lid = new VisualElement { pickingMode = PickingMode.Ignore };
            lid.AddToClassList("boite-dessin__couvercle");
            lid.style.backgroundColor = Color.Lerp(box.openingTint, Color.black, 0.2f);
            drawn.Add(lid);
            image.Add(drawn);
            return image;
        }

        /// La barre segmentée des taux (360 × 18, un segment par rareté de poids > 0, largeur
        /// proportionnelle au poids) et la ligne des pourcentages colorés. Tout est lu dans les
        /// données de la boîte (Pirates : 60 % · 30 % · 10 %).
        static VisualElement RatesBar(LootBox box)
        {
            var wrap = new VisualElement { pickingMode = PickingMode.Ignore };
            wrap.AddToClassList("rates-bar-wrap");
            var bar = new VisualElement { pickingMode = PickingMode.Ignore };
            bar.AddToClassList("rates-bar");
            var line = new VisualElement { pickingMode = PickingMode.Ignore };
            line.AddToClassList("rates-line");
            var rarities = Shop.RaritiesIn(box);
            for (int i = 0; i < rarities.Count; i++)
            {
                var r = rarities[i];
                var seg = new VisualElement { pickingMode = PickingMode.Ignore };
                seg.AddToClassList("rates-seg");
                seg.style.flexGrow = Shop.WeightOf(box, r);
                seg.style.backgroundColor = r.color;
                bar.Add(seg);
                if (i > 0) line.Add(ShopUi.NewLabel("·", "rates-line__sep"));
                var pct = ShopUi.NewLabel(ShopUi.Percent(Shop.RarityChance(box, r)), "rates-line__pct");
                ShopUi.ColorText(pct, r);
                line.Add(pct);
            }
            wrap.Add(bar);
            wrap.Add(line);
            return wrap;
        }

        // =====================================================================
        // La sous-page « Contenu et taux » (§15.4)
        // =====================================================================

        void BuildContentPage(LootBox box)
        {
            var back = new Button(() => { contentBox = null; Refresh(); }) { text = "← Retour" };
            back.AddToClassList("btn"); back.AddToClassList("btn--sombre"); back.AddToClassList("btq-back"); back.AddToClassList(UiSounds.TabClass);
            body.Add(back);

            var split = new VisualElement();
            split.AddToClassList("btq-content");
            var left = new VisualElement();
            left.AddToClassList("btq-content-left");
            left.Add(BoxImage(box, "btq-content-image"));
            left.Add(ShopUi.NewLabel(box.displayName, "btq-card-name"));
            left.Add(PriceRow(box));
            left.Add(FullRatesTable(box));
            left.Add(ShopUi.NewLabel("Chaque ouverture donne 1 objet. Les chances sont toujours les mêmes, à chaque ouverture.", "rates-note"));
            left.Add(ShopUi.NewLabel(OutfitBonusLine(box), "rates-note"));
            if (Shop.IsLocked(box))
            {
                left.Add(ShopUi.NewLabel("Débloquée après ta première renaissance", "missing"));
            }
            else
            {
                var open = new Button(() => OpenRequested?.Invoke(box)) { text = OpenText(box) };
                open.AddToClassList("btn"); open.AddToClassList("btn--vert"); open.AddToClassList("btq-open");
                open.AddToClassList(UiSounds.SilentClass);
                open.SetEnabled(Shop.CanOpen(box));
                left.Add(open);
                if (!Shop.CanOpen(box)) left.Add(ShopUi.NewLabel(MissingText(box), "missing"));
            }
            split.Add(left);

            var right = new ScrollView();
            right.AddToClassList("shop-scroll");
            right.AddToClassList("btq-content-right");
            PanelsController.SetupScroll(right);
            var cells = new VisualElement { pickingMode = PickingMode.Ignore };
            cells.AddToClassList("grid");
            var items = box.items.Where(i => i != null).ToList();
            for (int i = 0; i < items.Count; i++)
            {
                int position = i;
                VisualElement card = null;
                card = ShopUi.CollectionTile(items[i], () => detail.Open(items, position, card));
                cells.Add(card);
            }
            right.Add(cells);
            split.Add(right);
            body.Add(split);
        }

        /// v5 §4.5 : le bonus de tenue de chaque rareté de la boîte, lu dans le catalogue,
        /// SANS plafond (plus de « 50 % maximum »).
        static string OutfitBonusLine(LootBox box)
        {
            var parts = Shop.RaritiesIn(box).Select(r => $"{r.displayName} +{r.coinBonusPercent} %");
            return "Chaque objet équipé donne un bonus de pièces sur les trésors : " + string.Join(", ", parts) + ".";
        }

        /// Le tableau complet : rareté, chance, nombre d'objets, chance par objet (raretés de
        /// poids > 0 seulement). Tout est lu dans les données.
        static VisualElement FullRatesTable(LootBox box)
        {
            var table = new VisualElement { pickingMode = PickingMode.Ignore };
            table.AddToClassList("rates");
            table.Add(RateRow(null, "Rareté", "Chance", "Objets", "Par objet", header: true));
            foreach (var rarity in Shop.RaritiesIn(box))
            {
                int count = Shop.ItemsOfRarity(box, rarity.id).Count;
                double chance = Shop.RarityChance(box, rarity);
                table.Add(RateRow(rarity, rarity.displayName, ShopUi.Percent(chance), count.ToString(),
                                  count > 0 ? ShopUi.Percent(chance / count) : "—", header: false));
            }
            return table;
        }

        static VisualElement RateRow(RarityDef rarity, string name, string chance, string count, string each, bool header)
        {
            var row = new VisualElement { pickingMode = PickingMode.Ignore };
            row.AddToClassList("rate-row");
            if (header) row.AddToClassList("rate-row--header");
            var dotCell = new VisualElement { pickingMode = PickingMode.Ignore };
            dotCell.AddToClassList("rate-dot-cell");
            if (rarity != null)
            {
                var dot = new VisualElement { pickingMode = PickingMode.Ignore };
                dot.AddToClassList("rate-dot");
                dot.style.backgroundColor = rarity.color;
                dotCell.Add(dot);
            }
            row.Add(dotCell);
            var nameLabel = ShopUi.NewLabel(name, "rate-cell");
            nameLabel.AddToClassList("rate-name");
            if (rarity != null) ShopUi.ColorText(nameLabel, rarity);
            row.Add(nameLabel);
            foreach (var (text, cls) in new[] { (chance, "rate-chance"), (count, "rate-count"), (each, "rate-chance") })
            {
                var l = ShopUi.NewLabel(text, "rate-cell");
                l.AddToClassList(cls);
                row.Add(l);
            }
            return row;
        }

        // =====================================================================
        // L'onglet « Diamants » (§15.5) : packs AFFICHÉS, DÉSACTIVÉS, sans prix
        // =====================================================================

        void BuildPacks()
        {
            body.Add(ShopUi.NewLabel("Bientôt disponible. Pour l'instant, les diamants se gagnent uniquement en jouant : parfois, un trésor en contient !", "btq-packs-intro"));
            var grid = new VisualElement { pickingMode = PickingMode.Ignore };
            grid.AddToClassList("btq-packs");
            foreach (var pack in EconomyCatalog.Instance.DiamondPacks)
            {
                var card = new VisualElement();
                card.AddToClassList("btq-pack");
                var image = new VisualElement { pickingMode = PickingMode.Ignore };
                image.AddToClassList("btq-pack-image");
                if (pack.icon != null) image.style.backgroundImage = new StyleBackground(pack.icon);
                else image.Add(Icon("diamant diamant--grand"));
                card.Add(image);
                card.Add(ShopUi.NewLabel(pack.displayName, "btq-pack-name"));
                card.Add(ShopUi.NewLabel($"{pack.amount} ◆", "btq-pack-amount"));
                // Le bouton est TOUJOURS désactivé : aucun achat n'est possible (règle d'or v5).
                var soon = new Button { text = "Bientôt disponible" };
                soon.AddToClassList("btn"); soon.AddToClassList("btn--sombre"); soon.AddToClassList("btq-pack-btn");
                soon.SetEnabled(false);
                card.Add(soon);
                grid.Add(card);
            }
            body.Add(grid);
        }

        // =====================================================================
        // Outils de test
        // =====================================================================

        /// OUTIL DE TEST : choisit un onglet (0 Boîtes, 1 Pirates, 2 Diamants).
        public void DebugTab(int tab) { lastTab = (Tab)tab; contentBox = null; Refresh(); }
        /// Les textes visibles de la page (pour les tests).
        public string Text => string.Join(" | ", page.Query<Label>().ToList().Where(l => l.resolvedStyle.display != DisplayStyle.None).Select(l => l.text))
                              + " | " + string.Join(" | ", page.Query<Button>().ToList().Select(b => b.text + (b.enabledSelf ? "" : "(grisé)")));
    }
}
