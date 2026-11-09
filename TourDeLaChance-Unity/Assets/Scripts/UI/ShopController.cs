using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace TourDeLaChance
{
    /// <summary>
    /// L'interface du shop (Shop.uxml). Elle AFFICHE le catalogue et TRANSMET les clics :
    /// acheter et équiper sont décidés par la classe Shop, ouvrir/fermer par le GameManager.
    /// Elle s'occupe aussi de l'aperçu : cliquer une carte montre l'objet sur le personnage,
    /// et fermer le shop remet les objets équipés.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class ShopController : MonoBehaviour
    {
        [SerializeField] GameManager game;
        [SerializeField] PlayerAvatar avatar;

        VisualElement root, cardsRow;
        Label walletLabel, missingLabel;
        Button closeButton, colorTab, hatTab, actionButton;

        ShopCategory currentTab = ShopCategory.Couleur;
        ShopItem selected;
        bool panelShown;

        // Anti double-clic : juste après un achat, le bouton devient « Équiper ».
        // Sans cette petite pause, le 2e clic d'un double-clic équiperait l'objet
        // alors que le joueur voulait seulement l'acheter.
        const float ClickPauseAfterBuy = 0.4f;
        float ignoreClicksUntil;

        void OnEnable()
        {
            var doc = GetComponent<UIDocument>().rootVisualElement;
            root = doc.Q<VisualElement>("shop-root");
            cardsRow = doc.Q<VisualElement>("cards");
            walletLabel = doc.Q<Label>("shop-wallet");
            missingLabel = doc.Q<Label>("missing");
            closeButton = doc.Q<Button>("close");
            colorTab = doc.Q<Button>("tab-couleur");
            hatTab = doc.Q<Button>("tab-chapeau");
            actionButton = doc.Q<Button>("action");

            closeButton.clicked += game.CloseShop;
            colorTab.clicked += ShowColorTab;
            hatTab.clicked += ShowHatTab;
            actionButton.clicked += OnActionClicked;
            game.Changed += OnGameChanged;
            Shop.Changed += Refresh;

            panelShown = false;
            root.style.display = DisplayStyle.None; // caché au démarrage
            OnGameChanged();
        }

        void OnDisable()
        {
            closeButton.clicked -= game.CloseShop;
            colorTab.clicked -= ShowColorTab;
            hatTab.clicked -= ShowHatTab;
            actionButton.clicked -= OnActionClicked;
            game.Changed -= OnGameChanged;
            Shop.Changed -= Refresh;
        }

        void Update()
        {
            // La touche Échap ferme le shop, comme le bouton « Fermer ».
            var kb = Keyboard.current;
            if (game.ShopOpen && kb != null && kb.escapeKey.wasPressedThisFrame)
                game.CloseShop();
        }

        /// Le GameManager a changé : on ouvre ou ferme le panneau pour suivre ShopOpen.
        void OnGameChanged()
        {
            if (game.ShopOpen == panelShown) return;
            panelShown = game.ShopOpen;

            if (panelShown)
            {
                root.style.display = DisplayStyle.Flex;
                SelectTab(ShopCategory.Couleur); // onglet « Couleurs » par défaut
            }
            else
            {
                root.style.display = DisplayStyle.None;
                avatar.ShowEquipped(); // fin de l'aperçu : on remet les objets équipés
            }
        }

        void ShowColorTab() => SelectTab(ShopCategory.Couleur);
        void ShowHatTab() => SelectTab(ShopCategory.Chapeau);

        /// Change d'onglet : la carte sélectionnée devient l'objet équipé de cet onglet.
        void SelectTab(ShopCategory category)
        {
            currentTab = category;
            selected = Shop.GetEquipped(category);
            Refresh();
        }

        /// Clic sur une carte : sélection + aperçu sur le personnage (rien n'est sauvegardé).
        void SelectCard(ShopItem item)
        {
            selected = item;
            avatar.Show(item);
            Refresh();
        }

        /// Clic sur le bouton d'action : acheter ou équiper selon l'état de l'objet.
        /// Shop.TryBuy / TryEquip vérifient eux-mêmes si c'est permis.
        void OnActionClicked()
        {
            if (selected == null || Time.unscaledTime < ignoreClicksUntil) return;
            if (!Shop.IsOwned(selected))
            {
                if (Shop.TryBuy(selected)) ignoreClicksUntil = Time.unscaledTime + ClickPauseAfterBuy;
            }
            else if (!Shop.IsEquipped(selected))
            {
                if (Shop.TryEquip(selected)) avatar.Show(selected);
            }
            // (Shop.Changed déclenche Refresh)
        }

        /// Redessine tout le panneau à partir de l'état du Shop et du porte-monnaie.
        void Refresh()
        {
            if (!panelShown) return;

            walletLabel.text = $"Porte-monnaie : {Wallet.Coins} pièces";
            SetClass(colorTab, "shop-tab--active", currentTab == ShopCategory.Couleur);
            SetClass(hatTab, "shop-tab--active", currentTab == ShopCategory.Chapeau);

            // Les cartes sont recréées à chaque fois : il n'y en a que 5, c'est très simple.
            cardsRow.Clear();
            List<ShopItem> items = Shop.ItemsIn(currentTab);
            foreach (var item in items)
                cardsRow.Add(CreateCard(item));

            RefreshAction();
        }

        VisualElement CreateCard(ShopItem item)
        {
            // Une carte est un bouton (cliquable) qui contient visuel, nom et étiquette d'état.
            var card = new Button(() => SelectCard(item));
            card.AddToClassList("card");
            SetClass(card, "card--selected", item == selected);

            if (item.Category == ShopCategory.Couleur)
            {
                var swatch = new VisualElement();
                swatch.AddToClassList("card-swatch");
                swatch.style.backgroundColor = item.Color;
                card.Add(swatch);
            }
            else
            {
                var hatName = new Label(item.DisplayName);
                hatName.AddToClassList("card-hat");
                card.Add(hatName);
            }

            var nameLabel = new Label(item.DisplayName);
            nameLabel.AddToClassList("card-name");
            card.Add(nameLabel);

            var state = new Label();
            state.AddToClassList("card-state");
            if (Shop.IsEquipped(item))
            {
                state.text = "Équipé ✓";
                state.AddToClassList("card-state--equipped");
            }
            else if (Shop.IsOwned(item))
            {
                state.text = item.IsFree ? "Gratuit" : "Possédé";
            }
            else
            {
                state.text = $"{item.Price} pièces";
                state.AddToClassList("card-state--price");
            }
            card.Add(state);
            return card;
        }

        /// Le bouton d'action suit le tableau de la spec (§5.2, zone d'action).
        void RefreshAction()
        {
            actionButton.RemoveFromClassList("shop-action--buy");
            actionButton.RemoveFromClassList("shop-action--equip");
            missingLabel.text = "";

            if (selected == null)
            {
                actionButton.style.display = DisplayStyle.None;
                return;
            }
            actionButton.style.display = DisplayStyle.Flex;

            if (!Shop.IsOwned(selected))
            {
                actionButton.text = $"Acheter — {selected.Price}";
                actionButton.AddToClassList("shop-action--buy");
                bool canBuy = Shop.CanBuy(selected);
                actionButton.SetEnabled(canBuy);
                if (!canBuy)
                    missingLabel.text = $"Il te manque {Shop.MissingCoins(selected)} pièces";
            }
            else if (!Shop.IsEquipped(selected))
            {
                actionButton.text = "Équiper";
                actionButton.AddToClassList("shop-action--equip");
                actionButton.SetEnabled(true);
            }
            else
            {
                actionButton.text = "Équipé ✓";
                actionButton.SetEnabled(false);
            }
        }

        static void SetClass(VisualElement element, string className, bool on)
        {
            if (on) element.AddToClassList(className);
            else element.RemoveFromClassList(className);
        }
    }
}
