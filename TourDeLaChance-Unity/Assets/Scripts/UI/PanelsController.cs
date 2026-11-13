using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace TourDeLaChance
{
    /// <summary>
    /// Les POP-UPS v4 et leurs boutons d'ouverture (Panels.uxml, spec v4 §4.3, §5.5, §5.6, §6.2).
    /// Il AFFICHE et TRANSMET : quelle pop-up peut s'ouvrir et laquelle est ouverte est décidé
    /// par le GameManager (CanOpenPanel, CanOpenSettings, PanelOpen) ; ouvrir une boîte,
    /// équiper, vendre par Shop ; acheter une amélioration par Upgrades ; le classement par
    /// Leaderboard ; le son par SoundSettings.
    ///
    /// Il gère :
    ///   - la colonne de gauche (Boutique, Inventaire, Index, Améliorations), la colonne de
    ///     droite (Classement) et la roue dentée (Réglages) : cachées sur l'écran titre (sauf
    ///     la roue), elles glissent en place à la fin du fondu de « Jouer » ;
    ///   - le fond assombri et la fenêtre centrée (ouverture qui rebondit, fermeture, passage
    ///     d'une pop-up à l'autre), son titre qui déborde avec sa grande icône (v5.1), le bouton fermer, Échap ;
    ///   - la vitrine du cheval (Inventaire, Index) ;
    ///   - et confie le contenu aux pages (BoutiquePage, InventoryPage, IndexPage, UpgradesPage,
    ///     LeaderboardPopup, SettingsPage, NamePage).
    /// Changer de pop-up ou fermer remet les objets équipés sur le cheval (fin de l'aperçu).
    ///
    /// Correctif B1 : pendant une ouverture de boîte, les pages ne se redessinent PAS (elles
    /// le font après « OK ») ; seul le porte-monnaie défile. Correctif B9 : tout ce qui est
    /// branché dans OnEnable est débranché dans OnDisable.
    ///
    /// v5 : boutons de colonne RONDS d'icône seule et info-bulle maison (§7.1), bouton
    /// Renaissance sous le Classement (§13.5), plus d'icône dans le bandeau (§7.2), Index sans
    /// vitrine (§8), Boutique à onglets (§15), page Renaissance (§13.3), petits messages
    /// (toasts), et application de la règle « 2 accessoires » au démarrage (§4.4).
    ///
    /// v6 : la Route des trophées, devenue « L'Ascension » en v6.5 (TrophyRoadPage, clic sur le badge d'étage) ; les petits
    /// messages des récompenses (collection complète, rattrapage des paliers), 1,5 s d'écart ;
    /// la vérification des collections après chaque ouverture (« OK ») ; les bulles en image.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class PanelsController : MonoBehaviour
    {
        [SerializeField] GameManager game;
        [SerializeField] PlayerAvatar avatar;
        [SerializeField] PlayerAnimator avatarAnimator; // petit saut d'accueil à l'aperçu
        [SerializeField] BoxOpeningController opening;  // l'écran d'ouverture d'une boîte
        [SerializeField] HorseShowcase showcase;        // la caméra de la vitrine (facultative)
        [Tooltip("v5 §8.5 : le cheval vierge (fiche agrandie de l'Index, « Contenu et taux »).")]
        [SerializeField] ShowcaseHorse blankHorse;
        [Tooltip("v6.5 §5 : fait tourner le cheval du joueur quand on le glisse dans la vitrine de l'Inventaire.")]
        [SerializeField] PlayerSpinner playerSpinner;

        [Header("Icônes des pop-ups (colonnes et bandeau)")]
        [SerializeField] Sprite iconBoutique;
        [SerializeField] Sprite iconInventaire;
        [SerializeField] Sprite iconIndex;
        [SerializeField] Sprite iconAmeliorations;
        [SerializeField] Sprite iconClassement;
        [SerializeField] Sprite iconReglages;
        [Tooltip("v5 §13.5 : icône de la Renaissance (Assets/Art/icones_detourees/icone_rebirth.png, " +
                 "lot d'images v5). Vide = une flèche circulaire dorée dessinée par le code.")]
        [SerializeField] Sprite iconRenaissance;

        // ---------- Les durées (spec §4.3 et §5.6) ----------
        const float OpenTime = 0.25f;       // fenêtre 0,85 → 1,04 → 1, opacité 0 → 1
        const float DimInTime = 0.2f;       // fond assombri 0 → 65 %
        const float CloseTime = 0.15f;      // fenêtre 1 → 0,9, opacité → 0, fond → 0
        const float SwitchTime = 0.15f;     // nouvelle fenêtre 0,9 → 1
        const float ColumnSlideTime = 0.3f; // chaque bouton de colonne, ease-out-back
        const float ColumnStagger = 0.05f;  // décalage entre deux boutons
        const float ColumnStartX = 160f;    // ils partent de 160 px hors de l'écran
        const float GearSlideTime = 0.2f;
        // La roue : sur l'écran titre en (32 ; 32) ; pendant le jeu, à droite du badge d'étage
        // (badge en x = 32, 200 px de large, 16 px d'écart ; centrée sur ses 116 px de haut).
        // v6 : la roue est une bulle de 96 px dont la face visible (Ø 62) est centrée en (48 ; 40).
        static readonly Vector2 GearInGameOffset = new Vector2(32f + 200f + 16f - 12f - 32f, 24f + 58f - 40f - 32f);

        VisualElement doc, dim, frame, window, header, walletRow, columnLeft, columnRight, gearHolder, toastLayer;
        VisualElement pageBoutique, pageInventaire, pageIndex, pageAmeliorations, pageClassement, pageReglages, pagePseudo, pageRenaissance, pageRoute;
        VisualElement invShowcase, popupIcon;
        Label title, walletLabel, potNote;
        Button closeButton, gearButton, colBoutique, colInventaire, colIndex, colAmeliorations, colClassement, colRenaissance;
        InfoBulle infoBulle;
        RebirthPage rebirthPage;
        TrophyRoadPage routePage;
        BoutiquePage boutique;
        InventoryPage inventory;
        IndexPage index;
        UpgradesPage upgrades;
        LeaderboardPopup leaderboard;
        SettingsPage settings;
        NamePage namePage;

        PanelId shownPanel = PanelId.None;
        bool showingName;      // la page « Choisis ton pseudo » est affichée dans la pop-up Classement
        bool frameVisible;
        bool refreshPending;   // B1 : une mise à jour attend la fin de l'ouverture de boîte
        bool openingRequested; // B1 : on est en train de demander une ouverture (avant Play)
        bool columnsShown;     // les colonnes ont fait leur arrivée après « Jouer »
        ShopItem pendingInventoryFocus;
        LootBox pendingBoxContent;
        ShopItem pendingShopItem; // v6 §6.1 : la fiche à ouvrir dans « Contenu et taux »
        readonly RollingCounter wallet = new RollingCounter();

        enum Anim { None, Opening, Closing, Switching }
        Anim anim = Anim.None;
        float animTime;
        Coroutine columnsIntro;

        /// La pop-up affichée (pour les tests).
        public PanelId ShownPanel => shownPanel;
        public bool FrameVisible => frameVisible;
        public bool ShowingNamePage => showingName;
        public bool ColumnsVisible => columnLeft.resolvedStyle.display == DisplayStyle.Flex;
        public UpgradesPage Upgrades_Page => upgrades;
        public LeaderboardPopup Leaderboard_Page => leaderboard;
        public SettingsPage Settings_Page => settings;
        public NamePage Name_Page => namePage;
        public InventoryPage Inventory_Page => inventory;
        public BoutiquePage Boutique_Page => boutique;
        public IndexPage Index_Page => index;
        public RebirthPage Rebirth_Page => rebirthPage;
        public TrophyRoadPage Route_Page => routePage;
        public string TooltipText => infoBulle != null ? infoBulle.Text : "";

        void OnEnable()
        {
            doc = GetComponent<UIDocument>().rootVisualElement;
            var root = doc.Q<VisualElement>("panels-root");
            dim = doc.Q<VisualElement>("popup-dim");
            frame = doc.Q<VisualElement>("popup-frame");
            window = doc.Q<VisualElement>("popup-window");
            header = doc.Q<VisualElement>("popup-header");
            title = doc.Q<Label>("popup-title");
            popupIcon = doc.Q<VisualElement>("popup-icon");
            walletRow = doc.Q<VisualElement>("popup-wallet-row");
            walletLabel = doc.Q<Label>("panel-wallet");
            potNote = doc.Q<Label>("pot-note");
            closeButton = doc.Q<Button>("close");
            columnLeft = doc.Q<VisualElement>("column");
            columnRight = doc.Q<VisualElement>("column-right");
            gearHolder = doc.Q<VisualElement>("gear-holder");
            gearButton = doc.Q<Button>("gear");
            toastLayer = doc.Q<VisualElement>("toast-layer");
            colBoutique = doc.Q<Button>("col-boutique");
            colInventaire = doc.Q<Button>("col-inventaire");
            colIndex = doc.Q<Button>("col-index");
            colAmeliorations = doc.Q<Button>("col-ameliorations");
            colClassement = doc.Q<Button>("col-classement");
            colRenaissance = doc.Q<Button>("col-renaissance");
            pageBoutique = doc.Q<VisualElement>("page-boutique");
            pageInventaire = doc.Q<VisualElement>("page-inventaire");
            pageIndex = doc.Q<VisualElement>("page-index");
            pageAmeliorations = doc.Q<VisualElement>("page-ameliorations");
            pageClassement = doc.Q<VisualElement>("page-classement");
            pageReglages = doc.Q<VisualElement>("page-reglages");
            pagePseudo = doc.Q<VisualElement>("page-pseudo");
            pageRenaissance = doc.Q<VisualElement>("page-renaissance");
            pageRoute = doc.Q<VisualElement>("page-route");
            // v6 : les nouvelles icônes (échoppe, roue sans trou), chargées par leur nom.
            if (UiImages.Boutique != null) iconBoutique = UiImages.Boutique;
            if (UiImages.Reglages != null) iconReglages = UiImages.Reglages;

            SetIcon(colBoutique, iconBoutique);
            SetIcon(colInventaire, iconInventaire);
            SetIcon(colIndex, iconIndex);
            SetIcon(colAmeliorations, iconAmeliorations);
            SetIcon(colClassement, iconClassement);
            SetRebirthIcon();

            // v5 §4.4 : une sauvegarde v4 qui porte 3 ou 4 accessoires passe à 2 (les plus rares).
            Shop.EnforceMaxAccessories();

            boutique = new BoutiquePage(pageBoutique, blankHorse);
            inventory = new InventoryPage(pageInventaire, this);
            // v6.5 : la vitrine du cheval est construite par l'Inventaire « Casier ». On y pose
            // l'image de la caméra, et on peut faire tourner le cheval en glissant sur toute la
            // scène (§5 : aucune indication à l'écran).
            invShowcase = inventory.Showcase;
            inventory.ShowcaseArea.AddManipulator(new DragToRotate(() => playerSpinner));
            index = new IndexPage(pageIndex, blankHorse);
            upgrades = new UpgradesPage(doc.Q<ScrollView>("page-ameliorations"), this);
            leaderboard = new LeaderboardPopup(pageClassement);
            settings = new SettingsPage(pageReglages);
            settings.Toast += ShowToast;
            namePage = new NamePage(pagePseudo);
            rebirthPage = new RebirthPage(pageRenaissance, game, iconAmeliorations, iconClassement);
            routePage = new TrophyRoadPage(pageRoute, game);

            // Toutes les zones qui défilent : barre dorée intégrée, molette 80 px par cran,
            // jamais de barre horizontale, petites ombres « encore du contenu » en haut et en bas.
            foreach (var sv in doc.Query<ScrollView>().ToList()) SetupScroll(sv);

            boutique.OpenRequested += OnOpenClicked;
            boutique.ShowInInventoryRequested += ShowInInventory;
            index.ShowInInventoryRequested += ShowInInventory;
            index.ShowBoxRequested += ShowBoxContent;
            index.ShowInShopRequested += ShowInShop;
            CollectionRewards.Granted += OnCollectionReward;
            TrophyRoad.CatchUpGranted += OnTrophyCatchUp;
            CoinExchange.Exchanged += OnExchanged;
            inventory.PreviewRequested += Preview;
            inventory.Equipped += OnEquipped;
            inventory.Sold += OnSold;
            upgrades.Bought += OnUpgradeBought;
            leaderboard.RenameRequested += ShowNamePage;
            namePage.Done += OnNameDone;

            closeButton.clicked += game.ClosePanel;
            gearButton.clicked += OnGear;
            colBoutique.clicked += OnColBoutique;
            colInventaire.clicked += OnColInventaire;
            colIndex.clicked += OnColIndex;
            colAmeliorations.clicked += OnColAmeliorations;
            colClassement.clicked += OnColClassement;
            colRenaissance.clicked += OnColRenaissance;
            dim.RegisterCallback<PointerDownEvent>(OnDimClicked);
            foreach (var b in ColumnButtons) b.RegisterCallback<PointerEnterEvent>(OnColumnHover);

            // v5 §7.1 : l'info-bulle maison (le « tooltip » d'UI Toolkit ne marche pas en jeu).
            infoBulle = new InfoBulle(doc.Q<VisualElement>("info-bulle"), doc.Q<Label>("info-bulle-texte"));
            infoBulle.Attach(colBoutique, "Boutique", InfoBulle.Side.Right, () => game.CanOpenPanel, "pas pendant une épreuve");
            infoBulle.Attach(colInventaire, "Inventaire", InfoBulle.Side.Right, () => game.CanOpenPanel, "pas pendant une épreuve");
            infoBulle.Attach(colIndex, "Index", InfoBulle.Side.Right, () => game.CanOpenPanel, "pas pendant une épreuve");
            infoBulle.Attach(colAmeliorations, "Améliorations", InfoBulle.Side.Right, () => game.CanOpenPanel, "pas pendant une épreuve");
            infoBulle.Attach(colClassement, "Classement", InfoBulle.Side.Left, () => game.CanOpenPanel, "pas pendant une épreuve");
            infoBulle.Attach(colRenaissance, "Renaissance", InfoBulle.Side.Left, () => game.CanOpenPanel, "pas pendant une épreuve");
            infoBulle.Attach(gearButton, "Réglages", InfoBulle.Side.Below, () => game.CanOpenSettings, "pas pendant une ouverture");

            game.Changed += OnGameChanged;
            game.FloorShown += OnFloorShown;
            Shop.Changed += Refresh;
            Upgrades.Changed += Refresh;
            Diamonds.Changed += Refresh;
            Rebirth.Changed += Refresh;
            if (opening != null) opening.Closed += OnOpeningClosed;
            UiSounds.Register(root); // ui_survol / ui_clic sur tous les boutons
            ButtonStyler.Register(root);

            shownPanel = PanelId.None;
            frameVisible = false;
            frame.style.display = DisplayStyle.None; // caché au démarrage
            dim.style.display = DisplayStyle.None;
            wallet.Snap(Wallet.Coins);
            OnGameChanged();
        }

        Button[] ColumnButtons => new[] { colBoutique, colInventaire, colIndex, colAmeliorations, colClassement, colRenaissance };

        void OnDisable()
        {
            boutique.OpenRequested -= OnOpenClicked;
            boutique.ShowInInventoryRequested -= ShowInInventory;
            index.ShowInInventoryRequested -= ShowInInventory;
            index.ShowBoxRequested -= ShowBoxContent;
            index.ShowInShopRequested -= ShowInShop;
            CollectionRewards.Granted -= OnCollectionReward;
            TrophyRoad.CatchUpGranted -= OnTrophyCatchUp;
            CoinExchange.Exchanged -= OnExchanged;
            inventory.PreviewRequested -= Preview;
            inventory.Equipped -= OnEquipped;
            inventory.Sold -= OnSold;
            upgrades.Bought -= OnUpgradeBought;
            leaderboard.RenameRequested -= ShowNamePage;
            namePage.Done -= OnNameDone;
            boutique.Dispose();
            inventory.Dispose();
            index.Dispose();
            upgrades.Dispose();
            leaderboard.Dispose();
            settings.Toast -= ShowToast;
            settings.Dispose();
            namePage.Dispose();
            rebirthPage.Dispose();
            routePage.Dispose();
            infoBulle.Dispose();

            closeButton.clicked -= game.ClosePanel;
            gearButton.clicked -= OnGear;
            colBoutique.clicked -= OnColBoutique;
            colInventaire.clicked -= OnColInventaire;
            colIndex.clicked -= OnColIndex;
            colAmeliorations.clicked -= OnColAmeliorations;
            colClassement.clicked -= OnColClassement;
            colRenaissance.clicked -= OnColRenaissance;
            dim.UnregisterCallback<PointerDownEvent>(OnDimClicked);
            foreach (var b in ColumnButtons) b.UnregisterCallback<PointerEnterEvent>(OnColumnHover);
            game.Changed -= OnGameChanged;
            game.FloorShown -= OnFloorShown;
            Shop.Changed -= Refresh;
            Upgrades.Changed -= Refresh;
            Diamonds.Changed -= Refresh;
            Rebirth.Changed -= Refresh;
            if (opening != null) opening.Closed -= OnOpeningClosed;
            // Correctif : à la fermeture du jeu, le document peut déjà être détruit (doc gardé).
            var root = doc?.Q<VisualElement>("panels-root");
            if (root != null) UiSounds.Unregister(root);
            if (showcase != null) showcase.SetActive(false);
            if (blankHorse != null) blankHorse.SetActive(false);
        }

        /// v5 §13.5 : l'icône de la Renaissance. Tant que l'image n'existe pas, une flèche
        /// circulaire dorée autour d'une étoile est DESSINÉE par le code (aucune image).
        void SetRebirthIcon()
        {
            var e = colRenaissance.Q<VisualElement>(className: "col-btn-icon");
            if (e == null) return;
            if (iconRenaissance != null) { e.style.backgroundImage = new StyleBackground(iconRenaissance); return; }
            e.generateVisualContent += DrawRebirthIcon;
            e.MarkDirtyRepaint();
        }

        static void DrawRebirthIcon(MeshGenerationContext ctx)
        {
            var r = ctx.visualElement.contentRect;
            var p = ctx.painter2D;
            Vector2 c = r.center;
            float rad = Mathf.Min(r.width, r.height) * 0.34f;
            // La flèche circulaire : un arc épais (contour sombre puis or), et sa pointe.
            p.lineCap = LineCap.Round;
            foreach (var (w, col) in new[] { (17f, new Color(0.42f, 0.16f, 0.06f)), (10f, new Color(1f, 0.835f, 0.29f)) })
            {
                p.strokeColor = col;
                p.lineWidth = w;
                p.BeginPath();
                p.Arc(c, rad, Angle.Degrees(-60f), Angle.Degrees(230f));
                p.Stroke();
            }
            Vector2 tip = c + new Vector2(Mathf.Cos(-60f * Mathf.Deg2Rad), Mathf.Sin(-60f * Mathf.Deg2Rad)) * rad;
            p.fillColor = new Color(1f, 0.835f, 0.29f);
            p.strokeColor = new Color(0.42f, 0.16f, 0.06f);
            p.lineWidth = 3f;
            p.BeginPath();
            p.MoveTo(tip + new Vector2(14f, -2f));
            p.LineTo(tip + new Vector2(-10f, -14f));
            p.LineTo(tip + new Vector2(-4f, 14f));
            p.ClosePath();
            p.Fill();
            p.Stroke();
            // L'étoile au centre (5 branches).
            p.BeginPath();
            for (int i = 0; i < 10; i++)
            {
                float a = -Mathf.PI / 2f + i * Mathf.PI / 5f;
                float rr = i % 2 == 0 ? rad * 0.55f : rad * 0.24f;
                var pt = c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * rr;
                if (i == 0) p.MoveTo(pt); else p.LineTo(pt);
            }
            p.ClosePath();
            p.fillColor = new Color(1f, 0.95f, 0.7f);
            p.Fill();
            p.Stroke();
        }

        static void SetIcon(Button b, Sprite icon)
        {
            var e = b.Q<VisualElement>(className: "col-btn-icon");
            if (e != null && icon != null) e.style.backgroundImage = new StyleBackground(icon);
        }

        void OnColBoutique() => game.TogglePanel(PanelId.Boutique);
        void OnColInventaire() => game.TogglePanel(PanelId.Inventaire);
        void OnColIndex() => game.TogglePanel(PanelId.Index);
        void OnColAmeliorations() => game.TogglePanel(PanelId.Ameliorations);
        void OnColClassement() => game.TogglePanel(PanelId.Classement);
        void OnColRenaissance() => game.TogglePanel(PanelId.Renaissance);
        void OnGear() => game.TogglePanel(PanelId.Reglages);

        /// Un clic sur le fond assombri ferme la pop-up (le GameManager refuse pendant une
        /// ouverture de boîte).
        void OnDimClicked(PointerDownEvent evt) => game.ClosePanel();

        /// Survol d'un bouton de colonne : en plus du rebond, l'icône se dandine
        /// (rotation −6° → +6° → 0 en 0,3 s).
        void OnColumnHover(PointerEnterEvent evt)
        {
            if (!(evt.currentTarget is Button b) || !b.enabledInHierarchy) return;
            var icon = b.Q<VisualElement>(className: "col-btn-icon");
            if (icon != null) StartCoroutine(Wiggle(icon));
        }

        /// Le dandinement de l'icône au survol (art bible §12.17.3 : −8° → +8° → 0 en 0,35 s).
        static IEnumerator Wiggle(VisualElement icon)
        {
            for (float t = 0f; t < 0.35f; t += Time.deltaTime)
            {
                float k = t / 0.35f;
                float angle = k < 0.33f ? Mathf.Lerp(0f, -8f, k / 0.33f)
                            : k < 0.66f ? Mathf.Lerp(-8f, 8f, (k - 0.33f) / 0.33f)
                            : Mathf.Lerp(8f, 0f, (k - 0.66f) / 0.34f);
                icon.style.rotate = new Rotate(angle);
                yield return null;
            }
            icon.style.rotate = StyleKeyword.Null;
        }

        bool OpeningBusy => openingRequested || (opening != null && opening.IsBusy);

        void Update()
        {
            // Échap ferme la pop-up, comme « × » — sauf pendant (ou juste à la fin d')une
            // ouverture de boîte : là, Échap appartient à l'écran d'ouverture.
            var kb = Keyboard.current;
            bool openingBlocks = opening != null && opening.BlocksEscape;
            if (game.IsPanelOpen && !openingBlocks && kb != null && kb.escapeKey.wasPressedThisFrame)
            {
                // v5 : Échap ferme d'abord ce qui est par-dessus (fiche agrandie, fenêtre de
                // remplacement, confirmation), puis la pop-up au 2e appui.
                bool consumed = (shownPanel == PanelId.Index && index.HandleEscape())
                             || (shownPanel == PanelId.Boutique && boutique.HandleEscape())
                             || (shownPanel == PanelId.Inventaire && inventory.HandleEscape())
                             || (shownPanel == PanelId.Renaissance && rebirthPage.HandleEscape())
                             || (shownPanel == PanelId.Route && routePage.HandleEscape())
                             || (shownPanel == PanelId.Inventaire && inventory.HandleEscape());
                if (!consumed) game.ClosePanel();
            }
            if (shownPanel == PanelId.Index) index.Tick(Time.deltaTime);
            if (shownPanel == PanelId.Boutique) boutique.Tick(Time.deltaTime);
            if (shownPanel == PanelId.Route) routePage.Tick(Time.deltaTime);
            if (shownPanel == PanelId.Inventaire) inventory.Tick(Time.deltaTime);
            if (shownPanel == PanelId.Renaissance) rebirthPage.Tick(Time.deltaTime);
            TickToastQueue(Time.deltaTime);
            infoBulle.Tick(Time.deltaTime);
            TickToasts(Time.deltaTime);

            wallet.Tick(Time.deltaTime);
            walletLabel.text = ShopUi.Thousands(wallet.Displayed); // v6 (QA n°47) : « 11 992 »
            AnimatePopup();
        }

        // =====================================================================
        // Colonnes, roue dentée et pop-up : suivre le GameManager
        // =====================================================================

        void OnGameChanged()
        {
            UpdateColumns();

            // Rappel de la cagnotte (seulement si elle est supérieure à 0).
            int pot = game.Pot;
            potNote.text = pot > 0 ? $"Ta cagnotte en jeu ({pot}) ne se dépense pas : encaisse-la d'abord." : "";

            if (game.PanelOpen == shownPanel) return;
            PanelId previous = shownPanel;
            shownPanel = game.PanelOpen;
            showingName = false;
            avatar.ShowEquipped(); // fin de l'aperçu à chaque changement
            if (playerSpinner != null) playerSpinner.ResetView(); // v6.5 §5 : le cheval revient de face

            if (shownPanel == PanelId.None)
            {
                if (showcase != null) showcase.SetActive(false);
                if (blankHorse != null) blankHorse.SetActive(false);
                StartAnim(Anim.Closing);
                return;
            }
            frame.style.display = DisplayStyle.Flex;
            dim.style.display = DisplayStyle.Flex;
            frameVisible = true;
            if (previous == PanelId.None) wallet.Snap(Wallet.Coins);
            ShowPage(shownPanel);
            // Ouverture (fond 0 → 65 %, fenêtre qui rebondit) ou passage d'une pop-up à
            // l'autre (le fond reste, la nouvelle fenêtre fait sa petite apparition).
            StartAnim(previous == PanelId.None ? Anim.Opening : Anim.Switching);
        }

        /// Les colonnes et la roue dentée : visibles selon le moment, grisées quand c'est interdit.
        void UpdateColumns()
        {
            bool title = game.State == GameState.Title;
            // Colonnes : jamais sur l'écran titre (v4 §4.2), ni pendant l'ouverture d'une boîte.
            bool showColumns = !title && columnsShown && !game.BoxOpeningActive;
            columnLeft.style.display = showColumns ? DisplayStyle.Flex : DisplayStyle.None;
            columnRight.style.display = showColumns ? DisplayStyle.Flex : DisplayStyle.None;
            gearHolder.style.display = game.BoxOpeningActive ? DisplayStyle.None : DisplayStyle.Flex;
            // v5 §6.2 : pendant l'ouverture 3D, le studio filme tout l'écran : la pop-up (et son
            // fond assombri) sont cachées, puis retrouvées telles quelles après « OK ».
            var hidden = game.BoxOpeningActive ? Visibility.Hidden : Visibility.Visible;
            frame.style.visibility = hidden;
            dim.style.visibility = hidden;

            bool allowed = game.CanOpenPanel;
            foreach (var (b, id) in new[] { (colBoutique, PanelId.Boutique), (colInventaire, PanelId.Inventaire),
                                             (colIndex, PanelId.Index), (colAmeliorations, PanelId.Ameliorations),
                                             (colClassement, PanelId.Classement), (colRenaissance, PanelId.Renaissance) })
            {
                b.SetEnabled(allowed);
                ShopUi.SetClass(b, "col-btn--active", game.PanelOpen == id);
            }
            gearButton.SetEnabled(game.CanOpenSettings);
            // v5 §13.5 : badge doré « ×2 » sur le bouton une fois la renaissance faite.
            var badge = colRenaissance.Q<VisualElement>(className: "col-btn-badge");
            if (badge != null) ShopUi.SetClass(badge, "col-btn-badge--visible", Rebirth.IsDone);
        }

        /// Fin du fondu de « Jouer » (1er FloorShown) : les colonnes glissent en place.
        void OnFloorShown()
        {
            if (columnsShown) return;
            columnsShown = true;
            UpdateColumns();
            if (columnsIntro != null) StopCoroutine(columnsIntro);
            columnsIntro = StartCoroutine(ColumnsIntro());
        }

        /// L'arrivée des colonnes (spec §4.3) : chaque bouton part de 160 px hors de l'écran et
        /// arrive en 0,3 s avec un petit dépassement, du haut vers le bas, 0,05 s d'écart, avec
        /// un ui_survol de plus en plus aigu. Le Classement arrive avec le 1er bouton de gauche.
        /// La roue dentée glisse à droite du badge d'étage (0,2 s).
        IEnumerator ColumnsIntro()
        {
            var left = new[] { colBoutique, colInventaire, colIndex, colAmeliorations };
            var right = new[] { colClassement, colRenaissance };
            float[] pitches = { 1f, 1.06f, 1.12f, 1.19f };
            foreach (var b in left) b.style.translate = new Translate(-ColumnStartX, 0f);
            foreach (var b in right) b.style.translate = new Translate(ColumnStartX, 0f);

            float total = ColumnStagger * (left.Length - 1) + ColumnSlideTime;
            int played = 0;
            for (float t = 0f; t < total + Time.deltaTime; t += Time.deltaTime)
            {
                for (int i = 0; i < left.Length; i++)
                {
                    float k = Mathf.Clamp01((t - i * ColumnStagger) / ColumnSlideTime);
                    left[i].style.translate = new Translate(-ColumnStartX * (1f - Ease.OutBack(k)), 0f);
                    if (i < right.Length) right[i].style.translate = new Translate(ColumnStartX * (1f - Ease.OutBack(k)), 0f);
                }
                while (played < left.Length && t >= played * ColumnStagger)
                {
                    if (SoundManager.Instance != null) SoundManager.Instance.Play(Sfx.UiSurvol, 0.5f, pitches[played]);
                    played++;
                }
                float g = Ease.Out(Mathf.Clamp01(t / GearSlideTime));
                gearHolder.style.translate = new Translate(GearInGameOffset.x * g, GearInGameOffset.y * g);
                yield return null;
            }
            foreach (var b in left) b.style.translate = StyleKeyword.Null;
            foreach (var b in right) b.style.translate = StyleKeyword.Null;
            gearHolder.style.translate = new Translate(GearInGameOffset.x, GearInGameOffset.y);
            columnsIntro = null;
        }

        // =====================================================================
        // La fenêtre : taille, bandeau, page
        // =====================================================================

        void ShowPage(PanelId panel)
        {
            // La vitrine du cheval du joueur : seulement l'Inventaire (v5 §8.1 : plus dans l'Index).
            bool split = panel == PanelId.Inventaire;
            if (showcase != null)
            {
                showcase.SetActive(split);
                if (split) invShowcase.style.backgroundImage = new StyleBackground(Background.FromRenderTexture(showcase.Texture));
            }
            // Le cheval vierge ne sert qu'à la fiche agrandie : c'est elle qui le rallume.
            if (blankHorse != null) blankHorse.SetActive(false);

            Vector2 size;
            string key, text;
            if (panel == PanelId.Classement && showingName)
            {
                size = new Vector2(640f, 420f); key = "pseudo"; text = "Choisis ton pseudo";
            }
            else switch (panel)
            {
                case PanelId.Boutique: size = new Vector2(1160f, 820f); key = "boutique"; text = "Boutique"; break;
                case PanelId.Inventaire: size = new Vector2(1600f, 860f); key = "inventaire"; text = "Inventaire"; break; // v6.5 : « Casier » (art bible §16.1.1)
                case PanelId.Index: size = new Vector2(1360f, 820f); key = "index"; text = "Index"; break;
                case PanelId.Ameliorations: size = new Vector2(1000f, 840f); key = "ameliorations"; text = "Améliorations"; break;
                case PanelId.Classement: size = new Vector2(920f, 840f); key = "classement"; text = "Classement"; break;
                case PanelId.Renaissance: size = new Vector2(1100f, 840f); key = "renaissance"; text = "Renaissance"; break;  // v6 §15.4
                case PanelId.Route: size = new Vector2(1120f, 860f); key = "route"; text = "L'Ascension"; break;      // v6.5 : art bible §16.2.1
                // v6 (art bible §15.7) : 640 × 600 avec la nouvelle ligne Graphismes.
                default: size = new Vector2(640f, 600f); key = "reglages"; text = "Réglages"; break;
            }
            frame.style.width = size.x;
            frame.style.height = size.y;
            title.text = text;
            foreach (var k in new[] { "boutique", "inventaire", "index", "ameliorations", "classement", "reglages", "pseudo", "renaissance", "route" })
                ShopUi.SetClass(header, "popup-header--" + k, k == key);
            SetHeaderIcon(panel);
            // v6.5 : la croix de l'Inventaire (fenêtre de 1600 px) rentre un peu, pour ne pas
            // toucher le compteur de diamants du HUD en 1280 × 720.
            ShopUi.SetClass(closeButton, "popup-close--interieur", panel == PanelId.Inventaire);

            // Le porte-monnaie et la note de la cagnotte : Inventaire, Index, Améliorations (la
            // Boutique et la Renaissance affichent leurs propres soldes).
            // v6.5 : plus dans l'Inventaire « Casier », dont les 3 zones prennent toute la hauteur.
            bool shopPopup = panel == PanelId.Index || panel == PanelId.Ameliorations;
            walletRow.style.display = shopPopup ? DisplayStyle.Flex : DisplayStyle.None;
            potNote.style.display = (shopPopup || panel == PanelId.Boutique) && game.Pot > 0 ? DisplayStyle.Flex : DisplayStyle.None;

            ShopUi.SetClass(pageBoutique, "shop-page--active", panel == PanelId.Boutique);
            ShopUi.SetClass(pageInventaire, "shop-page--active", panel == PanelId.Inventaire);
            ShopUi.SetClass(pageIndex, "shop-page--active", panel == PanelId.Index);
            ShopUi.SetClass(pageAmeliorations, "shop-page--active", panel == PanelId.Ameliorations);
            ShopUi.SetClass(pageClassement, "shop-page--active", panel == PanelId.Classement && !showingName);
            ShopUi.SetClass(pagePseudo, "shop-page--active", panel == PanelId.Classement && showingName);
            ShopUi.SetClass(pageReglages, "shop-page--active", panel == PanelId.Reglages);
            ShopUi.SetClass(pageRenaissance, "shop-page--active", panel == PanelId.Renaissance);
            ShopUi.SetClass(pageRoute, "shop-page--active", panel == PanelId.Route);

            switch (panel)
            {
                case PanelId.Boutique: boutique.Open(pendingBoxContent, pendingShopItem); break;
                case PanelId.Inventaire:
                    inventory.Open(pendingInventoryFocus);
                    // v5 §4.4 : le message de la migration, une seule fois.
                    if (Shop.MigrationToastPending)
                    {
                        Shop.MigrationToastPending = false;
                        ShowToast("Nouveau : 2 accessoires à la fois. On a gardé tes 2 plus rares.");
                    }
                    break;
                case PanelId.Index: index.Open(); break;
                case PanelId.Ameliorations: upgrades.Open(); break;
                case PanelId.Renaissance: rebirthPage.Open(); break;
                case PanelId.Route: routePage.Open(); break;
                case PanelId.Classement:
                    // 1re ouverture : on demande d'abord le pseudo (§12.7), jamais au lancement.
                    if (!showingName && !Leaderboard.HasName) { ShowNamePage(); return; }
                    if (showingName) namePage.Open(); else leaderboard.Open();
                    break;
                case PanelId.Reglages: settings.Refresh(); break;
            }
            pendingInventoryFocus = null;
            pendingBoxContent = null;
            pendingShopItem = null;
        }

        /// v5.1 (art bible §12.18) : la grande icône du menu revient, en haut à gauche du titre.
        /// C'est la même image que le bouton de la colonne (le pseudo prend celle du Classement).
        /// Sans image de Renaissance, la flèche dorée est dessinée par le code, comme dans la colonne.
        void SetHeaderIcon(PanelId panel)
        {
            Sprite icon = panel switch
            {
                PanelId.Boutique => iconBoutique,
                PanelId.Inventaire => iconInventaire,
                PanelId.Index => iconIndex,
                PanelId.Ameliorations => iconAmeliorations,
                PanelId.Classement => iconClassement,
                PanelId.Renaissance => iconRenaissance,
                PanelId.Route => UiImages.Route,
                _ => iconReglages
            };
            popupIcon.generateVisualContent -= DrawRebirthIcon; // jamais deux fois
            bool drawn = panel == PanelId.Renaissance && icon == null;
            if (drawn) popupIcon.generateVisualContent += DrawRebirthIcon;
            popupIcon.style.backgroundImage = icon != null ? new StyleBackground(icon) : new StyleBackground(StyleKeyword.None);
            popupIcon.style.display = icon != null || drawn ? DisplayStyle.Flex : DisplayStyle.None;
            popupIcon.MarkDirtyRepaint();
        }

        /// « Choisis ton pseudo » (1re ouverture du Classement, ou « Changer de pseudo »).
        void ShowNamePage()
        {
            if (shownPanel != PanelId.Classement) return;
            bool wasName = showingName;
            showingName = true;
            ShowPage(PanelId.Classement);
            if (!wasName)
            {
                StartAnim(Anim.Switching);
                if (SoundManager.Instance != null) SoundManager.Instance.Play(Sfx.UiBoutiqueOuvre, 1f, 1.12f);
            }
        }

        /// Pseudo validé : retour à la liste du Classement.
        void OnNameDone()
        {
            if (shownPanel != PanelId.Classement) return;
            showingName = false;
            ShowPage(PanelId.Classement);
            StartAnim(Anim.Switching);
        }

        // =====================================================================
        // Animations de la fenêtre (spec §5.6)
        // =====================================================================

        void StartAnim(Anim a)
        {
            anim = a;
            animTime = 0f;
            AnimatePopup();
        }

        void AnimatePopup()
        {
            if (anim == Anim.None) return;
            animTime += Time.deltaTime;
            switch (anim)
            {
                case Anim.Opening:
                {
                    float k = Mathf.Clamp01(animTime / OpenTime);
                    float s = Ease.Pop(k, 0.85f, 1.04f);
                    frame.style.scale = new Scale(new Vector3(s, s, 1f));
                    frame.style.opacity = Mathf.Clamp01(k / 0.6f);
                    dim.style.opacity = Mathf.Clamp01(animTime / DimInTime);
                    if (animTime >= Mathf.Max(OpenTime, DimInTime)) EndAnim();
                    break;
                }
                case Anim.Switching:
                {
                    float k = Mathf.Clamp01(animTime / SwitchTime);
                    float s = Mathf.Lerp(0.9f, 1f, Ease.OutBack(k));
                    frame.style.scale = new Scale(new Vector3(s, s, 1f));
                    frame.style.opacity = 1f;
                    dim.style.opacity = 1f;
                    if (k >= 1f) EndAnim();
                    break;
                }
                case Anim.Closing:
                {
                    float k = Mathf.Clamp01(animTime / CloseTime);
                    float s = Mathf.Lerp(1f, 0.9f, Ease.In(k));
                    frame.style.scale = new Scale(new Vector3(s, s, 1f));
                    frame.style.opacity = 1f - Ease.In(k);
                    dim.style.opacity = 1f - k;
                    if (k >= 1f)
                    {
                        frame.style.display = DisplayStyle.None;
                        dim.style.display = DisplayStyle.None;
                        frameVisible = false;
                        EndAnim();
                    }
                    break;
                }
            }
        }

        void EndAnim()
        {
            if (anim != Anim.Closing)
            {
                frame.style.scale = new Scale(Vector3.one);
                frame.style.opacity = 1f;
                dim.style.opacity = 1f;
            }
            anim = Anim.None;
        }

        // =====================================================================
        // Défilement intégré (spec §5.7)
        // =====================================================================

        /// Prépare une zone qui défile (publique : les pages qui créent leurs ScrollView par le
        /// code l'appellent aussi).
        public static void SetupScroll(ScrollView sv)
        {
            sv.mode = ScrollViewMode.Vertical;
            sv.verticalScrollerVisibility = ScrollerVisibility.Auto;
            sv.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            sv.mouseWheelScrollSize = 80f;
            sv.touchScrollBehavior = ScrollView.TouchScrollBehavior.Clamped; // pas d'effet élastique
            var top = new VisualElement { pickingMode = PickingMode.Ignore };
            top.AddToClassList("scroll-fade");
            top.AddToClassList("scroll-fade--haut");
            var bottom = new VisualElement { pickingMode = PickingMode.Ignore };
            bottom.AddToClassList("scroll-fade");
            bottom.AddToClassList("scroll-fade--bas");
            // hierarchy.Add : posées sur la zone qui défile elle-même, pas dans son contenu.
            sv.hierarchy.Add(top);
            sv.hierarchy.Add(bottom);
            void Update()
            {
                float max = sv.contentContainer.layout.height - sv.contentViewport.layout.height;
                float y = sv.scrollOffset.y;
                top.style.display = max > 1f && y > 1f ? DisplayStyle.Flex : DisplayStyle.None;
                bottom.style.display = max > 1f && y < max - 1f ? DisplayStyle.Flex : DisplayStyle.None;
            }
            sv.verticalScroller.valueChanged += _ => Update();
            sv.contentContainer.RegisterCallback<GeometryChangedEvent>(_ => Update());
            sv.RegisterCallback<GeometryChangedEvent>(_ => Update());
        }

        /// La collection, le porte-monnaie ou un niveau ont changé : on redessine la page visible.
        void Refresh()
        {
            if (wallet.Target != Wallet.Coins) wallet.RollTo(Wallet.Coins, 0.4f, Ease.Out);
            if (shownPanel == PanelId.None) return;
            // B1 : pendant une ouverture de boîte, rien ne se redessine (le compteur de
            // collection révélerait « nouveau / doublon » avant l'animation).
            if (OpeningBusy) { refreshPending = true; return; }
            RefreshPage();
        }

        void RefreshPage()
        {
            refreshPending = false;
            switch (shownPanel)
            {
                case PanelId.Boutique: boutique.Refresh(); break;
                case PanelId.Inventaire: inventory.Refresh(); break;
                case PanelId.Index: index.Refresh(); break;
                case PanelId.Ameliorations: upgrades.Refresh(); break;
                case PanelId.Renaissance: rebirthPage.Refresh(); break;
                case PanelId.Route: routePage.Refresh(); break;
                case PanelId.Classement: if (!showingName) leaderboard.Refresh(); break;
            }
        }

        // =====================================================================
        // Navigation, aperçu, équiper, vendre, ouvrir, améliorer
        // =====================================================================

        /// « Voir la boîte » (fiche agrandie de l'Index) : ouvre la Boutique directement sur la
        /// sous-page « Contenu et taux » de cette boîte (v5 §8.4, §15.4).
        void ShowBoxContent(LootBox box)
        {
            pendingBoxContent = box;
            if (shownPanel == PanelId.Boutique) { boutique.Open(box); pendingBoxContent = null; }
            else game.OpenPanel(PanelId.Boutique);
        }

        /// v6 §6.1 : « Voir dans la Boutique » (fiche d'un objet non obtenu de l'Index) : ouvre la
        /// Boutique sur « Contenu et taux » de sa boîte, avec la fiche de l'objet déjà ouverte.
        void ShowInShop(ShopItem item)
        {
            var box = Shop.BoxOf(item);
            if (box == null) return;
            pendingBoxContent = box;
            pendingShopItem = item;
            if (shownPanel == PanelId.Boutique) { boutique.Open(box, item); pendingBoxContent = null; pendingShopItem = null; }
            else game.OpenPanel(PanelId.Boutique);
        }

        /// « Voir dans l'inventaire » (Index) : ouvre l'Inventaire sur cet objet.
        void ShowInInventory(ShopItem item)
        {
            pendingInventoryFocus = item;
            game.OpenPanel(PanelId.Inventaire);
        }

        /// Aperçu (Inventaire) : la tenue TELLE QU'ELLE SERAIT après équipement (v5 §4.4 : si
        /// 2 accessoires sont portés et que l'emplacement est libre, on enlève le moins rare).
        void Preview(ShopItem item)
        {
            avatar.ShowEquipped();
            if (item == null) return; // v6.5 : rien à essayer, le cheval garde sa tenue
            if (Shop.NeedsReplaceChoice(item))
            {
                var removed = Shop.LeastRare(Shop.EquippedAccessories());
                if (removed != null) avatar.Show(Shop.DefaultAccessory(removed.Slot));
            }
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

        /// Achat d'un niveau : son (« max » au niveau 10, v4 §10.1) et petit saut du cheval.
        void OnUpgradeBought(string id, int level)
        {
            if (SoundManager.Instance != null)
                SoundManager.Instance.Play(level >= Upgrades_.MaxLevel ? Sfx.AmeliorationMax : Sfx.AmeliorationAchat);
            if (avatarAnimator != null) avatarAnimator.WelcomeHop();
        }

        /// Fin de l'écran d'ouverture (« OK ») : retour à la Boutique, objets équipés, et les
        /// pages sont enfin redessinées (B1).
        void OnOpeningClosed()
        {
            avatar.ShowEquipped();
            // v6 §6.3 : l'ouverture qui vient de compléter une collection donne sa récompense,
            // juste après l'écran de résultat (au clic sur OK). Le toast suit (OnCollectionReward).
            CollectionRewards.GrantAllPending();
            if (shownPanel == PanelId.Boutique) RefreshPage();
            else if (shownPanel != PanelId.None) game.OpenPanel(PanelId.Boutique);
            if (refreshPending) RefreshPage();
            OnGameChanged();
        }

        /// Clic sur « Ouvrir » : la règle (payer, tirer, sauvegarder) est dans Shop.TryOpenBox ;
        /// ici on transmet le clic puis on lance l'animation avec le résultat déjà connu.
        void OnOpenClicked(LootBox box)
        {
            if (opening == null || opening.IsBusy || game.BoxOpeningActive) return;
            openingRequested = true; // B1 : Shop.Changed arrive AVANT Play(), on ne redessine pas
            if (Shop.TryOpenBox(box, out var result)) opening.Play(result);
            openingRequested = false;
        }

        // =====================================================================
        // Outils de test (depuis `unity command script eval`)
        // =====================================================================

        public void DebugClickOpen(string boxId) => OnOpenClicked(Shop.FindBox(boxId));
        public void DebugClickBuy(string upgradeId) => upgrades.DebugClickBuy(upgradeId);
        /// Simule un clic sur un bouton de colonne (1 Boutique … 4 Améliorations, 5 Classement,
        /// 6 Réglages). Refusé comme un vrai clic si l'ouverture est interdite.
        public void DebugClickColumn(int panel) => game.TogglePanel((PanelId)panel);
        public void DebugClickDim() => game.ClosePanel();
        public string DebugBoutiqueCollectionText()
        {
            var label = pageBoutique.Q<Label>(className: "progress-text");
            return label != null ? label.text : "";
        }

        // =====================================================================
        // v6 : récompenses (collection, paliers) et échange
        // =====================================================================

        // Les messages en attente : un toutes les 1,5 s (v6 §6.3).
        readonly Queue<string> toastQueue = new Queue<string>();
        float toastQueueTimer;
        const float ToastGap = 1.5f;

        /// Ajoute un message à la file (affiché dès que le précédent a 1,5 s).
        public void QueueToast(string text) => toastQueue.Enqueue(text);

        void TickToastQueue(float dt)
        {
            toastQueueTimer -= dt;
            if (toastQueue.Count == 0 || toastQueueTimer > 0f) return;
            ShowToast(toastQueue.Dequeue());
            toastQueueTimer = ToastGap;
        }

        void OnCollectionReward(LootBox box, int diamonds)
        {
            QueueToast($"Collection complète : {box.displayName} ! +{diamonds} {ShopUi.Diamant}");
            if (SoundManager.Instance != null) SoundManager.Instance.Play(Sfx.DiamantGain1);
            Refresh();
        }

        void OnTrophyCatchUp(int diamonds, int record)
        {
            QueueToast($"L'Ascension : +{diamonds} {ShopUi.Diamant} pour ton record (étage {record})");
            if (SoundManager.Instance != null) SoundManager.Instance.Play(Sfx.DiamantGain1);
        }

        /// v6 §5.4 : échange confirmé : son de pièces (les compteurs défilent par Refresh).
        void OnExchanged(CoinPackDef pack)
        {
            if (SoundManager.Instance != null) SoundManager.Instance.Play(Sfx.PiecesAchat);
            Refresh();
        }

        // =====================================================================
        // v5 : petits messages (toasts, art bible §12.12)
        // =====================================================================

        readonly List<(VisualElement e, float age)> toasts = new List<(VisualElement, float)>();
        const float ToastTime = 3.5f;

        /// Affiche un petit message en haut de l'écran pendant 3,5 s (il apparaît et s'efface).
        public void ShowToast(string text)
        {
            var toast = new VisualElement { pickingMode = PickingMode.Ignore };
            toast.AddToClassList("toast");
            toast.Add(ShopUi.NewLabel(text, "toast__texte"));
            toastLayer.Add(toast);
            toasts.Add((toast, 0f));
        }

        void TickToasts(float dt)
        {
            for (int i = toasts.Count - 1; i >= 0; i--)
            {
                var (e, age) = toasts[i];
                age += dt;
                e.style.opacity = age < 0.2f ? age / 0.2f : age > ToastTime - 0.3f ? Mathf.Clamp01((ToastTime - age) / 0.3f) : 1f;
                e.style.translate = new Translate(0f, age < 0.2f ? -20f * (1f - age / 0.2f) : 0f);
                if (age >= ToastTime) { e.RemoveFromHierarchy(); toasts.RemoveAt(i); }
                else toasts[i] = (e, age);
            }
        }

        /// Le texte du dernier petit message affiché (pour les tests).
        public string LastToastText
        {
            get
            {
                if (toasts.Count == 0) return "";
                var l = toasts[toasts.Count - 1].e.Q<Label>();
                return l != null ? l.text : "";
            }
        }
    }
}
