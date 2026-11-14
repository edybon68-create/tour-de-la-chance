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
    /// v6 : 4 onglets sur toute la largeur (Classiques · Pirates · Animaux · Échanges, §5.1) ; la
    /// section ÉCHANGES remplace « Diamants · Bientôt » : on donne des diamants contre des pièces
    /// (§5.4, règle dans CoinExchange) ; les packs désactivés viennent dessous seulement si
    /// showDiamondPacks est vrai ; chaque carte dit sa récompense de collection (§6.3) ; la
    /// Boutique peut s'ouvrir sur « Contenu et taux » avec la fiche d'un objet déjà ouverte.
    ///
    /// Ce n'est pas un MonoBehaviour : PanelsController le crée. Il AFFICHE et TRANSMET les
    /// clics ; ouvrir une boîte est décidé par la classe Shop.
    /// </summary>
    public class BoutiquePage : IDisposable
    {
        /// Les sections de la liste, dans l'ordre (un onglet par section).
        public enum Tab { Boites, Pirates, Animaux, Echanges }

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
        /// v7 (point 15) : la prochaine ouverture se fera sur cette section (ex. clic sur le
        /// compteur de diamants du HUD → « Diamants »).
        public static void OpenNextOn(Tab tab) => lastTab = tab;

        /// v7 : fait défiler la Boutique déjà ouverte jusqu'à une section (comme un clic d'onglet).
        public void ShowTab(Tab tab) => ScrollTo(tab);

        public void Open(LootBox content = null, ShopItem showItem = null)
        {
            detail.Close();
            CloseExchangeConfirm();
            contentBox = content;
            scrollTarget = null;
            jumpPending = content == null;
            Refresh();
            // v6 §6.1 : « Voir dans la Boutique » : la fiche de l'objet est déjà ouverte (3D visible).
            if (content != null && showItem != null)
            {
                var items = content.items.Where(i => i != null).ToList();
                int at = items.IndexOf(showItem);
                if (at >= 0) detail.Open(items, at, null);
            }
        }

        public bool HandleEscape()
        {
            if (exchangeConfirm != null) { CloseExchangeConfirm(); return true; }
            if (detail.HandleEscape()) return true;
            if (contentBox != null) { contentBox = null; jumpPending = true; Refresh(); return true; }
            return false;
        }

        public void Tick(float dt)
        {
            detail.Tick(dt);
            TickScroll(dt);
            TickBoxes();
            TickFlyingCoins();
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

        /// Les quatre sections de la liste, chacune sous son titre « autocollant » (v7 §17.3.2).
        void BuildSections()
        {
            scroll.Clear();
            sections.Clear();
            animatedBoxes.Clear();
            AddSection(Tab.Boites, "Boîtes classiques", Shop.Boxes.Where(b => b.family == BoxFamily.Classique), icon: UiImages.CategorieClassiques);
            // v7 : le paragraphe « D'où viennent les diamants ? » quitte la liste : il est derrière
            // le bouton « i » du titre des Boîtes de pirates.
            AddSection(Tab.Pirates, "Boîtes de pirates", Shop.Boxes.Where(b => b.family == BoxFamily.Pirates), withInfo: true, icon: UiImages.CategoriePirates);
            AddSection(Tab.Animaux, "Boîtes animaux", Shop.Boxes.Where(b => b.family == BoxFamily.Animaux), icon: UiImages.CategorieAnimaux);
            // v6 §5.4 : la section Échanges (diamants → pièces), puis, SEULEMENT si on les montre,
            // les packs de diamants désactivés « Bientôt disponible » dessous.
            var exchange = NewSection(Tab.Echanges, "Échanges", null, RatePill());
            BuildExchange(exchange);
            if (EconomyCatalog.Instance.ShowDiamondPacks) BuildPacks();
        }

        /// Une section : le titre « autocollant du Casier » aligné à gauche (icône 64 px, titre
        /// 44 px), un élément facultatif à droite du titre, puis un filet.
        VisualElement NewSection(Tab tab, string title, Sprite icon, VisualElement extra = null)
        {
            var section = new VisualElement { pickingMode = PickingMode.Ignore };
            section.AddToClassList("btq-section");
            section.AddToClassList("bq7-section");
            section.Add(SectionTitle7(title, icon, extra));
            scroll.Add(section);
            sections[tab] = section;
            return section;
        }

        /// Le titre de section v7 (§17.3.2). Sans image : un diamant (sections Diamants).
        static VisualElement SectionTitle7(string title, Sprite icon, VisualElement extra)
        {
            var row = new VisualElement { pickingMode = PickingMode.Ignore };
            row.AddToClassList("bq7-titre");
            var picture = new VisualElement { pickingMode = PickingMode.Ignore };
            picture.AddToClassList("bq7-titre__icone");
            if (icon != null) picture.style.backgroundImage = new StyleBackground(icon);
            else picture.AddToClassList("diamant");
            row.Add(picture);
            row.Add(ShopUi.NewLabel(title, "bq7-titre__texte"));
            if (extra != null) row.Add(extra);
            var wrap = new VisualElement { pickingMode = PickingMode.Ignore };
            wrap.Add(row);
            var line = new VisualElement { pickingMode = PickingMode.Ignore };
            line.AddToClassList("bq7-filet");
            wrap.Add(line);
            return wrap;
        }

        /// v7 : l'icône de la catégorie (categorie_classiques…) ; tant qu'elle manque, la 1re boîte.
        VisualElement AddSection(Tab tab, string title, IEnumerable<LootBox> boxes, bool withInfo = false, Sprite icon = null)
        {
            var list = boxes.ToList();
            if (icon == null) icon = list.Select(b => b.icon).FirstOrDefault(i => i != null);
            VisualElement extra = null;
            if (withInfo)
            {
                var info = InfoButton(OpenDiamondNote);
                info.AddToClassList("bq7-titre__info");
                extra = info;
            }
            var section = NewSection(tab, title, icon, extra);
            var grid = new VisualElement { pickingMode = PickingMode.Ignore };
            grid.AddToClassList("btq-grid");
            grid.AddToClassList("bq7-grille");
            foreach (var box in list) grid.Add(CompactCard(box));
            section.Add(grid);
            return section;
        }

        /// Le bouton rond « i » (le même que L'Ascension) : 56 × 56, « i » blanc.
        static Button InfoButton(Action onClick)
        {
            var info = new Button(onClick);
            foreach (var c in new[] { "btn", "bulle", "bulle-petit", "bulle-petit--ardoise", "bq7-info" }) info.AddToClassList(c);
            info.Add(ShopUi.NewLabel("i", "bq7-info__texte"));
            return info;
        }

        /// À droite du titre « Échanges » : le taux, en pilule (« 1 ◆ = 200 ● »), lu dans les données.
        static VisualElement RatePill()
        {
            int rate = EconomyCatalog.Instance.ExchangeBaseRate; // v7 §1.6 : lu directement
            var pill = new VisualElement { pickingMode = PickingMode.Ignore };
            pill.AddToClassList("bq7-taux");
            pill.Add(ShopUi.NewLabel("1", "bq7-taux__texte"));
            pill.Add(Icon("diamant bq7-taux__icone"));
            pill.Add(ShopUi.NewLabel($"= {ShopUi.Thousands(rate)}", "bq7-taux__texte"));
            pill.Add(Icon("coin bq7-taux__icone"));
            return pill;
        }

        /// La petite fenêtre « D'où viennent les diamants ? » (le paragraphe d'avant, inchangé).
        void OpenDiamondNote()
        {
            CloseExchangeConfirm();
            exchangeConfirm = new VisualElement();
            exchangeConfirm.AddToClassList("v5-confirm");
            var box = new VisualElement();
            box.AddToClassList("v5-confirm-box");
            box.Add(ShopUi.NewLabel("D'où viennent les diamants ?", "btq-note__titre"));
            box.Add(ShopUi.NewLabel(DiamondOriginText(), "v5-confirm-text"));
            var buttons = new VisualElement();
            buttons.AddToClassList("v5-confirm-buttons");
            var ok = new Button(CloseExchangeConfirm) { text = "OK" };
            ok.AddToClassList("btn"); ok.AddToClassList("btn--vert"); ok.AddToClassList("v5-confirm-btn");
            buttons.Add(ok);
            box.Add(buttons);
            exchangeConfirm.Add(box);
            page.Add(exchangeConfirm);
            ButtonStyler.Apply(exchangeConfirm);
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
            // v6 §5.1 : 4 onglets de même largeur, qui remplissent la place jusqu'aux monnaies.
            AddTab(Tab.Boites, "Classiques", "coin coin--small");
            AddTab(Tab.Pirates, "Pirates", "diamant diamant--petit");
            AddTab(Tab.Animaux, "Animaux", "coin coin--small");
            AddTab(Tab.Echanges, "Diamants", "diamant diamant--petit"); // v7 §17.3.1 : échanges ET packs
            // Le dernier onglet colle aux monnaies (24 px de marge portés par les monnaies).
            if (tabsRow.childCount > 0) tabsRow[tabsRow.childCount - 1].style.marginRight = 0;
            tabsRow.style.display = contentBox != null ? DisplayStyle.None : DisplayStyle.Flex;
            // QA v7 p2 n°10 : sur « Contenu et taux », plus de solde seul sur sa ligne (le HUD le montre).
            balanceRow.style.display = contentBox != null ? DisplayStyle.None : DisplayStyle.Flex;
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

        // v7 (§17.3.3) : la carte d'une boîte ressemble à une case du Casier en grand. La boîte
        // en héros, la jauge de collection, les aperçus en CASES DE RARETÉ (la couleur dit la
        // rareté, le % reste dessous : règle d'or, les taux sont toujours visibles), UN gros
        // bouton vert. « Contenu et taux » est derrière le bouton « i ».

        /// Les boîtes qui flottent et leurs rayons qui tournent (animés dans Tick).
        readonly List<(VisualElement image, RaysElement rays, float phase)> animatedBoxes = new List<(VisualElement, RaysElement, float)>();

        VisualElement CompactCard(LootBox box)
        {
            var card = new VisualElement();
            card.AddToClassList("btq-card");
            card.AddToClassList("bq7-carte");
            card.AddToClassList("carte");
            card.AddToClassList(box.family switch
            {
                BoxFamily.Pirates => "carte--bleue",
                BoxFamily.Animaux => "carte--verte",
                _ => "carte--rose"
            });
            bool locked = Shop.IsLocked(box);
            if (locked) card.AddToClassList("bq7-carte--verrou");

            // 1. Le haut : la boîte (170 px) sur sa lueur et ses rayons, à droite le titre, la
            //    jauge et la récompense de collection.
            var top = new VisualElement { pickingMode = PickingMode.Ignore };
            top.AddToClassList("bq7-haut");
            var hero = new VisualElement { pickingMode = PickingMode.Ignore };
            hero.AddToClassList("bq7-heros");
            var glow = new VisualElement { pickingMode = PickingMode.Ignore };
            glow.AddToClassList("bq7-heros__lueur");
            glow.style.backgroundImage = new StyleBackground(UiFxTextures.Halo);
            hero.Add(glow);
            var rays = new RaysElement();
            rays.AddToClassList("bq7-heros__rayons");
            rays.Configure(10, new Color(1f, 1f, 1f, 0.30f));
            hero.Add(rays);
            var image = BoxImage(box, "bq7-heros__boite");
            hero.Add(image);
            animatedBoxes.Add((image, rays, animatedBoxes.Count * 0.7f));
            if (locked)
            {
                var padlock = Lock();
                padlock.AddToClassList("bq7-cadenas");
                hero.Add(padlock);
            }
            top.Add(hero);

            // QA v7 p2 n°2 : le titre passe EN HAUT de la carte, sur toute la largeur, sur UNE
            // ligne : les deux cartes d'une rangée gardent la même mise en page.
            card.Insert(0, ShopUi.NewLabel(box.displayName, "bq7-titre-carte"));
            var right = new VisualElement { pickingMode = PickingMode.Ignore };
            right.AddToClassList("bq7-droite");
            int found = Shop.FoundCount(box), total = box.items.Count;
            right.Add(Gauge(found, total));
            int reward = CollectionRewards.RewardOf(box);
            if (reward > 0) right.Add(RewardPill(reward, CollectionRewards.Received(box)));
            top.Add(right);
            card.Add(top);

            // Le bouton « i » : il ouvre « Contenu et taux » (la page détaillée, inchangée).
            var info = InfoButton(() => Open(box));
            info.AddToClassList("bq7-carte__info");
            card.Add(info);

            // 2. Les aperçus en cases de rareté, avec leur % (taux de la RARETÉ, lu dans la boîte).
            card.Add(PreviewCases(box));

            // 3. UN seul gros bouton : « Ouvrir ● 250 » (gris s'il n'y a pas assez), ou verrouillé.
            Button open;
            if (locked)
            {
                open = new Button();
                open.AddToClassList("bq7-ouvrir--verrou");
                var rebirth = new VisualElement { pickingMode = PickingMode.Ignore };
                rebirth.AddToClassList("bq7-ouvrir__renaissance");
                open.Add(rebirth);
                open.Add(ShopUi.NewLabel("Après la Renaissance", "bulle-rect__texte bq7-ouvrir__petit"));
                open.SetEnabled(false);
            }
            else
            {
                // v7 (retour de l'auteur, point 15) : le PRIX est toujours sur le bouton
                // (« Ouvrir ◆ 5 ») ; s'il n'y a pas assez, le même bouton devient gris.
                open = new Button(() => OpenRequested?.Invoke(box));
                open.AddToClassList(UiSounds.SilentClass); // le son de paiement suffit
                open.Add(ShopUi.NewLabel("Ouvrir", "bulle-rect__texte bq7-ouvrir__verbe"));
                open.Add(Icon((box.currency == BoxCurrency.Diamants ? "diamant" : "coin") + " bq7-ouvrir__monnaie"));
                open.Add(ShopUi.NewLabel(ShopUi.Thousands(box.price), "bulle-rect__texte bq7-ouvrir__prix"));
                open.SetEnabled(Shop.CanOpen(box));
            }
            foreach (var c in new[] { "btn", "bulle", "bulle-rect", "bulle-rect--vert", "btq-open", "bq7-ouvrir" }) open.AddToClassList(c);
            card.Add(open);
            return card;
        }

        /// La jauge de collection 260 × 28, « 5 / 16 » écrit DANS la jauge.
        static VisualElement Gauge(int found, int total)
        {
            var gauge = new VisualElement { pickingMode = PickingMode.Ignore };
            gauge.AddToClassList("bq7-jauge");
            var fill = new VisualElement { pickingMode = PickingMode.Ignore };
            fill.AddToClassList("bq7-jauge__plein");
            fill.style.width = Length.Percent(total > 0 ? 100f * found / total : 0f);
            gauge.Add(fill);
            gauge.Add(ShopUi.NewLabel($"{found} / {total}", "bq7-jauge__texte progress-text"));
            return gauge;
        }

        /// La récompense de collection : médaille + diamant + nombre (pas de phrase). Reçue : verte avec une coche.
        static VisualElement RewardPill(int reward, bool received)
        {
            var pill = new VisualElement { pickingMode = PickingMode.Ignore };
            pill.AddToClassList("bq7-recompense");
            if (received)
            {
                pill.AddToClassList("bq7-recompense--recue");
                var check = new VisualElement { pickingMode = PickingMode.Ignore };
                check.AddToClassList("bq7-recompense__coche");
                check.Add(Icon("casier-coche__court"));
                check.Add(Icon("casier-coche__long"));
                pill.Add(check);
            }
            else pill.Add(Icon("bq7-recompense__medaille"));
            pill.Add(Icon("diamant bq7-recompense__diamant"));
            pill.Add(ShopUi.NewLabel(reward.ToString(), "bq7-recompense__texte"));
            return pill;
        }

        /// Les aperçus : une mini-case du Casier par rareté (Pirates : 2 par rareté, avec UN %
        /// par paire). Un Secret jamais obtenu reste un « ? » (les Pirates : toujours « ? »).
        static VisualElement PreviewCases(LootBox box)
        {
            var row = new VisualElement { pickingMode = PickingMode.Ignore };
            row.AddToClassList("bq7-cases");
            bool pairs = box.family == BoxFamily.Pirates;
            foreach (var rarity in Shop.RaritiesIn(box))
            {
                var group = new VisualElement { pickingMode = PickingMode.Ignore };
                group.AddToClassList("bq7-groupe");
                var frames = new VisualElement { pickingMode = PickingMode.Ignore };
                frames.AddToClassList("bq7-groupe__cases");
                var items = Shop.ItemsOfRarity(box, rarity.id);
                var shown = pairs
                    ? items.OrderByDescending(i => i.Icon != null).Take(2).ToList()
                    : items.Take(1).ToList();
                foreach (var item in shown)
                {
                    var cell = new VisualElement { pickingMode = PickingMode.Position };
                    cell.AddToClassList("bq7-case");
                    cell.AddToClassList("case--" + rarity.id);
                    bool mystery = item.RarityId == "secret" && (pairs || Shop.CountOf(item) < 1);
                    cell.Add(ShopUi.Visual(item, mystery ? ShopUi.Look.Mystery : ShopUi.Look.Normal, "bq7-case__visuel"));
                    cell.tooltip = mystery ? "???" : item.DisplayName;
                    frames.Add(cell);
                }
                group.Add(frames);
                group.Add(ShopUi.NewLabel(ShopUi.Percent(Shop.RarityChance(box, rarity)), "bq7-pourcent taux-badge"));
                row.Add(group);
            }
            return row;
        }

        /// Les boîtes flottent de ±4 px en 2,4 s ; leurs rayons font un tour en 40 s.
        void TickBoxes()
        {
            float t = Time.realtimeSinceStartup;
            foreach (var (image, rays, phase) in animatedBoxes)
            {
                if (image.panel == null) continue;
                image.style.translate = new Translate(0f, 4f * Mathf.Sin(t * Mathf.PI * 2f / 2.4f + phase));
                rays.style.rotate = new Rotate(t * 9f);
            }
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

        // v7 (retour de l'auteur, point 15) : « Contenu et taux » refait dans l'esprit du Casier et
        // de la Renaissance. À gauche, UNE vitrine de la couleur de la famille : la boîte en héros,
        // la jauge de collection, la liste des raretés (couleur, chance, bonus de tenue) et le gros
        // bouton. À droite, la grille des objets en CASES DE RARETÉ, chacune avec sa chance. Tous
        // les taux restent visibles (règle d'or) ; les deux paragraphes deviennent une seule phrase.
        void BuildContentPage(LootBox box)
        {
            animatedBoxes.Clear();
            var back = new Button(() => { contentBox = null; jumpPending = true; Refresh(); }) { text = "← Retour" };
            back.AddToClassList("btn"); back.AddToClassList("btn--sombre"); back.AddToClassList("btq-back"); back.AddToClassList(UiSounds.TabClass);
            contentHost.Add(back);

            var split = new VisualElement();
            split.AddToClassList("btq-content");
            split.AddToClassList("ct7");

            // ---- La vitrine (gauche) ----
            var left = new VisualElement();
            left.AddToClassList("ct7-vitrine");
            left.AddToClassList("carte");
            left.AddToClassList(box.family switch
            {
                BoxFamily.Pirates => "carte--bleue",
                BoxFamily.Animaux => "carte--verte",
                _ => "carte--rose"
            });
            var hero = new VisualElement { pickingMode = PickingMode.Ignore };
            hero.AddToClassList("ct7-heros");
            var glow = new VisualElement { pickingMode = PickingMode.Ignore };
            glow.AddToClassList("ct7-heros__lueur");
            glow.style.backgroundImage = new StyleBackground(UiFxTextures.Halo);
            hero.Add(glow);
            var rays = new RaysElement();
            rays.AddToClassList("ct7-heros__rayons");
            rays.Configure(10, new Color(1f, 1f, 1f, 0.30f));
            hero.Add(rays);
            var image = BoxImage(box, "ct7-heros__boite");
            hero.Add(image);
            animatedBoxes.Add((image, rays, 0f));
            left.Add(hero);
            left.Add(ShopUi.NewLabel(box.displayName, "ct7-nom"));
            int found = Shop.FoundCount(box), total = box.items.Count;
            var gauge = Gauge(found, total);
            gauge.AddToClassList("ct7-jauge");
            left.Add(gauge);

            // Les raretés : une ligne par rareté (case de couleur, nom, chance, bonus de tenue).
            var rates = new VisualElement { pickingMode = PickingMode.Ignore };
            rates.AddToClassList("ct7-taux");
            foreach (var rarity in Shop.RaritiesIn(box))
            {
                var row = new VisualElement { pickingMode = PickingMode.Ignore };
                row.AddToClassList("ct7-ligne");
                var swatch = new VisualElement { pickingMode = PickingMode.Ignore };
                swatch.AddToClassList("ct7-ligne__case");
                swatch.AddToClassList("case--" + rarity.id);
                row.Add(swatch);
                var name = ShopUi.NewLabel(rarity.displayName, "ct7-ligne__nom");
                row.Add(name);
                row.Add(ShopUi.NewLabel(ShopUi.Percent(Shop.RarityChance(box, rarity)), "ct7-ligne__chance"));
                var bonus = new VisualElement { pickingMode = PickingMode.Ignore };
                bonus.AddToClassList("ct7-ligne__bonus");
                bonus.Add(Icon("coin ct7-ligne__piece"));
                bonus.Add(ShopUi.NewLabel($"+{rarity.coinBonusPercent}\u00A0%", "ct7-ligne__bonus-texte"));
                row.Add(bonus);
                rates.Add(row);
            }
            left.Add(rates);

            // Le gros bouton (prix toujours visible, gris s'il n'y a pas assez).
            Button open;
            if (Shop.IsLocked(box))
            {
                open = new Button();
                var rebirth = new VisualElement { pickingMode = PickingMode.Ignore };
                rebirth.AddToClassList("bq7-ouvrir__renaissance");
                open.Add(rebirth);
                open.Add(ShopUi.NewLabel("Après la Renaissance", "bulle-rect__texte bq7-ouvrir__petit"));
                open.SetEnabled(false);
            }
            else
            {
                open = new Button(() => OpenRequested?.Invoke(box));
                open.AddToClassList(UiSounds.SilentClass);
                open.Add(ShopUi.NewLabel("Ouvrir", "bulle-rect__texte bq7-ouvrir__verbe"));
                open.Add(Icon((box.currency == BoxCurrency.Diamants ? "diamant" : "coin") + " bq7-ouvrir__monnaie"));
                open.Add(ShopUi.NewLabel(ShopUi.Thousands(box.price), "bulle-rect__texte bq7-ouvrir__prix"));
                open.SetEnabled(Shop.CanOpen(box));
            }
            foreach (var c in new[] { "btn", "bulle", "bulle-rect", "bulle-rect--vert", "btq-open", "ct7-ouvrir" }) open.AddToClassList(c);
            left.Add(open);
            split.Add(left);

            // ---- La grille (droite) ----
            var right = new VisualElement();
            right.AddToClassList("ct7-droite");
            var head = new VisualElement { pickingMode = PickingMode.Ignore };
            head.AddToClassList("ct7-entete");
            head.Add(ShopUi.NewLabel("Contenu", "ct7-titre"));
            head.Add(ShopUi.NewLabel($"{found} / {total}", "ct7-compteur"));
            right.Add(head);
            var line = new VisualElement { pickingMode = PickingMode.Ignore };
            line.AddToClassList("bq7-filet");
            right.Add(line);
            var list = new ScrollView();
            list.AddToClassList("shop-scroll");
            list.AddToClassList("ct7-liste");
            PanelsController.SetupScroll(list);
            var cells = new VisualElement { pickingMode = PickingMode.Ignore };
            cells.AddToClassList("ct7-grille");
            var items = box.items.Where(i => i != null).ToList();
            for (int i = 0; i < items.Count; i++)
            {
                int position = i;
                var item = items[i];
                var rarity = Shop.RaritiesIn(box).FirstOrDefault(r => r.id == item.RarityId);
                Button cell = null;
                cell = new Button(() => detail.Open(items, position, cell));
                cell.AddToClassList("ct7-case");
                cell.AddToClassList("case--" + item.RarityId);
                bool owned = Shop.CountOf(item) >= 1;
                bool mystery = !owned && item.RarityId == "secret";
                cell.Add(ShopUi.Visual(item, mystery ? ShopUi.Look.Mystery : ShopUi.Look.Normal, "ct7-case__visuel"));
                if (rarity != null)
                {
                    int sameRarity = Shop.ItemsOfRarity(box, rarity.id).Count;
                    double each = sameRarity > 0 ? Shop.RarityChance(box, rarity) / sameRarity : 0.0;
                    cell.Add(ShopUi.NewLabel(ShopUi.Percent(System.Math.Round(each, 2)), "ct7-case__chance"));
                }
                if (owned)
                {
                    var check = new VisualElement { pickingMode = PickingMode.Ignore };
                    check.AddToClassList("ct7-case__coche");
                    check.Add(Icon("casier-coche__court"));
                    check.Add(Icon("casier-coche__long"));
                    cell.Add(check);
                }
                cells.Add(cell);
            }
            list.Add(cells);
            right.Add(list);
            right.Add(ShopUi.NewLabel("1 objet par ouverture. Les chances ne changent jamais.", "ct7-pied"));
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
        // v6 §5.4 : la section « Échanges » (diamants → pièces, jamais l'inverse)
        // =====================================================================

        VisualElement exchangeConfirm;

        // v7 (§17.3.4) : trois cartes or, sans nom (la taille de la pile le dit), le GAIN en héros,
        // un bouton vert « ◆ 1 » (pas de verbe : le vert seul dit « échanger »).
        readonly Dictionary<string, VisualElement> exchangeCards = new Dictionary<string, VisualElement>();

        void BuildExchange(VisualElement section)
        {
            exchangeCards.Clear();
            var packs = EconomyCatalog.Instance.CoinPacks;
            var row = new VisualElement { pickingMode = PickingMode.Ignore };
            row.AddToClassList("ech-cartes");
            row.AddToClassList("bq7-ech-cartes");
            for (int rank = 0; rank < packs.Count; rank++)
            {
                var p = packs[rank];
                var card = new VisualElement();
                card.AddToClassList("ech-carte");
                card.AddToClassList("bq7-ech-carte");
                card.AddToClassList("carte");
                card.AddToClassList("carte--or");
                var image = new VisualElement { pickingMode = PickingMode.Ignore };
                image.AddToClassList("ech-image");
                image.AddToClassList("bq7-ech-image");
                image.Add(PopupDecor.CardRays());
                var picture = new VisualElement { pickingMode = PickingMode.Ignore };
                picture.AddToClassList("ech-image__piles");
                if (p.icon != null) picture.style.backgroundImage = new StyleBackground(p.icon);
                else picture.Add(CoinPile(rank));
                image.Add(picture);
                card.Add(image);
                // v7 §1.4 : le ruban « +15 % » (seulement si le lot a un bonus), calculé à partir des données.
                int bonus = EconomyCatalog.Instance.ExchangeBonusPercent(p);
                if (bonus > 0)
                {
                    var ribbon = new VisualElement { pickingMode = PickingMode.Ignore };
                    ribbon.AddToClassList("bq7-ech-ruban");
                    ribbon.Add(ShopUi.NewLabel($"+{bonus}\u00A0%", "bq7-ech-ruban__texte"));
                    card.Add(ribbon);
                }
                // Le gain, en héros : pièce 44 px + « +1 000 ».
                var gain = new VisualElement { pickingMode = PickingMode.Ignore };
                gain.AddToClassList("bq7-ech-gain");
                gain.Add(Icon("coin bq7-ech-gain__piece"));
                gain.Add(ShopUi.NewLabel($"+{ShopUi.Thousands(p.coins)}", "bq7-ech-gain__texte"));
                card.Add(gain);
                // v7 §1.2 : le bouton affiche toujours le prix (« ◆ 25 ») ; gris s'il n'y a pas
                // assez, sans aucun lien ni proposition (on n'achète jamais de diamants).
                var button = new Button(() => OpenExchangeConfirm(p));
                button.Add(Icon("diamant bq7-ech-btn__diamant"));
                button.Add(ShopUi.NewLabel(p.diamonds.ToString(), "bulle-rect__texte bq7-ech-btn__texte"));
                button.SetEnabled(CoinExchange.CanExchange(p));
                foreach (var c in new[] { "btn", "bulle", "bulle-rect", "bulle-rect--vert", "ech-btn", "bq7-ech-btn" }) button.AddToClassList(c);
                card.Add(button);
                row.Add(card);
                exchangeCards[p.id] = card;
            }
            section.Add(row);
        }

        // ---- Les pièces volantes après un échange (§17.3.4) : 10 pièces de 36 px volent vers le
        //      solde de pièces en 0,7 s, l'une après l'autre (0,04 s d'écart). Pur spectacle. ----
        readonly List<(VisualElement coin, Vector2 from, Vector2 to, float start)> flyingCoins = new List<(VisualElement, Vector2, Vector2, float)>();

        void StartFlyingCoinsFrom(Vector2 worldFrom, Vector2 worldTo)
        {
            Vector2 from = page.WorldToLocal(worldFrom);
            Vector2 to = page.WorldToLocal(worldTo);
            float now = Time.realtimeSinceStartup;
            for (int i = 0; i < 10; i++)
            {
                var coin = Icon("coin bq7-piece-volante");
                coin.style.position = Position.Absolute;
                coin.style.left = from.x - 18f; coin.style.top = from.y - 18f;
                coin.style.opacity = 0f;
                page.Add(coin);
                flyingCoins.Add((coin, from + new Vector2((i - 4.5f) * 8f, 0f), to, now + i * CoinStagger));
            }
        }

        // v7 (point 10) : vol plus long (1,3 s) et plus étalé, pour qu'on voie les pièces arriver.
        const float CoinFlight = 1.3f, CoinStagger = 0.09f;
        float coinBumpAt = -10f;

        void TickFlyingCoins()
        {
            float now = Time.realtimeSinceStartup;
            // Le « tic » : la pièce du solde passe à 1,35 puis revient à 1 en 0,15 s.
            var balanceCoin = balanceRow.Q<VisualElement>(className: "coin");
            if (balanceCoin != null)
            {
                float b = Mathf.Clamp01(1f - (now - coinBumpAt) / 0.15f);
                balanceCoin.style.scale = new Scale(new Vector3(1f + 0.35f * b, 1f + 0.35f * b, 1f));
            }
            for (int i = flyingCoins.Count - 1; i >= 0; i--)
            {
                var (coin, from, to, start) = flyingCoins[i];
                float u = (now - start) / CoinFlight;
                if (u < 0f) continue;
                if (u >= 1f || coin.panel == null)
                {
                    coin.RemoveFromHierarchy(); flyingCoins.RemoveAt(i);
                    coinBumpAt = now; // le « tic » du solde : il gonfle un instant à chaque pièce
                    continue;
                }
                // Une courbe : la pièce monte un peu avant de filer vers le solde.
                var pos = Vector2.Lerp(from, to, Ease.InOut(u)) + Vector2.up * (-90f * Mathf.Sin(u * Mathf.PI));
                coin.style.left = pos.x - 18f; coin.style.top = pos.y - 18f;
                coin.style.opacity = u < 0.1f ? u / 0.1f : 1f;
            }
        }

        /// Une pile de pièces dessinée (substitut tant que l'image du lot n'existe pas) :
        /// d'autant plus de pièces que le lot est gros.
        static VisualElement CoinPile(int rank)
        {
            // v7 §1.4 : une pile différente pour chacun des 8 lots (1 à 8 pièces selon le rang).
            var pile = new VisualElement { pickingMode = PickingMode.Ignore };
            pile.style.flexDirection = FlexDirection.Row;
            pile.style.flexWrap = Wrap.Wrap;
            pile.style.justifyContent = Justify.Center;
            pile.style.alignItems = Align.Center;
            pile.style.width = 130; pile.style.height = 120;
            int n = Mathf.Clamp(rank + 1, 1, 8);
            for (int i = 0; i < n; i++)
            {
                var coin = Icon("coin");
                coin.style.width = 40; coin.style.height = 40; coin.style.marginLeft = -4;
                pile.Add(coin);
            }
            return pile;
        }

        /// La confirmation (taille S) : « Échanger 5 ◆ contre 1 000 pièces ? »
        void OpenExchangeConfirm(CoinPackDef pack)
        {
            if (!CoinExchange.CanExchange(pack)) { UiSounds.PlayRefus(); return; }
            CloseExchangeConfirm();
            exchangeConfirm = new VisualElement();
            exchangeConfirm.AddToClassList("v5-confirm");
            var box = new VisualElement();
            box.AddToClassList("v5-confirm-box");
            int bonus = EconomyCatalog.Instance.ExchangeBonusPercent(pack);
            string named = bonus > 0 ? $" ({pack.displayName}, +{bonus}\u00A0%)" : "";
            box.Add(ShopUi.NewLabel($"Échanger {pack.diamonds} {ShopUi.Diamant} contre {ShopUi.Thousands(pack.coins)} pièces{named} ? Les diamants servent aussi aux reprises et aux Boîtes Pirates.", "v5-confirm-text"));
            var buttons = new VisualElement();
            buttons.AddToClassList("v5-confirm-buttons");
            var yes = new Button(() => ConfirmExchange(pack)) { text = "Échanger" };
            yes.AddToClassList("btn"); yes.AddToClassList("btn--vert"); yes.AddToClassList("v5-confirm-btn"); yes.AddToClassList(UiSounds.SilentClass);
            var no = new Button(CloseExchangeConfirm) { text = "Annuler" };
            no.AddToClassList("btn"); no.AddToClassList("btn--sombre"); no.AddToClassList("v5-confirm-btn");
            buttons.Add(yes);
            buttons.Add(no);
            box.Add(buttons);
            exchangeConfirm.Add(box);
            page.Add(exchangeConfirm);
            ButtonStyler.Apply(exchangeConfirm);
        }

        void CloseExchangeConfirm()
        {
            if (exchangeConfirm != null) exchangeConfirm.RemoveFromHierarchy();
            exchangeConfirm = null;
        }

        /// « Échanger » : la règle (relire le solde, payer, créditer, sauvegarder) est dans
        /// CoinExchange ; un double-clic ne fait qu'un échange s'il ne reste pas assez.
        void ConfirmExchange(CoinPackDef pack)
        {
            CloseExchangeConfirm();
            // Les positions à l'écran sont lues AVANT l'échange : l'échange change le solde de
            // diamants, et la Boutique est aussitôt redessinée (les anciennes cartes disparaissent).
            exchangeCards.TryGetValue(pack.id, out var fromCard);
            Vector2? from = fromCard != null && fromCard.panel != null ? fromCard.worldBound.center : (Vector2?)null;
            var target = balanceRow.Q<VisualElement>(className: "coin");
            Vector2? to = target != null && target.panel != null ? target.worldBound.center : (Vector2?)null;
            bool done = CoinExchange.TryExchange(pack);
            if (!done) UiSounds.PlayRefus();
            Refresh();
            if (done && from.HasValue && to.HasValue) StartFlyingCoinsFrom(from.Value, to.Value);
        }

        /// OUTILS DE TEST : clic sur « Échanger » d'un lot, puis sur « Échanger » ou « Annuler ».
        public void DebugClickExchange(string packId) => OpenExchangeConfirm(CoinExchange.Find(packId));
        public void DebugConfirmExchange(string packId) => ConfirmExchange(CoinExchange.Find(packId));
        public void DebugCancelExchange() => CloseExchangeConfirm();
        public bool ExchangeConfirmOpen => exchangeConfirm != null;

        // =====================================================================
        // Les packs de diamants : AFFICHÉS, INACTIFS, sans prix (v5 §15.5, v7 §17.3.5)
        // =====================================================================

        // v7 (art bible §17.3.5) : une VITRINE FERMÉE. On voit ce qui viendra, en gris-lavande
        // calme, sous UN SEUL badge « Bientôt disponible ». Aucun prix, aucun nom, aucune
        // promesse, et SURTOUT AUCUN BOUTON : rien dans cette section ne reçoit de clic
        // (pickingMode Ignore partout). Il n'existe aucun paiement dans le jeu.

        static Texture2D packGradient; // le dégradé des cartes (fabriqué une seule fois)

        /// La section « Packs de diamants » (sous les échanges), seulement si showDiamondPacks.
        void BuildPacks()
        {
            var section = new VisualElement { pickingMode = PickingMode.Ignore };
            section.AddToClassList("btq-section");
            section.AddToClassList("pk-section");
            section.Add(SectionTitle7("Packs de diamants", null, null));

            // Le puits gris-lavande qui contient les cartes, le badge et le reflet.
            var vitrine = new VisualElement { pickingMode = PickingMode.Ignore };
            vitrine.AddToClassList("pk-vitrine");
            var row = new VisualElement { pickingMode = PickingMode.Ignore };
            row.AddToClassList("pk-cartes");
            if (packGradient == null)
                packGradient = UiFxTextures.VerticalGradient(new Color32(0xF1, 0xEE, 0xF6, 0xFF), new Color32(0xD3, 0xCD, 0xE0, 0xFF));
            foreach (var pack in EconomyCatalog.Instance.DiamondPacks)
                row.Add(PackCard(pack));
            vitrine.Add(row);

            // Le reflet : une bande blanche penchée qui traverse la vitrine toutes les 5 s.
            var shine = new VisualElement { pickingMode = PickingMode.Ignore };
            shine.AddToClassList("pk-reflet");
            vitrine.Add(shine);
            AnimateShine(vitrine, shine);

            // LE badge, un seul, centré sur la vitrine et penché.
            var badge = new VisualElement { pickingMode = PickingMode.Ignore };
            badge.AddToClassList("pk-badge");
            var hourglass = UiImages.Sablier;
            if (hourglass != null)
            {
                var icon = new VisualElement { pickingMode = PickingMode.Ignore };
                icon.AddToClassList("pk-badge__sablier");
                icon.style.backgroundImage = new StyleBackground(hourglass);
                badge.Add(icon);
            }
            else badge.AddToClassList("pk-badge--sans-image"); // repli : le texte seul, bien centré
            badge.Add(ShopUi.NewLabel("Bientôt disponible", "pk-badge__texte"));
            vitrine.Add(badge);

            section.Add(vitrine);
            section.Add(ShopUi.NewLabel("Pour l'instant, les diamants se gagnent en jouant.", "pk-phrase"));
            scroll.Add(section);
        }

        /// Une carte de pack : l'image (grisée) sur une lueur, puis la quantité. Pas de nom, pas
        /// de prix, pas de bouton.
        static VisualElement PackCard(DiamondPackDef pack)
        {
            var card = new VisualElement { pickingMode = PickingMode.Ignore };
            card.AddToClassList("pk-carte");
            card.style.backgroundImage = new StyleBackground(packGradient);
            var image = new VisualElement { pickingMode = PickingMode.Ignore };
            image.AddToClassList("pk-image");
            var glow = new VisualElement { pickingMode = PickingMode.Ignore };
            glow.AddToClassList("pk-lueur");
            glow.style.backgroundImage = new StyleBackground(UiFxTextures.Halo);
            image.Add(glow);
            var picture = new VisualElement { pickingMode = PickingMode.Ignore };
            picture.AddToClassList("pk-image__pack");
            if (pack.icon != null) picture.style.backgroundImage = new StyleBackground(pack.icon);
            else picture.Add(Icon("diamant diamant--grand"));
            image.Add(picture);
            card.Add(image);
            var amount = new VisualElement { pickingMode = PickingMode.Ignore };
            amount.AddToClassList("pk-quantite");
            amount.Add(ShopUi.NewLabel(pack.amount.ToString(), "pk-quantite__texte"));
            amount.Add(Icon("diamant pk-quantite__diamant"));
            card.Add(amount);
            return card;
        }

        /// Fait passer le reflet de gauche à droite en 1,2 s, toutes les 5 s, tant que la
        /// vitrine est affichée (l'animation s'arrête toute seule quand on la retire).
        static void AnimateShine(VisualElement vitrine, VisualElement shine)
        {
            const float Period = 5f, Pass = 1.2f;
            float start = Time.realtimeSinceStartup;
            IVisualElementScheduledItem item = null;
            item = vitrine.schedule.Execute(() =>
            {
                if (vitrine.panel == null) { item?.Pause(); return; }
                float t = (Time.realtimeSinceStartup - start) % Period;
                float width = vitrine.resolvedStyle.width;
                if (float.IsNaN(width) || width <= 0f) width = 1100f;
                float k = Mathf.Clamp01(t / Pass);
                shine.style.left = Mathf.Lerp(-120f, width + 60f, k);
                shine.style.opacity = t < Pass ? 1f : 0f;
            }).Every(33);
        }

        // =====================================================================
        // Outils de test
        // =====================================================================

        /// OUTIL DE TEST : nombre de pièces volantes en l'air.
        public int FlyingCoinCount => flyingCoins.Count;

        /// OUTIL DE TEST : clic sur un onglet (0 Classiques, 1 Pirates, 2 Animaux, 3 Échanges) :
        /// la liste défile en douceur jusqu'à la section.
        public void DebugTab(int tab) => ScrollTo((Tab)tab);
        /// OUTIL DE TEST : fait défiler la liste « à la main » jusqu'à la position y.
        public void DebugScrollTo(float y) { scrollTarget = null; scroll.scrollOffset = new Vector2(0f, y); }
        /// Les textes visibles de la page (pour les tests).
        public string Text => string.Join(" | ", page.Query<Label>().ToList().Where(l => l.resolvedStyle.display != DisplayStyle.None).Select(l => l.text))
                              + " | " + string.Join(" | ", page.Query<Button>().ToList().Select(b => PopupDecor.TextOf(b) + (b.enabledSelf ? "" : "(grisé)")));
    }
}
