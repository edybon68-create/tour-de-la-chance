using UnityEngine;
using UnityEngine.UIElements;

namespace TourDeLaChance
{
    /// <summary>
    /// Le spectacle des coffres précieux, dessiné dans le calque "fx-layer" du HUD
    /// (spec v3 §5.3.1 et §5.3.2). Pur affichage, avec son propre hasard visuel.
    ///
    /// - À l'ouverture d'un coffre de Bronze, d'Argent, d'Or ou de Diamant : halo, rayons,
    ///   étincelles, flash blanc et secousse de caméra (Or, Diamant), son coffre_eclat_*.
    ///   RÈGLE D'ÉQUITÉ : tout cela part quand le couvercle s'ouvre, AVANT que le contenu
    ///   soit connu de l'affichage, et c'est IDENTIQUE pour un trésor et pour un piège.
    ///   Seule la fin diffère, après la révélation : trésor → l'effet reste pendant le
    ///   jaillissement des pièces puis s'efface en 0,4 s ; piège → il s'éteint en 0,15 s.
    /// - À l'arrivée d'un étage précieux : 4 étincelles par coffre (couleur des ferrures).
    /// - Coffres de Diamant fermés : 3 étincelles scintillent en continu autour de chacun.
    /// Le halo, les rayons et les étincelles réutilisent ceux de l'ouverture de boîte
    /// (UiFxTextures, RaysElement). Tailles en px de la référence 1920 × 1080.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class ChestRarityFx : MonoBehaviour
    {
        [SerializeField] GameManager game;
        [SerializeField] ChestTrial chestTrial;
        [SerializeField] CameraRig cameraRig;

        /// Les effets d'ouverture d'une rareté (tableau §5.3.2). Index = rang (0 = Bois).
        struct Look
        {
            public float halo, haloAlpha; // diamètre (px) et opacité
            public int rays; public float raySpeed;
            public int sparks;
            public float flash;           // opacité du flash blanc plein écran (0 = aucun)
            public float shake, shakeTime;// secousse de caméra (m, s)
            public bool pulse;            // le halo bat (Diamant)
        }

        static readonly Look[] Looks =
        {
            new Look(),
            new Look { halo = 300, haloAlpha = 0.4f, sparks = 8 },
            new Look { halo = 390, haloAlpha = 0.5f, rays = 6, raySpeed = 30, sparks = 14 },
            new Look { halo = 480, haloAlpha = 0.6f, rays = 10, raySpeed = 45, sparks = 22, flash = 0.2f, shake = 0.03f, shakeTime = 0.2f },
            new Look { halo = 570, haloAlpha = 0.7f, rays = 14, raySpeed = 60, sparks = 32, flash = 0.35f, shake = 0.05f, shakeTime = 0.3f, pulse = true },
        };

        const int SparkPool = 60;
        const float TreasureHold = 1.0f;  // le temps du jaillissement des pièces
        const float TreasureFade = 0.4f;
        const float TrapFade = 0.15f;

        VisualElement layer, halo, flash;
        RaysElement rays;
        class Spark { public VisualElement e; public Vector2 pos, vel; public float age, life, size; public bool alive, twinkle; }
        readonly Spark[] sparks = new Spark[SparkPool];
        readonly System.Random fxRandom = new System.Random();

        // Effet d'ouverture en cours
        Look look;
        int fxChest = -1;
        float fxTime = -1f;          // temps depuis l'ouverture du couvercle (−1 = rien)
        float fadeStart = -1f, fadeDuration;
        float rayAngle;
        float flashTime = -1f;
        float diamondTimer;

        void OnEnable()
        {
            layer = GetComponent<UIDocument>().rootVisualElement.Q<VisualElement>("fx-layer");
            Build();
            chestTrial.PickedLidOpening += OnLidOpening;
            chestTrial.ChestRevealed += OnRevealed;
            game.FloorShown += OnFloorShown;
            game.FloorPrepared += StopAll;
        }

        void OnDisable()
        {
            chestTrial.PickedLidOpening -= OnLidOpening;
            chestTrial.ChestRevealed -= OnRevealed;
            game.FloorShown -= OnFloorShown;
            game.FloorPrepared -= StopAll;
        }

        void Build()
        {
            if (halo != null) return;
            // Flash plein écran blanc (sous les autres effets du calque).
            flash = new VisualElement { pickingMode = PickingMode.Ignore };
            flash.style.position = Position.Absolute;
            flash.style.left = 0; flash.style.top = 0; flash.style.right = 0; flash.style.bottom = 0;
            flash.style.backgroundColor = Color.white;
            flash.style.opacity = 0f;
            layer.Add(flash);

            halo = Centered();
            halo.style.backgroundImage = new StyleBackground(UiFxTextures.Halo);
            halo.style.display = DisplayStyle.None;
            rays = new RaysElement();
            rays.style.position = Position.Absolute;
            rays.style.display = DisplayStyle.None;
            layer.Add(rays);
            for (int i = 0; i < SparkPool; i++)
            {
                var e = new VisualElement { pickingMode = PickingMode.Ignore };
                e.style.position = Position.Absolute;
                e.style.backgroundImage = new StyleBackground(UiFxTextures.Sparkle);
                e.style.display = DisplayStyle.None;
                layer.Add(e);
                sparks[i] = new Spark { e = e };
            }
        }

        VisualElement Centered()
        {
            var e = new VisualElement { pickingMode = PickingMode.Ignore };
            e.style.position = Position.Absolute;
            layer.Add(e);
            return e;
        }

        /// Position d'un coffre dans le calque : son centre + (0 ; 0,9 ; 0).
        Vector2 ChestPoint(int index)
        {
            var cam = Camera.main;
            if (cam == null || layer.panel == null) return Vector2.zero;
            Vector3 world = chestTrial.GetChest(index).transform.position + new Vector3(0f, 0.9f, 0f);
            Vector2 panelPos = RuntimePanelUtils.CameraTransformWorldToPanel(layer.panel, world, cam);
            return layer.WorldToLocal(panelPos);
        }

        static void Place(VisualElement e, Vector2 center, float size)
        {
            e.style.width = size;
            e.style.height = size;
            e.style.left = center.x - size * 0.5f;
            e.style.top = center.y - size * 0.5f;
        }

        // =====================================================================
        // Ouverture d'un coffre précieux
        // =====================================================================

        void OnLidOpening(int index)
        {
            int rank = chestTrial.RarityRank;
            if (rank <= 0 || rank >= Looks.Length) return;
            var rarity = Upgrades.Rarity(rank);
            look = Looks[rank];
            fxChest = index;
            fxTime = 0f;
            fadeStart = -1f;
            rayAngle = 0f;

            Vector2 c = ChestPoint(index);
            Color main = rarity != null ? rarity.fxColor : Color.white;
            Color second = rarity != null ? rarity.fxColor2 : Color.white;
            Place(halo, c, look.halo);
            halo.style.unityBackgroundImageTintColor = WithAlpha(main, look.haloAlpha);
            halo.style.display = DisplayStyle.Flex;
            if (look.rays > 0)
            {
                Place(rays, c, look.halo * 1.1f);
                // Diamant : couleurs alternées ; sinon la couleur de la rareté.
                if (rank >= 4) rays.Configure(look.rays, WithAlpha(main, 0.45f), WithAlpha(second, 0.45f));
                else rays.Configure(look.rays, WithAlpha(main, 0.45f));
                rays.style.display = DisplayStyle.Flex;
            }
            for (int i = 0; i < look.sparks; i++)
                Emit(c, i % 2 == 0 || rank < 4 ? main : second, burst: true);
            if (look.flash > 0f) flashTime = 0f;
            if (look.shake > 0f && cameraRig != null) cameraRig.Shake(look.shake, look.shakeTime);

            Sfx sound = rank switch { 1 => Sfx.CoffreEclatBronze, 2 => Sfx.CoffreEclatArgent, 3 => Sfx.CoffreEclatOr, _ => Sfx.CoffreEclatDiamant };
            if (SoundManager.Instance != null) SoundManager.Instance.Play(sound);
            Render();
        }

        /// Après la révélation (et seulement après) : la fin de l'effet diffère.
        void OnRevealed(int index, bool trap, bool picked)
        {
            if (!picked || fxTime < 0f || index != fxChest) return;
            fadeStart = trap ? fxTime : fxTime + TreasureHold;
            fadeDuration = trap ? TrapFade : TreasureFade;
        }

        // =====================================================================
        // Arrivée d'un étage précieux, scintillement des diamants
        // =====================================================================

        void OnFloorShown()
        {
            int rank = chestTrial.RarityRank;
            if (rank <= 0) return;
            var rarity = Upgrades.Rarity(rank);
            Color main = rarity != null ? rarity.fxColor : Color.white;
            for (int i = 0; i < chestTrial.ChestCount; i++)
            {
                Vector2 c = ChestPoint(i);
                for (int k = 0; k < 4; k++)
                    Twinkle(c + new Vector2(Range(-110f, 110f), Range(-90f, 60f)), main, 0.08f * i);
            }
        }

        void StopAll()
        {
            fxTime = -1f;
            halo.style.display = DisplayStyle.None;
            rays.style.display = DisplayStyle.None;
            flash.style.opacity = 0f;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            UpdateSparks(dt);

            // Flash blanc : opacité max → 0 en 0,25 s.
            if (flashTime >= 0f)
            {
                flashTime += dt;
                float k = flashTime / 0.25f;
                flash.style.opacity = look.flash * Mathf.Clamp01(1f - k);
                if (k >= 1f) flashTime = -1f;
            }

            // Diamant : une étincelle toutes les 0,6 s autour de chaque coffre fermé.
            bool diamondIdle = chestTrial.RarityRank >= 4 && game.State == GameState.Choosing && !game.IsPanelOpen;
            if (diamondIdle)
            {
                diamondTimer -= dt;
                if (diamondTimer <= 0f)
                {
                    diamondTimer += 0.6f;
                    var rarity = Upgrades.Rarity(4);
                    for (int i = 0; i < chestTrial.ChestCount; i++)
                    {
                        Color col = rarity == null ? Color.white : (fxRandom.Next(2) == 0 ? rarity.fxColor : rarity.fxColor2);
                        Twinkle(ChestPoint(i) + new Vector2(Range(-120f, 120f), Range(-110f, 70f)), col, 0f, 1.8f);
                    }
                }
            }
            else diamondTimer = 0f;

            if (fxTime < 0f) return;
            fxTime += dt;
            rayAngle += look.raySpeed * dt;
            Render();
        }

        void Render()
        {
            float appear = Mathf.Clamp01(fxTime / 0.2f);
            float fade = 1f;
            if (fadeStart >= 0f && fxTime >= fadeStart) fade = 1f - Mathf.Clamp01((fxTime - fadeStart) / fadeDuration);
            float pulse = look.pulse ? 1f + 0.08f * (1f - Mathf.Cos(2f * Mathf.PI * fxTime / 0.6f)) : 1f;
            halo.style.opacity = appear * fade;
            halo.style.scale = new Scale(Vector3.one * pulse * Mathf.Lerp(0.6f, 1f, appear));
            rays.style.opacity = appear * fade;
            rays.style.rotate = new Rotate(rayAngle);
            if (fadeStart >= 0f && fade <= 0f) StopAll();
        }

        // =====================================================================
        // Étincelles (réserve de 60, réutilisées)
        // =====================================================================

        void Emit(Vector2 center, Color color, bool burst)
        {
            var s = FreeSpark();
            if (s == null) return;
            float angle = Range(0f, Mathf.PI * 2f);
            s.pos = center;
            s.vel = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * Range(300f, 650f);
            s.age = 0f;
            s.life = Range(0.7f, 1.1f);
            s.size = Range(20f, 36f);
            s.twinkle = false;
            Show(s, color);
        }

        /// Une étincelle qui scintille sur place (0 → 1 → 0), après un délai.
        void Twinkle(Vector2 at, Color color, float delay, float life = 0.6f)
        {
            var s = FreeSpark();
            if (s == null) return;
            s.pos = at;
            s.vel = Vector2.zero;
            s.age = -delay;
            s.life = life;
            s.size = Range(18f, 30f);
            s.twinkle = true;
            Show(s, color);
            s.e.style.opacity = 0f;
        }

        Spark FreeSpark()
        {
            foreach (var s in sparks) if (!s.alive) return s;
            return null;
        }

        static void Show(Spark s, Color color)
        {
            s.alive = true;
            s.e.style.width = s.size;
            s.e.style.height = s.size;
            s.e.style.unityBackgroundImageTintColor = color;
            s.e.style.display = DisplayStyle.Flex;
        }

        void UpdateSparks(float dt)
        {
            const float gravity = 900f; // px/s² vers le bas
            foreach (var s in sparks)
            {
                if (!s.alive) continue;
                s.age += dt;
                if (s.age < 0f) continue; // pas encore né (délai)
                if (s.age >= s.life) { s.alive = false; s.e.style.display = DisplayStyle.None; continue; }
                float k = s.age / s.life;
                if (s.twinkle)
                {
                    float sc = Ease.Sine(k);
                    s.e.style.opacity = 1f;
                    s.e.style.scale = new Scale(Vector3.one * sc);
                }
                else
                {
                    s.vel.y += gravity * dt;
                    s.pos += s.vel * dt;
                    s.e.style.scale = new Scale(Vector3.one);
                    s.e.style.opacity = 1f - Mathf.Clamp01((k - 0.6f) / 0.4f);
                }
                s.e.style.left = s.pos.x - s.size * 0.5f;
                s.e.style.top = s.pos.y - s.size * 0.5f;
            }
        }

        static Color WithAlpha(Color c, float a) { c.a = a; return c; }

        float Range(float min, float max) => min + (float)fxRandom.NextDouble() * (max - min);

        /// Pour les tests : l'effet d'ouverture est-il visible, et à quelle taille ?
        public bool OpeningFxVisible => fxTime >= 0f && halo.style.display == DisplayStyle.Flex;
        public float OpeningHaloSize => halo.resolvedStyle.width;
        public int AliveSparks { get { int n = 0; foreach (var s in sparks) if (s.alive) n++; return n; } }
    }
}
