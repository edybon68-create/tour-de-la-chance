using UnityEngine;
using UnityEngine.UIElements;

namespace TourDeLaChance
{
    /// <summary>
    /// L'interface (HUD) : affiche l'étage, la cagnotte, le porte-monnaie, un message,
    /// et les boutons Encaisser / Continuer / Rejouer / Boutique. Elle ne décide de rien :
    /// elle montre l'état du GameManager et lui transmet les clics.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class HudController : MonoBehaviour
    {
        [SerializeField] GameManager game;

        VisualElement hudRoot;
        Label floorLabel, potLabel, walletLabel, messageLabel;
        Button cashOutButton, continueButton, replayButton, shopButton;

        void OnEnable()
        {
            // On retrouve les éléments de Hud.uxml par leur "name".
            var root = GetComponent<UIDocument>().rootVisualElement;
            hudRoot = root.Q<VisualElement>("root");
            floorLabel = root.Q<Label>("floor");
            potLabel = root.Q<Label>("pot");
            walletLabel = root.Q<Label>("wallet");
            messageLabel = root.Q<Label>("message");
            cashOutButton = root.Q<Button>("cashout");
            continueButton = root.Q<Button>("continue");
            replayButton = root.Q<Button>("replay");
            shopButton = root.Q<Button>("shop");

            cashOutButton.clicked += game.CashOut;
            continueButton.clicked += game.Continue;
            replayButton.clicked += game.NewRun;
            shopButton.clicked += game.OpenShop;
            game.Changed += Refresh;
            Shop.Changed += Refresh; // après un achat, le porte-monnaie change
            Refresh();
        }

        void OnDisable()
        {
            cashOutButton.clicked -= game.CashOut;
            continueButton.clicked -= game.Continue;
            replayButton.clicked -= game.NewRun;
            shopButton.clicked -= game.OpenShop;
            game.Changed -= Refresh;
            Shop.Changed -= Refresh;
        }

        void Refresh()
        {
            // Écran titre : le HUD entier est caché (l'écran titre est affiché par-dessus).
            bool title = game.State == GameState.Title;
            Show(hudRoot, !title);
            if (title) return;

            floorLabel.text = $"Étage {game.Floor}";
            potLabel.text = $"Cagnotte : {game.Pot}";
            walletLabel.text = $"Porte-monnaie : {Wallet.Coins}";

            // Shop ouvert : on cache les boutons et le message (la barre du haut reste).
            bool shop = game.ShopOpen;
            bool decision = !shop && game.State == GameState.Decision;
            bool over = !shop && (game.State == GameState.Lost || game.State == GameState.CashedOut);
            Show(cashOutButton, decision);
            Show(continueButton, decision);
            Show(replayButton, over);
            Show(shopButton, game.CanOpenShop); // la règle est dans le GameManager
            Show(messageLabel, !shop);

            messageLabel.text = game.State switch
            {
                GameState.Choosing => "Choisis un coffre ! Un seul est piégé…",
                GameState.Revealing => "…",
                GameState.Decision => $"+{game.LastGain} pièces ! Encaisser ou tenter l'étage {game.Floor + 1} ?",
                GameState.Lost => "Piégé ! Tu perds ta cagnotte.",
                GameState.CashedOut => $"Bien joué ! Tu encaisses {game.LastGain} pièces.",
                _ => ""
            };
        }

        static void Show(VisualElement element, bool visible) =>
            element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
    }
}
