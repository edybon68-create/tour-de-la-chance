using UnityEngine;
using UnityEngine.UIElements;

namespace TourDeLaChance
{
    /// <summary>
    /// L'interface (HUD) : affiche l'étage, la cagnotte, le porte-monnaie, un message,
    /// et les boutons Encaisser / Continuer / Rejouer. Elle ne décide de rien :
    /// elle montre l'état du GameManager et lui transmet les clics.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class HudController : MonoBehaviour
    {
        [SerializeField] GameManager game;

        Label floorLabel, potLabel, walletLabel, messageLabel;
        Button cashOutButton, continueButton, replayButton;

        void OnEnable()
        {
            // On retrouve les éléments de Hud.uxml par leur "name".
            var root = GetComponent<UIDocument>().rootVisualElement;
            floorLabel = root.Q<Label>("floor");
            potLabel = root.Q<Label>("pot");
            walletLabel = root.Q<Label>("wallet");
            messageLabel = root.Q<Label>("message");
            cashOutButton = root.Q<Button>("cashout");
            continueButton = root.Q<Button>("continue");
            replayButton = root.Q<Button>("replay");

            cashOutButton.clicked += game.CashOut;
            continueButton.clicked += game.Continue;
            replayButton.clicked += game.NewRun;
            game.Changed += Refresh;
            Refresh();
        }

        void OnDisable()
        {
            cashOutButton.clicked -= game.CashOut;
            continueButton.clicked -= game.Continue;
            replayButton.clicked -= game.NewRun;
            game.Changed -= Refresh;
        }

        void Refresh()
        {
            floorLabel.text = $"Étage {game.Floor}";
            potLabel.text = $"Cagnotte : {game.Pot}";
            walletLabel.text = $"Porte-monnaie : {Wallet.Coins}";

            bool decision = game.State == GameState.Decision;
            bool over = game.State == GameState.Lost || game.State == GameState.CashedOut;
            Show(cashOutButton, decision);
            Show(continueButton, decision);
            Show(replayButton, over);

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
