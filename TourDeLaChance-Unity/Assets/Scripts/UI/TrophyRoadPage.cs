using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace TourDeLaChance
{
    /// <summary>
    /// La pop-up « L'Ascension » (spec v6.5 §6, art bible §16.2 et §16.3), qui remplace la route
    /// des trophées de la v6 : une colonne de DALLES en relief (rendues dans Blender) qu'on fait
    /// défiler, de l'étage 1 (en bas) à max(30, record + 5) en haut. On REGARDE au lieu de lire :
    ///   - à gauche, une barre de progression verticale (or jusqu'au record) et les numéros d'étage ;
    ///   - sur chaque dalle, les récompenses en images (pièces, diamants), le drapeau du record, le
    ///     cheval « toi » ; sur les paliers, la carte de la récompense (et « Récupérer » si elle est
    ///     gagnée mais pas encore prise) ;
    ///   - à droite, un bouton « i » par étage qui ouvre le détail chiffré (gain possible, chance
    ///     d'y arriver, tes coffres en ce moment) : la règle d'or veut que l'information reste
    ///     accessible et vraie.
    /// Tous les nombres viennent de TrophyRoad (qui utilise les fonctions du jeu) : aucune règle
    /// ici. Ce n'est pas un MonoBehaviour : PanelsController le crée et l'avance (Tick).
    /// (Le nom de la classe est gardé pour ne pas débrancher PanelsController.)
    ///
    /// Comment c'est construit : toute la colonne est UN grand élément de hauteur calculée, et
    /// chaque chose y est placée en position absolue, avec les coordonnées en pixels du §16.3.6.
    /// Les calques, de l'arrière vers l'avant : fond (bandes de couleur, losanges, ombre, étoiles),
    /// barre de progression, numéros, dalles, puis UN calque pour tout ce qui est posé sur les
    /// dalles (sinon la dalle du dessous, dessinée après, recouvrirait le drapeau de celle du dessus),
    /// puis les boutons « i ».
    /// </summary>
    public class TrophyRoadPage : IDisposable
    {
        // ---- Mesures (pixels de référence, art bible §16.2.1 et §16.3.6) ----
        const float ContentWidth = 1032f;           // la zone de route, moins la barre de défilement
        const float TileCenterX = 590f;             // les dalles sont centrées sur x = 590
        const float NumberCenterX = 110f, InfoCenterX = 990f;
        const float TopPadding = 40f, BottomPadding = 40f;
        const float Gap = 36f;                      // vide entre le bas d'une dalle et l'arrière de la suivante
        const float FadeHeight = 320f;              // fondu entre deux couleurs de décor

        // Les deux formats de dalle : taille affichée, marges transparentes, repères.
        struct TileShape
        {
            public float W, H, MarginTop, MarginBottom, TopCenterY;
            public Vector2 LeftPoint, RightPoint, HorsePoint, FlagPoint, PedestalPoint, ButtonPoint;
        }

        static readonly TileShape Normal = new TileShape
        {
            W = 560f, H = 340f, MarginTop = 44f, MarginBottom = 11f, TopCenterY = 166f,
            LeftPoint = new Vector2(124f, 206f), RightPoint = new Vector2(436f, 206f),
            HorsePoint = new Vector2(280f, 236f), FlagPoint = new Vector2(205f, 110f)
        };

        static readonly TileShape Milestone = new TileShape
        {
            W = 716f, H = 430f, MarginTop = 52f, MarginBottom = 20f, TopCenterY = 210f,
            PedestalPoint = new Vector2(358f, 182f), HorsePoint = new Vector2(358f, 326f),
            FlagPoint = new Vector2(275f, 94f), ButtonPoint = new Vector2(358f, 400f)
        };

        // Les 4 décors des dalles = les 4 paliers d'altitude du jeu (§16.3.1 v2). Les noms de
        // fichiers sont gardés (herbe = palier 1, pierre = 2, nuages = 3, galaxie = 4). Les bandes
        // du fond prennent la couleur de caméra (cameraColor) de chaque palier.
        static readonly string[] DecorNames = { "herbe", "pierre", "nuages", "galaxie" };
        static readonly Color[] DecorColors =
        {
            Hex("#7DBDEC"), Hex("#C98A96"), Hex("#3E3668"), Hex("#1F1730")
        };

        readonly VisualElement page;
        readonly GameManager game;
        ScrollView scroll;
        VisualElement content, fillBar, recordPill, youMark, youGlow, flag, goButton, infoVeil;
        Label infoTitleLabel;
        VisualElement infoLines;
        readonly List<(VisualElement card, VisualElement rays, Button button, Label number)> claimables = new List<(VisualElement, VisualElement, Button, Label)>();
        readonly List<(VisualElement e, Vector2 from, Vector2 to, float start)> flyers = new List<(VisualElement, Vector2, Vector2, float)>();
        readonly Dictionary<int, float> floorCenterY = new Dictionary<int, float>();
        float youCenterY, fillTop, fillBottom;
        float time, openTime;
        bool scrollPending, refreshPending;
        float claimTime = -10f;
        readonly List<Action> claimActions = new List<Action>();
        float scrollFrom = -1f, scrollTo, scrollStart;
        StarField stars;

        /// L'étage marqué « toi » (pour les tests).
        public int YouFloor { get; private set; }
        public float ScrollY => scroll != null ? scroll.scrollOffset.y : 0f;
        /// Tous les textes de la page (pour les tests).
        public string Text => string.Join(" | ", page.Query<Label>().ToList().ConvertAll(l => l.text));
        /// Le texte de la fenêtre « i » ouverte ("" si fermée).
        public string InfoText => infoVeil != null && infoVeil.parent != null
            ? infoTitleLabel.text + " | " + string.Join(" | ", infoLines.Query<Label>().ToList().ConvertAll(l => l.text)) : "";

        public TrophyRoadPage(VisualElement page, GameManager game)
        {
            this.page = page;
            this.game = game;
        }

        public void Dispose() { }

        public void Open()
        {
            Refresh();
            scrollPending = true;
            openTime = time;
        }

        /// L'étage où se trouve le joueur : pendant une partie, l'étage en cours ; après une
        /// partie (Perdu, Encaissé), l'étage atteint à la dernière partie ; sinon le record.
        int CurrentFloor()
        {
            bool inRun = game.State == GameState.Choosing || game.State == GameState.Revealing || game.State == GameState.Climbing;
            if (inRun) return Mathf.Max(1, game.Floor);
            if (game.State == GameState.Lost || game.State == GameState.CashedOut) return Mathf.Max(1, game.LastRunFloor > 0 ? game.LastRunFloor : game.Floor);
            return Mathf.Max(1, game.Record);
        }

        /// Le décor d'un étage : celui du palier d'altitude du JEU (même fonction, rien en dur).
        static int DecorOf(int floor) => Mathf.Clamp(GameManager.TierForFloor(floor), 1, 4) - 1;

        // =====================================================================
        // Construction
        // =====================================================================

        public void Refresh()
        {
            // Pendant la petite fête de « Récupérer » (1 s), on ne redessine pas : on le fera après.
            if (time - claimTime < 1f) { refreshPending = true; return; }
            refreshPending = false;
            page.Clear();
            claimActions.Clear();
            claimables.Clear();
            flyers.Clear();
            floorCenterY.Clear();
            int record = game.Record;
            int you = CurrentFloor();
            YouFloor = you;
            int top = TrophyRoad.TopFloorShown(Mathf.Max(record, you));

            // 1. Les positions verticales : de HAUT en BAS (l'étage le plus haut d'abord).
            var tops = new Dictionary<int, float>();
            float y = TopPadding;
            for (int f = top; f >= 1; f--)
            {
                var shape = ShapeOf(f);
                tops[f] = y;
                floorCenterY[f] = y + shape.TopCenterY;
                if (f > 1)
                {
                    var below = ShapeOf(f - 1);
                    y += shape.H + Gap - (shape.MarginBottom + below.MarginTop);
                }
                else y += shape.H;
            }
            float height = y + BottomPadding;

            // 2. La zone de route et son défilement.
            var zone = new VisualElement();
            zone.AddToClassList("asc-zone");
            scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.AddToClassList("shop-scroll");
            scroll.AddToClassList("asc-scroll");
            scroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            scroll.verticalScrollerVisibility = ScrollerVisibility.AlwaysVisible;
            scroll.mouseWheelScrollSize = 80f;
            scroll.touchScrollBehavior = ScrollView.TouchScrollBehavior.Clamped;
            scroll.verticalScroller.valueChanged += _ => UpdateGoButton();
            zone.Add(scroll);
            content = new VisualElement { pickingMode = PickingMode.Ignore };
            content.AddToClassList("asc-contenu");
            content.style.height = height;
            scroll.Add(content);

            // 3. Le fond : bandes de couleur, losanges, ombre du couloir gauche, étoiles.
            var bands = Layer("asc-fond");
            var boundaries = new List<(float y, Color above, Color below)>();
            for (int f = top; f > 1; f--)
                if (DecorOf(f) != DecorOf(f - 1))
                    boundaries.Add(((floorCenterY[f] + floorCenterY[f - 1]) * 0.5f, DecorColors[DecorOf(f)], DecorColors[DecorOf(f - 1)]));
            Color topColor = DecorColors[DecorOf(top)], bottomColor = DecorColors[0];
            bands.generateVisualContent += ctx => DrawBands(ctx, height, topColor, bottomColor, boundaries);
            content.Add(bands);
            content.Add(Layer("asc-losanges"));
            var shade = Layer("asc-ombre-gauche");
            shade.generateVisualContent += DrawLeftShade;
            content.Add(shade);
            // Les étoiles : seulement sur le palier « Dans les étoiles » (le plus bas de ses étages).
            int firstGalaxy = 0;
            for (int f = 2; f <= top; f++) if (DecorOf(f) == 3 && DecorOf(f - 1) != 3) { firstGalaxy = f; break; }
            if (DecorOf(top) == 3)
            {
                float galaxyBottom = firstGalaxy > 1 ? (floorCenterY[firstGalaxy] + floorCenterY[firstGalaxy - 1]) * 0.5f + FadeHeight * 0.5f : height;
                stars = new StarField(ContentWidth, galaxyBottom);
                content.Add(stars.Element);
            }
            else stars = null;

            // 4. La barre de progression (de l'étage 1 au record).
            float barBottom = floorCenterY[1];
            var track = Box("asc-barre");
            track.style.top = 0f;
            track.style.height = barBottom;
            content.Add(track);
            fillBottom = barBottom;
            fillTop = record >= 1 && floorCenterY.ContainsKey(record) ? floorCenterY[record] : barBottom;
            fillBar = Box("asc-barre__plein");
            fillBar.Add(Box("asc-barre__reflet"));
            content.Add(fillBar);
            SetFill(0f);

            // 5. Les numéros, les dalles, puis le calque de ce qui est posé dessus.
            var numbers = Layer("asc-calque");
            var tiles = Layer("asc-calque");
            var overlay = Layer("asc-calque");
            var infos = Layer("asc-calque");
            content.Add(numbers);
            content.Add(tiles);
            content.Add(overlay);
            content.Add(infos);
            youMark = null;
            for (int f = top; f >= 1; f--)
                BuildFloor(f, tops[f], record, you, numbers, tiles, overlay, infos);

            // Le repère du record (pilule or + drapeau + numéro), au bout du remplissage.
            if (record >= 1 && floorCenterY.ContainsKey(record))
            {
                recordPill = Box("asc-record");
                recordPill.style.top = floorCenterY[record] - 26f;
                recordPill.Add(Box("asc-record__pointe"));
                recordPill.Add(Box("asc-record__drapeau"));
                recordPill.Add(ShopUi.NewLabel(record.ToString(), "asc-record__texte"));
                content.Add(recordPill);
            }
            else recordPill = null;

            // 6. « Ma position » : bulle or avec l'icône du skin, si on s'éloigne de « toi ».
            goButton = new Button(() => { scrollPending = true; scrollFrom = -1f; });
            foreach (var c in new[] { "btn", "bulle", "bulle-petit", "bulle-petit--or", "asc-ma-position" }) goButton.AddToClassList(c);
            var skinIcon = Box("asc-ma-position__icone");
            var skin = Shop.GetEquippedSkin();
            if (skin != null && skin.Icon != null) skinIcon.style.backgroundImage = new StyleBackground(skin.Icon);
            goButton.Add(skinIcon);
            goButton.style.display = DisplayStyle.None;
            zone.Add(goButton);

            page.Add(zone);
            ButtonStyler.Apply(page);
        }

        static TileShape ShapeOf(int floor) => TrophyRoad.IsMilestone(floor) ? Milestone : Normal;

        void BuildFloor(int f, float tileTop, int record, int you, VisualElement numbers, VisualElement tiles, VisualElement overlay, VisualElement infos)
        {
            bool milestone = TrophyRoad.IsMilestone(f);
            var shape = milestone ? Milestone : Normal;
            float left = TileCenterX - shape.W * 0.5f;
            bool unexplored = f > record;
            float centerY = floorCenterY[f];

            // La dalle (image) : variante A aux étages impairs, B aux pairs (§16.3.1).
            string decor = DecorNames[DecorOf(f)];
            string variant = milestone ? "palier" : (f % 2 == 1 ? "a" : "b");
            var tile = Box($"asc-dalle asc-dalle--{decor}-{variant}");
            if (unexplored) tile.AddToClassList("asc-dalle--sombre");
            tile.style.left = left; tile.style.top = tileTop; tile.style.width = shape.W; tile.style.height = shape.H;
            tiles.Add(tile);

            Vector2 P(Vector2 p) => new Vector2(left + p.x, tileTop + p.y);

            // Le numéro d'étage (caché à l'étage du record : la pilule le montre déjà).
            var number = ShopUi.NewLabel(f.ToString(), "asc-numero");
            if (milestone) number.AddToClassList("asc-numero--palier");
            if (milestone && !unexplored) number.AddToClassList("asc-numero--or");
            if (unexplored) number.AddToClassList("asc-numero--inexplore");
            number.style.top = centerY - (milestone ? 32f : 24f);
            if (f == record) number.style.visibility = Visibility.Hidden;
            numbers.Add(number);

            // Le bouton « i » (détail chiffré de l'étage).
            var info = new Button(() => OpenInfo(f));
            foreach (var c in new[] { "btn", "bulle", "bulle-petit", "bulle-petit--ardoise", "asc-info" }) info.AddToClassList(c);
            info.Add(ShopUi.NewLabel("i", "asc-info__texte"));
            info.style.top = centerY - 32f;
            infos.Add(info);

            if (milestone) BuildReward(f, P(shape.PedestalPoint), P(shape.ButtonPoint), number, overlay, unexplored);
            else
            {
                // Pièces : 1, 2 ou 3 selon le décor (le gain monte avec l'étage ; le chiffre exact est dans le « i »).
                int coins = Mathf.Min(3, DecorOf(f) + 1);
                var coinPoint = P(shape.LeftPoint);
                for (int i = 0; i < coins; i++)
                {
                    var coin = Box("asc-piece");
                    coin.style.left = coinPoint.x - 32f;
                    coin.style.top = coinPoint.y - 64f - 18f * i;
                    if (unexplored) coin.AddToClassList("asc-pose--sombre");
                    overlay.Add(coin);
                }
                // Diamants : le vrai nombre d'un trésor en diamants à cet étage (spec v6 §8.2).
                int diamonds = Mathf.Clamp(EconomyCatalog.Instance.DiamondsForFloor(f), 1, 5);
                var dPoint = P(shape.RightPoint);
                float width = diamonds * 56f - (diamonds - 1) * 12f;
                for (int i = 0; i < diamonds; i++)
                {
                    var d = Box("asc-diamant");
                    d.style.left = dPoint.x - width * 0.5f + i * 44f;
                    d.style.top = dPoint.y - 56f;
                    if (unexplored) d.AddToClassList("asc-pose--sombre");
                    overlay.Add(d);
                }
                if (Rebirth.DiamondMultiplier > 1)
                {
                    var x2 = ShopUi.NewLabel($"×{Rebirth.DiamondMultiplier}", "asc-x2");
                    x2.style.left = dPoint.x + width * 0.5f - 18f;
                    x2.style.top = dPoint.y - 74f;
                    overlay.Add(x2);
                }
            }

            // Le drapeau du record : le pied du mât (37 % / 84 % de l'image) sur le point « drapeau ».
            if (f == record)
            {
                var fp = P(shape.FlagPoint);
                flag = Box("asc-drapeau");
                flag.style.left = fp.x - 0.37f * 96f;
                flag.style.top = fp.y - 0.84f * 96f;
                overlay.Add(flag);
            }
            // « Toi » : l'icône du skin équipé, pieds sur le point « cheval », ellipse or qui pulse.
            if (f == you)
            {
                var hp = P(shape.HorsePoint);
                youGlow = Box("asc-toi__lueur");
                youGlow.style.left = hp.x - 55f;
                youGlow.style.top = hp.y - 17f;
                overlay.Add(youGlow);
                youMark = Box("asc-toi");
                var skin = Shop.GetEquippedSkin();
                if (skin != null && skin.Icon != null) youMark.style.backgroundImage = new StyleBackground(skin.Icon);
                youMark.style.left = hp.x - 50f;
                youMark.style.top = hp.y - 100f;
                overlay.Add(youMark);
                youCenterY = hp.y - 50f;
            }
        }

        /// La carte de la récompense d'un palier, dans l'un de ses 3 états (§16.2.5).
        void BuildReward(int f, Vector2 pedestal, Vector2 buttonPoint, Label number, VisualElement overlay, bool unexplored)
        {
            int reward = TrophyRoad.RewardAt(f);
            bool received = TrophyRoad.IsReceived(f);
            bool claimable = !unexplored && !received;   // gagné mais pas encore pris
            float cardLeft = pedestal.x - 100f, cardTop = pedestal.y - 230f;

            RaysElement rays = null;
            if (claimable)
            {
                rays = new RaysElement();
                rays.AddToClassList("asc-carte__rayons");
                rays.Configure(12, new Color(1f, 1f, 1f, 0.55f));
                rays.style.left = pedestal.x - 150f;
                rays.style.top = cardTop + 115f - 150f;
                overlay.Add(rays);
            }

            var card = Box("asc-carte");
            if (unexplored) card.AddToClassList("asc-carte--verrou");
            card.style.left = cardLeft;
            card.style.top = cardTop;
            card.Add(Box("asc-carte__lueur"));
            card.Add(Box("asc-carte__diamant"));
            card.Add(ShopUi.NewLabel($"+{reward}", "asc-carte__nombre"));
            if (unexplored) card.Add(BoutiquePage.Lock());
            if (received) card.Add(Check());
            overlay.Add(card);

            if (claimable)
            {
                var button = new Button();
                foreach (var c in new[] { "btn", "bulle", "bulle-rect", "bulle-rect--vert", "asc-recuperer" }) button.AddToClassList(c);
                button.Add(ShopUi.NewLabel("Récupérer", "bulle-rect__texte"));
                button.Add(Box("btn-eclat"));
                button.style.left = buttonPoint.x - 130f;
                button.style.top = buttonPoint.y - 52f;
                Action claim = () => Claim(f, card, button);
                button.clicked += claim;
                claimActions.Add(claim);
                overlay.Add(button);
                claimables.Add((card, rays, button, number));
            }
        }

        static VisualElement Check()
        {
            var check = Box("asc-carte__coche");
            check.Add(Box("casier-coche__court"));
            check.Add(Box("casier-coche__long"));
            return check;
        }

        /// « Récupérer » : TrophyRoad donne le palier (même fonction que le jeu), puis petite fête :
        /// la carte saute, 6 diamants s'envolent vers le haut à droite, la coche arrive, le bouton
        /// s'efface. La page est redessinée par PanelsController (Diamonds.Changed).
        void Claim(int floor, VisualElement card, Button button)
        {
            if (TrophyRoad.IsReceived(floor)) return;
            claimTime = time;
            TrophyRoad.GrantUpTo(floor, out _);
            if (SoundManager.Instance != null) SoundManager.Instance.Play(Sfx.Vente);
            button.SetEnabled(false);
            button.style.opacity = 0f;
            card.style.scale = new Scale(new Vector3(1.25f, 1.25f, 1f));
            card.Add(Check());
            var from = new Vector2(card.resolvedStyle.left + 100f, card.resolvedStyle.top + 90f);
            var to = new Vector2(ContentWidth - 40f, scroll.scrollOffset.y + 10f);
            for (int i = 0; i < 6; i++)
            {
                var d = Box("asc-envol");
                d.style.left = from.x; d.style.top = from.y;
                content.Add(d);
                flyers.Add((d, from + new Vector2((i - 2.5f) * 18f, 0f), to, time + i * 0.05f));
            }
        }

        // =====================================================================
        // La fenêtre « i » (détail de l'étage)
        // =====================================================================

        void OpenInfo(int floor)
        {
            CloseInfo();
            var row = TrophyRoad.RowFor(floor);
            infoVeil = Box("asc-info-voile");
            infoVeil.RegisterCallback<PointerDownEvent>(e => { if (e.target == infoVeil) CloseInfo(); });
            var box = Box("asc-info-boite");
            var thumb = Box($"asc-info-vignette asc-dalle--{DecorNames[DecorOf(floor)]}-{(TrophyRoad.IsMilestone(floor) ? "palier" : (floor % 2 == 1 ? "a" : "b"))}");
            box.Add(thumb);
            infoTitleLabel = ShopUi.NewLabel($"Étage {floor}", "asc-info-titre");
            box.Add(infoTitleLabel);
            infoLines = Box("asc-info-lignes", PickingMode.Ignore);

            string gain = row.GainMin == row.GainMax ? $"+{ShopUi.Thousands(row.GainMin)}" : $"+{ShopUi.Thousands(row.GainMin)} à +{ShopUi.Thousands(row.GainMax)}";
            infoLines.Add(InfoLine("asc-info-icone--piece", $"Trésor : <color=#3B7DD8>{gain}</color> pièces"));
            infoLines.Add(InfoLine("asc-info-icone--chemin", floor > 1
                ? $"Y arriver : <color=#3B7DD8>{ReachText(row.ReachChance)}</color>"
                : "Y arriver : <color=#3B7DD8>tu y es dès le départ</color>"));
            infoLines.Add(InfoLine("asc-info-icone--diamant", $"Trésor en diamants : <color=#3B7DD8>{row.DiamondsPerTreasure}</color> {ShopUi.Diamant}"));
            if (floor > 1) infoLines.Add(InfoSmall($"Cagnotte en arrivant : ≈ {ShopUi.Thousands(row.PotAverage)} pièces"));
            if (row.MilestoneReward > 0)
                infoLines.Add(InfoSmall(TrophyRoad.IsReceived(floor) ? $"Palier : +{row.MilestoneReward} {ShopUi.Diamant} (reçu)" : $"Palier : +{row.MilestoneReward} {ShopUi.Diamant} la première fois"));
            // Ce qui ne dépend PAS de l'étage (§8.2) : l'ancien bandeau « Tes coffres en ce moment ».
            int chests = Upgrades.Level(Upgrades_.Chests), traps = Upgrades.Level(Upgrades_.Traps);
            infoLines.Add(InfoSmall($"Tes coffres (à tous les étages) : {TrophyRoad.ChestCountText(traps)} · 1 piège · risque {ShopUi.Number(Upgrades.AverageRiskPercent(traps))} %"));
            var w = Upgrades.ChestWeights(chests);
            var rarities = new List<string>();
            for (int i = 0; i < w.Length && i < Upgrades.RarityCount; i++)
                if (w[i] > 0) rarities.Add($"{Upgrades.Rarity(i).shortName} {w[i]} %");
            infoLines.Add(InfoSmall("Rareté des coffres : " + string.Join(" · ", rarities)));
            box.Add(infoLines);

            var ok = new Button(CloseInfo) { text = "OK" };
            ok.AddToClassList("btn"); ok.AddToClassList("btn--vert"); ok.AddToClassList("asc-info-ok");
            box.Add(ok);
            infoVeil.Add(box);
            page.Add(infoVeil);
            ButtonStyler.Apply(infoVeil);
        }

        static VisualElement InfoLine(string iconClass, string text)
        {
            var line = Box("asc-info-ligne", PickingMode.Ignore);
            line.Add(Box("asc-info-icone " + iconClass, PickingMode.Ignore));
            var label = ShopUi.NewLabel(text, "asc-info-texte");
            label.enableRichText = true;
            line.Add(label);
            return line;
        }

        static Label InfoSmall(string text) => ShopUi.NewLabel(text, "asc-info-petit");

        void CloseInfo()
        {
            if (infoVeil != null) infoVeil.RemoveFromHierarchy();
            infoVeil = null;
        }

        /// Échap ferme d'abord la fenêtre « i ».
        public bool HandleEscape()
        {
            if (infoVeil == null) return false;
            CloseInfo();
            return true;
        }

        /// « 8,8 % », ou « moins de 0,1 % » pour les très petites chances.
        static string ReachText(double chance)
        {
            double pct = chance * 100.0;
            return pct < 0.1 ? "moins de 0,1 %" : ShopUi.Percent(Math.Round(pct, 1));
        }

        // =====================================================================
        // Les animations (appelé à chaque image quand L'Ascension est ouverte)
        // =====================================================================

        public void Tick(float dt)
        {
            time += dt;
            float sinceOpen = time - openTime;
            if (refreshPending && time - claimTime >= 1f) Refresh();

            // Ouverture : le remplissage monte jusqu'au record en 0,6 s (ease-out), la pilule rebondit.
            SetFill(Ease.Out(Mathf.Clamp01(sinceOpen / 0.6f)));
            if (recordPill != null)
            {
                float u = Mathf.Clamp01((sinceOpen - 0.45f) / 0.3f);
                float s = u <= 0f ? 0f : 1f + 0.2f * Mathf.Sin(u * Mathf.PI);
                recordPill.style.scale = new Scale(new Vector3(s, s, 1f));
            }
            // Le drapeau se balance de ±4° en 2 s ; l'ellipse de « toi » pulse en 1,6 s.
            if (flag != null) flag.style.rotate = new Rotate(4f * Mathf.Sin(time * Mathf.PI));
            if (youGlow != null)
            {
                float k = Mathf.Repeat(time, 1.6f) / 1.6f;
                youGlow.style.opacity = 0.6f * (1f - k);
                youGlow.style.scale = new Scale(new Vector3(1f + 0.4f * k, 1f + 0.4f * k, 1f));
            }
            // Les paliers à récupérer : rayons (un tour en 12 s), carte qui sautille, bouton qui pulse.
            foreach (var (card, rays, button, number) in claimables)
            {
                if (button.enabledSelf)
                {
                    card.style.translate = new Translate(0f, -8f * Mathf.Abs(Mathf.Sin(time * Mathf.PI / 1.2f)));
                    float p = 1f + 0.06f * Mathf.Sin(time * Mathf.PI * 2f);
                    button.style.scale = new Scale(new Vector3(p, p, 1f));
                    number.style.opacity = 0.75f + 0.25f * Mathf.Cos(time * Mathf.PI);
                    var shine = button.Q<VisualElement>(className: "btn-eclat");
                    if (shine != null)
                    {
                        float phase = Mathf.Repeat(time, 2.5f);
                        shine.style.opacity = phase < 0.5f ? 1f : 0f;
                        shine.style.left = Mathf.Lerp(-60f, 280f, phase / 0.5f);
                    }
                }
                else
                {
                    // Après le clic : la carte revient à sa taille, le bouton reste effacé.
                    card.style.translate = new Translate(0f, 0f);
                    float s = Mathf.MoveTowards(card.resolvedStyle.scale.value.x, 1f, dt * 2f);
                    card.style.scale = new Scale(new Vector3(s, s, 1f));
                }
                if (rays != null) rays.style.rotate = new Rotate(time * 30f);
            }
            // Les diamants qui s'envolent (0,7 s).
            for (int i = flyers.Count - 1; i >= 0; i--)
            {
                var (e, from, to, start) = flyers[i];
                float u = (time - start) / 0.7f;
                if (u < 0f) continue;
                if (u >= 1f) { e.RemoveFromHierarchy(); flyers.RemoveAt(i); continue; }
                var pos = Vector2.Lerp(from, to, Ease.Out(u)) + Vector2.up * (-80f * Mathf.Sin(u * Mathf.PI));
                e.style.left = pos.x; e.style.top = pos.y;
                e.style.opacity = 1f - u * 0.5f;
            }
            stars?.Tick(time);

            // Défilement d'ouverture : la dalle « toi » au tiers bas, glissé de 200 px en 0,5 s.
            if (scrollPending && scroll != null && youMark != null)
            {
                float view = scroll.contentViewport.layout.height, total = content.layout.height;
                if (!float.IsNaN(view) && view > 0f && total > 0f)
                {
                    scrollPending = false;
                    float max = Mathf.Max(0f, total - view);
                    scrollTo = Mathf.Clamp(youCenterY - view * (2f / 3f), 0f, max);
                    scrollFrom = Mathf.Clamp(scrollTo - 200f, 0f, max);
                    scrollStart = time;
                }
            }
            if (scrollFrom >= 0f && scroll != null)
            {
                float u = Mathf.Clamp01((time - scrollStart) / 0.5f);
                scroll.scrollOffset = new Vector2(0f, Mathf.Lerp(scrollFrom, scrollTo, Ease.Out(u)));
                if (u >= 1f) scrollFrom = -1f;
                UpdateGoButton();
            }
        }

        void SetFill(float k)
        {
            if (fillBar == null) return;
            float top = Mathf.Lerp(fillBottom, fillTop, k);
            fillBar.style.top = top;
            fillBar.style.height = Mathf.Max(0f, fillBottom - top);
        }

        /// « Ma position » apparaît quand la dalle « toi » sort de la vue.
        void UpdateGoButton()
        {
            if (goButton == null || youMark == null || scroll == null) return;
            float y = youCenterY - scroll.scrollOffset.y, view = scroll.contentViewport.layout.height;
            bool far = !float.IsNaN(view) && view > 0f && (y < -60f || y > view + 60f);
            goButton.style.display = far ? DisplayStyle.Flex : DisplayStyle.None;
        }

        // =====================================================================
        // Dessins faits par le code
        // =====================================================================

        /// Les bandes de couleur du fond : une couleur par décor, fondu de 320 px au passage.
        static void DrawBands(MeshGenerationContext ctx, float height, Color topColor, Color bottomColor, List<(float y, Color above, Color below)> boundaries)
        {
            var stops = new List<(float y, Color c)> { (0f, topColor) };
            foreach (var b in boundaries)
            {
                stops.Add((b.y - FadeHeight * 0.5f, b.above));
                stops.Add((b.y + FadeHeight * 0.5f, b.below));
            }
            stops.Add((height, bottomColor));
            var mesh = ctx.Allocate(4 * (stops.Count - 1), 6 * (stops.Count - 1));
            for (int i = 0; i < stops.Count - 1; i++)
            {
                var (y0, c0) = stops[i];
                var (y1, c1) = stops[i + 1];
                mesh.SetNextVertex(new Vertex { position = new Vector3(0f, y0, Vertex.nearZ), tint = c0 });
                mesh.SetNextVertex(new Vertex { position = new Vector3(ContentWidth, y0, Vertex.nearZ), tint = c0 });
                mesh.SetNextVertex(new Vertex { position = new Vector3(ContentWidth, y1, Vertex.nearZ), tint = c1 });
                mesh.SetNextVertex(new Vertex { position = new Vector3(0f, y1, Vertex.nearZ), tint = c1 });
                ushort b0 = (ushort)(i * 4);
                mesh.SetNextIndex(b0); mesh.SetNextIndex((ushort)(b0 + 1)); mesh.SetNextIndex((ushort)(b0 + 2));
                mesh.SetNextIndex(b0); mesh.SetNextIndex((ushort)(b0 + 2)); mesh.SetNextIndex((ushort)(b0 + 3));
            }
        }

        /// L'ombre du couloir gauche : encre à 35 % (x = 0) → transparent (x = 170).
        static void DrawLeftShade(MeshGenerationContext ctx)
        {
            var r = ctx.visualElement.contentRect;
            var dark = new Color(0.067f, 0.051f, 0.122f, 0.35f);
            var clear = new Color(0.067f, 0.051f, 0.122f, 0f);
            var mesh = ctx.Allocate(4, 6);
            mesh.SetNextVertex(new Vertex { position = new Vector3(0f, 0f, Vertex.nearZ), tint = dark });
            mesh.SetNextVertex(new Vertex { position = new Vector3(170f, 0f, Vertex.nearZ), tint = clear });
            mesh.SetNextVertex(new Vertex { position = new Vector3(170f, r.height, Vertex.nearZ), tint = clear });
            mesh.SetNextVertex(new Vertex { position = new Vector3(0f, r.height, Vertex.nearZ), tint = dark });
            mesh.SetNextIndex(0); mesh.SetNextIndex(1); mesh.SetNextIndex(2);
            mesh.SetNextIndex(0); mesh.SetNextIndex(2); mesh.SetNextIndex(3);
        }

        /// Les étoiles de la galaxie : petits points crème qui scintillent lentement.
        class StarField
        {
            public readonly VisualElement Element;
            readonly Vector2[] pos;
            readonly float[] size, alpha, period, phase;
            float now;

            public StarField(float width, float height)
            {
                Element = new VisualElement { pickingMode = PickingMode.Ignore };
                Element.AddToClassList("asc-etoiles");
                Element.style.height = height;
                int count = Mathf.Clamp(Mathf.RoundToInt(width * height / 9000f), 0, 800);
                var random = new System.Random(30); // toujours le même ciel
                pos = new Vector2[count]; size = new float[count]; alpha = new float[count]; period = new float[count]; phase = new float[count];
                for (int i = 0; i < count; i++)
                {
                    pos[i] = new Vector2((float)random.NextDouble() * width, (float)random.NextDouble() * height);
                    size[i] = 1f + (float)random.NextDouble();           // rayon 1 à 2 px (Ø 2 à 4)
                    alpha[i] = 0.4f + 0.5f * (float)random.NextDouble();
                    period[i] = 1.5f + 1.5f * (float)random.NextDouble();
                    phase[i] = (float)random.NextDouble() * 6.28f;
                }
                Element.generateVisualContent += Draw;
            }

            public void Tick(float t) { now = t; Element.MarkDirtyRepaint(); }

            void Draw(MeshGenerationContext ctx)
            {
                var p = ctx.painter2D;
                for (int i = 0; i < pos.Length; i++)
                {
                    float a = alpha[i] * (0.65f + 0.35f * Mathf.Sin(now * 6.28f / period[i] + phase[i]));
                    p.fillColor = new Color(1f, 0.945f, 0.839f, a); // #FFF1D6
                    p.BeginPath();
                    p.Arc(pos[i], size[i], 0f, 360f);
                    p.Fill();
                }
            }
        }

        // =====================================================================
        // Petits outils
        // =====================================================================

        static VisualElement Box(string classes, PickingMode picking = PickingMode.Ignore)
        {
            var e = new VisualElement { pickingMode = picking };
            foreach (var c in classes.Split(' ')) e.AddToClassList(c);
            return e;
        }

        static VisualElement Layer(string classes) => Box(classes + " asc-plein");

        static Color Hex(string hex) => ColorUtility.TryParseHtmlString(hex, out var c) ? c : Color.magenta;

        // ---------- Outils de test ----------
        /// OUTIL DE TEST : fait défiler la route.
        public void DebugScrollTo(float y) { if (scroll != null) { scrollFrom = -1f; scroll.scrollOffset = new Vector2(0f, y); } }
        /// OUTIL DE TEST : fait défiler pour montrer un étage au milieu de la vue.
        public void DebugShowFloor(int floor)
        {
            if (scroll == null || !floorCenterY.ContainsKey(floor)) return;
            scrollFrom = -1f; scrollPending = false;
            float view = scroll.contentViewport.layout.height;
            scroll.scrollOffset = new Vector2(0f, Mathf.Max(0f, floorCenterY[floor] - view * 0.5f));
        }
        /// OUTILS DE TEST : la fenêtre « i » d'un étage, et « Récupérer » sur un palier.
        public void DebugInfo(int floor) => OpenInfo(floor);
        public int ClaimableCount => claimables.FindAll(c => c.button.enabledSelf).Count;
        public void DebugClaimFirst()
        {
            for (int i = 0; i < claimables.Count; i++) if (claimables[i].button.enabledSelf) { claimActions[i](); return; }
        }
    }
}
