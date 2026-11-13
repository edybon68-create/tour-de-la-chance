using UnityEngine;
using UnityEngine.UIElements;

namespace TourDeLaChance
{
    /// <summary>
    /// Le compteur de DIAMANTS du HUD (spec v5 §10.4), sous le porte-monnaie, et le badge
    /// « ×2 » de la renaissance à côté du porte-monnaie (§13.5). Pur affichage.
    ///
    /// Il n'apparaît qu'une fois un diamant gagné (stats_diamants_gagnes > 0) : un « 0 »
    /// permanent inviterait à en acheter. Le nombre défile en 0,4 s quand il change, avec un
    /// petit « tink » (diamant_compteur) par diamant ajouté (au plus 6).
    /// L'icône est un diamant DESSINÉ par l'interface (classe .diamant) tant que l'image
    /// icone_diamant.png n'existe pas (lot d'images v5).
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class DiamondCounterView : MonoBehaviour
    {
        [SerializeField] GameManager game;
        [Tooltip("Image icone_diamant.png (facultative : sinon un diamant dessiné par le code).")]
        [SerializeField] Sprite diamondIcon;

        VisualElement counter, icon, rebirthBadge;
        Label label;
        readonly RollingCounter rolling = new RollingCounter();
        int ticksToPlay;
        float tickTimer;
        float pulseTime = -1f;

        /// Le compteur est-il visible ? (pour les tests)
        public bool IsVisible => counter.resolvedStyle.display == DisplayStyle.Flex;
        public int Displayed => rolling.Displayed;

        void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            counter = root.Q<VisualElement>("diamond-counter");
            icon = root.Q<VisualElement>("diamond-icon");
            label = root.Q<Label>("diamonds");
            rebirthBadge = root.Q<VisualElement>("rebirth-badge");
            if (diamondIcon != null)
            {
                icon.RemoveFromClassList("diamant");
                icon.AddToClassList("diamant-image");
                icon.style.backgroundImage = new StyleBackground(diamondIcon);
            }
            rolling.Snap(Diamonds.Count);
            Diamonds.Changed += OnDiamondsChanged;
            Rebirth.Changed += Refresh;
            game.Changed += Refresh;
            Refresh();
        }

        void OnDisable()
        {
            Diamonds.Changed -= OnDiamondsChanged;
            Rebirth.Changed -= Refresh;
            game.Changed -= Refresh;
        }

        void OnDiamondsChanged()
        {
            int target = Diamonds.Count;
            int gained = target - rolling.Target;
            rolling.RollTo(target, 0.4f, Ease.Out);
            if (gained > 0)
            {
                ticksToPlay = Mathf.Min(6, gained);
                tickTimer = 0f;
                pulseTime = 0f;
            }
            Refresh();
        }

        void Refresh()
        {
            bool title = game.State == GameState.Title;
            counter.style.display = !title && Diamonds.CounterUnlocked ? DisplayStyle.Flex : DisplayStyle.None;
            rebirthBadge.style.display = !title && Rebirth.IsDone ? DisplayStyle.Flex : DisplayStyle.None;
            label.text = ShopUi.Thousands(rolling.Displayed);
        }

        void Update()
        {
            rolling.Tick(Time.deltaTime);
            label.text = ShopUi.Thousands(rolling.Displayed);
            // Un « tink » par diamant ajouté, répartis sur les 0,4 s du défilement.
            if (ticksToPlay > 0)
            {
                tickTimer -= Time.deltaTime;
                if (tickTimer <= 0f)
                {
                    if (SoundManager.Instance != null) SoundManager.Instance.Play(Sfx.DiamantCompteur, 1f, 1f, 0.04f);
                    ticksToPlay--;
                    tickTimer = 0.4f / 6f;
                }
            }
            float s = 1f;
            if (pulseTime >= 0f)
            {
                pulseTime += Time.deltaTime;
                s = 1f + 0.2f * Ease.Sine(pulseTime / 0.3f);
                if (pulseTime >= 0.3f) pulseTime = -1f;
            }
            label.style.scale = new Scale(new Vector3(s, s, 1f));
        }

        /// La position (dans le panneau) de l'icône du compteur : les diamants 3D y volent.
        public Vector2 IconPanelPosition => icon.worldBound.center;
    }
}
