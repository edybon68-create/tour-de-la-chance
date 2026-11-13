using UnityEngine;
using UnityEngine.UIElements;

namespace TourDeLaChance
{
    /// <summary>
    /// v6 §9.2 : la fenêtre de reprise au centre a DISPARU. L'offre est maintenant sur l'écran du
    /// bas (boutons « Rejouer » et « Continuer — N ◆ », dans HudController). Ce script ne garde
    /// que la petite SPIRALE bleutée « retour en arrière » (0,4 s) quand la reprise est acceptée,
    /// et la confirmation « Tu es sûr ? » (§9.3) que « Rejouer » ouvre quand « Continuer » est
    /// proposé. Elle est ici (document des pop-ups) pour que son voile couvre TOUTE la fenêtre,
    /// colonnes comprises. Pur affichage : les décisions sont dans le GameManager.
    /// (Le nom de la classe est gardé pour ne pas débrancher la scène.)
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class ReviveOfferView : MonoBehaviour
    {
        [SerializeField] GameManager game;

        const float SpiralTime = 0.4f;

        VisualElement layer, spiral, sureVeil;
        Label sureText;
        float spiralTime = -1f;

        /// La confirmation est-elle ouverte ? (HudController ne réagit alors plus à rien)
        public bool ConfirmOpen => sureVeil != null && sureVeil.style.display == DisplayStyle.Flex;
        public string ConfirmText => ConfirmOpen ? sureText.text : "";

        void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            layer = root.Q<VisualElement>("revive-layer");
            layer.Clear();
            layer.pickingMode = PickingMode.Ignore;
            spiral = new VisualElement { pickingMode = PickingMode.Ignore };
            spiral.AddToClassList("revive-spiral");
            spiral.generateVisualContent += DrawSpiral;
            spiral.style.display = DisplayStyle.None;
            layer.Add(spiral);
            BuildConfirm();
            game.ReviveStarted += OnReviveStarted;
            game.Changed += OnGameChanged;
        }

        void OnDisable()
        {
            game.ReviveStarted -= OnReviveStarted;
            game.Changed -= OnGameChanged;
        }

        // =====================================================================
        // v6 §9.3 : « Tu es sûr ? »
        // =====================================================================

        void BuildConfirm()
        {
            sureVeil = new VisualElement();
            sureVeil.AddToClassList("sur-voile");
            var box = new VisualElement();
            box.AddToClassList("sur-boite");
            box.Add(ShopUi.NewLabel("Tu es sûr ?", "sur-titre"));
            sureText = ShopUi.NewLabel("", "sur-texte");
            box.Add(sureText);
            var buttons = new VisualElement();
            buttons.AddToClassList("sur-boutons");
            var yes = new Button(OnYes) { text = "Oui, rejouer" };
            yes.AddToClassList("btn"); yes.AddToClassList("btn--rouge-vif"); yes.AddToClassList("sur-btn");
            // « Annuler » est le bouton PAR DÉFAUT (liseré or) : Entrée et Échap annulent.
            var no = new Button(CloseConfirm) { text = "Annuler" };
            no.AddToClassList("btn"); no.AddToClassList("btn--sombre"); no.AddToClassList("sur-btn"); no.AddToClassList("sur-btn--defaut");
            buttons.Add(yes);
            buttons.Add(no);
            box.Add(buttons);
            sureVeil.Add(box);
            sureVeil.style.display = DisplayStyle.None;
            layer.Add(sureVeil);
            UiSounds.Register(sureVeil);
            ButtonStyler.Register(sureVeil);
        }

        /// Ouvre la confirmation (seulement quand la reprise est proposée).
        public void OpenConfirm()
        {
            if (game.State != GameState.ReviveOffer) return;
            sureText.text = $"Tu vas perdre ta cagnotte de {ShopUi.Thousands(game.Pot)} pièces. Tu peux encore la sauver pour {game.NextReviveCost} {ShopUi.Diamant}.";
            sureVeil.style.display = DisplayStyle.Flex;
        }

        public void CloseConfirm() => sureVeil.style.display = DisplayStyle.None;

        /// « Oui, rejouer » : la partie se termine et la nouvelle commence (GameManager).
        void OnYes()
        {
            CloseConfirm();
            game.DeclineReviveAndReplay();
        }

        void OnGameChanged()
        {
            if (game.State != GameState.ReviveOffer && ConfirmOpen) CloseConfirm();
        }

        /// OUTILS DE TEST : les deux boutons de la confirmation.
        public void DebugConfirmYes() => OnYes();
        public void DebugConfirmNo() => CloseConfirm();

        void Update()
        {
            // Entrée = Annuler, Échap = Annuler (on ne perd jamais une partie par erreur).
            if (ConfirmOpen)
            {
                var kb = UnityEngine.InputSystem.Keyboard.current;
                if (kb != null && (kb.escapeKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame))
                    CloseConfirm();
            }
            if (spiralTime < 0f) return;
            spiralTime += Time.deltaTime;
            spiral.MarkDirtyRepaint();
            if (spiralTime >= SpiralTime) { spiralTime = -1f; spiral.style.display = DisplayStyle.None; }
        }

        void OnReviveStarted()
        {
            spiralTime = 0f;
            spiral.style.display = DisplayStyle.Flex;
            spiral.MarkDirtyRepaint();
        }

        /// La spirale bleutée « retour en arrière » : une spirale qui tourne à l'envers et
        /// s'efface en 0,4 s, dessinée par le code (aucune image).
        void DrawSpiral(MeshGenerationContext ctx)
        {
            if (spiralTime < 0f) return;
            var r = ctx.visualElement.contentRect;
            if (r.width <= 0f) return;
            float k = Mathf.Clamp01(spiralTime / SpiralTime);
            var p = ctx.painter2D;
            Vector2 c = r.center;
            float maxR = Mathf.Max(r.width, r.height) * 0.6f;
            p.lineWidth = 26f;
            p.lineCap = LineCap.Round;
            p.strokeColor = new Color(0.45f, 0.75f, 1f, 0.5f * (1f - k));
            p.BeginPath();
            const int steps = 120;
            float turn = -k * Mathf.PI * 3f; // elle tourne à l'envers (on rembobine)
            for (int i = 0; i <= steps; i++)
            {
                float u = i / (float)steps;
                float a = turn + u * Mathf.PI * 6f;
                var pt = c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * (u * maxR * (0.4f + 0.6f * k));
                if (i == 0) p.MoveTo(pt); else p.LineTo(pt);
            }
            p.Stroke();
        }
    }
}
