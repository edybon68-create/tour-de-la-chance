using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

namespace TourDeLaChance
{
    /// <summary>
    /// L'écran titre (Title.uxml) : le logo et le bouton « Jouer » (v4 : la roue dentée des
    /// Réglages est dessinée par PanelsController, au-dessus de tout).
    /// Il ne décide de rien : il est visible tant que le GameManager est dans l'état Title,
    /// et le bouton demande simplement au GameManager de lancer la partie (StartGame).
    ///
    /// Animations (spec §3.12) : le logo apparaît (échelle 0,8 → 1,05 → 1) puis flotte ;
    /// « Jouer » apparaît 0,4 s après et « respire » ; au clic, l'écran s'efface en 0,4 s.
    ///
    /// v4 §4.4 : sur l'écran titre, seule la pop-up Réglages peut s'ouvrir. Le titre reste
    /// affiché sous le fond assombri, mais « Jouer » n'est ni cliquable ni activable au clavier
    /// (on lui retire le focus, correctif B2). À la fermeture, « Jouer » refonctionne.
    /// « Jouer » reçoit l'éclat qui passe (le seul bouton qui l'a).
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class TitleController : MonoBehaviour
    {
        [SerializeField] GameManager game;

        VisualElement root, logo, playWrap;
        Button playButton;
        bool visible;
        bool coveredByPopup;  // la pop-up Réglages est ouverte par-dessus l'écran titre
        float logoTime, playTime, shineTime;
        Coroutine fade;
        VisualElement shine;

        void OnEnable()
        {
            var doc = GetComponent<UIDocument>().rootVisualElement;
            root = doc.Q<VisualElement>("title-root");
            logo = doc.Q<VisualElement>("logo");
            playWrap = doc.Q<VisualElement>("play-wrap");
            playButton = doc.Q<Button>("play");
            UiSounds.Register(root);
            // « Jouer » joue son propre son (ui_jouer) au lieu du ui_clic habituel, et GARDE
            // le focus clavier (Entrée / Espace lancent la partie depuis l'écran titre).
            playButton.AddToClassList(UiSounds.SilentClass);
            playButton.AddToClassList(ButtonStyler.KeepFocusClass);
            ButtonStyler.Register(root);
            if (shine == null) shine = ButtonStyler.AddShine(playButton);

            playButton.clicked += OnPlayClicked;
            game.Changed += Refresh;
            visible = false;
            Refresh();
        }

        void OnDisable()
        {
            playButton.clicked -= OnPlayClicked;
            game.Changed -= Refresh;
            UiSounds.Unregister(root);
        }

        void OnPlayClicked()
        {
            if (game.State != GameState.Title || game.IsPanelOpen) return;
            if (SoundManager.Instance != null) SoundManager.Instance.Play(Sfx.UiJouer);
            game.StartGame();
        }

        /// Affiché seulement pendant l'état Title. En le quittant, l'écran s'efface (0,4 s)
        /// puis il est caché (display: none) : il ne capte plus aucun clic.
        void Refresh()
        {
            bool title = game.State == GameState.Title;
            if (title) UpdatePanelCover();
            if (title == visible) return;
            visible = title;
            if (fade != null) StopCoroutine(fade);

            if (title)
            {
                root.style.display = DisplayStyle.Flex;
                root.style.opacity = 1f;
                root.pickingMode = PickingMode.Position;
                logoTime = 0f;
                playTime = 0f;
                playButton.Focus(); // la touche Entrée/Espace peut aussi « cliquer »
            }
            else
            {
                fade = StartCoroutine(FadeOut());
            }
        }

        /// La pop-up Réglages s'ouvre (ou se ferme) par-dessus l'écran titre : le titre reste
        /// visible, mais « Jouer » ne réagit plus ni au clic ni au clavier (focus retiré, B2).
        void UpdatePanelCover()
        {
            bool cover = game.IsPanelOpen;
            if (cover == coveredByPopup) return;
            coveredByPopup = cover;
            if (cover)
            {
                playButton.Blur();
                playButton.focusable = false;
                playButton.pickingMode = PickingMode.Ignore;
            }
            else
            {
                playButton.focusable = true;
                playButton.Focus();
            }
        }

        /// Pour les tests : la pop-up Réglages couvre-t-elle l'écran titre ?
        public bool CoveredByPopup => coveredByPopup;
        /// Pour les tests : « Jouer » peut-il recevoir Entrée / Espace en ce moment ?
        public bool PlayFocusable => playButton.focusable;

        IEnumerator FadeOut()
        {
            // Pendant le fondu, plus aucun clic n'est capté par l'écran titre.
            root.pickingMode = PickingMode.Ignore;
            playButton.pickingMode = PickingMode.Ignore;
            for (float t = 0f; t < GameManager.TitleFadeDuration; t += Time.deltaTime)
            {
                root.style.opacity = 1f - t / GameManager.TitleFadeDuration;
                yield return null;
            }
            root.style.display = DisplayStyle.None;
        }

        void Update()
        {
            if (!visible) return;
            float dt = Time.deltaTime;
            shineTime += dt;
            ButtonStyler.TickShine(shine, playButton, shineTime);
            // Réglages ouverts : « Jouer » ne se laisse pas cliquer (le reste continue).
            if (coveredByPopup) playButton.pickingMode = PickingMode.Ignore;

            // Logo : apparition en 0,6 s (opacité 0 → 1, échelle 0,8 → 1,05 → 1),
            // puis il flotte : ±6 px, période 3 s.
            logoTime += dt;
            float k = Mathf.Clamp01(logoTime / 0.6f);
            float s = Ease.Pop(k, 0.8f, 1.05f);
            logo.style.opacity = Mathf.Clamp01(k / 0.7f);
            logo.style.scale = new Scale(new Vector3(s, s, 1f));
            float floatY = logoTime > 0.6f ? 6f * Mathf.Sin(2f * Mathf.PI * (logoTime - 0.6f) / 3f) : 0f;
            logo.style.translate = new Translate(0f, floatY);

            // « Jouer » : apparaît 0,4 s après le logo (comme les boutons du HUD, 0,25 s),
            // puis respire : échelle 1 ↔ 1,05, période 1,6 s. Cliquable une fois apparu.
            float p = logoTime - 0.4f;
            if (p < 0f)
            {
                playWrap.style.opacity = 0f;
                playButton.pickingMode = PickingMode.Ignore;
            }
            else if (p < 0.25f)
            {
                float ps = Ease.Pop(p / 0.25f, 0.6f, 1.08f);
                playWrap.style.opacity = Mathf.Clamp01(p / 0.25f / 0.7f);
                playWrap.style.scale = new Scale(new Vector3(ps, ps, 1f));
            }
            else
            {
                playTime += dt;
                float b = 1f + 0.025f * (1f - Mathf.Cos(2f * Mathf.PI * playTime / 1.6f));
                playWrap.style.opacity = 1f;
                playWrap.style.scale = new Scale(new Vector3(b, b, 1f));
                if (!coveredByPopup) playButton.pickingMode = PickingMode.Position;
            }
        }
    }
}
