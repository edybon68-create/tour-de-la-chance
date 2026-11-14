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
    /// avant le choix (règle d'or). v6 §8.3 (remplacé en v7) : un clic sur la barre ouvrait la
    /// route des trophées ; bannière « Trophée : étage 10 ! +2 ◆ » à un palier ;
    /// mention du bonus de diamants de la tenue ; « dont 1 grâce à ta tenue ».
    ///
    /// Elle ne bloque JAMAIS : survoler ou choisir un coffre, appuyer sur 1-6 ou encaisser la
    /// range aussitôt, et elle ne reçoit pas les clics (sauf la barre compacte).
    ///
    /// v7 (art bible §17.5) : carte ALLÉGÉE. Un titre, un héros (le gain du trésor, le plus gros
    /// texte de la carte, règle d'or), une rangée de pastilles (coffres, chance de diamants,
    /// tenue, Renaissance) et un seul autocollant penché (record, reprise ou palier). Le reste
    /// (phrase, ce qu'on vient de gagner, nom de la rareté des coffres, mentions détaillées)
    /// est dans la vue « TOUTES LES INFOS » : un clic sur la barre compacte rouvre la carte
    /// 2 s avec ces détails en plus (mandat V7, point 3 ; comme avant la v6). C'est le badge
    /// ÉTAGE en haut à gauche qui ouvre L'Ascension (FloorBadgeView).
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
        [Tooltip("v7 §17.5 : icône de la Renaissance pour la pastille « ×2 » (icone_rebirth.png). Vide = le mot « Renaissance ».")]
        [SerializeField] Sprite rebirthIcon;

        // ---------- Les durées (§9.3) ----------
        const float ArriveTime = 0.25f;  // glisse du haut et grandit 0,85 → 1,04 → 1
        const float BigTime = 2.0f;      // reste grande…
        const float BigTimeBanner = 2.6f;// …plus longtemps s'il y a une bannière spéciale
        const float FoldTime = 0.3f;     // se range en barre compacte
        const float QuickFoldTime = 0.15f; // rangée aussitôt (survol d'un coffre, choix…)
        const float ReopenTime = 2.0f;
        const float ReviveTime = 0.9f;   // v7 : carte « Reprise réussie » raccourcie   // v7 : un clic sur la barre rouvre la carte 2 s (toutes les infos)

        enum Phase { Hidden, Arriving, Big, Folding, Compact }
        Phase phase = Phase.Hidden;
        float phaseTime, bigDuration, foldDuration = FoldTime;

        VisualElement layer, card, pillsRow, bar, details;
        Label title, sticker, heroGain, phrase, won, rarityLine, mentions, barGain;
        // Les pastilles v7 : (la pastille, son icône, son texte).
        VisualElement pillChests, pillChestsIcon, pillDiamonds, pillOutfit, pillOutfitIcon, pillRebirth, pillRebirthIcon;
        Label pillChestsText, pillDiamondsText, pillOutfitText, pillRebirthText;

        /// v7 : vrai quand la carte a été rouverte par un clic (vue « toutes les infos »).
        bool detailed;

        /// Les textes de la carte (pour les tests).
        public string Text { get; private set; } = "";
        public string BarText => barTextValue;
        string barTextValue = "";
        VisualElement barParts; // V7.1 n°10 : « 4 coffres » • « ◆ 2 % », séparés par des points
        public bool IsBig => phase == Phase.Arriving || phase == Phase.Big;
        public bool IsCompact => phase == Phase.Compact;
        /// v7 : la carte montre-t-elle « toutes les infos » (rouverte par un clic) ?
        public bool IsDetailed => detailed && IsBig;
        /// OUTIL DE TEST : simule un clic sur la barre compacte.
        public void DebugClickBar() => OnBarClicked(null);

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

        /// Fabrique la carte et la barre (une seule fois). v7 §17.5 : titre, héros, pastilles,
        /// autocollant ; les détails sont dans un bloc caché, montré seulement après un clic.
        void Build()
        {
            layer.Clear();
            layer.pickingMode = PickingMode.Ignore;

            card = Box("recap-card rc7");
            // L'autocollant penché, à cheval sur le bord haut (un seul à la fois).
            sticker = Text_("rc7-autocollant", card);
            title = Text_("recap-title rc7-titre", card);

            // LE HÉROS : le gain du trésor, en grand chiffre or posé sur une lueur (v7, retour de
            // l'auteur, point 13 : plus de pilule « bouton pièce »). Règle d'or : le gain est
            // toujours affiché avant le choix, et c'est le plus gros texte de la carte.
            var hero = Box("rc7-heros", card);
            var heroGlow = Box("rc7-heros__lueur", hero);
            heroGlow.style.backgroundImage = new StyleBackground(UiFxTextures.Halo);
            Text_("rc7-heros__mot", hero).text = "TRÉSOR";
            var heroRow = Box("rc7-heros__ligne", hero);
            Box("coin rc7-heros__piece", heroRow);
            heroGain = Text_("rc7-heros__gain", heroRow);

            pillsRow = Box("rc7-pastilles", card);
            pillChests = Pill7(pillsRow, out pillChestsIcon, out pillChestsText);
            pillDiamonds = Pill7(pillsRow, out var diamondIcon, out pillDiamondsText);
            diamondIcon.AddToClassList("diamant");
            pillDiamonds.AddToClassList("rc7-pastille--diamants");
            pillOutfit = Pill7(pillsRow, out pillOutfitIcon, out pillOutfitText);
            pillOutfit.AddToClassList("rc7-pastille--sombre");
            pillRebirth = Pill7(pillsRow, out pillRebirthIcon, out pillRebirthText);
            pillRebirth.AddToClassList("rc7-pastille--sombre");

            // « Toutes les infos » : seulement quand la carte est rouverte par un clic.
            details = Box("rc7-details", card);
            phrase = Text_("recap-phrase", details);
            won = Text_("recap-won", details);
            rarityLine = Text_("rc7-details__ligne", details);
            // v7 (point 13) : 3 niveaux seulement. Titre, gain, puis TOUT le reste à la même taille.
            phrase.AddToClassList("rc7-details__ligne");
            won.AddToClassList("rc7-details__ligne");
            mentions = Text_("recap-mentions rc7-details__ligne", details);
            layer.Add(card);

            // La barre compacte : la SEULE partie cliquable (elle rouvre la carte, toutes les infos).
            // Même ordre de lecture que la carte : le gain d'abord.
            bar = new VisualElement { pickingMode = PickingMode.Position };
            bar.AddToClassList("recap-bar");
            bar.AddToClassList("rc7-barre");
            Box("coin rc7-barre__piece", bar);
            barGain = Text_("rc7-barre__gain", bar);
            barParts = Box("rc7-barre__parties", bar);
            layer.Add(bar);
        }

        /// Une pastille v7 : une icône de 30 px et un texte.
        static VisualElement Pill7(VisualElement parent, out VisualElement icon, out Label text)
        {
            var pill = Box("rc7-pastille", parent);
            icon = Box("rc7-pastille__icone", pill);
            text = Text_("rc7-pastille__texte", pill);
            return pill;
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
            foreach (var c in className.Split(' ')) l.AddToClassList(c); // plusieurs classes possibles
            parent.Add(l);
            return l;
        }

        // =====================================================================
        // Remplir la carte (tout est lu dans le FloorSetup et l'état du jeu)
        // =====================================================================

        void Fill()
        {
            var setup = game.Setup;
            int floor = game.Floor;
            title.text = $"ÉTAGE {floor}";
            // Le héros : le gain du trésor (le multiplicateur de rareté y est déjà compris).
            heroGain.text = $"+{ShopUi.Thousands(setup.Gain)}";

            // ---- L'autocollant : une seule chose à la fois (trophée/record, reprise, palier) ----
            bool newTier = floor > 1 && GameManager.TierForFloor(floor) != GameManager.TierForFloor(floor - 1);
            string tierName = newTier && decor != null ? decor.TierName(GameManager.TierForFloor(floor)) : "";
            bool trophy = game.TrophyDiamondsThisFloor > 0;
            string kind;
            if (trophy) { sticker.text = $"Trophée : étage {game.TrophyFloorThisFloor} ! +{game.TrophyDiamondsThisFloor} {ShopUi.Diamant}"; kind = "record"; }
            else if (game.NewRecordThisFloor && TextPicker.Texts != null) { sticker.text = TextPicker.Texts.recordLine; kind = "record"; }
            else if (game.RevivedThisFloor) { sticker.text = "Reprise réussie"; kind = "reprise"; }
            else if (!string.IsNullOrEmpty(tierName)) { sticker.text = tierName; kind = "palier"; }
            else { sticker.text = ""; kind = ""; }
            foreach (var k in new[] { "record", "reprise", "palier" }) ShopUi.SetClass(sticker, "rc7-autocollant--" + k, k == kind);
            Show(sticker, sticker.text.Length > 0);
            ShopUi.SetClass(card, "rc7--autocollant", sticker.text.Length > 0);

            // ---- Les pastilles ----
            bool precious = setup.RarityRank >= 1 && setup.Rarity != null;
            pillChestsText.text = $"{setup.ChestCount} coffres · 1 piège";
            // Fond = couleur de la rareté des coffres de l'étage, icône du coffre de cette rareté.
            if (setup.Rarity != null)
            {
                pillChests.style.backgroundColor = setup.Rarity.bodyColor;
                SetIcon(pillChestsIcon, setup.Rarity.icon);
            }
            else { pillChests.style.backgroundColor = StyleKeyword.Null; SetIcon(pillChestsIcon, null); }

            Show(pillDiamonds, setup.DiamondChancePermille > 0);
            pillDiamondsText.text = $"{ShopUi.Number(setup.DiamondChancePermille / 10f)}\u00A0%";

            Show(pillOutfit, setup.OutfitBonusPercent > 0);
            pillOutfitText.text = $"+{setup.OutfitBonusPercent}\u00A0%";
            var skin = Shop.GetEquippedSkin();
            SetIcon(pillOutfitIcon, skin != null ? skin.Icon : null);

            Show(pillRebirth, setup.RebirthMultiplier > 1);
            SetIcon(pillRebirthIcon, rebirthIcon);
            pillRebirthText.text = rebirthIcon != null ? $"×{setup.RebirthMultiplier}" : $"Renaissance ×{setup.RebirthMultiplier}";

            // ---- « Toutes les infos » (vue détaillée, après un clic) ----
            // La phrase drôle de montée ; après une reprise, on n'est PAS monté (v6 QA n°42).
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

            // Le nom de la rareté des coffres (le ×3 est déjà dans le gain du héros).
            rarityLine.text = precious ? $"{setup.Rarity.displayName} : trésor ×{ShopUi.Number(setup.Rarity.multiplier)}" : "";
            Show(rarityLine, precious);

            // Les mentions détaillées (seulement si non nulles / actives).
            // QA v7 p2 n°13 : seulement ce que les pastilles ne disent pas déjà (pas de « 2 % » en double).
            var m = new List<string>();
            if (setup.DiamondBonusPercent > 0) m.Add($"Bonus de diamants de ta tenue : +{setup.DiamondBonusPercent}\u00A0%");
            if (setup.OutfitBonusPercent > 0) m.Add($"Bonus de pièces de ta tenue : +{setup.OutfitBonusPercent}\u00A0%");
            mentions.text = string.Join("   ·   ", m);
            Show(mentions, m.Count > 0);

            // Couleur du palier (celle du badge d'étage).
            for (int t = 1; t <= 4; t++) ShopUi.SetClass(card, "recap-card--palier" + t, GameManager.TierForFloor(floor) == t);

            // La barre compacte : « ● +103 · 3 coffres · ◆ 1,5 % » (le gain d'abord).
            barGain.text = $"+{ShopUi.Thousands(setup.Gain)}";
            var b = new List<string> { $"{setup.ChestCount} coffres" };
            if (setup.DiamondChancePermille > 0) b.Add($"{ShopUi.Diamant} {ShopUi.Number(setup.DiamondChancePermille / 10f)}\u00A0%");
            barTextValue = " · " + string.Join(" · ", b);
            // V7.1 n°10 : le gain d'abord (40 px), puis les petites infos (24 px) séparées par un point rond.
            barParts.Clear();
            foreach (var part in b)
            {
                Box("rc7-barre__point", barParts);
                var l = Text_("recap-bar-text rc7-barre__texte", barParts);
                l.text = part;
            }

            bigDuration = sticker.text.Length > 0 ? BigTimeBanner : BigTime;
            Text = string.Join(" | ", new[] { title.text, sticker.text, $"Trésor {heroGain.text}", pillChestsText.text,
                                                setup.DiamondChancePermille > 0 ? pillDiamondsText.text : "",
                                                setup.OutfitBonusPercent > 0 ? pillOutfitText.text : "",
                                                setup.RebirthMultiplier > 1 ? pillRebirthText.text : "",
                                                phrase.text, won.text, rarityLine.text, mentions.text });
        }

        /// Pose une image sur une icône de pastille ; sans image, l'icône est masquée.
        static void SetIcon(VisualElement icon, Sprite sprite)
        {
            if (sprite != null) icon.style.backgroundImage = new StyleBackground(sprite);
            else icon.style.backgroundImage = StyleKeyword.Null;
            // L'icône des diamants a son image par la classe USS « diamant » : on la garde.
            bool hasClassImage = icon.ClassListContains("diamant");
            icon.style.display = sprite != null || hasClassImage ? DisplayStyle.Flex : DisplayStyle.None;
        }

        /// v7 : la vue « toutes les infos » montre le bloc de détails en plus.
        void SetDetailed(bool on)
        {
            detailed = on;
            Show(details, on);
            ShopUi.SetClass(card, "rc7--details", on);
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
            SetDetailed(false);
            bigDuration = sticker.text.Length > 0 ? BigTimeBanner : BigTime;
            // v7 (point 13) : après une reprise, la carte ne reste qu'un instant : elle ne cache
            // plus le cheval (le détail reste à un clic, sur la barre).
            if (game.RevivedThisFloor) bigDuration = ReviveTime;
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
                SetDetailed(false);
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

        /// v7 (mandat point 3) : un clic sur la barre compacte ROUVRE la carte pendant 2 s, avec
        /// « toutes les infos » (comme avant la v6). L'Ascension s'ouvre par le badge ÉTAGE.
        void OnBarClicked(ClickEvent evt)
        {
            if (phase != Phase.Compact || !Allowed) return;
            Fill();
            SetDetailed(true);
            bigDuration = ReopenTime;
            SetPhase(Phase.Arriving);
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
