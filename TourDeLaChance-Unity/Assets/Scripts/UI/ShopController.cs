using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace TourDeLaChance
{
    /// <summary>
    /// Le panneau latéral du shop (Shop.uxml, spec boites §7.2 et §7.3). Il AFFICHE et
    /// TRANSMET les clics : ouvrir une boîte, équiper et vendre sont décidés par la classe
    /// Shop ; ouvrir / fermer le panneau par le GameManager.
    ///
    /// Il gère : le glissement du panneau, les trois onglets, le rappel du porte-monnaie,
    /// la page « Boutique » (une carte par boîte), et l'aperçu sur le personnage. Les pages
    /// « Inventaire » et « Index » sont confiées à InventoryPage et IndexPage.
    /// Fermer le panneau ou changer d'onglet remet les objets équipés sur le personnage.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class ShopController : MonoBehaviour
    {
        enum Tab { Boutique, Inventaire, Index }

        [SerializeField] GameManager game;
        [SerializeField] PlayerAvatar avatar;
        [SerializeField] PlayerAnimator avatarAnimator; // petit saut d'accueil à l'aperçu
        [SerializeField] BoxOpeningController opening;  // l'écran d'ouverture d'une boîte

        const float PanelWidth = 660f;   // 440 px de la spec × 1,5 (référence 1920 × 1080)
        const float SlideInTime = 0.25f;  // ease-out
        const float SlideOutTime = 0.2f;  // ease-in

        VisualElement root, panel, pageBoutique, pageInventaire, pageIndex;
        ScrollView boxList;
        Label walletLabel;
        Button closeButton, tabBoutique, tabInventaire, tabIndex;
        InventoryPage inventory;
        IndexPage index;

        Tab currentTab = Tab.Boutique;
        bool panelShown;
        float slideTime = -1f; // ≥ 0 pendant un glissement
        bool slidingIn;
        readonly RollingCounter wallet = new RollingCounter();

        /// Le panneau est-il affiché (pour les tests) ?
        public bool PanelShown => panelShown;

        void OnEnable()
        {
            var doc = GetComponent<UIDocument>().rootVisualElement;
            root = doc.Q<VisualElement>("shop-root");
            panel = doc.Q<VisualElement>("panel");
            pageBoutique = doc.Q<VisualElement>("page-boutique");
            boxList = doc.Q<ScrollView>("page-boutique");
            pageInventaire = doc.Q<VisualElement>("page-inventaire");
            pageIndex = doc.Q<VisualElement>("page-index");
            walletLabel = doc.Q<Label>("shop-wallet");
            closeButton = doc.Q<Button>("close");
            tabBoutique = doc.Q<Button>("tab-boutique");
            tabInventaire = doc.Q<Button>("tab-inventaire");
            tabIndex = doc.Q<Button>("tab-index");

            inventory = new InventoryPage(pageInventaire);
            index = new IndexPage(pageIndex);
            inventory.PreviewRequested += Preview;
            inventory.Equipped += OnEquipped;
            inventory.Sold += OnSold;
            index.PreviewRequested += Preview;
            index.ShowInInventoryRequested += ShowInInventory;

            closeButton.clicked += game.CloseShop;
            tabBoutique.clicked += OnTabBoutique;
            tabInventaire.clicked += OnTabInventaire;
            tabIndex.clicked += OnTabIndex;
            game.Changed += OnGameChanged;
            Shop.Changed += Refresh;
            if (opening != null) opening.Closed += OnOpeningClosed;
            UiSounds.Register(root); // ui_survol / ui_clic sur tous les boutons du panneau

            panelShown = false;
            root.style.display = DisplayStyle.None; // caché au démarrage
            OnGameChanged();
        }

        void OnDisable()
        {
            closeButton.clicked -= game.CloseShop;
            tabBoutique.clicked -= OnTabBoutique;
            tabInventaire.clicked -= OnTabInventaire;
            tabIndex.clicked -= OnTabIndex;
            game.Changed -= OnGameChanged;
            Shop.Changed -= Refresh;
            if (opening != null) opening.Closed -= OnOpeningClosed;
            UiSounds.Unregister(root);
        }

        void Update()
        {
            // Échap ferme le panneau, comme « × » — sauf pendant (ou juste à la fin d')une
            // ouverture de boîte : là, Échap appartient à l'écran d'ouverture.
            var kb = Keyboard.current;
            bool openingBusy = opening != null && opening.BlocksEscape;
            if (game.ShopOpen && !openingBusy && kb != null && kb.escapeKey.wasPressedThisFrame)
                game.CloseShop();

            wallet.Tick(Time.deltaTime);
            walletLabel.text = wallet.Displayed.ToString();
            AnimateSlide();
        }

        // =====================================================================
        // Ouvrir / fermer le panneau
        // =====================================================================

        /// Le GameManager a changé : on ouvre ou ferme le panneau pour suivre ShopOpen.
        void OnGameChanged()
        {
            if (game.ShopOpen == panelShown) return;
            panelShown = game.ShopOpen;

            if (panelShown)
            {
                root.style.display = DisplayStyle.Flex;
                wallet.Snap(Wallet.Coins);
                SelectTab(Tab.Boutique); // onglet « Boutique » par défaut (spec §7.1)
                StartSlide(true);
            }
            else
            {
                StartSlide(false);
                avatar.ShowEquipped(); // fin de l'aperçu : on remet les objets équipés
            }
        }

        void StartSlide(bool goingIn)
        {
            slidingIn = goingIn;
            slideTime = 0f;
            AnimateSlide();
        }

        /// Glissement depuis la droite : 0,25 s ease-out à l'ouverture, 0,2 s ease-in à la fermeture.
        void AnimateSlide()
        {
            if (slideTime < 0f) return;
            slideTime += Time.deltaTime;
            float duration = slidingIn ? SlideInTime : SlideOutTime;
            float k = Mathf.Clamp01(slideTime / duration);
            float x = slidingIn ? PanelWidth * (1f - Ease.Out(k)) : PanelWidth * Ease.In(k);
            panel.style.translate = new Translate(x, 0f);
            if (k >= 1f)
            {
                slideTime = -1f;
                if (!slidingIn) root.style.display = DisplayStyle.None;
            }
        }

        // =====================================================================
        // Onglets
        // =====================================================================

        void OnTabBoutique() => SelectTab(Tab.Boutique);
        void OnTabInventaire() => SelectTab(Tab.Inventaire);
        void OnTabIndex() => SelectTab(Tab.Index);

        /// Change d'onglet : l'aperçu s'arrête (on remet les objets équipés).
        void SelectTab(Tab tab, ShopItem inventoryFocus = null, LootBox indexFocus = null)
        {
            currentTab = tab;
            avatar.ShowEquipped();
            ShopUi.SetClass(tabBoutique, "shop-tab--active", tab == Tab.Boutique);
            ShopUi.SetClass(tabInventaire, "shop-tab--active", tab == Tab.Inventaire);
            ShopUi.SetClass(tabIndex, "shop-tab--active", tab == Tab.Index);
            ShopUi.SetClass(pageBoutique, "shop-page--active", tab == Tab.Boutique);
            ShopUi.SetClass(pageInventaire, "shop-page--active", tab == Tab.Inventaire);
            ShopUi.SetClass(pageIndex, "shop-page--active", tab == Tab.Index);

            switch (tab)
            {
                case Tab.Boutique: RefreshBoutique(); boxList.scrollOffset = Vector2.zero; break;
                case Tab.Inventaire: inventory.Open(inventoryFocus); break;
                case Tab.Index: index.Open(indexFocus); break;
            }
        }

        /// Raccourci « Voir dans l'inventaire » depuis l'Index.
        void ShowInInventory(ShopItem item) => SelectTab(Tab.Inventaire, inventoryFocus: item);

        /// La collection ou le porte-monnaie a changé : on redessine la page visible.
        void Refresh()
        {
            if (!panelShown) return;
            if (wallet.Target != Wallet.Coins) wallet.RollTo(Wallet.Coins, 0.4f, Ease.Out);
            switch (currentTab)
            {
                case Tab.Boutique: RefreshBoutique(); break;
                case Tab.Inventaire: inventory.Refresh(); break;
                case Tab.Index: index.Refresh(); break;
            }
        }

        // =====================================================================
        // Aperçu, équiper, vendre (transmis par les pages)
        // =====================================================================

        void Preview(ShopItem item)
        {
            avatar.Show(item);
            if (avatarAnimator != null) avatarAnimator.WelcomeHop();
        }

        void OnEquipped(ShopItem item)
        {
            avatar.ShowEquipped();
            if (avatarAnimator != null) avatarAnimator.WelcomeHop();
        }

        void OnSold()
        {
            if (SoundManager.Instance != null) SoundManager.Instance.Play(Sfx.Vente);
        }

        /// Fin de l'écran d'ouverture (« OK ») : retour à l'onglet Boutique, objets équipés.
        void OnOpeningClosed()
        {
            if (panelShown) SelectTab(Tab.Boutique);
            else avatar.ShowEquipped();
        }

        // =====================================================================
        // Page « Boutique » : une carte par boîte (spec §7.3)
        // =====================================================================

        void RefreshBoutique()
        {
            boxList.Clear();
            foreach (var box in Shop.Boxes) boxList.Add(BoxCard(box));
        }

        VisualElement BoxCard(LootBox box)
        {
            var card = new VisualElement();
            card.AddToClassList("box-card");

            // 1. Image + nom + prix
            var top = new VisualElement { pickingMode = PickingMode.Ignore };
            top.AddToClassList("box-top");
            var image = new VisualElement { pickingMode = PickingMode.Ignore };
            image.AddToClassList("box-image");
            if (box.icon != null) image.style.backgroundImage = new StyleBackground(box.icon);
            top.Add(image);
            var titleCol = new VisualElement { pickingMode = PickingMode.Ignore };
            titleCol.AddToClassList("box-title-col");
            titleCol.Add(ShopUi.NewLabel(box.displayName, "box-name"));
            var priceRow = new VisualElement { pickingMode = PickingMode.Ignore };
            priceRow.AddToClassList("price-row");
            priceRow.Add(ShopUi.NewLabel(box.price.ToString(), "price-text"));
            var coin = new VisualElement { pickingMode = PickingMode.Ignore };
            coin.AddToClassList("coin");
            coin.AddToClassList("coin--small");
            priceRow.Add(coin);
            titleCol.Add(priceRow);
            top.Add(titleCol);
            card.Add(top);

            // 2. Progression
            int found = Shop.FoundCount(box), total = box.items.Count;
            var progressRow = new VisualElement { pickingMode = PickingMode.Ignore };
            progressRow.AddToClassList("progress-row");
            progressRow.Add(ShopUi.NewLabel($"Collection : {found} / {total}", "progress-text"));
            var bar = new VisualElement { pickingMode = PickingMode.Ignore };
            bar.AddToClassList("progress-bar");
            var fill = new VisualElement { pickingMode = PickingMode.Ignore };
            fill.AddToClassList("progress-fill");
            fill.style.width = Length.Percent(total > 0 ? 100f * found / total : 0f);
            bar.Add(fill);
            progressRow.Add(bar);
            card.Add(progressRow);

            // 3. Tableau des taux : TOUT est lu dans le catalogue (aucun chiffre en dur).
            card.Add(RatesTable(box));
            card.Add(ShopUi.NewLabel("Chaque ouverture donne 1 objet. Les chances sont toujours les mêmes, à chaque ouverture.", "rates-note"));

            // 5. Collection complète : la boîte reste ouvrable.
            if (Shop.IsComplete(box))
                card.Add(ShopUi.NewLabel("Collection complète ! Chaque ouverture donnera un doublon.", "complete-banner"));

            // 4. Bouton « Ouvrir — 100 » (grisé + « Il te manque X pièces » si pas assez).
            var buttons = new VisualElement();
            buttons.AddToClassList("box-buttons");
            var openButton = new Button(() => OnOpenClicked(box)) { text = $"Ouvrir — {box.price}" };
            openButton.AddToClassList("open-btn");
            openButton.AddToClassList(UiSounds.SilentClass); // le son de paiement suffit
            bool canOpen = Shop.CanOpen(box);
            openButton.SetEnabled(canOpen);
            buttons.Add(openButton);
            if (!canOpen) buttons.Add(ShopUi.NewLabel($"Il te manque {Shop.MissingCoins(box)} pièces", "missing"));
            card.Add(buttons);

            var link = new Button(() => SelectTab(Tab.Index, indexFocus: box)) { text = "Voir le contenu" };
            link.AddToClassList("link-btn");
            card.Add(link);
            return card;
        }

        VisualElement RatesTable(LootBox box)
        {
            var table = new VisualElement { pickingMode = PickingMode.Ignore };
            table.AddToClassList("rates");
            table.Add(RateRow(null, "Rareté", "Chance", "Objets", header: true));
            foreach (var rarity in Shop.Catalog.Rarities)
            {
                int count = Shop.ItemsOfRarity(box, rarity.id).Count;
                table.Add(RateRow(rarity, rarity.displayName, ShopUi.Percent(Shop.RarityChance(rarity)),
                                  count.ToString(), header: false));
            }
            return table;
        }

        static VisualElement RateRow(RarityDef rarity, string name, string chance, string count, bool header)
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
                dot.style.backgroundColor = rarity.color; // pastille : couleur de base
                dotCell.Add(dot);
            }
            row.Add(dotCell);

            var nameLabel = ShopUi.NewLabel(name, "rate-cell");
            nameLabel.AddToClassList("rate-name");
            if (rarity != null) ShopUi.ColorText(nameLabel, rarity); // nom : couleur « texte »
            row.Add(nameLabel);
            var chanceLabel = ShopUi.NewLabel(chance, "rate-cell");
            chanceLabel.AddToClassList("rate-chance");
            row.Add(chanceLabel);
            var countLabel = ShopUi.NewLabel(count, "rate-cell");
            countLabel.AddToClassList("rate-count");
            row.Add(countLabel);
            return row;
        }

        /// Clic sur « Ouvrir » : la règle (payer, tirer, sauvegarder) est dans Shop.TryOpenBox ;
        /// ici on transmet le clic puis on lance l'animation avec le résultat déjà connu.
        /// Double-clic : au 2e clic, l'écran d'ouverture recouvre déjà tout (et Shop refuse
        /// si le solde ne suffit plus).
        void OnOpenClicked(LootBox box)
        {
            if (opening == null || opening.IsBusy) return;
            if (Shop.TryOpenBox(box, out var result)) opening.Play(result);
        }

        /// OUTIL DE TEST : simule un clic sur « Ouvrir » d'une boîte (depuis `script eval`).
        public void DebugClickOpen(string boxId) => OnOpenClicked(Shop.FindBox(boxId));

        /// OUTIL DE TEST : affiche un onglet (0 = Boutique, 1 = Inventaire, 2 = Index).
        public void DebugSelectTab(int tab) => SelectTab((Tab)tab);
    }
}
