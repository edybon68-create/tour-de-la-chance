using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace TourDeLaChance
{
    /// <summary>
    /// La CARTE D'ÉTAGE (spec v5 §9), qui remplace les pastilles en bas (FloorTagView v3/v4).
    /// À chaque arrivée d'étage, une carte glisse du haut et dit en un coup d'œil ce qu'on vient
    /// de gagner et ce qui attend : « ÉTAGE 5 », la phrase drôle, « +60 pièces · cagnotte 210 »,
    /// la bannière spéciale (record, palier), puis « 5 coffres · 1 piège », « Coffres d'or :
    /// trésor ×3 », « Trésor : +150 » et les petites mentions (◆ Diamants : 3 %, Tenue +12 %,
    /// Renaissance ×2). Après 2 s (2,6 s avec une bannière), elle se range en une BARRE
    /// compacte en haut, qui reste visible pendant le choix : le gain est toujours affiché
    /// avant le choix (règle d'or). v6 §8.3 : un clic sur la barre ouvre la ROUTE DES TROPHÉES
    /// (au lieu de rouvrir la carte) ; bannière « Trophée : étage 10 ! +2 ◆ » à un palier ;
    /// mention du bonus de diamants de la tenue ; « dont 1 grâce à ta tenue ».
    ///
    /// Elle ne bloque JAMAIS : survoler ou choisir un coffre, appuyer sur 1-6 ou encaisser la
    /// range aussitôt, et elle ne reçoit pas les clics (sauf la barre compacte).
    /// Pur affichage : tout est lu dans le FloorSetup (préparé par le GameManager), le
    /// catalogue et l'état du jeu. AUCUN chiffre en dur. Le fait que CET étage soit « en
    /// diamants » n'est jamais montré : seulement la chance.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class FloorRecapView : MonoBehaviour
    {
        [SerializeField] GameManager game;
        [SerializeField] ChestTrial chestTrial;
        [SerializeField] AltitudeDecor decor; // nom du palier (« Dans les étoiles ! »)

        // ---------- Les durées (§9.3) ----------
        const float ArriveTime = 0.25f;  // glisse du haut et grandit 0,85 → 1,04 → 1
        const float BigTime = 2.0f;      // reste grande…
        const float BigTimeBanner = 2.6f;// …plus longtemps s'il y a une bannière spéciale
        const float FoldTime = 0.3f;     // se range en barre compacte
        const float QuickFoldTime = 0.15f; // rangée aussitôt (survol d'un coffre, choix…)

        enum Phase { Hidden, Arriving, Big, Folding, Compact }
        Phase phase = Phase.Hidden;
        float phaseTime, bigDuration, foldDuration = FoldTime;

        VisualElement layer, card, pillsRow, bar;
        Label title, phrase, won, banner, mentions, barText;
        Label pillChests, pillRarity, pillGain;
        VisualElement pillRarityBox;

        /// Les textes de la carte (pour les tests).
        public string Text { get; private set; } = "";
        public string BarText => barText.text;
        public bool IsBig => phase == Phase.Arriving || phase == Phase.Big;
        public bool IsCompact => phase == Phase.Compact;

        void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            layer = root.Q<VisualElement>("floor-recap");
            Build();
            game.Changed += OnGameChanged;
            game.FloorShown += OnFloorShown;
            if (chestTrial != null) chestTrial.HoverChanged += OnHover;
            bar.RegisterCallback<ClickEvent>(OnBarClicked);
            SetPhase(Phase.Hidden);
        }

        void OnDisable()
        {
            game.Changed -= OnGameChanged;
            game.FloorShown -= OnFloorShown;
            if (chestTrial != null) chestTrial.HoverChanged -= OnHover;
            bar.UnregisterCallback<ClickEvent>(OnBarClicked);
        }

        /// Fabrique la carte et la barre (une seule fois).
        void Build()
        {
            layer.Clear();
            layer.pickingMode = PickingMode.Ignore;

            card = Box("recap-card");
            title = Text_("recap-title", card);
            phrase = Text_("recap-phrase", card);
            won = Text_("recap-won", card);
            banner = Text_("recap-banner", card);
            // v6 (QA n°30) : le gain a SA PROPRE ligne, juste avant les autres pastilles. Avant,
            // il partageait la rangée des pastilles : quand elle débordait, il passait à la
            // ligne et chevauchait les petites mentions (gain illisible = règle d'or touchée).
            var gainRow = Box("recap-gain-row", card);
            pillGain = Pill(gainRow, "recap-pill recap-pill--gain", out _);
            pillsRow = Box("recap-pills", card);
            pillChests = Pill(pillsRow, "recap-pill", out _);
            pillRarity = Pill(pillsRow, "recap-pill", out pillRarityBox);
            mentions = Text_("recap-mentions", card);
            layer.Add(card);

            // La barre compacte : la SEULE partie cliquable (elle rouvre la carte).
            bar = new VisualElement { pickingMode = PickingMode.Position };
            bar.AddToClassList("recap-bar");
            barText = new Label { pickingMode = PickingMode.Ignore };
            barText.AddToClassList("recap-bar-text");
            bar.Add(barText);
            layer.Add(bar);
        }

        static VisualElement Box(string classes, VisualElement parent = null)
        {
            var e = new VisualElement { pickingMode = PickingMode.Ignore };
            foreach (var c in classes.Split(' ')) e.AddToClassList(c);
            parent?.Add(e);
            return e;
        }

        static Label Text_(string className, VisualElement parent)
        {
            var l = new Label { pickingMode = PickingMode.Ignore };
            l.AddToClassList(className);
            parent.Add(l);
            return l;
        }

        static Label Pill(VisualElement parent, string classes, out VisualElement pill)
        {
            pill = Box(classes, parent);
            return Text_("recap-pill-text", pill);
        }

        // =====================================================================
        // Remplir la carte (tout est lu dans le FloorSetup et l'état du jeu)
        // =====================================================================

        void Fill()
        {
            var setup = game.Setup;
            int floor = game.Floor;
            title.text = $"ÉTAGE {floor}";

            // La phrase drôle de montée (v3 §11) ; étage 1 : « C'est parti ! ».
            bool newTier = floor > 1 && GameManager.TierForFloor(floor) != GameManager.TierForFloor(floor - 1);
            string tierName = newTier && decor != null ? decor.TierName(GameManager.TierForFloor(floor)) : "";
            // v6 (QA n°42) : après une reprise, on n'est PAS monté (on rejoue l'étage) : pas de
            // phrase de montée, la ligne « Reprise réussie » suffit.
            phrase.text = floor <= 1 ? "C'est parti !"
                        : game.RevivedThisFloor ? ""
                        : TextPicker.ClimbLine(floor, "", game.Pot, game.LastGain);
            Show(phrase, !string.IsNullOrEmpty(phrase.text));

            // Ce que tu viens de gagner (après un trésor / une reprise).
            won.RemoveFromClassList("recap-won--diamants");
            if (game.RevivedThisFloor) won.text = $"Reprise réussie : on rejoue l'étage {floor}";
            else if (floor >= 2 && game.LastDiamonds > 0)
            {
                won.text = $"+{game.LastDiamonds} diamant{(game.LastDiamonds > 1 ? "s" : "")} !";
                // v6 §11.3 : quand le bonus de la tenue a ajouté un diamant, on le dit.
                if (game.LastDiamondsFromBonus > 0) won.text += $" (dont {game.LastDiamondsFromBonus} grâce à ta tenue)";
                won.AddToClassList("recap-won--diamants");
            }
            else if (floor >= 2 && game.LastGain > 0) won.text = $"+{game.LastGain} pièces · cagnotte {game.Pot}";
            else won.text = "";
            Show(won, won.text.Length > 0);

            // Bannière spéciale (remplace les bannières séparées) : record, nouveau palier.
            // v6 §8.4 : priorité Trophée > Nouveau record > décor (le trophée remplace le record).
            var lines = new List<string>();
            bool trophy = game.TrophyDiamondsThisFloor > 0;
            if (trophy) lines.Add($"Trophée : étage {game.TrophyFloorThisFloor} ! +{game.TrophyDiamondsThisFloor} {ShopUi.Diamant}");
            else if (game.NewRecordThisFloor && TextPicker.Texts != null) lines.Add(TextPicker.Texts.recordLine);
            if (!string.IsNullOrEmpty(tierName)) lines.Add(tierName);
            banner.text = string.Join(" · ", lines);
            ShopUi.SetClass(banner, "recap-banner--trophee", trophy);
            Show(banner, banner.text.Length > 0);
            // v6 (QA n°30) : carte moins chargée. Avec une bannière (record, palier), la phrase
            // drôle s'efface : la bannière est plus importante et la carte reste lisible.
            if (banner.text.Length > 0) Show(phrase, false);

            // Ce qui t'attend.
            pillChests.text = $"{setup.ChestCount} coffres · 1 piège";
            bool precious = setup.RarityRank >= 1 && setup.Rarity != null;
            Show(pillRarityBox, precious);
            if (precious) pillRarity.text = $"{setup.Rarity.displayName} : trésor ×{ShopUi.Number(setup.Rarity.multiplier)}";
            pillGain.text = $"Trésor : +{setup.Gain}";

            // Petites mentions (seulement si non nulles / actives).
            var m = new List<string>();
            if (setup.DiamondChancePermille > 0)
                m.Add($"{ShopUi.Diamant} Diamants : {ShopUi.Number(setup.DiamondChancePermille / 10f)}\u00A0%"
                      + (setup.DiamondBonusPercent > 0 ? $" · tenue {ShopUi.Diamant} +{setup.DiamondBonusPercent}\u00A0%" : ""));
            if (setup.OutfitBonusPercent > 0) m.Add($"Tenue +{setup.OutfitBonusPercent}\u00A0%");
            if (setup.RebirthMultiplier > 1) m.Add($"Renaissance ×{setup.RebirthMultiplier}");
            mentions.text = string.Join("   ·   ", m);
            Show(mentions, m.Count > 0);

            // Couleur du palier (celle du badge d'étage).
            for (int t = 1; t <= 4; t++) ShopUi.SetClass(card, "recap-card--palier" + t, GameManager.TierForFloor(floor) == t);

            // La barre compacte : « Trésor +150 · 5 coffres · Or ×3 · ◆ 3 % ».
            var b = new List<string> { $"Trésor +{setup.Gain}", $"{setup.ChestCount} coffres" };
            if (precious) b.Add($"{setup.Rarity.shortName} ×{ShopUi.Number(setup.Rarity.multiplier)}");
            if (setup.DiamondChancePermille > 0) b.Add($"{ShopUi.Diamant} {ShopUi.Number(setup.DiamondChancePermille / 10f)}\u00A0%");
            barText.text = string.Join(" · ", b);

            bigDuration = banner.text.Length > 0 ? BigTimeBanner : BigTime;
            Text = string.Join(" | ", new[] { title.text, phrase.text, won.text, banner.text, pillChests.text,
                                                precious ? pillRarity.text : "", pillGain.text, mentions.text });
        }

        static void Show(VisualElement e, bool on) => e.style.display = on ? DisplayStyle.Flex : DisplayStyle.None;

        // =====================================================================
        // La vie de la carte (§9.3)
        // =====================================================================

        /// La carte peut-elle être visible en ce moment ? Choix et révélation, sans pop-up,
        /// sans ouverture de boîte (cachée en Montée, Perdu, Encaissé, Reprise proposée).
        bool Allowed =>
            (game.State == GameState.Choosing || game.State == GameState.Revealing) &&
            !game.IsPanelOpen && !game.BoxOpeningActive && !game.RebirthPlaying;

        void OnFloorShown()
        {
            if (game.State != GameState.Choosing) return;
            Fill();
            SetPhase(Phase.Arriving);
            if (SoundManager.Instance != null) SoundManager.Instance.Play(Sfx.RecapEtage);
            if ((game.NewRecordThisFloor || game.TrophyDiamondsThisFloor > 0) && SoundManager.Instance != null) SoundManager.Instance.Play(Sfx.Record);
            if (game.TrophyDiamondsThisFloor > 0 && SoundManager.Instance != null) SoundManager.Instance.Play(Sfx.DiamantGain1);
        }

        void OnGameChanged()
        {
            if (!Allowed) { SetPhase(Phase.Hidden); return; }
            // Revenue d'une pop-up pendant le choix : la barre compacte réapparaît.
            if (phase == Phase.Hidden && game.State == GameState.Choosing && !game.TransitionLocked)
            {
                Fill();
                SetPhase(Phase.Compact);
            }
            // Dès qu'un coffre est choisi, la carte se range aussitôt.
            if (game.State == GameState.Revealing) Fold(QuickFoldTime);
        }

        /// Survol d'un coffre : la carte se range aussitôt (0,15 s).
        void OnHover(int index)
        {
            if (index >= 0) Fold(QuickFoldTime);
        }

        void Fold(float duration)
        {
            if (phase != Phase.Arriving && phase != Phase.Big) return;
            foldDuration = duration;
            SetPhase(Phase.Folding);
        }

        /// v6 §8.3 : la barre compacte ouvre la route des trophées (le GameManager décide si
        /// c'est permis). L'ancien « rouvrir la carte 2 s » est remplacé.
        void OnBarClicked(ClickEvent evt)
        {
            if (phase != Phase.Compact || !Allowed) return;
            if (game.CanOpen(PanelId.Route)) game.OpenPanel(PanelId.Route);
        }

        void SetPhase(Phase p)
        {
            phase = p;
            phaseTime = 0f;
            Render();
        }

        void Update()
        {
            if (phase == Phase.Hidden) return;
            // Encaisser, la touche 1 à 6 : l'état change et OnGameChanged range la carte ;
            // par sécurité, on vérifie aussi ici.
            if (!Allowed) { SetPhase(Phase.Hidden); return; }
            phaseTime += Time.deltaTime;
            switch (phase)
            {
                case Phase.Arriving: if (phaseTime >= ArriveTime) SetPhase(Phase.Big); break;
                case Phase.Big: if (phaseTime >= bigDuration) { foldDuration = FoldTime; SetPhase(Phase.Folding); } break;
                case Phase.Folding: if (phaseTime >= foldDuration) SetPhase(Phase.Compact); break;
            }
            Render();
        }

        void Render()
        {
            switch (phase)
            {
                case Phase.Hidden:
                    card.style.display = DisplayStyle.None;
                    bar.style.display = DisplayStyle.None;
                    break;
                case Phase.Arriving:
                {
                    float k = Mathf.Clamp01(phaseTime / ArriveTime);
                    float s = Ease.Pop(k, 0.85f, 1.04f);
                    card.style.display = DisplayStyle.Flex;
                    bar.style.display = DisplayStyle.None;
                    card.style.opacity = Mathf.Clamp01(k / 0.6f);
                    card.style.scale = new Scale(new Vector3(s, s, 1f));
                    card.style.translate = new Translate(0f, -40f * (1f - Ease.Out(k)));
                    break;
                }
                case Phase.Big:
                    card.style.display = DisplayStyle.Flex;
                    bar.style.display = DisplayStyle.None;
                    card.style.opacity = 1f;
                    card.style.scale = new Scale(Vector3.one);
                    card.style.translate = new Translate(0f, 0f);
                    break;
                case Phase.Folding:
                {
                    // Elle rétrécit vers le haut pendant que la barre apparaît.
                    float k = Ease.In(Mathf.Clamp01(phaseTime / foldDuration));
                    card.style.display = DisplayStyle.Flex;
                    bar.style.display = DisplayStyle.Flex;
                    card.style.opacity = 1f - k;
                    card.style.scale = new Scale(new Vector3(Mathf.Lerp(1f, 0.6f, k), Mathf.Lerp(1f, 0.2f, k), 1f));
                    card.style.translate = new Translate(0f, -110f * k);
                    bar.style.opacity = k;
                    break;
                }
                case Phase.Compact:
                    card.style.display = DisplayStyle.None;
                    bar.style.display = DisplayStyle.Flex;
                    bar.style.opacity = 1f;
                    break;
            }
        }
    }
}
