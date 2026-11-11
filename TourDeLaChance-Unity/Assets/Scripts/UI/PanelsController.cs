using System.Collections;
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
    ///     d'une pop-up à l'autre), son bandeau de titre, son icône, le bouton fermer, Échap ;
    ///   - la vitrine du cheval (Inventaire, Index) ;
    ///   - et confie le contenu aux pages (BoutiquePage, InventoryPage, IndexPage, UpgradesPage,
    ///     LeaderboardPopup, SettingsPage, NamePage).
    /// Changer de pop-up ou fermer remet les objets équipés sur le cheval (fin de l'aperçu).
    ///
    /// Correctif B1 : pendant une ouverture de boîte, les pages ne se redessinent PAS (elles
    /// le font après « OK ») ; seul le porte-monnaie défile. Correctif B9 : tout ce qui est
    /// branché dans OnEnable est débranché dans OnDisable.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class PanelsController : MonoBehaviour
    {
        [SerializeField] GameManager game;
        [SerializeField] PlayerAvatar avatar;
        [SerializeField] PlayerAnimator avatarAnimator; // petit saut d'accueil à l'aperçu
        [SerializeField] BoxOpeningController opening;  // l'écran d'ouverture d'une boîte
        [SerializeField] HorseShowcase showcase;        // la caméra de la vitrine (facultative)

        [Header("Icônes des pop-ups (colonnes et bandeau)")]
        [SerializeField] Sprite iconBoutique;
        [SerializeField] Sprite iconInventaire;
        [SerializeField] Sprite iconIndex;
        [SerializeField] Sprite iconAmeliorations;
        [SerializeField] Sprite iconClassement;
        [SerializeField] Sprite iconReglages;

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
        static readonly Vector2 GearInGameOffset = new Vector2(32f + 200f + 16f - 32f, 24f + 58f - 36f - 32f);

        VisualElement dim, frame, window, header, popupIcon, walletRow, columnLeft, columnRight, gearHolder;
        VisualElement pageBoutique, pageInventaire, pageIndex, pageAmeliorations, pageClassement, pageReglages, pagePseudo;
        VisualElement invShowcase, idxShowcase;
        Label title, walletLabel, potNote;
        Button closeButton, gearButton, colBoutique, colInventaire, colIndex, colAmeliorations, colClassement;
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
        LootBox pendingIndexFocus;
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

        void OnEnable()
        {
            var doc = GetComponent<UIDocument>().rootVisualElement;
            var root = doc.Q<VisualElement>("panels-root");
            dim = doc.Q<VisualElement>("popup-dim");
            frame = doc.Q<VisualElement>("popup-frame");
            window = doc.Q<VisualElement>("popup-window");
            header = doc.Q<VisualElement>("popup-header");
            popupIcon = doc.Q<VisualElement>("popup-icon");
            title = doc.Q<Label>("popup-title");
            walletRow = doc.Q<VisualElement>("popup-wallet-row");
            walletLabel = doc.Q<Label>("panel-wallet");
            potNote = doc.Q<Label>("pot-note");
            closeButton = doc.Q<Button>("close");
            columnLeft = doc.Q<VisualElement>("column");
            columnRight = doc.Q<VisualElement>("column-right");
            gearHolder = doc.Q<VisualElement>("gear-holder");
            gearButton = doc.Q<Button>("gear");
            colBoutique = doc.Q<Button>("col-boutique");
            colInventaire = doc.Q<Button>("col-inventaire");
            colIndex = doc.Q<Button>("col-index");
            colAmeliorations = doc.Q<Button>("col-ameliorations");
            colClassement = doc.Q<Button>("col-classement");
            pageBoutique = doc.Q<VisualElement>("page-boutique");
            pageInventaire = doc.Q<VisualElement>("page-inventaire");
            pageIndex = doc.Q<VisualElement>("page-index");
            pageAmeliorations = doc.Q<VisualElement>("page-ameliorations");
            pageClassement = doc.Q<VisualElement>("page-classement");
            pageReglages = doc.Q<VisualElement>("page-reglages");
            pagePseudo = doc.Q<VisualElement>("page-pseudo");
            invShowcase = doc.Q<VisualElement>("inv-showcase");
            idxShowcase = doc.Q<VisualElement>("idx-showcase");

            SetIcon(colBoutique, iconBoutique);
            SetIcon(colInventaire, iconInventaire);
            SetIcon(colIndex, iconIndex);
            SetIcon(colAmeliorations, iconAmeliorations);
            SetIcon(colClassement, iconClassement);

            boutique = new BoutiquePage(doc.Q<ScrollView>("page-boutique"));
            inventory = new InventoryPage(pageInventaire);
            index = new IndexPage(pageIndex);
            upgrades = new UpgradesPage(doc.Q<ScrollView>("page-ameliorations"), this);
            leaderboard = new LeaderboardPopup(pageClassement);
            settings = new SettingsPage(pageReglages);
            namePage = new NamePage(pagePseudo);

            // Toutes les zones qui défilent : barre dorée intégrée, molette 80 px par cran,
            // jamais de barre horizontale, petites ombres « encore du contenu » en haut et en bas.
            foreach (var sv in doc.Query<ScrollView>().ToList()) SetupScroll(sv);

            boutique.OpenRequested += OnOpenClicked;
            boutique.ShowContentRequested += ShowContent;
            inventory.PreviewRequested += Preview;
            inventory.Equipped += OnEquipped;
            inventory.Sold += OnSold;
            index.PreviewRequested += Preview;
            index.ShowInInventoryRequested += ShowInInventory;
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
            dim.RegisterCallback<PointerDownEvent>(OnDimClicked);
            foreach (var b in new[] { colBoutique, colInventaire, colIndex, colAmeliorations, colClassement })
                b.RegisterCallback<PointerEnterEvent>(OnColumnHover);
            game.Changed += OnGameChanged;
            game.FloorShown += OnFloorShown;
            Shop.Changed += Refresh;
            Upgrades.Changed += Refresh;
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

        void OnDisable()
        {
            boutique.OpenRequested -= OnOpenClicked;
            boutique.ShowContentRequested -= ShowContent;
            inventory.PreviewRequested -= Preview;
            inventory.Equipped -= OnEquipped;
            inventory.Sold -= OnSold;
            index.PreviewRequested -= Preview;
            index.ShowInInventoryRequested -= ShowInInventory;
            upgrades.Bought -= OnUpgradeBought;
            leaderboard.RenameRequested -= ShowNamePage;
            namePage.Done -= OnNameDone;
            inventory.Dispose();
            index.Dispose();
            upgrades.Dispose();
            leaderboard.Dispose();
            settings.Dispose();
            namePage.Dispose();

            closeButton.clicked -= game.ClosePanel;
            gearButton.clicked -= OnGear;
            colBoutique.clicked -= OnColBoutique;
            colInventaire.clicked -= OnColInventaire;
            colIndex.clicked -= OnColIndex;
            colAmeliorations.clicked -= OnColAmeliorations;
            colClassement.clicked -= OnColClassement;
            dim.UnregisterCallback<PointerDownEvent>(OnDimClicked);
            foreach (var b in new[] { colBoutique, colInventaire, colIndex, colAmeliorations, colClassement })
                b.UnregisterCallback<PointerEnterEvent>(OnColumnHover);
            game.Changed -= OnGameChanged;
            game.FloorShown -= OnFloorShown;
            Shop.Changed -= Refresh;
            Upgrades.Changed -= Refresh;
            if (opening != null) opening.Closed -= OnOpeningClosed;
            UiSounds.Unregister(GetComponent<UIDocument>().rootVisualElement.Q<VisualElement>("panels-root"));
            if (showcase != null) showcase.SetActive(false);
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

        static IEnumerator Wiggle(VisualElement icon)
        {
            for (float t = 0f; t < 0.3f; t += Time.deltaTime)
            {
                float k = t / 0.3f;
                // −6° au premier tiers, +6° au deuxième, 0 à la fin.
                float angle = k < 0.33f ? Mathf.Lerp(0f, -6f, k / 0.33f)
                            : k < 0.66f ? Mathf.Lerp(-6f, 6f, (k - 0.33f) / 0.33f)
                            : Mathf.Lerp(6f, 0f, (k - 0.66f) / 0.34f);
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
                game.ClosePanel();

            wallet.Tick(Time.deltaTime);
            walletLabel.text = wallet.Displayed.ToString();
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

            if (shownPanel == PanelId.None)
            {
                if (showcase != null) showcase.SetActive(false);
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

            bool allowed = game.CanOpenPanel;
            foreach (var (b, id) in new[] { (colBoutique, PanelId.Boutique), (colInventaire, PanelId.Inventaire),
                                             (colIndex, PanelId.Index), (colAmeliorations, PanelId.Ameliorations),
                                             (colClassement, PanelId.Classement) })
            {
                b.SetEnabled(allowed);
                ShopUi.SetClass(b, "col-btn--active", game.PanelOpen == id);
            }
            gearButton.SetEnabled(game.CanOpenSettings);
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
            float[] pitches = { 1f, 1.06f, 1.12f, 1.19f };
            foreach (var b in left) b.style.translate = new Translate(-ColumnStartX, 0f);
            colClassement.style.translate = new Translate(ColumnStartX, 0f);

            float total = ColumnStagger * (left.Length - 1) + ColumnSlideTime;
            int played = 0;
            for (float t = 0f; t < total + Time.deltaTime; t += Time.deltaTime)
            {
                for (int i = 0; i < left.Length; i++)
                {
                    float k = Mathf.Clamp01((t - i * ColumnStagger) / ColumnSlideTime);
                    left[i].style.translate = new Translate(-ColumnStartX * (1f - Ease.OutBack(k)), 0f);
                    if (i == 0) colClassement.style.translate = new Translate(ColumnStartX * (1f - Ease.OutBack(k)), 0f);
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
            colClassement.style.translate = StyleKeyword.Null;
            gearHolder.style.translate = new Translate(GearInGameOffset.x, GearInGameOffset.y);
            columnsIntro = null;
        }

        // =====================================================================
        // La fenêtre : taille, bandeau, page
        // =====================================================================

        void ShowPage(PanelId panel)
        {
            bool split = panel == PanelId.Inventaire || panel == PanelId.Index;
            if (showcase != null)
            {
                showcase.SetActive(split);
                if (split)
                {
                    var bg = Background.FromRenderTexture(showcase.Texture);
                    invShowcase.style.backgroundImage = new StyleBackground(bg);
                    idxShowcase.style.backgroundImage = new StyleBackground(bg);
                }
            }

            Vector2 size;
            string key, text;
            Sprite icon;
            if (panel == PanelId.Classement && showingName)
            {
                size = new Vector2(640f, 420f); key = "pseudo"; text = "Choisis ton pseudo"; icon = iconClassement;
            }
            else switch (panel)
            {
                case PanelId.Boutique: size = new Vector2(1000f, 840f); key = "boutique"; text = "Boutique"; icon = iconBoutique; break;
                case PanelId.Inventaire: size = new Vector2(1240f, 840f); key = "inventaire"; text = "Inventaire"; icon = iconInventaire; break;
                case PanelId.Index: size = new Vector2(1240f, 840f); key = "index"; text = "Index"; icon = iconIndex; break;
                case PanelId.Ameliorations: size = new Vector2(1000f, 840f); key = "ameliorations"; text = "Améliorations"; icon = iconAmeliorations; break;
                case PanelId.Classement: size = new Vector2(920f, 840f); key = "classement"; text = "Classement"; icon = iconClassement; break;
                default: size = new Vector2(560f, 440f); key = "reglages"; text = "Réglages"; icon = iconReglages; break;
            }
            frame.style.width = size.x;
            frame.style.height = size.y;
            title.text = text;
            foreach (var k in new[] { "boutique", "inventaire", "index", "ameliorations", "classement", "reglages", "pseudo" })
                ShopUi.SetClass(header, "popup-header--" + k, k == key);
            popupIcon.style.backgroundImage = icon != null ? new StyleBackground(icon) : StyleKeyword.None;

            // Le porte-monnaie et la note de la cagnotte : pour les 4 pop-ups où l'on dépense.
            bool shopPopup = panel == PanelId.Boutique || panel == PanelId.Inventaire || panel == PanelId.Index || panel == PanelId.Ameliorations;
            walletRow.style.display = shopPopup ? DisplayStyle.Flex : DisplayStyle.None;
            potNote.style.display = shopPopup && game.Pot > 0 ? DisplayStyle.Flex : DisplayStyle.None;

            ShopUi.SetClass(pageBoutique, "shop-page--active", panel == PanelId.Boutique);
            ShopUi.SetClass(pageInventaire, "shop-page--active", panel == PanelId.Inventaire);
            ShopUi.SetClass(pageIndex, "shop-page--active", panel == PanelId.Index);
            ShopUi.SetClass(pageAmeliorations, "shop-page--active", panel == PanelId.Ameliorations);
            ShopUi.SetClass(pageClassement, "shop-page--active", panel == PanelId.Classement && !showingName);
            ShopUi.SetClass(pagePseudo, "shop-page--active", panel == PanelId.Classement && showingName);
            ShopUi.SetClass(pageReglages, "shop-page--active", panel == PanelId.Reglages);

            switch (panel)
            {
                case PanelId.Boutique: boutique.Open(); break;
                case PanelId.Inventaire: inventory.Open(pendingInventoryFocus); break;
                case PanelId.Index: index.Open(pendingIndexFocus); break;
                case PanelId.Ameliorations: upgrades.Open(); break;
                case PanelId.Classement:
                    // 1re ouverture : on demande d'abord le pseudo (§12.7), jamais au lancement.
                    if (!showingName && !Leaderboard.HasName) { ShowNamePage(); return; }
                    if (showingName) namePage.Open(); else leaderboard.Open();
                    break;
                case PanelId.Reglages: settings.Refresh(); break;
            }
            pendingInventoryFocus = null;
            pendingIndexFocus = null;
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

        static void SetupScroll(ScrollView sv)
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
                case PanelId.Classement: if (!showingName) leaderboard.Refresh(); break;
            }
        }

        // =====================================================================
        // Navigation, aperçu, équiper, vendre, ouvrir, améliorer
        // =====================================================================

        /// « Voir le contenu » (Boutique) : ouvre l'Index sur cette boîte.
        void ShowContent(LootBox box)
        {
            pendingIndexFocus = box;
            game.OpenPanel(PanelId.Index);
        }

        /// « Voir dans l'inventaire » (Index) : ouvre l'Inventaire sur cet objet.
        void ShowInInventory(ShopItem item)
        {
            pendingInventoryFocus = item;
            game.OpenPanel(PanelId.Inventaire);
        }

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
    }
}
