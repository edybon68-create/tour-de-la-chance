using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace TourDeLaChance
{
    /// <summary>
    /// La pop-up « Boutique » (spec v5 §15, retouchée en v5.1). UNE SEULE LISTE qui défile, en
    /// quatre sections qui se suivent, chacune sous un titre de séparation décoré :
    ///   1. les Boîtes classiques (payées en pièces) ;
    ///   2. les Boîtes de pirates (payées en diamants) et la note « D'où viennent les diamants ? » ;
    ///   3. les Boîtes Animaux (voile et cadenas avant la renaissance, contenu consultable) ;
    ///   4. les Diamants : packs AFFICHÉS mais DÉSACTIVÉS « Bientôt disponible », sans prix (aucun
    ///      paiement n'existe dans le jeu ; la section disparaît si showDiamondPacks est faux).
    /// Les onglets du haut sont des RACCOURCIS : un clic fait défiler en douceur jusqu'à la
    /// section (0,3 s, ralenti en fin de course) ; quand on fait défiler à la main, l'onglet de
    /// la section visible s'allume. Chaque carte montre l'image (avec un rayonnement), la
    /// collection, les TAUX toujours visibles (un objet par rareté avec le taux de la rareté,
    /// lu dans les données de la boîte), la pilule verte « Ouvrir [pièce] 250 » et « Contenu et
    /// taux » (sous-page avec le tableau complet et la grille des objets).
    ///
    /// Ce n'est pas un MonoBehaviour : PanelsController le crée. Il AFFICHE et TRANSMET les
    /// clics ; ouvrir une boîte est décidé par la classe Shop.
    /// </summary>
    public class BoutiquePage : IDisposable
    {
        /// Les sections de la liste, dans l'ordre (un onglet par section).
        public enum Tab { Boites, Pirates, Animaux, Diamants }

        /// La dernière section regardée, retenue pendant la session (jamais sauvegardée).
        static Tab lastTab = Tab.Boites;

        const float ScrollTime = 0.3f;      // le défilement en douceur d'un clic sur un onglet
        const float SectionMargin = 20f;    // le titre de section s'arrête sous le petit fondu du haut

        readonly VisualElement page, tabsRow, balanceRow, body, contentHost;
        readonly ScrollView scroll;
        readonly ItemDetailView detail;
        readonly Dictionary<Tab, Button> tabButtons = new Dictionary<Tab, Button>();
        readonly Dictionary<Tab, VisualElement> sections = new Dictionary<Tab, VisualElement>();
        LootBox contentBox; // la boîte de la sous-page « Contenu et taux » (null = la liste)

        // Le défilement en douceur : de scrollFrom à la section scrollTarget en ScrollTime.
        Tab? scrollTarget;   // la section visée (null = pas d'animation)
        bool jumpPending;    // à l'ouverture : sauter (sans animation) à la dernière section
        bool animStarted;
        float scrollFrom, scrollTime;

        /// Clic sur « Ouvrir » : PanelsController transmet à Shop.TryOpenBox.
        public event Action<LootBox> OpenRequested;

        /// « Voir dans l'inventaire » (fiche agrandie d'un objet de « Contenu et taux »).
        public event Action<ShopItem> ShowInInventoryRequested;

        public Tab CurrentTab => lastTab;
        public LootBox ContentBox => contentBox;
        public ItemDetailView Detail => detail;
        /// Pour les tests : la position de la liste et « une animation est en cours ».
        public float ScrollY => scroll.scrollOffset.y;
        public bool IsScrolling => scrollTarget.HasValue;

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

            // La liste unique (créée une fois : on garde sa position quand on la redessine).
            scroll = new ScrollView();
            scroll.AddToClassList("shop-scroll");
            scroll.AddToClassList("btq-scroll");
            PanelsController.SetupScroll(scroll);
            scroll.verticalScroller.valueChanged += _ => OnScrolled();
            // Après un redessin, les sections ne sont placées qu'à la mise en page suivante :
            // on revérifie alors quel onglet allumer.
            scroll.contentContainer.RegisterCallback<GeometryChangedEvent>(_ => OnScrolled());
            // La molette pendant une animation : la main reprend la main.
            scroll.RegisterCallback<WheelEvent>(_ => scrollTarget = null, TrickleDown.TrickleDown);
            body.Add(scroll);
            contentHost = new VisualElement();
            contentHost.AddToClassList("btq-content-host");
            body.Add(contentHost);

            detail = new ItemDetailView(page, horse);
            detail.ShowBoxRequested += box => Open(box);
            // v6 (QA n°34) : « Voir dans l'inventaire » depuis la fiche de « Contenu et taux » :
            // on transmet à PanelsController, qui ouvre l'Inventaire sur l'objet (comme l'Index).
            detail.ShowInInventoryRequested += item => ShowInInventoryRequested?.Invoke(item);
        }

        public void Dispose() { }

        /// Ouvre la Boutique (sur la dernière section regardée) ou directement sur « Contenu et
        /// taux » d'une boîte.
        public void Open(LootBox content = null)
        {
            detail.Close();
            contentBox = content;
            scrollTarget = null;
            jumpPending = content == null;
            Refresh();
        }

        public bool HandleEscape()
        {
            if (detail.HandleEscape()) return true;
            if (contentBox != null) { contentBox = null; jumpPending = true; Refresh(); return true; }
            return false;
        }

        public void Tick(float dt)
        {
            detail.Tick(dt);
            TickScroll(dt);
        }

        public void Refresh()
        {
            BuildTabs();
            BuildBalance();
            bool content = contentBox != null;
            scroll.style.display = content ? DisplayStyle.None : DisplayStyle.Flex;
            contentHost.style.display = content ? DisplayStyle.Flex : DisplayStyle.None;
            contentHost.Clear();
            if (content) BuildContentPage(contentBox);
            else BuildSections();
            ButtonStyler.Apply(page);
        }

        /// Les quatre sections de la liste, chacune sous son titre décoré.
        void BuildSections()
        {
            scroll.Clear();
            sections.Clear();
            AddSection(Tab.Boites, "Boîtes classiques", PopupDecor.Deco.Piece, Shop.Boxes.Where(b => b.family == BoxFamily.Classique));
            var pirates = AddSection(Tab.Pirates, "Boîtes de pirates", PopupDecor.Deco.Couronne, Shop.Boxes.Where(b => b.family == BoxFamily.Pirates));
            var note = new VisualElement { pickingMode = PickingMode.Ignore };
            note.AddToClassList("btq-note");
            note.Add(ShopUi.NewLabel("D'où viennent les diamants ?", "btq-note__titre"));
            note.Add(ShopUi.NewLabel(DiamondOriginText(), "btq-note__texte"));
            pirates.Add(note);
            AddSection(Tab.Animaux, "Boîtes Animaux", PopupDecor.Deco.Patte, Shop.Boxes.Where(b => b.family == BoxFamily.Animaux));
            if (EconomyCatalog.Instance.ShowDiamondPacks)
            {
                var packs = NewSection(Tab.Diamants, "Diamants", PopupDecor.Deco.Diamant);
                BuildPacks(packs);
            }
        }

        VisualElement NewSection(Tab tab, string title, PopupDecor.Deco deco)
        {
            var section = new VisualElement { pickingMode = PickingMode.Ignore };
            section.AddToClassList("btq-section");
            section.Add(PopupDecor.SectionTitle(title, deco));
            scroll.Add(section);
            sections[tab] = section;
            return section;
        }

        VisualElement AddSection(Tab tab, string title, PopupDecor.Deco deco, IEnumerable<LootBox> boxes)
        {
            var section = NewSection(tab, title, deco);
            var grid = new VisualElement { pickingMode = PickingMode.Ignore };
            grid.AddToClassList("btq-grid");
            foreach (var box in boxes) grid.Add(CompactCard(box));
            section.Add(grid);
            return section;
        }

        /// « Parfois, un trésor contient des diamants… (1 % à 5 %) » : chiffres lus dans le
        /// catalogue des raretés de coffres.
        static string DiamondOriginText()
        {
            var r = Upgrades.Catalog != null ? Upgrades.Catalog.ChestRarities : null;
            string range = r != null && r.Count > 0
                ? $" ({ShopUi.Number(r.Min(x => x.diamondChancePermille) / 10f)}\u00A0% à {ShopUi.Number(r.Max(x => x.diamondChancePermille) / 10f)}\u00A0%)" : "";
            return "Parfois, un trésor contient des diamants à la place des pièces : plus les coffres sont précieux, plus c'est fréquent" + range + ".";
        }

        // =====================================================================
        // Les onglets-raccourcis et le défilement en douceur
        // =====================================================================

        void BuildTabs()
        {
            tabsRow.Clear();
            tabButtons.Clear();
            bool packs = EconomyCatalog.Instance.ShowDiamondPacks;
            if (!packs && lastTab == Tab.Diamants) lastTab = Tab.Boites;
            AddTab(Tab.Boites, "Boîtes", "coin coin--small");
            AddTab(Tab.Pirates, "Pirates", "diamant diamant--petit");
            AddTab(Tab.Animaux, "Animaux", "coin coin--small");
            // Les diamants ne s'achètent pas : l'onglet reste grisé « Bientôt » (il montre
            // seulement la section qui l'explique).
            if (packs) AddTab(Tab.Diamants, "Diamants · Bientôt", "diamant diamant--petit").AddToClassList("v5-tab--bientot");
            tabsRow.style.display = contentBox != null ? DisplayStyle.None : DisplayStyle.Flex;
        }

        Button AddTab(Tab tab, string text, string iconClasses)
        {
            var b = new Button(() => ScrollTo(tab));
            b.AddToClassList("btn"); b.AddToClassList("btn--sombre"); b.AddToClassList("v5-tab"); b.AddToClassList(UiSounds.TabClass);
            var icon = new VisualElement { pickingMode = PickingMode.Ignore };
            foreach (var c in iconClasses.Split(' ')) icon.AddToClassList(c);
            b.Add(icon);
            b.Add(ShopUi.NewLabel(text, "v5-tab__texte"));
            tabsRow.Add(b);
            tabButtons[tab] = b;
            HighlightTab(lastTab);
            return b;
        }

        /// Allume l'onglet d'une section (et éteint les autres), sans rien reconstruire.
        void HighlightTab(Tab tab)
        {
            foreach (var kv in tabButtons) ShopUi.SetClass(kv.Value, "subtab--active", kv.Key == tab);
        }

        /// Clic sur un onglet : défile en douceur jusqu'à sa section (après être revenu de
        /// « Contenu et taux » si besoin).
        void ScrollTo(Tab tab)
        {
            if (!sections.ContainsKey(tab) && contentBox == null) return;
            lastTab = tab;
            if (contentBox != null) { contentBox = null; Refresh(); }
            HighlightTab(tab);
            scrollTarget = tab;
            animStarted = false;
            jumpPending = false;
        }

        /// La position (en haut de la liste) qui montre le début d'une section, sans dépasser
        /// le bas de la liste. NaN tant que Unity n'a pas encore placé les éléments.
        float TargetY(Tab tab)
        {
            if (!sections.TryGetValue(tab, out var section)) return float.NaN;
            float y = section.layout.y, content = scroll.contentContainer.layout.height, view = scroll.contentViewport.layout.height;
            if (float.IsNaN(y) || float.IsNaN(content) || float.IsNaN(view) || content <= 0f) return float.NaN;
            return Mathf.Clamp(y - SectionMargin, 0f, Mathf.Max(0f, content - view));
        }

        /// À chaque image : le saut d'ouverture, puis l'animation d'un clic d'onglet
        /// (ease-out : rapide au début, ralentie à l'arrivée).
        void TickScroll(float dt)
        {
            if (contentBox != null) return;
            if (jumpPending)
            {
                float y = TargetY(lastTab);
                if (float.IsNaN(y)) return; // la mise en page n'est pas encore faite
                jumpPending = false;
                scroll.scrollOffset = new Vector2(0f, y);
                HighlightTab(lastTab);
                return;
            }
            if (!scrollTarget.HasValue) return;
            float target = TargetY(scrollTarget.Value);
            if (float.IsNaN(target)) return;
            if (!animStarted) { animStarted = true; scrollFrom = scroll.scrollOffset.y; scrollTime = 0f; }
            scrollTime += dt;
            float k = Mathf.Clamp01(scrollTime / ScrollTime);
            scroll.scrollOffset = new Vector2(0f, Mathf.Lerp(scrollFrom, target, Ease.Out(k)));
            if (k >= 1f) scrollTarget = null;
        }

        /// La liste a bougé : si c'est à la main (pas pendant l'animation d'un clic), on allume
        /// l'onglet de la section visible : la dernière dont le début est passé sous le haut de
        /// la liste (ou la dernière section quand on est tout en bas).
        void OnScrolled()
        {
            if (scrollTarget.HasValue || jumpPending || contentBox != null || sections.Count == 0) return;
            // Des sections pas encore placées (juste recréées) : on attendra la mise en page.
            if (sections.Values.Any(sec => float.IsNaN(sec.layout.height) || sec.layout.height <= 0f)) return;
            float y = scroll.scrollOffset.y;
            float max = scroll.contentContainer.layout.height - scroll.contentViewport.layout.height;
            Tab visible = Tab.Boites;
            foreach (var kv in sections)
                if (kv.Value.layout.y - SectionMargin <= y + 40f) visible = kv.Key;
            if (max > 1f && y >= max - 1f) visible = sections.Keys.Max();
            if (visible == lastTab) return;
            lastTab = visible;
            HighlightTab(visible);
        }

        /// Le solde à droite des onglets : pièces et diamants (la liste montre les deux monnaies).
        void BuildBalance()
        {
            balanceRow.Clear();
            balanceRow.Add(ShopUi.NewLabel(ShopUi.Thousands(Wallet.Coins), "btq-balance__texte"));
            balanceRow.Add(Icon("coin coin--small"));
            balanceRow.Add(ShopUi.NewLabel(Diamonds.Count.ToString(), "btq-balance__texte btq-balance__texte--diamants"));
            balanceRow.Add(Icon("diamant diamant--petit"));
        }

        static VisualElement Icon(string classes)
        {
            var e = new VisualElement { pickingMode = PickingMode.Ignore };
            foreach (var c in classes.Split(' ')) e.AddToClassList(c);
            return e;
        }

        // =====================================================================
        // Les cartes (v5.1 : fond en dégradé vif, rayonnement, taux en badges)
        // =====================================================================

        VisualElement CompactCard(LootBox box)
        {
            var card = new VisualElement();
            card.AddToClassList("btq-card");
            card.AddToClassList("carte");
            card.AddToClassList(box.family switch
            {
                BoxFamily.Pirates => "carte--bleue",
                BoxFamily.Animaux => "carte--verte",
                _ => "carte--rose"
            });
            bool locked = Shop.IsLocked(box);

            // 1. Le titre, blanc contouré, centré.
            card.Add(ShopUi.NewLabel(box.displayName, "carte__titre"));

            // 2. L'illustration (sur son rayonnement) et la courte description à côté.
            var middle = new VisualElement { pickingMode = PickingMode.Ignore };
            middle.AddToClassList("carte__milieu");
            var illu = new VisualElement { pickingMode = PickingMode.Ignore };
            illu.AddToClassList("carte__illu");
            illu.Add(PopupDecor.CardRays());
            illu.Add(BoxImage(box, "btq-card-image"));
            middle.Add(illu);
            var desc = new VisualElement { pickingMode = PickingMode.Ignore };
            desc.AddToClassList("carte__desc");
            int found = Shop.FoundCount(box), total = box.items.Count;
            desc.Add(ShopUi.NewLabel($"{total} objets à collectionner !", "carte__texte"));
            desc.Add(ShopUi.NewLabel($"Collection : {found} / {total}", "progress-text"));
            desc.Add(ProgressBar(found, total));
            middle.Add(desc);
            card.Add(middle);

            // 3. Les taux, toujours visibles : un objet par rareté et son taux en badge.
            card.Add(PopupDecor.RateBadges(box));

            // 4. La pilule de prix « Ouvrir » et « Contenu et taux ».
            var buttons = new VisualElement();
            buttons.AddToClassList("btq-card-buttons");
            if (!locked)
            {
                var open = PopupDecor.PriceButton(() => OpenRequested?.Invoke(box), "Ouvrir", box.price, box.currency == BoxCurrency.Diamants);
                open.AddToClassList("btq-open");
                open.AddToClassList(UiSounds.SilentClass); // le son de paiement suffit
                open.SetEnabled(Shop.CanOpen(box));
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
                // v6 (QA n°37) : avant, le cadenas était au CENTRE de la carte et cachait
                // « Collection : 0 / 16 ». Maintenant il est posé sur l'image de la boîte, et la
                // phrase « Débloquée après… » s'écrit sous la barre de collection.
                // Le voile sombre est placé EN PREMIER (index 0) : il assombrit le fond de la carte
                // mais reste sous les textes, qui restent lisibles, et sous « Contenu et taux »,
                // qui reste cliquable. (BringToFront changerait l'ordre de la mise en page.)
                var veil = new VisualElement { pickingMode = PickingMode.Ignore };
                veil.AddToClassList("btq-lock");
                card.Insert(0, veil);
                var padlock = Lock();
                padlock.AddToClassList("btq-lock__cadenas");
                illu.Add(padlock);
                desc.Add(ShopUi.NewLabel("Débloquée après ta première renaissance", "btq-lock__texte"));
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

        static string MissingText(LootBox box) =>
            box.currency == BoxCurrency.Diamants ? $"Il te manque {Shop.MissingAmount(box)} {ShopUi.Diamant}" : $"Il te manque {ShopUi.Thousands(Shop.MissingAmount(box))} pièces";

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

        // =====================================================================
        // La sous-page « Contenu et taux » (§15.4)
        // =====================================================================

        void BuildContentPage(LootBox box)
        {
            var back = new Button(() => { contentBox = null; jumpPending = true; Refresh(); }) { text = "← Retour" };
            back.AddToClassList("btn"); back.AddToClassList("btn--sombre"); back.AddToClassList("btq-back"); back.AddToClassList(UiSounds.TabClass);
            contentHost.Add(back);

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
                var open = PopupDecor.PriceButton(() => OpenRequested?.Invoke(box), "Ouvrir", box.price, box.currency == BoxCurrency.Diamants);
                open.AddToClassList("btq-open");
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
            contentHost.Add(split);
        }

        /// v5 §4.5 : le bonus de tenue de chaque rareté de la boîte, lu dans le catalogue,
        /// SANS plafond (plus de « 50 % maximum »).
        static string OutfitBonusLine(LootBox box)
        {
            var parts = Shop.RaritiesIn(box).Select(r => $"{r.displayName} +{r.coinBonusPercent}\u00A0%");
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

        void BuildPacks(VisualElement section)
        {
            section.Add(ShopUi.NewLabel("Bientôt disponible. Pour l'instant, les diamants se gagnent uniquement en jouant : parfois, un trésor en contient !", "btq-packs-intro"));
            var grid = new VisualElement { pickingMode = PickingMode.Ignore };
            grid.AddToClassList("btq-packs");
            foreach (var pack in EconomyCatalog.Instance.DiamondPacks)
            {
                var card = new VisualElement();
                card.AddToClassList("btq-pack");
                card.AddToClassList("carte");
                card.AddToClassList("carte--violette");
                var image = new VisualElement { pickingMode = PickingMode.Ignore };
                image.AddToClassList("btq-pack-image");
                image.Add(PopupDecor.CardRays());
                var picture = new VisualElement { pickingMode = PickingMode.Ignore };
                picture.AddToClassList("btq-pack-picture");
                if (pack.icon != null) picture.style.backgroundImage = new StyleBackground(pack.icon);
                else picture.Add(Icon("diamant diamant--grand"));
                image.Add(picture);
                card.Add(image);
                card.Add(ShopUi.NewLabel(pack.displayName, "btq-pack-name"));
                card.Add(ShopUi.NewLabel($"{pack.amount} {ShopUi.Diamant}", "btq-pack-amount"));
                // Le bouton est TOUJOURS désactivé : aucun achat n'est possible (règle d'or v5).
                var soon = new Button { text = "Bientôt disponible" };
                soon.AddToClassList("btn"); soon.AddToClassList("btn--sombre"); soon.AddToClassList("btq-pack-btn");
                soon.SetEnabled(false);
                card.Add(soon);
                grid.Add(card);
            }
            section.Add(grid);
        }

        // =====================================================================
        // Outils de test
        // =====================================================================

        /// OUTIL DE TEST : clic sur un onglet (0 Boîtes, 1 Pirates, 2 Animaux, 3 Diamants) :
        /// la liste défile en douceur jusqu'à la section.
        public void DebugTab(int tab) => ScrollTo((Tab)tab);
        /// OUTIL DE TEST : fait défiler la liste « à la main » jusqu'à la position y.
        public void DebugScrollTo(float y) { scrollTarget = null; scroll.scrollOffset = new Vector2(0f, y); }
        /// Les textes visibles de la page (pour les tests).
        public string Text => string.Join(" | ", page.Query<Label>().ToList().Where(l => l.resolvedStyle.display != DisplayStyle.None).Select(l => l.text))
                              + " | " + string.Join(" | ", page.Query<Button>().ToList().Select(b => PopupDecor.TextOf(b) + (b.enabledSelf ? "" : "(grisé)")));
    }
}
