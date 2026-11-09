using UnityEngine;
using UnityEngine.UIElements;

namespace TourDeLaChance
{
    /// <summary>
    /// L'écran titre (Title.uxml) : le logo et le bouton « Jouer ».
    /// Il ne décide de rien : il est visible tant que le GameManager est dans l'état Title,
    /// et le bouton demande simplement au GameManager de lancer la partie (StartGame).
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class TitleController : MonoBehaviour
    {
        [SerializeField] GameManager game;

        VisualElement root;
        Button playButton;

        void OnEnable()
        {
            var doc = GetComponent<UIDocument>().rootVisualElement;
            root = doc.Q<VisualElement>("title-root");
            playButton = doc.Q<Button>("play");

            playButton.clicked += game.StartGame;
            game.Changed += Refresh;
            Refresh();
        }

        void OnDisable()
        {
            playButton.clicked -= game.StartGame;
            game.Changed -= Refresh;
        }

        /// Affiché seulement pendant l'état Title. Une fois caché (display: none),
        /// le panneau ne capte plus aucun clic.
        void Refresh()
        {
            bool title = game.State == GameState.Title;
            root.style.display = title ? DisplayStyle.Flex : DisplayStyle.None;
            if (title) playButton.Focus(); // la touche Entrée/Espace peut aussi « cliquer »
        }
    }
}
