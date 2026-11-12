using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace TourDeLaChance
{
    /// <summary>
    /// La fenêtre de REPRISE après un piège (spec v5 §11.2). 0,8 s après la révélation du piège
    /// (état ReviveOffer du GameManager), une petite fenêtre centrée, SANS fond assombri et SANS
    /// compte à rebours : « Oh non, un piège ! », « Utilise 1 ◆ pour garder ta cagnotte (210)
    /// et rejouer l'étage 5 avec de nouveaux coffres. », « Une seule reprise par partie · Tu as
    /// 7 ◆ », et deux boutons : « Continuer — 1 ◆ » (vert) et « Non merci » (Échap).
    /// Elle n'apparaît jamais sans diamant (le GameManager ne propose rien) et ne parle JAMAIS
    /// d'acheter des diamants (règle d'or n°7).
    ///
    /// Elle dessine aussi la petite SPIRALE bleutée « retour en arrière » (0,4 s) quand la reprise
    /// est acceptée. Pur affichage : elle transmet les clics au GameManager, qui décide.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class ReviveOfferView : MonoBehaviour
    {
        [SerializeField] GameManager game;

        const float ShowDelay = 0.8f;
        const float SpiralTime = 0.4f;

        VisualElement layer, window, spiral;
        Label text, reminder;
        Button continueButton, declineButton;
        float offerTime = -1f, spiralTime = -1f, appear;
        bool shown;

        public bool IsShown => shown;
        public string Text => shown ? text.text + " | " + reminder.text : "";

        void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            layer = root.Q<VisualElement>("revive-layer");
            Build();
            game.ReviveOffered += OnOffered;
            game.ReviveStarted += OnReviveStarted;
            game.Changed += OnGameChanged;
            continueButton.clicked += OnContinue;
            declineButton.clicked += OnDecline;
            Hide();
        }

        void OnDisable()
        {
            game.ReviveOffered -= OnOffered;
            game.ReviveStarted -= OnReviveStarted;
            game.Changed -= OnGameChanged;
            continueButton.clicked -= OnContinue;
            declineButton.clicked -= OnDecline;
        }

        void Build()
        {
            layer.Clear();
            layer.pickingMode = PickingMode.Ignore;
            spiral = new VisualElement { pickingMode = PickingMode.Ignore };
            spiral.AddToClassList("revive-spiral");
            spiral.generateVisualContent += DrawSpiral;
            layer.Add(spiral);

            window = new VisualElement();
            window.AddToClassList("revive-window");
            var header = new VisualElement { pickingMode = PickingMode.Ignore };
            header.AddToClassList("revive-header");
            header.Add(ShopUi.NewLabel("Oh non, un piège !", "revive-title"));
            window.Add(header);
            text = ShopUi.NewLabel("", "revive-text");
            window.Add(text);
            reminder = ShopUi.NewLabel("", "revive-reminder");
            window.Add(reminder);
            var buttons = new VisualElement();
            buttons.AddToClassList("revive-buttons");
            continueButton = new Button();
            continueButton.AddToClassList("btn"); continueButton.AddToClassList("btn--vert"); continueButton.AddToClassList("revive-btn");
            declineButton = new Button { text = "Non merci" };
            declineButton.AddToClassList("btn"); declineButton.AddToClassList("btn--sombre"); declineButton.AddToClassList("revive-btn");
            buttons.Add(continueButton);
            buttons.Add(declineButton);
            window.Add(buttons);
            layer.Add(window);
            UiSounds.Register(window);
            ButtonStyler.Register(window);
        }

        void OnOffered()
        {
            offerTime = 0f; // la fenêtre attend 0,8 s (on voit d'abord le piège)
        }

        void OnGameChanged()
        {
            if (game.State != GameState.ReviveOffer) { offerTime = -1f; Hide(); }
        }

        void Show()
        {
            shown = true;
            int pot = game.Pot;
            text.text = $"Utilise {GameManager.ReviveCost} {ShopUi.Diamant} pour garder ta cagnotte ({ShopUi.Thousands(pot)}) et rejouer l'étage {game.Floor} avec de nouveaux coffres.";
            reminder.text = $"Une seule reprise par partie · Tu as {Diamonds.Count} {ShopUi.Diamant}";
            continueButton.text = $"Continuer — {GameManager.ReviveCost} {ShopUi.Diamant}";
            window.style.display = DisplayStyle.Flex;
            appear = 0f;
            if (SoundManager.Instance != null) SoundManager.Instance.Play(Sfx.RepriseOffre);
        }

        void Hide()
        {
            shown = false;
            window.style.display = DisplayStyle.None;
        }

        void Update()
        {
            if (offerTime >= 0f && game.State == GameState.ReviveOffer)
            {
                offerTime += Time.deltaTime;
                if (!shown && offerTime >= ShowDelay) Show();
            }
            if (shown)
            {
                // Apparition : 0,85 → 1,04 → 1 en 0,25 s.
                appear += Time.deltaTime;
                float k = Mathf.Clamp01(appear / 0.25f);
                float s = Ease.Pop(k, 0.85f, 1.04f);
                window.style.scale = new Scale(new Vector3(s, s, 1f));
                window.style.opacity = Mathf.Clamp01(k / 0.6f);
                // Échap = « Non merci » (sauf si les Réglages sont ouverts par-dessus).
                var kb = Keyboard.current;
                if (kb != null && kb.escapeKey.wasPressedThisFrame && !game.IsPanelOpen) OnDecline();
            }
            if (spiralTime >= 0f)
            {
                spiralTime += Time.deltaTime;
                spiral.MarkDirtyRepaint();
                if (spiralTime >= SpiralTime) { spiralTime = -1f; spiral.style.display = DisplayStyle.None; }
            }
        }

        // La logique est dans le GameManager (un double-clic ne dépense qu'un diamant).
        void OnContinue() => game.AcceptRevive();
        void OnDecline() => game.DeclineRevive();

        void OnReviveStarted()
        {
            Hide();
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

        /// OUTILS DE TEST : clics sur les deux boutons.
        public void DebugContinue() => OnContinue();
        public void DebugDecline() => OnDecline();
    }
}
