using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace TourDeLaChance
{
    /// <summary>
    /// La colonne de boutons et le cadre des 4 panneaux (Panels.uxml, spec v3 §4).
    /// Il AFFICHE et TRANSMET : quel panneau peut s'ouvrir et lequel est ouvert est décidé
    /// par le GameManager (CanOpenPanel, PanelOpen) ; ouvrir une boîte, équiper, vendre par
    /// Shop ; acheter une amélioration par Upgrades.
    ///
    /// Il gère : les 4 boutons de la colonne (actif, grisé), le glissement du cadre depuis
    /// la gauche, l'en-tête (icône, titre, ×), le rappel du porte-monnaie et de la cagnotte,
    /// et confie le contenu à BoutiquePage, InventoryPage, IndexPage et UpgradesPage.
    /// Changer de panneau ou fermer remet les objets équipés sur le cheval (fin de l'aperçu).
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

        [Header("Icônes des panneaux (colonne et en-tête)")]
        [SerializeField] Sprite iconBoutique;
        [SerializeField] Sprite iconInventaire;
        [SerializeField] Sprite iconIndex;
        [SerializeField] Sprite iconAmeliorations;

        const float PanelWidth = 660f;
        const float SlideInTime = 0.25f;  // ease-out
        const float SlideOutTime = 0.2f;  // ease-in

        VisualElement root, frame, column, frameIcon;
        VisualElement pageBoutique, pageInventaire, pageIndex, pageAmeliorations;
        Label frameTitle, walletLabel, potNote;
        Button closeButton, colBoutique, colInventaire, colIndex, colAmeliorations;
        BoutiquePage boutique;
        InventoryPage inventory;
        IndexPage index;
        UpgradesPage upgrades;

        PanelId shownPanel = PanelId.None;
        float slideTime = -1f; // ≥ 0 pendant un glissement
        bool slidingIn;
        bool frameVisible;
        bool refreshPending;  // B1 : une mise à jour attend la fin de l'ouverture de boîte
        bool openingRequested; // B1 : on est en train de demander une ouverture (avant Play)
        ShopItem pendingInventoryFocus;
        LootBox pendingIndexFocus;
        readonly RollingCounter wallet = new RollingCounter();

        /// Le panneau affiché (pour les tests).
        public PanelId ShownPanel => shownPanel;
        public bool FrameVisible => frameVisible;
        public UpgradesPage Upgrades_Page => upgrades;

        void OnEnable()
        {
            var doc = GetComponent<UIDocument>().rootVisualElement;
            root = doc.Q<VisualElement>("panels-root");
            frame = doc.Q<VisualElement>("frame");
            column = doc.Q<VisualElement>("column");
            frameIcon = doc.Q<VisualElement>("frame-icon");
            frameTitle = doc.Q<Label>("frame-title");
            walletLabel = doc.Q<Label>("panel-wallet");
            potNote = doc.Q<Label>("pot-note");
            closeButton = doc.Q<Button>("close");
            colBoutique = doc.Q<Button>("col-boutique");
            colInventaire = doc.Q<Button>("col-inventaire");
            colIndex = doc.Q<Button>("col-index");
            colAmeliorations = doc.Q<Button>("col-ameliorations");
            pageBoutique = doc.Q<VisualElement>("page-boutique");
            pageInventaire = doc.Q<VisualElement>("page-inventaire");
            pageIndex = doc.Q<VisualElement>("page-index");
            pageAmeliorations = doc.Q<VisualElement>("page-ameliorations");

            SetIcon(colBoutique, iconBoutique);
            SetIcon(colInventaire, iconInventaire);
            SetIcon(colIndex, iconIndex);
            SetIcon(colAmeliorations, iconAmeliorations);

            boutique = new BoutiquePage(doc.Q<ScrollView>("page-boutique"));
            inventory = new InventoryPage(pageInventaire);
            index = new IndexPage(pageIndex);
            upgrades = new UpgradesPage(doc.Q<ScrollView>("page-ameliorations"), this);

            boutique.OpenRequested += OnOpenClicked;
            boutique.ShowContentRequested += ShowContent;
            inventory.PreviewRequested += Preview;
            inventory.Equipped += OnEquipped;
            inventory.Sold += OnSold;
            index.PreviewRequested += Preview;
            index.ShowInInventoryRequested += ShowInInventory;
            upgrades.Bought += OnUpgradeBought;

            closeButton.clicked += game.ClosePanel;
            colBoutique.clicked += OnColBoutique;
            colInventaire.clicked += OnColInventaire;
            colIndex.clicked += OnColIndex;
            colAmeliorations.clicked += OnColAmeliorations;
            game.Changed += OnGameChanged;
            Shop.Changed += Refresh;
            Upgrades.Changed += Refresh;
            if (opening != null) opening.Closed += OnOpeningClosed;
            UiSounds.Register(root); // ui_survol / ui_clic sur tous les boutons
            ButtonStyler.Register(root);

            shownPanel = PanelId.None;
            frameVisible = false;
            frame.style.display = DisplayStyle.None; // caché au démarrage
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
            inventory.Dispose();
            index.Dispose();
            upgrades.Dispose();

            closeButton.clicked -= game.ClosePanel;
            colBoutique.clicked -= OnColBoutique;
            colInventaire.clicked -= OnColInventaire;
            colIndex.clicked -= OnColIndex;
            colAmeliorations.clicked -= OnColAmeliorations;
            game.Changed -= OnGameChanged;
            Shop.Changed -= Refresh;
            Upgrades.Changed -= Refresh;
            if (opening != null) opening.Closed -= OnOpeningClosed;
            UiSounds.Unregister(root);
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

        bool OpeningBusy => openingRequested || (opening != null && opening.IsBusy);

        void Update()
        {
            // Échap ferme le panneau, comme « × » — sauf pendant (ou juste à la fin d')une
            // ouverture de boîte : là, Échap appartient à l'écran d'ouverture.
            var kb = Keyboard.current;
            bool openingBlocks = opening != null && opening.BlocksEscape;
            if (game.IsPanelOpen && !openingBlocks && kb != null && kb.escapeKey.wasPressedThisFrame)
                game.ClosePanel();

            wallet.Tick(Time.deltaTime);
            walletLabel.text = wallet.Displayed.ToString();
            AnimateSlide();
        }

        // =====================================================================
        // Colonne et cadre : suivre le GameManager
        // =====================================================================

        void OnGameChanged()
        {
            // La colonne : visible tout le temps, sauf pendant l'ouverture d'une boîte
            // (l'écran d'ouverture recouvre tout). Grisée quand l'ouverture est interdite.
            column.style.display = game.BoxOpeningActive ? DisplayStyle.None : DisplayStyle.Flex;
            bool allowed = game.CanOpenPanel;
            foreach (var (b, id) in new[] { (colBoutique, PanelId.Boutique), (colInventaire, PanelId.Inventaire),
                                             (colIndex, PanelId.Index), (colAmeliorations, PanelId.Ameliorations) })
            {
                b.SetEnabled(allowed);
                ShopUi.SetClass(b, "col-btn--active", game.PanelOpen == id);
            }

            // Rappel de la cagnotte (seulement si elle est supérieure à 0).
            int pot = game.Pot;
            potNote.text = pot > 0 ? $"Ta cagnotte en jeu ({pot}) ne se dépense pas : encaisse-la d'abord." : "";
            potNote.style.display = pot > 0 ? DisplayStyle.Flex : DisplayStyle.None;

            if (game.PanelOpen == shownPanel) return;
            PanelId previous = shownPanel;
            shownPanel = game.PanelOpen;
            avatar.ShowEquipped(); // fin de l'aperçu à chaque changement

            if (shownPanel == PanelId.None)
            {
                StartSlide(false);
                return;
            }
            if (previous == PanelId.None)
            {
                // Ouverture : le cadre glisse depuis la gauche, de derrière la colonne.
                frame.style.display = DisplayStyle.Flex;
                frameVisible = true;
                wallet.Snap(Wallet.Coins);
                StartSlide(true);
            }
            // Sinon : un autre panneau était ouvert, il est remplacé sans animation de fermeture.
            ShowPage(shownPanel);
        }

        void ShowPage(PanelId panel)
        {
            frameTitle.text = panel switch
            {
                PanelId.Boutique => "Boutique",
                PanelId.Inventaire => "Inventaire",
                PanelId.Index => "Index",
                _ => "Améliorations"
            };
            Sprite icon = panel switch
            {
                PanelId.Boutique => iconBoutique,
                PanelId.Inventaire => iconInventaire,
                PanelId.Index => iconIndex,
                _ => iconAmeliorations
            };
            frameIcon.style.backgroundImage = icon != null ? new StyleBackground(icon) : StyleKeyword.None;
            ShopUi.SetClass(pageBoutique, "shop-page--active", panel == PanelId.Boutique);
            ShopUi.SetClass(pageInventaire, "shop-page--active", panel == PanelId.Inventaire);
            ShopUi.SetClass(pageIndex, "shop-page--active", panel == PanelId.Index);
            ShopUi.SetClass(pageAmeliorations, "shop-page--active", panel == PanelId.Ameliorations);

            switch (panel)
            {
                case PanelId.Boutique: boutique.Open(); break;
                case PanelId.Inventaire: inventory.Open(pendingInventoryFocus); break;
                case PanelId.Index: index.Open(pendingIndexFocus); break;
                case PanelId.Ameliorations: upgrades.Open(); break;
            }
            pendingInventoryFocus = null;
            pendingIndexFocus = null;
        }

        void StartSlide(bool goingIn)
        {
            slidingIn = goingIn;
            slideTime = 0f;
            AnimateSlide();
        }

        /// Glissement depuis la gauche : 0,25 s ease-out à l'ouverture, 0,2 s ease-in à la fermeture.
        void AnimateSlide()
        {
            if (slideTime < 0f) return;
            slideTime += Time.deltaTime;
            float duration = slidingIn ? SlideInTime : SlideOutTime;
            float k = Mathf.Clamp01(slideTime / duration);
            float x = slidingIn ? -PanelWidth * (1f - Ease.Out(k)) : -PanelWidth * Ease.In(k);
            frame.style.translate = new Translate(x, 0f);
            if (k >= 1f)
            {
                slideTime = -1f;
                if (!slidingIn) { frame.style.display = DisplayStyle.None; frameVisible = false; }
            }
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

        /// Achat d'un niveau : son (« max » au niveau 5) et petit saut d'accueil du cheval.
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
        public void DebugClickColumn(int panel) => game.TogglePanel((PanelId)panel);
        public string DebugBoutiqueCollectionText()
        {
            var label = pageBoutique.Q<Label>(className: "progress-text");
            return label != null ? label.text : "";
        }
    }
}
