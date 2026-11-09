using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

namespace TourDeLaChance
{
    /// <summary>
    /// L'écran titre (Title.uxml) : le logo, le bouton « Jouer » et les boutons du son.
    /// Il ne décide de rien : il est visible tant que le GameManager est dans l'état Title,
    /// et le bouton demande simplement au GameManager de lancer la partie (StartGame).
    ///
    /// Animations (spec §3.12) : le logo apparaît (échelle 0,8 → 1,05 → 1) puis flotte ;
    /// « Jouer » apparaît 0,4 s après et « respire » ; au clic, l'écran s'efface en 0,4 s.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class TitleController : MonoBehaviour
    {
        [SerializeField] GameManager game;

        VisualElement root, logo, playWrap;
        Button playButton;
        SoundToggleButtons soundButtons;
        bool visible;
        float logoTime, playTime;
        Coroutine fade;

        void OnEnable()
        {
            var doc = GetComponent<UIDocument>().rootVisualElement;
            root = doc.Q<VisualElement>("title-root");
            logo = doc.Q<VisualElement>("logo");
            playWrap = doc.Q<VisualElement>("play-wrap");
            playButton = doc.Q<Button>("play");
            soundButtons = new SoundToggleButtons(doc.Q<Button>("music-toggle"), doc.Q<Button>("effects-toggle"));
            UiSounds.Register(root);
            // « Jouer » joue son propre son (ui_jouer) au lieu du ui_clic habituel.
            playButton.AddToClassList(UiSounds.SilentClass);

            playButton.clicked += OnPlayClicked;
            game.Changed += Refresh;
            visible = false;
            Refresh();
        }

        void OnDisable()
        {
            playButton.clicked -= OnPlayClicked;
            game.Changed -= Refresh;
            soundButtons?.Dispose();
            UiSounds.Unregister(root);
        }

        void OnPlayClicked()
        {
            if (game.State != GameState.Title) return;
            if (SoundManager.Instance != null) SoundManager.Instance.Play(Sfx.UiJouer);
            game.StartGame();
        }

        /// Affiché seulement pendant l'état Title. En le quittant, l'écran s'efface (0,4 s)
        /// puis il est caché (display: none) : il ne capte plus aucun clic.
        void Refresh()
        {
            bool title = game.State == GameState.Title;
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
                playButton.pickingMode = PickingMode.Position;
            }
        }
    }
}
