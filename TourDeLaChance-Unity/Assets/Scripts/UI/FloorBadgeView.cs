using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

namespace TourDeLaChance
{
    /// <summary>
    /// Le BADGE D'ÉTAGE du HUD (spec v4 §6.1), qui remplace le texte « Étage N » : un écusson
    /// arrondi en haut à gauche, « ÉTAGE » en or et le nombre en grand. Pur affichage : le
    /// nombre est lu dans l'état du jeu (game.Floor), jamais écrit dans l'UXML.
    ///   - couleur selon le palier de décor : bleu (1-3), orange (4-6), violet (7-9), nuit
    ///     étoilée à partir de 10 (5 petites étoiles qui scintillent) ; elle change pendant le
    ///     noir de la montée (FloorPrepared) ;
    ///   - « pop » du nombre (1 → 1,35 → 1 en 0,3 s) quand le nouvel étage se montre ;
    ///   - record battu pendant la partie : contour or et étiquette « RECORD » ;
    ///   - au tout premier étage (fin du fondu de « Jouer »), le badge apparaît (0,6 → 1,08 → 1) ;
    ///   - v6 §8.3 : un CLIC sur le badge ouvre la route des trophées (le GameManager décide si
    ///     c'est permis : jamais pendant une épreuve, une transition ou une reprise proposée).
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class FloorBadgeView : MonoBehaviour
    {
        [SerializeField] GameManager game;

        VisualElement badge, stars;
        Label number;
        int shownTier = -1;
        bool appeared;
        float starTime;
        readonly VisualElement[] starElements = new VisualElement[5];
        static readonly Vector2[] StarPositions =
            { new Vector2(18, 16), new Vector2(166, 22), new Vector2(30, 82), new Vector2(158, 78), new Vector2(98, 6) };

        void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            badge = root.Q<VisualElement>("badge");
            stars = root.Q<VisualElement>("badge-stars");
            number = root.Q<Label>("floor");
            ButtonStyler.EnableShadow(badge); // même ombre portée que les gros boutons
            stars.Clear();
            for (int i = 0; i < starElements.Length; i++)
            {
                var star = new VisualElement { pickingMode = PickingMode.Ignore };
                star.AddToClassList("badge-star");
                star.style.backgroundImage = new StyleBackground(UiFxTextures.Sparkle);
                star.style.left = StarPositions[i].x;
                star.style.top = StarPositions[i].y;
                stars.Add(star);
                starElements[i] = star;
            }
            badge.RegisterCallback<ClickEvent>(OnBadgeClicked);
            game.Changed += Refresh;
            game.FloorPrepared += Refresh;
            game.FloorShown += OnFloorShown;
            Refresh();
        }

        void OnDisable()
        {
            badge.UnregisterCallback<ClickEvent>(OnBadgeClicked);
            game.Changed -= Refresh;
            game.FloorPrepared -= Refresh;
            game.FloorShown -= OnFloorShown;
        }

        /// Clic sur le badge : la route des trophées (ou la ferme si elle est déjà ouverte).
        void OnBadgeClicked(ClickEvent evt)
        {
            if (game.PanelOpen != PanelId.Route && !game.CanOpen(PanelId.Route)) { UiSounds.PlayRefus(); return; }
            game.TogglePanel(PanelId.Route);
        }

        /// Le texte affiché (pour les tests).
        public string NumberText => number.text;
        public int TierShown => shownTier;
        public bool RecordShown => badge.ClassListContains("badge--record");

        void Refresh()
        {
            if (game.State == GameState.Title) return; // le HUD entier est caché
            int floor = Mathf.Max(1, game.Floor);
            number.text = floor.ToString();
            ShopUi.SetClass(number, "badge-number--long", floor >= 100); // 3 chiffres : taille 52
            int tier = game.Tier;
            if (tier != shownTier)
            {
                for (int t = 1; t <= 4; t++) ShopUi.SetClass(badge, "badge--palier" + t, t == tier);
                shownTier = tier;
            }
            ShopUi.SetClass(badge, "badge--record", game.RecordBeatenThisRun);
        }

        void OnFloorShown()
        {
            Refresh();
            if (!appeared)
            {
                appeared = true;
                StartCoroutine(Scale(badge, 0.25f, 0.6f, 1.08f));
            }
            else StartCoroutine(Scale(number, 0.3f, 1f, 1.35f));
        }

        /// Un « pop » : from → overshoot → 1 (courbe Ease.Pop).
        static IEnumerator Scale(VisualElement e, float duration, float from, float overshoot)
        {
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                float s = from == 1f ? 1f + (overshoot - 1f) * Ease.Sine(t / duration) : Ease.Pop(t / duration, from, overshoot);
                e.style.scale = new Scale(new Vector3(s, s, 1f));
                yield return null;
            }
            e.style.scale = StyleKeyword.Null;
        }

        void Update()
        {
            if (shownTier != 4) return;
            // Palier 4 : chaque étoile scintille (0,4 ↔ 1, période 1,5 s), décalées entre elles.
            starTime += Time.deltaTime;
            for (int i = 0; i < starElements.Length; i++)
                starElements[i].style.opacity = 0.7f + 0.3f * Mathf.Sin(2f * Mathf.PI * (starTime / 1.5f + i / 5f));
        }
    }
}
