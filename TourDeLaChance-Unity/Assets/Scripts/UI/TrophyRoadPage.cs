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
    /// v7 (art bible §17.2) : L'Ascension devient VIVANTE ET FESTIVE, sans rien changer aux règles :
    ///   - ciel en dégradé par palier (bleu de jour, coucher de soleil corail, lavande, nuit), avec
    ///     soleil / lune qui respirent, nuages qui dérivent, étoiles qui scintillent, étoile filante ;
    ///   - l'étage du joueur sous un PROJECTEUR (faisceau, rayons, lueur, anneaux, cheval qui
    ///     sautille, étincelles, médaillon bleu) : on le repère en une demi-seconde ;
    ///   - paliers en fête (bande de lumière, guirlande, rayons dorés, médaillon or, ruban) ;
    ///   - barre de progression façon jauge (rayures qui montent, bout lumineux, crans, jalons,
    ///     tirets jusqu'au prochain palier, repère « toi ») ;
    ///   - ouverture animée « on monte » (1,5 s, interrompue au premier clic ou coup de molette) ;
    ///   - « Récupérer » bien visible (socle sombre, rayons dorés en deux couches, étincelles) ;
    ///   - « Ma position » sort de la route (bande de titre) et les couloirs sont recalculés pour
    ///     que rien ne défile de côté (les « i » ne sont plus coupés).
    /// En Graphismes « Faible », rien ne dérive et l'ouverture est raccourcie (§17.2.13).
    ///
    /// Comment c'est construit : toute la colonne est UN grand élément de hauteur calculée, et
    /// chaque chose y est placée en position absolue, avec les coordonnées en pixels du §16.3.6.
    /// Les calques, de l'arrière vers l'avant (§17.2.1) : 1 ciel, 2 ciel vivant (astres, étoiles,
    /// nuages lointains), 3 lumières, 4 nuages proches, 5 dalles, 6 posés (sur les dalles),
    /// 7 boutons et confettis. La barre et les numéros sont dans leur couloir, au-dessus de tout.
    /// </summary>
    public class TrophyRoadPage : IDisposable
    {
        // ---- Mesures (pixels de référence, art bible §17.2.12) ----
        // Largeur utile : 1056 − 2 × 5 (contour) − 24 (barre de défilement) = 1022 px. Le contenu ne
        // dépasse JAMAIS cette largeur : il n'y a donc plus de défilement de côté.
        const float ContentWidth = 1022f;
        const float TileCenterX = 560f;             // les dalles sont centrées sur x = 560
        const float NumberCenterX = 118f, InfoCenterX = 962f;
        const float BarCenterX = 40f, BarWidth = 40f; // couloir de la barre : x 20 → 60
        const float TopPadding = 40f, BottomPadding = 40f;
        const float Gap = 36f;                      // vide entre le bas d'une dalle et l'arrière de la suivante
        const float FadeHeight = 320f;              // fondu entre deux ciels

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
        // fichiers sont gardés (herbe = palier 1, pierre = 2, nuages = 3, galaxie = 4).
        static readonly string[] DecorNames = { "herbe", "pierre", "nuages", "galaxie" };
        // v7 §17.2.2 : le ciel de chaque palier, du HAUT du palier vers son BAS (comme un vrai ciel).
        static readonly Color[] SkyTop = { Hex("#5FB4F5"), Hex("#F0809A"), Hex("#6C5BD6"), Hex("#120D2B") };
        static readonly Color[] SkyBottom = { Hex("#A8DEFF"), Hex("#FFC29E"), Hex("#A99BFF"), Hex("#2E2366") };
        // v7 §17.2.3 : la teinte des nuages selon le palier (la nuit, très discrets).
        static readonly Color[] CloudTint = { Hex("#FFFFFF"), Hex("#FFE6DA"), Hex("#E4DAF0"), Hex("#5A4E8C") };

        readonly VisualElement page;
        readonly GameManager game;
        ScrollView scroll;
        VisualElement zone, content, fillBar, recordPill, youMark, flag, goButton, goChevron, infoVeil;
        Label infoTitleLabel;
        VisualElement infoLines;
        // Un palier « gagné, pas encore pris » : sa carte, son bouton, son numéro et sa fête.
        class Claimable
        {
            public VisualElement card, ring, glow, arrow, milestoneDot;
            public RaysElement bigRays, smallRays;
            public Button button;
            public VisualElement number;
            public readonly List<VisualElement> sparkles = new List<VisualElement>();
            public Vector2 center;   // le centre de la carte dans le contenu
            public bool confettiDone;
        }
        readonly List<Claimable> claimables = new List<Claimable>();
        readonly List<(VisualElement e, Vector2 from, Vector2 to, float start)> flyers = new List<(VisualElement, Vector2, Vector2, float)>();
        readonly Dictionary<int, float> floorCenterY = new Dictionary<int, float>();
        float youCenterY, fillTop, fillBottom;
        float time, openTime;
        bool refreshPending;
        float claimTime = -10f;
        readonly List<Action> claimActions = new List<Action>();
        StarField stars;

        // ---- v7 : ce qui bouge ----
        VisualElement fxLayer;                                       // calque 7 : confettis, étincelles
        readonly AscensionFx.Confetti confetti = new AscensionFx.Confetti();
        readonly List<(VisualElement e, float baseOpacity, float period)> breathers = new List<(VisualElement, float, float)>(); // astres qui respirent
        class Drifter { public VisualElement e; public float x, y, speed, width, bobPeriod, bobPhase; }
        readonly List<Drifter> clouds = new List<Drifter>();
        readonly List<(RaysElement rays, float turnSeconds)> spinners = new List<(RaysElement, float)>(); // rayons qui tournent
        readonly List<(VisualElement e, float swingPhase)> garlands = new List<(VisualElement, float)>();
        // L'étage du joueur sous le projecteur.
        VisualElement beam, youRays, youGround, youRing1, youRing2, youMedal;
        readonly List<(VisualElement e, Vector2 from, float delay)> youSparkles = new List<(VisualElement, Vector2, float)>();
        float youMarkTop;
        // La barre de progression.
        AscensionFx.Stripes stripes;
        VisualElement fillTip, youTrackMark;
        readonly List<(VisualElement dot, float y, bool pulse)> barMilestones = new List<(VisualElement, float, bool)>();
        // L'étoile filante (palier 4).
        VisualElement shootingStar;
        float nextShootingStar = 8f, shootingStart = -10f;
        float galaxyTop = -1f, galaxyBottom = -1f;
        // L'ouverture « on monte » (§17.2.8).
        bool openPending, opening, arrived;
        float openScrollFrom, openScrollTo, openDuration;
        float goFrom = -1f, goTo, goStart;             // « Ma position » : retour en 0,4 s
        readonly System.Random random = new System.Random();

        static bool Low => GraphicsLevel.Current == GraphicsLevel.Low;

        /// L'étage marqué « toi » (pour les tests).
        public int YouFloor { get; private set; }
        public float ScrollY => scroll != null ? scroll.scrollOffset.y : 0f;
        /// v7 : l'ouverture animée est-elle en cours ? (pour les tests)
        public bool Opening => opening || openPending;
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
            openPending = true;
            arrived = false;
            openTime = time;
            foreach (var c in claimables) c.confettiDone = false;
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

        static AltitudeDecor altitudeDecor;
        /// Le nom du palier d'altitude (« Dans les nuages ! »), lu dans le décor du jeu.
        static string TierName(int tier)
        {
            if (altitudeDecor == null) altitudeDecor = UnityEngine.Object.FindFirstObjectByType<AltitudeDecor>();
            return altitudeDecor != null ? altitudeDecor.TierName(tier) : "";
        }

        // =====================================================================
        // Construction
        // =====================================================================

        public void Refresh()
        {
            // Pendant la petite fête de « Récupérer » (1 s), on ne redessine pas : on le fera après.
            if (time - claimTime < 1f) { refreshPending = true; return; }
            refreshPending = false;
            float keepScroll = scroll != null ? scroll.scrollOffset.y : -1f;
            page.Clear();
            claimActions.Clear();
            claimables.Clear();
            flyers.Clear();
            floorCenterY.Clear();
            confetti.Clear();
            breathers.Clear(); clouds.Clear(); spinners.Clear(); garlands.Clear(); youSparkles.Clear(); barMilestones.Clear();
            beam = youRays = youGround = youRing1 = youRing2 = youMedal = youTrackMark = null;
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

            // 2. La zone de route et son défilement (VERTICAL SEULEMENT, §17.2.12).
            zone = new VisualElement();
            zone.AddToClassList("asc-zone");
            scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.AddToClassList("shop-scroll");
            scroll.AddToClassList("asc-scroll");
            scroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            scroll.verticalScrollerVisibility = ScrollerVisibility.AlwaysVisible;
            scroll.mouseWheelScrollSize = 80f;
            scroll.touchScrollBehavior = ScrollView.TouchScrollBehavior.Clamped;
            scroll.verticalScroller.valueChanged += _ => UpdateGoButton();
            // L'ouverture s'interrompt au premier clic, défilement ou coup de molette (§17.2.8).
            zone.RegisterCallback<PointerDownEvent>(_ => SkipOpening(), TrickleDown.TrickleDown);
            zone.RegisterCallback<WheelEvent>(_ => SkipOpening(), TrickleDown.TrickleDown);
            zone.Add(scroll);
            content = new VisualElement { pickingMode = PickingMode.Ignore };
            content.AddToClassList("asc-contenu");
            content.style.height = height;
            scroll.Add(content);

            // 3. Calque 1 : le ciel en dégradé par palier, les losanges, l'ombre du couloir gauche.
            var sky = Layer("asc-fond");
            var stops = SkyStops(top, height);
            sky.generateVisualContent += ctx => DrawSky(ctx, stops);
            content.Add(sky);
            content.Add(Layer("asc-losanges"));
            var shade = Layer("asc-ombre-gauche");
            shade.generateVisualContent += DrawLeftShade;
            content.Add(shade);

            // Calque 2 : ciel vivant (astres, étoiles, nuages lointains).
            var living = Layer("asc-calque");
            content.Add(living);
            AddCelestials(living, top);
            AddStars(living, top, height);
            // Calque 3 : lumières (projecteur du joueur, bandes et rayons des paliers).
            var lights = Layer("asc-calque");
            content.Add(lights);
            // Calque 4 : nuages proches. Les nuages sont tirés après les étages (ils évitent les numéros).
            var nearClouds = Layer("asc-calque");
            content.Add(nearClouds);

            // 4. La barre de progression (de l'étage 1 au record) : une jauge de jeu vidéo.
            BuildBar(top, record, you);

            // 5. Calques 5 à 7 : les dalles, ce qui est posé dessus, puis les boutons et les effets.
            var numbers = Layer("asc-calque");
            var tiles = Layer("asc-calque");
            var overlay = Layer("asc-calque");
            var infos = Layer("asc-calque");
            fxLayer = Layer("asc-calque");
            content.Add(tiles);
            content.Add(overlay);
            content.Add(infos);
            content.Add(numbers);   // les numéros restent dans leur couloir, au-dessus de tout
            content.Add(fxLayer);
            youMark = null;
            for (int f = top; f >= 1; f--)
                BuildFloor(f, tops[f], record, you, numbers, tiles, overlay, infos, lights);
            AddClouds(living, nearClouds, top, height);

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
            // Le repère « toi » sur la piste (après la pilule du record, pour passer devant).
            if (youTrackMark != null) content.Add(youTrackMark);

            // L'étoile filante (palier 4 seulement), cachée jusqu'à son passage.
            shootingStar = null;
            if (galaxyBottom > 0f && !Low)
            {
                shootingStar = new VisualElement { pickingMode = PickingMode.Ignore };
                shootingStar.AddToClassList("asc-filante");
                shootingStar.generateVisualContent += DrawShootingStar;
                shootingStar.style.display = DisplayStyle.None;
                fxLayer.Add(shootingStar);
            }

            page.Add(zone);

            // 6. « Ma position » : SORT de la route (§17.2.12), dans la bande de titre de la fenêtre.
            goButton = new Button(GoToYou);
            goButton.AddToClassList("btn");
            goButton.AddToClassList("asc-ma-position7");
            var skinIcon = Box("asc-ma-position7__icone");
            var skin = Shop.GetEquippedSkin();
            if (skin != null && skin.Icon != null) skinIcon.style.backgroundImage = new StyleBackground(skin.Icon);
            goButton.Add(skinIcon);
            goButton.Add(ShopUi.NewLabel("Ma position", "asc-ma-position7__texte"));
            goChevron = Box("asc-ma-position7__chevron");
            goChevron.Add(Box("asc-chevron__barre asc-chevron__barre--a"));
            goChevron.Add(Box("asc-chevron__barre asc-chevron__barre--b"));
            goButton.Add(goChevron);
            goButton.style.display = DisplayStyle.None;
            page.Add(goButton);

            ButtonStyler.Apply(page);
            // Un redessin pendant que la fenêtre est ouverte garde la position de lecture.
            if (keepScroll >= 0f && !openPending) scroll.schedule.Execute(() => scroll.scrollOffset = new Vector2(0f, keepScroll));
        }

        static TileShape ShapeOf(int floor) => TrophyRoad.IsMilestone(floor) ? Milestone : Normal;

        /// Les arrêts de couleur du ciel, de haut en bas : chaque palier a son dégradé (haut plus
        /// profond, bas plus clair), avec un fondu de 320 px entre deux paliers.
        List<(float y, Color c)> SkyStops(int top, float height)
        {
            var stops = new List<(float y, Color c)> { (0f, SkyTop[DecorOf(top)]) };
            for (int f = top; f > 1; f--)
                if (DecorOf(f) != DecorOf(f - 1))
                {
                    float b = (floorCenterY[f] + floorCenterY[f - 1]) * 0.5f;
                    stops.Add((b - FadeHeight * 0.5f, SkyBottom[DecorOf(f)]));
                    stops.Add((b + FadeHeight * 0.5f, SkyTop[DecorOf(f - 1)]));
                }
            stops.Add((height, SkyBottom[0]));
            return stops;
        }

        /// La hauteur (y) du milieu d'un étage « à virgule » (5,5 = entre les étages 5 et 6).
        float FloorY(float floor)
        {
            int a = Mathf.FloorToInt(floor), b = a + 1;
            if (!floorCenterY.ContainsKey(a)) return -1f;
            if (!floorCenterY.ContainsKey(b) || Mathf.Approximately(floor, a)) return floorCenterY[a];
            return Mathf.Lerp(floorCenterY[a], floorCenterY[b], floor - a);
        }

        /// Les astres (§17.2.2) : soleil de jour, soleil couchant, lune. Ils respirent en 4 s.
        void AddCelestials(VisualElement layer, int top)
        {
            void Add(float floor, float x, float d, string color, float opacity)
            {
                float y = FloorY(floor);
                if (y < 0f) return;
                var c = AscensionFx.Hex(color);
                var glow = AscensionFx.Glow(new Color(c.r, c.g, c.b, 1f), d, d, x, y);
                glow.style.opacity = opacity;
                layer.Add(glow);
                breathers.Add((glow, opacity, 4f));
            }
            Add(3f, 860f, 520f, "#FFF3B0", 0.55f);
            Add(5.5f, 180f, 640f, "#FFB27A", 0.50f);
            Add(8f, 880f, 360f, "#F2ECFF", 0.40f);
        }

        /// Les étoiles (§17.2.4) : dans la moitié haute du palier 3 (clairsemées) et dans tout le
        /// palier 4 (plus denses, avec 20 % d'étincelles à 4 branches).
        void AddStars(VisualElement layer, int top, float height)
        {
            var regions = new List<(float y0, float y1, float area, float sparkleShare)>();
            galaxyTop = galaxyBottom = -1f;
            // Les limites de chaque palier présent (en y), du haut vers le bas.
            for (int tier = 3; tier >= 2; tier--)
            {
                float y0 = float.MaxValue, y1 = -1f;
                for (int f = 1; f <= top; f++)
                    if (DecorOf(f) == tier) { y0 = Mathf.Min(y0, floorCenterY[f] - 250f); y1 = Mathf.Max(y1, floorCenterY[f] + 250f); }
                if (y1 < 0f) continue;
                if (tier == 3) { y0 = 0f; galaxyTop = 0f; galaxyBottom = Mathf.Min(height, y1); regions.Add((0f, galaxyBottom, 9000f, 0.2f)); }
                else regions.Add((Mathf.Max(0f, y0), (y0 + y1) * 0.5f, 18000f, 0f));
            }
            if (regions.Count == 0) { stars = null; return; }
            stars = new StarField(ContentWidth, height, regions);
            layer.Add(stars.Element);
        }

        /// Les nuages (§17.2.3) : lointains (calque 2) et proches (calque 4), qui dérivent de
        /// gauche à droite. Jamais à moins de 60 px d'un numéro d'étage.
        void AddClouds(VisualElement far, VisualElement near, int top, float height)
        {
            int farCount = Mathf.Max(1, Mathf.RoundToInt(height / 700f));
            int nearCount = Mathf.Max(1, Mathf.RoundToInt(height / 900f));
            for (int i = 0; i < farCount; i++) clouds.Add(NewCloud(far, height, i % 2 == 0 ? 2 : 3, 160f, 220f, 0.45f, 10f, 14f));
            for (int i = 0; i < nearCount; i++) clouds.Add(NewCloud(near, height, i % 2 == 0 ? 1 : 2, 260f, 340f, 0.85f, 20f, 28f));
        }

        Drifter NewCloud(VisualElement layer, float height, int image, float minW, float maxW, float opacity, float minSpeed, float maxSpeed)
        {
            float width = Mathf.Lerp(minW, maxW, (float)random.NextDouble());
            float y = CloudHeight(height, width);
            int tier = TierAtY(y);
            float alpha = tier == 3 ? opacity * 0.5f : opacity;        // nuages de nuit : très discrets
            var cloud = new AscensionFx.Cloud(UiImages.Nuage(image), width, CloudTint[tier], alpha);
            var d = new Drifter
            {
                e = cloud, width = width, y = y,
                x = Mathf.Lerp(-360f, ContentWidth, (float)random.NextDouble()),   // départs décalés
                speed = Mathf.Lerp(minSpeed, maxSpeed, (float)random.NextDouble()),
                bobPeriod = Mathf.Lerp(3f, 5f, (float)random.NextDouble()),
                bobPhase = (float)random.NextDouble() * 6.28f
            };
            cloud.style.left = d.x; cloud.style.top = d.y;
            layer.Add(cloud);
            return d;
        }

        /// Une hauteur au hasard pour un nuage, à plus de 60 px de tout numéro d'étage.
        float CloudHeight(float height, float width)
        {
            float h = width * 0.5f, y = 0f;
            for (int attempt = 0; attempt < 12; attempt++)
            {
                y = Mathf.Lerp(0f, Mathf.Max(0f, height - h), (float)random.NextDouble());
                bool ok = true;
                foreach (var cy in floorCenterY.Values)
                    if (cy > y - 60f - 32f && cy < y + h + 60f + 32f) { ok = false; break; }
                if (ok) break;
            }
            return y;
        }

        /// Le palier (0 à 3) de l'étage le plus proche d'une hauteur.
        int TierAtY(float y)
        {
            int best = 1; float bestD = float.MaxValue;
            foreach (var kv in floorCenterY)
            {
                float d = Mathf.Abs(kv.Value - y);
                if (d < bestD) { bestD = d; best = kv.Key; }
            }
            return DecorOf(best);
        }

        /// La barre de progression v7 (§17.2.7) : piste 40 px, remplissage en dégradé avec des
        /// rayures qui montent, bout lumineux, crans d'étage, jalons de palier, tirets jusqu'au
        /// prochain palier, repère « toi ».
        void BuildBar(int top, int record, int you)
        {
            float barBottom = floorCenterY[1];
            var track = Box("asc-barre asc-barre7");
            track.style.top = 0f;
            track.style.height = barBottom + 20f;
            content.Add(track);
            fillBottom = barBottom + 16f;
            fillTop = record >= 1 && floorCenterY.ContainsKey(record) ? floorCenterY[record] : barBottom;
            fillBar = Box("asc-barre__plein asc-barre7__plein");
            fillBar.style.backgroundImage = new StyleBackground(BarGradient);
            stripes = new AscensionFx.Stripes();
            fillBar.Add(stripes);
            fillBar.Add(Box("asc-barre__reflet asc-barre7__reflet"));
            content.Add(fillBar);

            // Les crans d'étage : sombres dans la partie vide, bruns sur le remplissage.
            for (int f = 1; f <= top; f++)
            {
                if (TrophyRoad.IsMilestone(f)) continue;
                var notch = Box("asc-cran" + (f <= record ? " asc-cran--plein" : ""));
                notch.style.top = floorCenterY[f] - 2f;
                content.Add(notch);
            }
            // Les tirets « jusqu'où il reste à monter » : du record au prochain palier pas encore atteint.
            int next = -1;
            for (int f = record + 1; f <= top; f++) if (TrophyRoad.IsMilestone(f)) { next = f; break; }
            if (next > 0)
            {
                float from = fillTop - 30f, to = floorCenterY[next] + 30f;
                for (float yy = from; yy > to; yy -= 24f)
                {
                    var dash = Box("asc-tiret");
                    dash.style.top = yy - 12f;
                    content.Add(dash);
                }
            }
            // Les jalons de palier sur la piste.
            for (int f = 1; f <= top; f++)
            {
                if (!TrophyRoad.IsMilestone(f)) continue;
                bool reached = f <= record;
                bool claimable = reached && !TrophyRoad.IsReceived(f);
                var dot = Box("asc-jalon" + (reached ? " asc-jalon--atteint" : ""));
                dot.style.top = floorCenterY[f] - 20f;
                if (reached) dot.Add(Box("asc-jalon__diamant"));
                else dot.Add(MiniLock());
                content.Add(dot);
                barMilestones.Add((dot, floorCenterY[f], claimable));
            }
            // Le bout lumineux, au sommet du remplissage.
            fillTip = AscensionFx.Glow(AscensionFx.Hex("#FFF3B0"), 72f, 72f, BarCenterX, fillTop);
            fillTip.style.opacity = 0.7f;
            content.Add(fillTip);
            SetFill(0f);

            // Le repère « toi » : bulle or 60 px avec l'icône du skin (à droite de la pilule du record s'il tombe dessus).
            // (Sur l'étage du record, c'est le médaillon bleu, à droite de la pilule, qui le dit.)
            if (floorCenterY.ContainsKey(you) && you != record)
            {
                youTrackMark = Box("asc-toi-piste");
                var icon = Box("asc-toi-piste__icone");
                var skin = Shop.GetEquippedSkin();
                if (skin != null && skin.Icon != null) icon.style.backgroundImage = new StyleBackground(skin.Icon);
                youTrackMark.Add(icon);
                youTrackMark.style.top = floorCenterY[you] - 30f;
                youTrackMark.style.left = BarCenterX - 30f;
            }
        }

        static Texture2D barGradient;
        /// Le dégradé du remplissage : #FFE66B en haut → #FFB020 en bas (fabriqué une fois).
        static Texture2D BarGradient => barGradient != null ? barGradient
            : barGradient = UiFxTextures.VerticalGradient(AscensionFx.Hex("#FFE66B"), AscensionFx.Hex("#FFB020"));

        /// Un petit cadenas dessiné (corps 14 × 11, anse 10 px) pour les jalons pas encore atteints.
        static VisualElement MiniLock()
        {
            var lockBox = Box("asc-mini-cadenas");
            lockBox.Add(Box("asc-mini-cadenas__anse"));
            lockBox.Add(Box("asc-mini-cadenas__corps"));
            return lockBox;
        }

        void BuildFloor(int f, float tileTop, int record, int you, VisualElement numbers, VisualElement tiles, VisualElement overlay, VisualElement infos, VisualElement lights)
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
            var topCenter = new Vector2(TileCenterX, tileTop + shape.TopCenterY);

            // Le numéro d'étage. v7 : médaillon bleu pour l'étage du joueur, or (ou gris) pour un palier.
            VisualElement numberElement;
            if (f == you)
            {
                youMedal = Medal(f, "asc-medaillon asc-medaillon--toi", 76f, centerY);
                // Sur l'étage du record, la pilule du record occupe le couloir : le médaillon se
                // met juste à sa droite (il ne passe jamais dessous).
                if (f == record) youMedal.style.left = 200f - 38f;
                numberElement = youMedal;
                numbers.Add(youMedal);
            }
            else if (milestone)
            {
                numberElement = Medal(f, "asc-medaillon asc-medaillon--palier" + (unexplored ? " asc-medaillon--verrou" : ""), 84f, centerY);
                // Caché à l'étage du record : la pilule le montre déjà.
                if (f == record) numberElement.style.visibility = Visibility.Hidden;
                numbers.Add(numberElement);
            }
            else
            {
                var number = ShopUi.NewLabel(f.ToString(), "asc-numero");
                if (unexplored) number.AddToClassList("asc-numero--inexplore");
                number.style.left = NumberCenterX - 40f;
                number.style.top = centerY - 24f;
                // Caché à l'étage du record : la pilule le montre déjà.
                if (f == record) number.style.visibility = Visibility.Hidden;
                numbers.Add(number);
                numberElement = number;
            }

            // Le bouton « i » (détail chiffré de l'étage), dans son couloir à droite.
            var info = new Button(() => OpenInfo(f));
            foreach (var c in new[] { "btn", "bulle", "bulle-petit", "bulle-petit--ardoise", "asc-info" }) info.AddToClassList(c);
            info.Add(ShopUi.NewLabel("i", "asc-info__texte"));
            info.style.left = InfoCenterX - 32f;
            info.style.top = centerY - 32f;
            infos.Add(info);

            // Le ruban du palier d'altitude (au 1er étage de chaque palier : 1, 4, 7, 10…).
            bool firstOfTier = f == 1 || DecorOf(f) != DecorOf(f - 1);
            if (firstOfTier)
            {
                string name = TierName(DecorOf(f) + 1);
                if (!string.IsNullOrEmpty(name))
                {
                    var ribbon = Box("asc-ruban");
                    if (UiImages.Ruban != null) { ribbon.style.backgroundImage = new StyleBackground(UiImages.Ruban); ribbon.AddToClassList("asc-ruban--image"); }
                    ribbon.Add(ShopUi.NewLabel(name, "asc-ruban__texte"));
                    ribbon.style.left = TileCenterX;   // centré par « translate: -50% » (V7.uss)
                    // Au-dessus de la dalle ; sur un palier, la carte récompense (220 × 250) dépasse
                    // du dessus : le ruban monte alors au-dessus de la carte (vérifié en capture).
                    ribbon.style.top = milestone ? tileTop + shape.PedestalPoint.y - 250f - 14f - 88f
                                                 : tileTop - 36f - 88f + shape.MarginTop;
                    overlay.Add(ribbon);
                }
            }

            if (milestone)
            {
                bool reached = !unexplored;
                // La bande de fête : toute la largeur, 360 px de haut, centrée sur la dalle.
                var band = AscensionFx.Glow(reached ? AscensionFx.Hex("#FFF3B0") : Color.white, ContentWidth * 1.3f, 360f, ContentWidth * 0.5f, tileTop + shape.H * 0.5f);
                band.style.opacity = reached ? 0.22f : 0.08f;
                lights.Add(band);
                // Les rayons dorés (palier atteint), sauf si c'est l'étage du joueur (son projecteur les remplace).
                if (reached && f != you)
                {
                    var ped = P(shape.PedestalPoint);
                    var rays = AscensionFx.Rays(20, new Color(1f, 0.835f, 0.29f, 0.28f), 760f, ped.x, ped.y);
                    lights.Add(rays);
                    spinners.Add((rays, 60f));
                }
                // La guirlande de fanions (derrière la carte, au-dessus des coffres du fond).
                var garland = Garland(reached);
                garland.style.left = TileCenterX - 320f;
                garland.style.top = tileTop + 8f;
                overlay.Add(garland);
                if (reached) garlands.Add((garland, (float)random.NextDouble() * 6.28f));

                BuildReward(f, P(shape.PedestalPoint), P(shape.ButtonPoint), numberElement, overlay, unexplored);
            }
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
            // « Toi » : sous le PROJECTEUR (§17.2.5).
            if (f == you) BuildSpotlight(P(shape.HorsePoint), topCenter, tileTop, shape, lights, overlay);
        }

        /// Un médaillon rond (numéro d'étage) : bleu pour le joueur, or / gris pour un palier.
        static VisualElement Medal(int floor, string classes, float d, float centerY)
        {
            var medal = Box(classes);
            medal.style.width = d; medal.style.height = d;
            medal.style.left = NumberCenterX - d * 0.5f;
            medal.style.top = centerY - d * 0.5f;
            medal.Add(ShopUi.NewLabel(floor.ToString(), "asc-medaillon__texte"));
            return medal;
        }

        /// L'étage du joueur : faisceau, rayons, lueur au sol (calque 3) ; anneaux et cheval qui
        /// sautille (calque 6) ; étincelles qui montent des bords de la dalle (calque 7).
        void BuildSpotlight(Vector2 horse, Vector2 topCenter, float tileTop, TileShape shape, VisualElement lights, VisualElement overlay)
        {
            beam = new AscensionFx.Beam();
            beam.style.width = 420f; beam.style.height = 760f;
            beam.style.left = topCenter.x - 210f;
            beam.style.top = topCenter.y - 760f;
            lights.Add(beam);
            var rays = AscensionFx.Rays(16, new Color(1f, 0.89f, 0.54f, 0.40f), 640f, topCenter.x, topCenter.y);
            youRays = rays;
            lights.Add(rays);
            spinners.Add((rays, 40f));
            var gc = AscensionFx.Hex("#FFE9A0");
            youGround = AscensionFx.Glow(new Color(gc.r, gc.g, gc.b, 0.5f), 560f, 220f, topCenter.x, topCenter.y);
            lights.Add(youGround);

            // Deux anneaux or qui s'agrandissent en s'effaçant (le 2e part 0,8 s plus tard).
            youRing1 = Box("asc-toi7__anneau");
            youRing2 = Box("asc-toi7__anneau");
            foreach (var ring in new[] { youRing1, youRing2 })
            {
                ring.style.left = horse.x - 70f;
                ring.style.top = horse.y - 22f;
                overlay.Add(ring);
            }
            youMark = Box("asc-toi asc-toi7");
            var skin = Shop.GetEquippedSkin();
            if (skin != null && skin.Icon != null) youMark.style.backgroundImage = new StyleBackground(skin.Icon);
            youMark.style.left = horse.x - 55f;
            youMarkTop = horse.y - 110f;
            youMark.style.top = youMarkTop;
            overlay.Add(youMark);
            youCenterY = horse.y - 55f;

            // 6 étincelles qui montent des bords de la dalle (décalées de 0,35 s).
            if (!Low)
                for (int i = 0; i < 6; i++)
                {
                    float x = TileCenterX + (i % 2 == 0 ? -1f : 1f) * (shape.W * 0.30f + 20f * (i / 2));
                    var spark = new AscensionFx.Sparkle4(AscensionFx.Hex("#FFE38A"), 16f);
                    var from = new Vector2(x, topCenter.y + 20f);
                    spark.style.left = from.x - 8f; spark.style.top = from.y - 8f;
                    spark.style.opacity = 0f;
                    fxLayer.Add(spark);
                    youSparkles.Add((spark, from, i * 0.35f));
                }
        }

        /// La guirlande de fanions : l'image du lot 9 si elle est là, sinon des fanions dessinés.
        static VisualElement Garland(bool reached)
        {
            var garland = Box("asc-guirlande");
            var sprite = UiImages.Guirlande;
            if (sprite != null) garland.style.backgroundImage = new StyleBackground(sprite);
            else garland.generateVisualContent += DrawFlags;
            if (!reached) garland.style.unityBackgroundImageTintColor = AscensionFx.Hex("#A8A4BC");
            if (!reached) garland.AddToClassList("asc-guirlande--eteinte");
            return garland;
        }

        /// Repli dessiné : une ficelle qui pend et 9 fanions aux couleurs de la fête.
        static void DrawFlags(MeshGenerationContext ctx)
        {
            var r = ctx.visualElement.contentRect;
            bool off = ctx.visualElement.ClassListContains("asc-guirlande--eteinte");
            string[] colors = { "#FF5FA2", "#FFD54A", "#3BC8FF", "#6BE34A", "#A35CE0" };
            var p = ctx.painter2D;
            // La ficelle : une courbe qui pend de 50 px au milieu.
            Vector2 Rope(float u) => new Vector2(r.width * (0.04f + 0.92f * u), 18f + 50f * 4f * u * (1f - u));
            p.strokeColor = AscensionFx.Hex("#110D1F");
            p.lineWidth = 4f;
            p.BeginPath();
            p.MoveTo(Rope(0f));
            for (int i = 1; i <= 20; i++) p.LineTo(Rope(i / 20f));
            p.Stroke();
            const int n = 9;
            for (int i = 0; i < n; i++)
            {
                float u0 = (i + 0.15f) / n, u1 = (i + 0.85f) / n;
                Vector2 a = Rope(u0), b = Rope(u1), tip = (a + b) * 0.5f + new Vector2(0f, 54f);
                var c = AscensionFx.Hex(colors[i % colors.Length]);
                if (off) c = Color.Lerp(c, AscensionFx.Hex("#A8A4BC"), 0.8f);
                p.fillColor = c;
                p.strokeColor = AscensionFx.Hex("#110D1F");
                p.lineWidth = 3f;
                p.BeginPath(); p.MoveTo(a); p.LineTo(b); p.LineTo(tip); p.ClosePath();
                p.Fill(); p.Stroke();
            }
        }

        /// La carte de la récompense d'un palier, dans l'un de ses 3 états (§16.2.5 ; v7 : 220 × 250).
        void BuildReward(int f, Vector2 pedestal, Vector2 buttonPoint, VisualElement number, VisualElement overlay, bool unexplored)
        {
            int reward = TrophyRoad.RewardAt(f);
            bool received = TrophyRoad.IsReceived(f);
            bool claimable = !unexplored && !received;   // gagné mais pas encore pris
            float cardLeft = pedestal.x - 110f, cardTop = pedestal.y - 250f;
            var cardCenter = new Vector2(pedestal.x, cardTop + 125f);

            var cl = claimable ? new Claimable { center = cardCenter, number = number } : null;
            if (claimable)
            {
                // §17.2.9 : socle sombre (contraste sur tous les ciels), puis rayons dorés en 2 couches, puis lueur.
                var ink = AscensionFx.Hex("#110D1F");
                overlay.Add(AscensionFx.Glow(new Color(ink.r, ink.g, ink.b, 0.30f), 640f, 640f, cardCenter.x, cardCenter.y));
                cl.bigRays = AscensionFx.Rays(14, new Color(1f, 0.835f, 0.29f, 0.95f), 680f, cardCenter.x, cardCenter.y); // v7 : 440 → 680, la carte (220 × 250) cachait presque tout
                overlay.Add(cl.bigRays);
                cl.smallRays = AscensionFx.Rays(10, new Color(1f, 0.965f, 0.784f, 0.95f), 500f, cardCenter.x, cardCenter.y);
                overlay.Add(cl.smallRays);
                var g = AscensionFx.Hex("#FFF3B0");
                cl.glow = AscensionFx.Glow(new Color(g.r, g.g, g.b, 0.7f), 360f, 360f, cardCenter.x, cardCenter.y);
                overlay.Add(cl.glow);
            }

            var card = Box("asc-carte asc-carte7");
            if (unexplored) card.AddToClassList("asc-carte--verrou");
            card.style.left = cardLeft;
            card.style.top = cardTop;
            card.Add(Box("asc-carte__lueur"));
            card.Add(Box("asc-carte__diamant"));
            card.Add(ShopUi.NewLabel($"+{reward}", "asc-carte__nombre"));
            if (unexplored) card.Add(BoutiquePage.Lock());
            if (received) card.Add(Check());
            if (claimable)
            {
                // L'anneau or qui « respire » autour du contour d'encre.
                cl.ring = Box("asc-carte7__anneau");
                card.Add(cl.ring);
            }
            overlay.Add(card);

            if (claimable)
            {
                var button = new Button();
                foreach (var c in new[] { "btn", "bulle", "bulle-rect", "bulle-rect--vert", "asc-recuperer", "asc-recuperer7" }) button.AddToClassList(c);
                button.Add(ShopUi.NewLabel("Récupérer", "bulle-rect__texte"));
                button.Add(Box("btn-eclat"));
                button.style.left = buttonPoint.x - 150f;
                button.style.top = buttonPoint.y - 56f;
                Action claim = () => Claim(f, card, button);
                button.clicked += claim;
                claimActions.Add(claim);
                overlay.Add(button);
                // La petite flèche or au-dessus du bouton, qui pointe vers la carte.
                cl.arrow = Box("asc-fleche");
                cl.arrow.Add(Box("asc-chevron__barre asc-chevron__barre--a"));
                cl.arrow.Add(Box("asc-chevron__barre asc-chevron__barre--b"));
                cl.arrow.style.left = buttonPoint.x - 20f;
                cl.arrow.style.top = buttonPoint.y - 96f;
                overlay.Add(cl.arrow);
                // 8 étincelles qui tournent lentement autour de la carte.
                if (!Low)
                    for (int i = 0; i < 8; i++)
                    {
                        var s = new AscensionFx.Sparkle4(AscensionFx.Hex("#FFF6C8"), 18f);
                        overlay.Add(s);
                        cl.sparkles.Add(s);
                    }
                cl.card = card;
                cl.button = button;
                claimables.Add(cl);
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
            var from = new Vector2(card.resolvedStyle.left + 110f, card.resolvedStyle.top + 95f);
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
            if (refreshPending && time - claimTime >= 1f) Refresh();
            if (scroll == null) return;
            float view = scroll.contentViewport.layout.height;
            float scrollY = scroll.scrollOffset.y;
            bool ready = !float.IsNaN(view) && view > 0f && content.layout.height > 0f;
            // « N'animer que ce qui est visible » (±1 hauteur de zone, §17.2.13).
            bool Near(float y) => !ready || (y > scrollY - view && y < scrollY + 2f * view);

            TickOpening(ready, view);

            // Les astres respirent (opacité ×0,82 → ×1,09 en 4 s, aller-retour).
            foreach (var (e, baseOpacity, period) in breathers)
                e.style.opacity = baseOpacity * (0.955f + 0.135f * Mathf.Sin(time * Mathf.PI * 2f / period));

            // Les nuages dérivent de gauche à droite et flottent de ±4 px.
            if (!Low)
                foreach (var c in clouds)
                {
                    c.x += c.speed * dt;
                    if (c.x > ContentWidth + 40f) { c.x = -360f; c.y = CloudHeight(content.layout.height, c.width); }
                    if (!Near(c.y)) continue;
                    c.e.style.left = c.x;
                    c.e.style.top = c.y + 4f * Mathf.Sin(time * Mathf.PI * 2f / c.bobPeriod + c.bobPhase);
                }

            // Les rayons qui tournent (immobiles en Graphismes « Faible »).
            if (!Low)
                foreach (var (rays, turn) in spinners)
                    rays.style.rotate = new Rotate(time * 360f / turn);

            // Les guirlandes se balancent (±1,5° en 3 s) et s'étirent un tout petit peu.
            foreach (var (g, phase) in garlands)
            {
                float s = Mathf.Sin(time * Mathf.PI * 2f / 3f + phase);
                g.style.rotate = new Rotate(1.5f * s);
                g.style.scale = new Scale(new Vector3(1f, 1f + 0.015f * (s + 1f), 1f));
            }

            // Le drapeau se balance de ±4° en 2 s.
            if (flag != null) flag.style.rotate = new Rotate(4f * Mathf.Sin(time * Mathf.PI));

            TickSpotlight();
            TickBar(dt);
            TickClaimables(dt, Near);
            stars?.Tick(time, Low);
            TickShootingStar(ready, view, scrollY);

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
            confetti.Tick(dt);

            // « Ma position » : retour vers l'étage du joueur en 0,4 s, puis le cheval saute.
            if (goFrom >= 0f)
            {
                float u = Mathf.Clamp01((time - goStart) / 0.4f);
                scroll.scrollOffset = new Vector2(0f, Mathf.Lerp(goFrom, goTo, InOutCubic(u)));
                if (u >= 1f) { goFrom = -1f; hopTime = time; }
                UpdateGoButton();
            }
        }

        // ---------- L'ouverture « on monte » (§17.2.8) ----------
        float hopTime = -10f;   // le dernier « petit saut » du cheval (arrivée ou « Ma position »)

        void TickOpening(bool ready, float view)
        {
            if (openPending && ready && youMark != null)
            {
                openPending = false;
                opening = true;
                float total = content.layout.height, max = Mathf.Max(0f, total - view);
                // L'arrivée : la dalle du joueur au tiers bas de la zone.
                openScrollTo = Mathf.Clamp(youCenterY - view * (2f / 3f), 0f, max);
                // Le départ : 6 étages plus bas (ou l'étage 1).
                float startY = floorCenterY.TryGetValue(Mathf.Max(1, YouFloor - 6), out var sy) ? sy : youCenterY;
                openScrollFrom = Mathf.Clamp(startY - view * (2f / 3f), 0f, max);
                openDuration = Low ? 0.4f : 1.5f;
                openTime = time;
                scroll.scrollOffset = new Vector2(0f, openScrollFrom);
                SetSpotlight(0f);
            }
            if (!opening) return;
            float t = time - openTime;
            float k = Low ? 0.4f / 1.5f : 1f;            // « Faible » : tout est 3,75 fois plus court
            float scrollStart = 0.15f * k, scrollEnd = 1.05f * k;
            float u = Mathf.Clamp01((t - scrollStart) / (scrollEnd - scrollStart));
            scroll.scrollOffset = new Vector2(0f, Mathf.Lerp(openScrollFrom, openScrollTo, InOutCubic(u)));
            SetFill(InOutCubic(u));
            UpdateGoButton();
            // L'arrivée : le cheval tombe avec un rebond, le projecteur s'allume, le médaillon apparaît.
            if (t >= scrollEnd && !arrived) Arrive();
            if (arrived)
            {
                float a = Mathf.Clamp01((t - scrollEnd) / (0.3f * k));
                SetSpotlight(a);
            }
            if (t >= openDuration) FinishOpening();
        }

        void Arrive()
        {
            arrived = true;
            hopTime = time;
            dropTime = time;
            if (youMark != null)
                confetti.Burst(fxLayer, new Vector2(TileCenterX, youCenterY), Low ? 8 : 16);
        }

        float dropTime = -10f;

        /// Passe directement à l'état final (premier clic, défilement, molette).
        void SkipOpening()
        {
            if (!opening && !openPending) return;
            if (openPending) return; // pas encore commencé : on laissera l'ouverture se placer
            if (scroll != null) scroll.scrollOffset = new Vector2(0f, openScrollTo);
            if (!arrived) { arrived = true; }
            FinishOpening();
        }

        void FinishOpening()
        {
            opening = false;
            SetFill(1f);
            SetSpotlight(1f);
            UpdateGoButton();
        }

        /// Allume (0 → 1) le faisceau, les rayons et la lueur de l'étage du joueur.
        void SetSpotlight(float a)
        {
            if (beam != null) beam.style.opacity = a;
            if (youRays != null) youRays.style.opacity = a;
            if (youGround != null) youGround.style.opacity = a;
        }

        // ---------- L'étage du joueur ----------
        void TickSpotlight()
        {
            if (youMark == null) return;
            // Le projecteur respire (70 → 100 % en 2,4 s), une fois allumé.
            if (beam != null && !opening) beam.style.opacity = 0.85f + 0.15f * Mathf.Sin(time * Mathf.PI * 2f / 2.4f);
            // Le cheval : chute avec rebond à l'arrivée (0,35 s), puis sautille (−6 px en 1,2 s).
            float drop = time - dropTime;
            float y;
            if (opening && !arrived) y = -60f;
            else if (drop < 0.35f) y = -60f * (1f - OutBounce(drop / 0.35f));
            else
            {
                float hop = time - hopTime;
                y = Low ? 0f : -6f * Mathf.Abs(Mathf.Sin(time * Mathf.PI / 1.2f));
                if (hop >= 0.35f && hop < 0.7f) y -= 16f * Mathf.Sin((hop - 0.35f) / 0.35f * Mathf.PI); // petit saut
            }
            youMark.style.translate = new Translate(0f, y);
            // Les deux anneaux (1,6 s chacun, le 2e décalé de 0,8 s).
            RingStep(youRing1, time);
            RingStep(youRing2, time - 0.8f);
            // Le médaillon bleu : apparition 0 → 1,2 → 1 à l'arrivée, puis pulsation 1 → 1,08 en 1,6 s.
            if (youMedal != null)
            {
                float s;
                float since = time - dropTime;
                if (opening && !arrived) s = 0f;
                else if (since < 0.3f) s = Ease.Pop(since / 0.3f, 0f, 1.2f);
                else s = 1f + 0.04f * (1f - Mathf.Cos(time * Mathf.PI * 2f / 1.6f));
                youMedal.style.scale = new Scale(new Vector3(s, s, 1f));
            }
            // Les étincelles qui montent (80 px en 2,2 s, en s'effaçant).
            foreach (var (e, from, delay) in youSparkles)
            {
                float u = Mathf.Repeat(time - delay, 2.2f) / 2.2f;
                e.style.top = from.y - 8f - 80f * u;
                e.style.opacity = (opening && !arrived) ? 0f : Mathf.Sin(u * Mathf.PI);
            }
        }

        static void RingStep(VisualElement ring, float t)
        {
            if (ring == null) return;
            float u = Mathf.Repeat(t, 1.6f) / 1.6f;
            ring.style.opacity = 0.7f * (1f - u);
            float s = 1f + 0.5f * u;
            ring.style.scale = new Scale(new Vector3(s, s, 1f));
        }

        // ---------- La barre de progression ----------
        void TickBar(float dt)
        {
            if (!opening && !Low && stripes != null) stripes.SetOffset(time * 28f / 1.2f * 2f);
            if (fillTip != null) fillTip.style.opacity = 0.5f + 0.2f * (1f + Mathf.Sin(time * Mathf.PI * 2f / 1.2f));
            float fillY = fillBar != null ? fillBar.style.top.value.value : 0f;
            foreach (var (dot, y, pulse) in barMilestones)
            {
                float s = 1f;
                if (pulse) s = 1f + 0.075f * (1f - Mathf.Cos(time * Mathf.PI * 2f));
                // Pendant l'ouverture : petit rebond quand le remplissage passe le jalon.
                if (opening)
                {
                    float d = fillY - y;
                    if (d < 0f && d > -40f) s = 1f + 0.25f * Mathf.Sin(-d / 40f * Mathf.PI);
                }
                dot.style.scale = new Scale(new Vector3(s, s, 1f));
            }
        }

        // ---------- Les paliers à récupérer (§17.2.9) ----------
        void TickClaimables(float dt, Func<float, bool> near)
        {
            foreach (var c in claimables)
            {
                if (c.button.enabledSelf)
                {
                    c.card.style.translate = new Translate(0f, -8f * Mathf.Abs(Mathf.Sin(time * Mathf.PI / 1.2f)));
                    float p = 1f + 0.03f * (1f - Mathf.Cos(time * Mathf.PI * 2f));
                    c.button.style.scale = new Scale(new Vector3(p, p, 1f));
                    if (c.number != null) c.number.style.opacity = 0.75f + 0.25f * Mathf.Cos(time * Mathf.PI);
                    var shine = c.button.Q<VisualElement>(className: "btn-eclat");
                    if (shine != null)
                    {
                        float phase = Mathf.Repeat(time, 2.5f);
                        shine.style.opacity = phase < 0.5f ? 1f : 0f;
                        shine.style.left = Mathf.Lerp(-60f, 320f, phase / 0.5f);
                    }
                    if (c.ring != null) c.ring.style.opacity = 0.8f + 0.2f * Mathf.Cos(time * Mathf.PI * 2f / 1.4f);
                    if (c.glow != null) c.glow.style.opacity = 0.675f + 0.175f * Mathf.Sin(time * Mathf.PI * 2f / 1.2f);
                    if (c.arrow != null) c.arrow.style.translate = new Translate(0f, -3f + 3f * Mathf.Sin(time * Mathf.PI * 2f / 0.8f));
                    if (!Low)
                    {
                        if (c.bigRays != null) c.bigRays.style.rotate = new Rotate(time * 360f / 14f);
                        if (c.smallRays != null) c.smallRays.style.rotate = new Rotate(18f - time * 360f / 20f);
                    }
                    // Les 8 étincelles tournent autour de la carte (rayon 150, un tour en 8 s) et scintillent.
                    for (int i = 0; i < c.sparkles.Count; i++)
                    {
                        float a = time * Mathf.PI * 2f / 8f + i * Mathf.PI * 2f / c.sparkles.Count;
                        var pos = c.center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 150f;
                        var e = c.sparkles[i];
                        e.style.left = pos.x - 9f; e.style.top = pos.y - 9f;
                        float tw = 0.6f + 0.4f * Mathf.Sin(time * Mathf.PI * 2f / (1.4f + 0.2f * i) + i);
                        e.style.opacity = tw;
                        e.style.scale = new Scale(new Vector3(0.6f + 0.5f * tw, 0.6f + 0.5f * tw, 1f));
                    }
                    // Les confettis : la 1re fois que la carte entre dans la vue (une fois par ouverture).
                    if (!c.confettiDone && !openPending && (!opening || time - openTime >= 1.4f) && IsVisible(c.center.y))
                    {
                        c.confettiDone = true;
                        confetti.Burst(fxLayer, c.center, Low ? 8 : 24);
                    }
                }
                else
                {
                    // Après le clic : la carte revient à sa taille, le bouton et sa fête s'effacent.
                    c.card.style.translate = new Translate(0f, 0f);
                    float s = Mathf.MoveTowards(c.card.resolvedStyle.scale.value.x, 1f, dt * 2f);
                    c.card.style.scale = new Scale(new Vector3(s, s, 1f));
                    if (c.arrow != null) c.arrow.style.opacity = 0f;
                }
            }
        }

        bool IsVisible(float y)
        {
            if (scroll == null) return false;
            float view = scroll.contentViewport.layout.height, top = scroll.scrollOffset.y;
            return !float.IsNaN(view) && y > top + 40f && y < top + view - 40f;
        }

        // ---------- L'étoile filante (palier 4) ----------
        void TickShootingStar(bool ready, float view, float scrollY)
        {
            if (shootingStar == null || !ready) return;
            // Seulement si le haut de la partie visible est dans la nuit.
            bool night = scrollY + 80f < galaxyBottom;
            if (time >= nextShootingStar && night && !opening)
            {
                shootingStart = time;
                nextShootingStar = time + Mathf.Lerp(7f, 11f, (float)random.NextDouble());
                float x = Mathf.Lerp(200f, 600f, (float)random.NextDouble());
                shootingStar.userData = new Vector2(x, scrollY + 60f + 80f * (float)random.NextDouble());
            }
            else if (time >= nextShootingStar) nextShootingStar = time + 2f;
            float u = (time - shootingStart) / 0.6f;
            if (u < 0f || u > 1.2f) { shootingStar.style.display = DisplayStyle.None; return; }
            var start = (Vector2)shootingStar.userData;
            float k = Ease.In(Mathf.Clamp01(u));
            // −25° : vers la droite et vers le bas.
            var dir = new Vector2(Mathf.Cos(25f * Mathf.Deg2Rad), Mathf.Sin(25f * Mathf.Deg2Rad));
            var pos = start + dir * 420f * k;
            shootingStar.style.display = DisplayStyle.Flex;
            shootingStar.style.left = pos.x - 140f;
            shootingStar.style.top = pos.y;
            shootingStar.style.opacity = u <= 1f ? 1f : 1f - (u - 1f) / 0.2f;
        }

        /// Le trait de l'étoile filante : 140 × 3, blanc à 90 % devant → transparent derrière.
        static void DrawShootingStar(MeshGenerationContext ctx)
        {
            var head = new Color(1f, 1f, 1f, 0.9f);
            var tail = new Color(1f, 1f, 1f, 0f);
            var mesh = ctx.Allocate(4, 6);
            mesh.SetNextVertex(new Vertex { position = new Vector3(0f, 0f, Vertex.nearZ), tint = tail });
            mesh.SetNextVertex(new Vertex { position = new Vector3(140f, 0f, Vertex.nearZ), tint = head });
            mesh.SetNextVertex(new Vertex { position = new Vector3(140f, 3f, Vertex.nearZ), tint = head });
            mesh.SetNextVertex(new Vertex { position = new Vector3(0f, 3f, Vertex.nearZ), tint = tail });
            mesh.SetNextIndex(0); mesh.SetNextIndex(1); mesh.SetNextIndex(2);
            mesh.SetNextIndex(0); mesh.SetNextIndex(2); mesh.SetNextIndex(3);
        }

        void SetFill(float k)
        {
            if (fillBar == null) return;
            float top = Mathf.Lerp(fillBottom, fillTop, k);
            fillBar.style.top = top;
            fillBar.style.height = Mathf.Max(0f, fillBottom - top);
            if (fillTip != null) fillTip.style.top = top - 36f;
        }

        /// « Ma position » : ramène à l'étage du joueur en 0,4 s.
        void GoToYou()
        {
            if (scroll == null || youMark == null) return;
            float view = scroll.contentViewport.layout.height, max = Mathf.Max(0f, content.layout.height - view);
            goFrom = scroll.scrollOffset.y;
            goTo = Mathf.Clamp(youCenterY - view * (2f / 3f), 0f, max);
            goStart = time;
        }

        /// « Ma position » apparaît quand la dalle « toi » sort de la vue ; son chevron pointe vers
        /// le bas si l'étage du joueur est plus bas que la vue, vers le haut sinon.
        void UpdateGoButton()
        {
            if (goButton == null || youMark == null || scroll == null) return;
            float y = youCenterY - scroll.scrollOffset.y, view = scroll.contentViewport.layout.height;
            bool far = !opening && !float.IsNaN(view) && view > 0f && (y < -60f || y > view + 60f);
            goButton.style.display = far ? DisplayStyle.Flex : DisplayStyle.None;
            if (goChevron != null) ShopUi.SetClass(goChevron, "asc-ma-position7__chevron--bas", y > view);
        }

        // ---------- Courbes ----------
        static float InOutCubic(float t)
        {
            t = Mathf.Clamp01(t);
            return t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) * 0.5f;
        }

        static float OutBounce(float t)
        {
            t = Mathf.Clamp01(t);
            const float n = 7.5625f, d = 2.75f;
            if (t < 1f / d) return n * t * t;
            if (t < 2f / d) { t -= 1.5f / d; return n * t * t + 0.75f; }
            if (t < 2.5f / d) { t -= 2.25f / d; return n * t * t + 0.9375f; }
            t -= 2.625f / d; return n * t * t + 0.984375f;
        }

        // =====================================================================
        // Dessins faits par le code
        // =====================================================================

        /// Le ciel : un dégradé vertical continu entre les arrêts de couleur (§17.2.2).
        static void DrawSky(MeshGenerationContext ctx, List<(float y, Color c)> stops)
        {
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

        /// Les étoiles (§17.2.4) : points crème et étincelles à 4 branches qui scintillent
        /// (taille 0,6 → 1,1 et opacité 30 → 100 %, en 1,4 à 2,8 s). Tout est dessiné en une fois.
        class StarField
        {
            public readonly VisualElement Element;
            readonly Vector2[] pos;
            readonly float[] size, period, phase;
            readonly bool[] sparkle;
            float now;
            bool frozen;

            public StarField(float width, float height, List<(float y0, float y1, float area, float sparkleShare)> regions)
            {
                Element = new VisualElement { pickingMode = PickingMode.Ignore };
                Element.AddToClassList("asc-etoiles");
                Element.style.height = height;
                var random = new System.Random(30); // toujours le même ciel
                var p = new List<Vector2>(); var s = new List<float>(); var per = new List<float>(); var ph = new List<float>(); var sp = new List<bool>();
                foreach (var (y0, y1, area, share) in regions)
                {
                    int count = Mathf.Clamp(Mathf.RoundToInt(width * Mathf.Max(0f, y1 - y0) / area), 0, 700);
                    for (int i = 0; i < count; i++)
                    {
                        bool isSparkle = random.NextDouble() < share;
                        p.Add(new Vector2((float)random.NextDouble() * width, Mathf.Lerp(y0, y1, (float)random.NextDouble())));
                        s.Add(isSparkle ? 7f + 4f * (float)random.NextDouble() : 1f + (float)random.NextDouble()); // demi-taille
                        per.Add(1.4f + 1.4f * (float)random.NextDouble());
                        ph.Add((float)random.NextDouble() * 6.28f);
                        sp.Add(isSparkle);
                    }
                }
                pos = p.ToArray(); size = s.ToArray(); period = per.ToArray(); phase = ph.ToArray(); sparkle = sp.ToArray();
                Element.generateVisualContent += Draw;
            }

            public void Tick(float t, bool low)
            {
                if (low) { if (!frozen) { frozen = true; now = 0f; Element.MarkDirtyRepaint(); } return; }
                frozen = false;
                now = t;
                Element.MarkDirtyRepaint();
            }

            void Draw(MeshGenerationContext ctx)
            {
                var p = ctx.painter2D;
                var cream = new Color(1f, 0.945f, 0.839f, 1f); // #FFF1D6
                for (int i = 0; i < pos.Length; i++)
                {
                    float w = 0.5f + 0.5f * Mathf.Sin(now * 6.28f / period[i] + phase[i]); // 0 → 1
                    float scale = 0.6f + 0.5f * w, alpha = 0.3f + 0.7f * w;
                    p.fillColor = new Color(cream.r, cream.g, cream.b, alpha);
                    float r = size[i] * scale;
                    var c = pos[i];
                    p.BeginPath();
                    if (sparkle[i])
                    {
                        float t = r * 0.22f;
                        p.MoveTo(c + new Vector2(0, -r)); p.LineTo(c + new Vector2(t, -t)); p.LineTo(c + new Vector2(r, 0)); p.LineTo(c + new Vector2(t, t));
                        p.LineTo(c + new Vector2(0, r)); p.LineTo(c + new Vector2(-t, t)); p.LineTo(c + new Vector2(-r, 0)); p.LineTo(c + new Vector2(-t, -t));
                        p.ClosePath();
                    }
                    else p.Arc(c, r, 0f, 360f);
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
        /// OUTIL DE TEST : fait défiler la route (et termine l'ouverture animée).
        public void DebugScrollTo(float y) { if (scroll != null) { SkipOpening(); goFrom = -1f; scroll.scrollOffset = new Vector2(0f, y); UpdateGoButton(); } }
        /// OUTIL DE TEST : fait défiler pour montrer un étage au milieu de la vue.
        public void DebugShowFloor(int floor)
        {
            if (scroll == null || !floorCenterY.ContainsKey(floor)) return;
            SkipOpening(); openPending = false; goFrom = -1f;
            float view = scroll.contentViewport.layout.height;
            scroll.scrollOffset = new Vector2(0f, Mathf.Max(0f, floorCenterY[floor] - view * 0.5f));
            UpdateGoButton();
        }
        /// OUTIL DE TEST : clic sur « Ma position ».
        public void DebugGoToYou() => GoToYou();
        /// OUTIL DE TEST : « Ma position » est-il affiché ?
        public bool GoButtonShown => goButton != null && goButton.resolvedStyle.display == DisplayStyle.Flex;
        /// OUTILS DE TEST : la fenêtre « i » d'un étage, et « Récupérer » sur un palier.
        public void DebugInfo(int floor) => OpenInfo(floor);
        public int ClaimableCount => claimables.FindAll(c => c.button.enabledSelf).Count;
        public void DebugClaimFirst()
        {
            for (int i = 0; i < claimables.Count; i++) if (claimables[i].button.enabledSelf) { claimActions[i](); return; }
        }
    }
}
