using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

namespace TourDeLaChance
{
    /// <summary>
    /// L'étiquette d'étage du HUD (spec v3 §5.6) : 3 pastilles qui disent ce que vaut
    /// l'étage : « Trésor : +54 », « Coffres d'or ×3 » (seulement Bronze ou mieux) et
    /// « 4 coffres · 1 piège ». Pur affichage : tout est lu dans le FloorSetup de l'étage
    /// (préparé par le GameManager) et dans le catalogue des raretés : aucun chiffre en dur.
    ///
    /// Visible pendant le choix et la révélation ; cachée pendant la montée, en fin de
    /// partie et quand un panneau est ouvert. Elle apparaît avec le message (fin de la montée).
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class FloorTagView : MonoBehaviour
    {
        [SerializeField] GameManager game;
        [Tooltip("Icône de la pastille « Trésor » (la pièce).")]
        [SerializeField] Sprite coinIcon;
        [Tooltip("Icône de la pastille « N coffres · 1 piège » (coffre teinté rouge).")]
        [SerializeField] Sprite trapIcon;

        static readonly Color TrapTint = new Color(0.85f, 0.25f, 0.25f);

        VisualElement tagRow;
        Label gainText, rarityText, riskText;
        VisualElement rarityPill, rarityIcon, riskIcon;
        bool visible;
        Coroutine fade;

        void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            tagRow = root.Q<VisualElement>("floor-tag");
            tagRow.Clear();
            gainText = AddPill(coinIcon, Color.white, out _, out _);
            rarityText = AddPill(null, Color.white, out rarityPill, out rarityIcon);
            riskText = AddPill(trapIcon, TrapTint, out _, out riskIcon);
            tagRow.style.display = DisplayStyle.None;
            visible = false;
            game.Changed += Refresh;
            game.FloorShown += OnFloorShown;
            Refresh();
        }

        void OnDisable()
        {
            game.Changed -= Refresh;
            game.FloorShown -= OnFloorShown;
        }

        Label AddPill(Sprite icon, Color tint, out VisualElement pill, out VisualElement iconElement)
        {
            pill = new VisualElement { pickingMode = PickingMode.Ignore };
            pill.AddToClassList("floor-pill");
            iconElement = new VisualElement { pickingMode = PickingMode.Ignore };
            iconElement.AddToClassList("floor-pill-icon");
            if (icon != null) iconElement.style.backgroundImage = new StyleBackground(icon);
            iconElement.style.unityBackgroundImageTintColor = tint;
            pill.Add(iconElement);
            var label = new Label { pickingMode = PickingMode.Ignore };
            label.AddToClassList("floor-pill-text");
            pill.Add(label);
            tagRow.Add(pill);
            return label;
        }

        /// Les textes de l'étiquette, lus dans le FloorSetup (pour les tests aussi).
        public string GainText => gainText.text;
        public string RarityText => rarityPill.style.display == DisplayStyle.None ? "" : rarityText.text;
        public string RiskText => riskText.text;
        public bool IsVisible => visible;

        void Fill()
        {
            var setup = game.Setup;
            gainText.text = $"Trésor : +{setup.Gain}";
            bool precious = setup.RarityRank >= 1 && setup.Rarity != null;
            rarityPill.style.display = precious ? DisplayStyle.Flex : DisplayStyle.None;
            if (precious)
            {
                rarityText.text = $"{setup.Rarity.displayName} ×{ShopUi.Number(setup.Rarity.multiplier)}";
                rarityIcon.style.backgroundImage = setup.Rarity.icon != null ? new StyleBackground(setup.Rarity.icon) : StyleKeyword.None;
            }
            // Toujours affichée, même sans amélioration : on montre la chance (1 piège sur N).
            riskText.text = $"{setup.ChestCount} coffres · 1 piège";
            var wood = Upgrades.Rarity(0);
            if (trapIcon == null && wood != null && wood.icon != null) riskIcon.style.backgroundImage = new StyleBackground(wood.icon);
        }

        void Refresh()
        {
            var state = game.State;
            bool show = (state == GameState.Choosing || state == GameState.Revealing) && !game.IsPanelOpen;
            if (show) Fill();
            if (show == visible) return;
            visible = show;
            if (fade != null) StopCoroutine(fade);
            if (show)
            {
                tagRow.style.display = DisplayStyle.Flex;
                // Pendant une transition, elle attend FloorShown pour apparaître avec le message.
                tagRow.style.opacity = game.TransitionLocked ? 0f : 1f;
            }
            else tagRow.style.display = DisplayStyle.None;
        }

        /// Fin de la montée / du remélange : elle apparaît avec le message (0,2 s).
        void OnFloorShown()
        {
            Refresh();
            if (!visible) return;
            if (fade != null) StopCoroutine(fade);
            fade = StartCoroutine(FadeIn());
        }

        IEnumerator FadeIn()
        {
            for (float t = 0f; t < 0.2f; t += Time.deltaTime)
            {
                tagRow.style.opacity = t / 0.2f;
                yield return null;
            }
            tagRow.style.opacity = 1f;
        }
    }
}
