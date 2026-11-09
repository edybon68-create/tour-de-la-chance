using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace TourDeLaChance
{
    /// <summary>
    /// L'écran d'ouverture d'une boîte (BoxOpening.uxml, spec boites §7.6 et §7.7).
    /// Il reçoit un résultat DÉJÀ TIRÉ ET SAUVEGARDÉ (Shop.TryOpenBox) et le raconte :
    ///   1. la boîte arrive, 2. elle tremble (1,2 s), 3. elle s'ouvre dans un flash blanc,
    ///   4. la révélation, plus spectaculaire selon la rareté, 5. l'écran de résultat.
    ///
    /// ÉQUITÉ : les phases 1 à 3 sont IDENTIQUES pour toutes les raretés (même durée, même
    /// mouvement, même son, aucune couleur de rareté) : on ne peut pas deviner le résultat
    /// avant l'ouverture. Ce script ne décide de rien : il affiche.
    ///
    /// Toute l'animation est calculée à partir d'une seule horloge (« t », en secondes depuis
    /// le clic sur « Ouvrir ») : à chaque image, on regarde où on en est et on place tout.
    /// Passer l'animation revient simplement à avancer l'horloge jusqu'au résultat.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class BoxOpeningController : MonoBehaviour
    {
        [SerializeField] PlayerAvatar avatar;
        [SerializeField] PlayerAnimator avatarAnimator;
        [SerializeField] MusicPlayer music;

        /// Les tailles de la spec sont écrites pour 1280 × 720 ; notre interface est réglée
        /// pour 1920 × 1080 : on multiplie donc les pixels de la spec par 1,5.
        const float Px = 1.5f;

        // Les moments de l'animation commune (spec §7.6), en secondes depuis le clic.
        const float ArriveEnd = 0.3f;     // la boîte est arrivée
        const float ShakeEnd = 1.5f;      // fin de la secousse (0,3 + 1,2)
        const float RevealStart = 1.75f;  // fin de l'ouverture (1,5 + 0,25)
        const float SkipAllowedAt = 0.5f; // avant, les clics sont ignorés (double-clic)
        const float SecretBlack = 0.4f;   // écran noir du Secret

        /// Les effets de révélation d'une rareté (tableau §7.6). Pur affichage.
        struct RevealLook
        {
            public float halo, haloAlpha; // diamètre (px spec) et opacité
            public bool pulse;            // le halo bat (1 ↔ 1,1)
            public int rays;              // nombre de rayons (0 = aucun)
            public float raySpeed;        // °/s
            public int sparks;            // nombre d'étincelles
            public float flash;           // opacité du flash coloré (0 = aucun)
            public float shake, shakeTime;// secousse de l'écran (px spec, s)
            public string word;           // « Rare ! »
            public float wordSize;        // taille du texte (px spec)
            public float duration;        // durée de la révélation (pause noire comprise)
            public float cardPeak;        // échelle max de la carte (1,15 ; 1,3 pour le Légendaire)
            public bool jumpingLetters, rainbow, black;
        }

        static RevealLook LookFor(string rarityId) => rarityId switch
        {
            "peu_commun" => new RevealLook { halo = 300, haloAlpha = 0.6f, sparks = 10, word = "Peu commun", wordSize = 24, duration = 0.7f, cardPeak = 1.15f },
            "rare" => new RevealLook { halo = 340, haloAlpha = 0.7f, rays = 8, raySpeed = 30, sparks = 18, flash = 0.3f, word = "Rare !", wordSize = 30, duration = 1.0f, cardPeak = 1.15f },
            "super_rare" => new RevealLook { halo = 380, haloAlpha = 0.75f, pulse = true, rays = 12, raySpeed = 45, sparks = 28, flash = 0.4f, shake = 6, shakeTime = 0.3f, word = "Super rare !", wordSize = 34, duration = 1.4f, cardPeak = 1.15f },
            "legendaire" => new RevealLook { halo = 440, haloAlpha = 0.8f, pulse = true, rays = 16, raySpeed = 60, sparks = 45, flash = 0.6f, shake = 10, shakeTime = 0.4f, word = "LÉGENDAIRE !", wordSize = 40, duration = 2.0f, cardPeak = 1.3f, jumpingLetters = true },
            "secret" => new RevealLook { halo = 480, haloAlpha = 0.85f, pulse = true, rays = 20, raySpeed = 60, sparks = 60, flash = 0.8f, shake = 12, shakeTime = 0.5f, word = "SECRET !!!", wordSize = 44, duration = 2.6f, cardPeak = 1.15f, jumpingLetters = true, rainbow = true, black = true },
            _ => new RevealLook { halo = 260, haloAlpha = 0.5f, word = "Commun", wordSize = 24, duration = 0.5f, cardPeak = 1.15f }
        };

        // ---------- Éléments de l'écran ----------
        VisualElement root, veil, content, stage, result, black, flash;
        VisualElement glow, boxImage, halo, card, cardIcon, word;
        RaysElement rays;
        Label nameLabel, rarityLabel, boxLine, badge;
        Button equipButton, sellButton, reopenButton, okButton;
        readonly List<Label> letters = new List<Label>();

        // ---------- État de l'animation en cours ----------
        BoxResult current;
        RevealLook look;
        float t;                 // l'horloge de l'animation
        float cardStart, revealEnd, resultStart;
        bool busy, skipped, resultShown;
        bool playedShake, playedOpen, playedReveal, sparksDone;
        float rayAngle;
        int closedFrame = -1;

        // ---------- Étincelles : une réserve de 60 éléments réutilisés ----------
        const int SparkPool = 60;
        class Spark { public VisualElement element; public Vector2 pos, vel; public float age, life; public bool alive; }
        readonly Spark[] sparks = new Spark[SparkPool];
        readonly System.Random fxRandom = new System.Random(); // hasard visuel, sans lien avec le tirage

        // Animations d'apparition des boutons : (bouton, heure de départ)
        readonly List<(Button button, float start)> appearing = new List<(Button, float)>();

        /// Une ouverture est en cours (animation ou écran de résultat).
        public bool IsBusy => busy;
        /// Échap est « pris » par cet écran (pendant l'ouverture, et l'image où il se ferme).
        public bool BlocksEscape => busy || closedFrame == Time.frameCount;
        /// L'écran de résultat est affiché (pour les tests).
        public bool ResultShown => resultShown;
        /// L'horloge de l'animation (pour les tests).
        public float Clock => t;
        public BoxResult Current => current;

        /// Prévient le shop que l'écran est fermé (« OK »).
        public event Action Closed;

        void OnEnable()
        {
            var doc = GetComponent<UIDocument>().rootVisualElement;
            root = doc.Q<VisualElement>("ov-root");
            veil = doc.Q<VisualElement>("ov-veil");
            content = doc.Q<VisualElement>("ov-content");
            stage = doc.Q<VisualElement>("ov-stage");
            result = doc.Q<VisualElement>("ov-result");
            black = doc.Q<VisualElement>("ov-black");
            flash = doc.Q<VisualElement>("ov-flash");
            nameLabel = doc.Q<Label>("ov-name");
            rarityLabel = doc.Q<Label>("ov-rarity");
            boxLine = doc.Q<Label>("ov-boxline");
            badge = doc.Q<Label>("ov-badge");
            equipButton = doc.Q<Button>("ov-equip");
            sellButton = doc.Q<Button>("ov-sell");
            reopenButton = doc.Q<Button>("ov-reopen");
            okButton = doc.Q<Button>("ov-ok");

            BuildStage();

            equipButton.clicked += OnEquip;
            sellButton.clicked += OnSell;
            reopenButton.clicked += OnReopen;
            okButton.clicked += Close;
            root.RegisterCallback<PointerDownEvent>(OnPointerDown);
            UiSounds.Register(root);
            root.style.display = DisplayStyle.None;
        }

        void OnDisable()
        {
            equipButton.clicked -= OnEquip;
            sellButton.clicked -= OnSell;
            reopenButton.clicked -= OnReopen;
            okButton.clicked -= Close;
            root.UnregisterCallback<PointerDownEvent>(OnPointerDown);
            UiSounds.Unregister(root);
        }

        /// Crée les effets autour du centre de la scène (une seule fois).
        void BuildStage()
        {
            stage.Clear();
            // Dans l'ordre d'affichage : la lueur et le halo derrière, la carte devant.
            glow = Centered(UiFxTextures.Halo);
            halo = Centered(UiFxTextures.Halo);
            rays = new RaysElement();
            rays.AddToClassList("ov-centered");
            stage.Add(rays);
            boxImage = Centered(null);

            card = new VisualElement { pickingMode = PickingMode.Ignore };
            card.AddToClassList("ov-card");
            cardIcon = new VisualElement { pickingMode = PickingMode.Ignore };
            cardIcon.AddToClassList("ov-card-icon");
            card.Add(cardIcon);
            word = new VisualElement { pickingMode = PickingMode.Ignore };
            word.AddToClassList("ov-rarity-word");
            card.Add(word);
            stage.Add(card);

            for (int i = 0; i < SparkPool; i++)
            {
                var e = new VisualElement { pickingMode = PickingMode.Ignore };
                e.AddToClassList("ov-spark");
                e.style.backgroundImage = new StyleBackground(UiFxTextures.Sparkle);
                e.style.display = DisplayStyle.None;
                stage.Add(e);
                sparks[i] = new Spark { element = e };
            }
        }

        VisualElement Centered(Texture2D texture)
        {
            var e = new VisualElement { pickingMode = PickingMode.Ignore };
            e.AddToClassList("ov-centered");
            if (texture != null) e.style.backgroundImage = new StyleBackground(texture);
            stage.Add(e);
            return e;
        }

        /// Place un élément centré sur le point de la scène, avec une taille en px de référence.
        static void Size(VisualElement e, float size)
        {
            e.style.width = size;
            e.style.height = size;
            e.style.left = -size * 0.5f;
            e.style.top = -size * 0.5f;
        }

        // =====================================================================
        // Lancer une ouverture
        // =====================================================================

        /// Lance l'animation pour un résultat déjà tiré et sauvegardé.
        public void Play(BoxResult r)
        {
            current = r;
            look = LookFor(r.Rarity != null ? r.Rarity.id : "commun");
            t = 0f;
            skipped = resultShown = false;
            playedShake = playedOpen = playedReveal = sparksDone = false;
            busy = true;
            rayAngle = 0f;
            cardStart = RevealStart + (look.black ? SecretBlack : 0f);
            revealEnd = RevealStart + look.duration;
            resultStart = revealEnd + 0.2f;

            root.style.display = DisplayStyle.Flex;
            result.style.display = DisplayStyle.None;
            appearing.Clear();
            foreach (var s in sparks) { s.alive = false; s.element.style.display = DisplayStyle.None; }

            // La boîte (image du catalogue) et sa lueur blanche neutre (aucune couleur de rareté).
            boxImage.style.backgroundImage = r.Box.icon != null ? new StyleBackground(r.Box.icon) : StyleKeyword.None;
            Size(boxImage, 300 * Px);
            Size(glow, 300 * Px);
            glow.style.unityBackgroundImageTintColor = Color.white;

            // La carte de l'objet, préparée mais cachée jusqu'à la révélation.
            cardIcon.style.backgroundImage = r.Item.Icon != null ? new StyleBackground(r.Item.Icon) : StyleKeyword.None;
            if (r.Item.Icon == null) cardIcon.style.backgroundColor = r.Item.Color;
            else cardIcon.style.backgroundColor = StyleKeyword.Null;
            BuildWord();
            card.style.left = -165f; // moitié de la largeur de l'icône (330 px)
            card.style.top = -165f;
            Size(halo, look.halo * Px);
            Size(rays, look.halo * Px * 1.1f);
            Color[] rayColors = look.rainbow
                ? new[] { 0f, 1f / 6f, 2f / 6f, 3f / 6f, 4f / 6f, 5f / 6f }.Select(h => WithAlpha(Hue(h), 0.45f)).ToArray()
                : new[] { WithAlpha(r.Rarity.color, 0.45f) };
            rays.Configure(look.rays, rayColors);

            // Pendant toute l'animation, la musique est atténuée (comme pour les résultats).
            if (music != null) music.Duck(resultStart);
            Play(Sfx.PiecesAchat);
            Render();
        }

        /// Le texte de rareté : une lettre par Label, pour les faire sauter une à une.
        void BuildWord()
        {
            word.Clear();
            letters.Clear();
            foreach (char c in look.word)
            {
                if (c == ' ')
                {
                    // Une espace seule dans un Label ne prend aucune place : on met un petit vide.
                    var gap = new VisualElement { pickingMode = PickingMode.Ignore };
                    gap.style.width = look.wordSize * Px * 0.3f;
                    word.Add(gap);
                    continue;
                }
                var l = new Label(c.ToString()) { pickingMode = PickingMode.Ignore };
                l.AddToClassList("ov-letter");
                l.style.fontSize = look.wordSize * Px;
                l.style.color = current.Rarity != null ? current.Rarity.textColor : Color.white;
                word.Add(l);
                letters.Add(l);
            }
        }

        // =====================================================================
        // L'horloge
        // =====================================================================

        void Update()
        {
            if (!busy) return;

            var kb = Keyboard.current;
            if (kb != null)
            {
                bool esc = kb.escapeKey.wasPressedThisFrame, space = kb.spaceKey.wasPressedThisFrame;
                if (resultShown && esc) { Close(); return; }   // Échap = « OK »
                if (!resultShown && (esc || space)) TrySkip();
            }

            float dt = Time.deltaTime;
            t += dt;
            rayAngle += look.raySpeed * (t > revealEnd ? 0.5f : 1f) * dt; // plus doux sur le résultat
            UpdateSparks(dt);
            Render();
            UpdateButtons();
        }

        void OnPointerDown(PointerDownEvent evt)
        {
            if (busy && !resultShown) TrySkip();
        }

        /// Passer l'animation : seulement à partir de 0,5 s après le clic sur « Ouvrir ».
        /// La carte apparaît directement avec son halo et ses rayons, sans étincelles.
        public void TrySkip()
        {
            if (!busy || resultShown || t < SkipAllowedAt) return;
            skipped = true;
            sparksDone = true;
            playedShake = playedOpen = true; // on ne joue plus les sons de la boîte
            if (!playedReveal) { playedReveal = true; PlayReveal(); } // on entend quand même la révélation
            t = Mathf.Max(t, resultStart);
            Render();
        }

        /// Place tout selon l'horloge t.
        void Render()
        {
            // --- Voile : apparaît en 0,2 s ---
            veil.style.opacity = Mathf.Clamp01(t / 0.2f);

            // --- Sons des phases communes (identiques pour toutes les raretés) ---
            if (!playedShake && t >= ArriveEnd) { playedShake = true; Play(Sfx.BoiteSecoue); }
            if (!playedOpen && t >= ShakeEnd) { playedOpen = true; Play(Sfx.BoiteOuverture); }
            if (!playedReveal && t >= RevealStart) { playedReveal = true; PlayReveal(); }

            // --- La boîte : arrivée, secousse, ouverture ---
            bool boxVisible = !skipped && t < RevealStart;
            boxImage.style.display = boxVisible ? DisplayStyle.Flex : DisplayStyle.None;
            glow.style.display = boxVisible && t >= ArriveEnd ? DisplayStyle.Flex : DisplayStyle.None;
            if (boxVisible)
            {
                float scale = 1f, rotation = 0f, hop = 0f, opacity = 1f;
                if (t < ArriveEnd) scale = Ease.Pop(t / ArriveEnd, 0f, 1.1f);
                else if (t < ShakeEnd)
                {
                    // Rotation gauche/droite à 10 allers-retours par seconde, amplitude 2° → 10°.
                    float s = t - ArriveEnd;
                    float amplitude = Mathf.Lerp(2f, 10f, s / (ShakeEnd - ArriveEnd));
                    rotation = amplitude * Mathf.Sin(2f * Mathf.PI * 10f * s);
                    // Petits sauts de 12 px toutes les 0,4 s (montée et descente en 0,2 s).
                    float phase = Mathf.Repeat(s, 0.4f) / 0.2f;
                    hop = phase < 1f ? -12f * Px * Mathf.Sin(Mathf.PI * phase) : 0f;
                    // Lueur blanche neutre qui grandit derrière la boîte.
                    float g = s / (ShakeEnd - ArriveEnd);
                    glow.style.scale = new Scale(Vector3.one * Mathf.Lerp(0.8f, 1.6f, g));
                    glow.style.opacity = Mathf.Lerp(0.2f, 0.7f, g);
                }
                else
                {
                    // Ouverture : la boîte grossit (1 → 1,25) et s'efface.
                    float k = (t - ShakeEnd) / (RevealStart - ShakeEnd);
                    scale = Mathf.Lerp(1f, 1.25f, k);
                    opacity = 1f - k;
                    glow.style.opacity = 0.7f * (1f - k);
                }
                boxImage.style.scale = new Scale(new Vector3(scale, scale, 1f));
                boxImage.style.rotate = new Rotate(rotation);
                boxImage.style.translate = new Translate(0f, hop);
                boxImage.style.opacity = opacity;
            }

            // --- Flash : blanc à l'ouverture (0 → 0,7 → 0), puis coloré à la révélation ---
            float flashAlpha = 0f;
            Color flashColor = Color.white;
            if (!skipped && t >= ShakeEnd && t < RevealStart)
                flashAlpha = 0.7f * Mathf.Sin(Mathf.PI * (t - ShakeEnd) / (RevealStart - ShakeEnd));
            else if (!skipped && look.flash > 0f && t >= cardStart && t < cardStart + 0.35f)
            {
                flashAlpha = look.flash * (1f - (t - cardStart) / 0.35f);
                flashColor = look.rainbow ? Color.white : current.Rarity.color;
            }
            flash.style.backgroundColor = flashColor;
            flash.style.opacity = flashAlpha;

            // --- Écran noir du Secret (0,4 s avec « … ») ---
            black.style.display = !skipped && look.black && t >= RevealStart && t < cardStart
                ? DisplayStyle.Flex : DisplayStyle.None;

            // --- La carte, le halo, les rayons ---
            bool cardVisible = t >= cardStart;
            card.style.display = cardVisible ? DisplayStyle.Flex : DisplayStyle.None;
            halo.style.display = cardVisible ? DisplayStyle.Flex : DisplayStyle.None;
            rays.style.display = cardVisible && look.rays > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            if (cardVisible)
            {
                float since = t - cardStart;
                float cardScale = skipped ? 1f : CardScale(since);
                card.style.scale = new Scale(new Vector3(cardScale, cardScale, 1f));

                Color haloColor = look.rainbow ? Hue(t / 2f) : current.Rarity.color; // Secret : la teinte fait le tour en 2 s
                haloColor.a = look.haloAlpha;
                halo.style.unityBackgroundImageTintColor = haloColor;
                float pulse = look.pulse ? 1f + 0.05f * (1f - Mathf.Cos(2f * Mathf.PI * t / 1.2f)) : 1f;
                float appear = skipped ? 1f : Mathf.Clamp01(since / 0.3f);
                halo.style.scale = new Scale(Vector3.one * pulse * Mathf.Lerp(0.6f, 1f, appear));
                halo.style.opacity = appear;
                rays.style.rotate = new Rotate(rayAngle);
                rays.style.opacity = appear;

                // Lettres : elles sautent une à une (Légendaire, Secret) ; arc-en-ciel pour le Secret.
                for (int i = 0; i < letters.Count; i++)
                {
                    float jump = 0f;
                    if (look.jumpingLetters && !skipped)
                    {
                        float k = (since - 0.3f - i * 0.07f) / 0.25f;
                        if (k > 0f && k < 1f) jump = -16f * Px * Mathf.Sin(Mathf.PI * k);
                    }
                    letters[i].style.translate = new Translate(0f, jump);
                    if (look.rainbow) letters[i].style.color = Hue(t / 2f + i / (float)letters.Count);
                }

                // Étincelles : elles jaillissent une fois, au début de la révélation.
                if (!sparksDone) { sparksDone = true; EmitSparks(); }
            }

            // --- Secousse de l'écran pendant la révélation ---
            float shakeX = 0f, shakeY = 0f;
            if (!skipped && look.shake > 0f && t >= cardStart && t < cardStart + look.shakeTime)
            {
                float k = 1f - (t - cardStart) / look.shakeTime; // s'amortit
                shakeX = look.shake * Px * k * Mathf.Sin(2f * Mathf.PI * 25f * t);
                shakeY = look.shake * Px * k * Mathf.Cos(2f * Mathf.PI * 21f * t) * 0.6f;
            }
            content.style.translate = new Translate(shakeX, shakeY);

            // --- L'écran de résultat, 0,2 s après la fin de la révélation ---
            if (!resultShown && t >= resultStart) ShowResult();
        }

        /// Échelle de la carte : 0,5 → pic (1,15 ou 1,3) → 1 en 0,3 s (0,6 s pour le Légendaire).
        float CardScale(float since)
        {
            float duration = look.cardPeak > 1.2f ? 0.6f : 0.3f;
            return since >= duration ? 1f : Ease.Pop(since / duration, 0.5f, look.cardPeak);
        }

        void PlayReveal()
        {
            string id = current.Rarity != null ? current.Rarity.id : "commun";
            Sfx sfx = id switch
            {
                "peu_commun" => Sfx.RevelationPeuCommun,
                "rare" => Sfx.RevelationRare,
                "super_rare" => Sfx.RevelationSuperRare,
                "legendaire" => Sfx.RevelationLegendaire,
                "secret" => Sfx.RevelationSecret,
                _ => Sfx.RevelationCommun
            };
            Play(sfx);
        }

        // =====================================================================
        // Étincelles
        // =====================================================================

        void EmitSparks()
        {
            for (int i = 0; i < look.sparks && i < SparkPool; i++)
            {
                var s = sparks[i];
                float angle = Range(0f, Mathf.PI * 2f);
                float speed = Range(200f, 500f) * Px;
                s.pos = Vector2.zero;
                s.vel = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed;
                s.age = 0f;
                s.life = Range(0.8f, 1.2f);
                s.alive = true;
                float size = Range(16f, 28f) * Px;
                s.element.style.width = size;
                s.element.style.height = size;
                s.element.style.display = DisplayStyle.Flex;
                s.element.style.unityBackgroundImageTintColor = SparkColor(i);
            }
        }

        Color SparkColor(int i)
        {
            if (look.rainbow) return Hue(i / 7f);
            if (current.Rarity != null && current.Rarity.id == "legendaire")
                return i % 2 == 0 ? current.Rarity.color : new Color(1f, 0.784f, 0.227f); // orange / or #FFC83A
            return current.Rarity != null ? current.Rarity.color : Color.white;
        }

        void UpdateSparks(float dt)
        {
            const float gravity = 600f * Px; // px/s² (vers le bas)
            foreach (var s in sparks)
            {
                if (!s.alive) continue;
                s.age += dt;
                if (s.age >= s.life) { s.alive = false; s.element.style.display = DisplayStyle.None; continue; }
                s.vel.y += gravity * dt;
                s.pos += s.vel * dt;
                float size = s.element.resolvedStyle.width;
                s.element.style.left = s.pos.x - size * 0.5f;
                s.element.style.top = s.pos.y - size * 0.5f;
                s.element.style.opacity = 1f - Mathf.Clamp01((s.age - s.life * 0.6f) / (s.life * 0.4f));
            }
        }

        // =====================================================================
        // Écran de résultat (§7.7)
        // =====================================================================

        void ShowResult()
        {
            resultShown = true;
            var item = current.Item;
            nameLabel.text = item.DisplayName;
            rarityLabel.text = current.Rarity != null ? current.Rarity.displayName : "";
            rarityLabel.style.color = current.Rarity != null ? current.Rarity.textColor : Color.white;
            boxLine.text = $"{current.Box.displayName} · n°{Shop.NumberOf(item):00}";
            badge.RemoveFromClassList("ov-badge--new");
            badge.RemoveFromClassList("ov-badge--dup");
            if (current.IsNew) { badge.text = "Nouveau !"; badge.AddToClassList("ov-badge--new"); }
            else { badge.text = $"Doublon — tu en as {current.Count}"; badge.AddToClassList("ov-badge--dup"); }
            result.style.display = DisplayStyle.Flex;

            RefreshButtonLabels();
            // Les boutons apparaissent l'un après l'autre (0,08 s), cliquables à la fin.
            int order = 0;
            foreach (var b in new[] { equipButton, sellButton, reopenButton, okButton })
            {
                if (b.style.display == DisplayStyle.None) continue;
                b.pickingMode = PickingMode.Ignore;
                b.style.opacity = 0f;
                b.style.scale = new Scale(new Vector3(0.6f, 0.6f, 1f));
                appearing.Add((b, t + 0.08f * order++));
            }
        }

        /// Met à jour les boutons selon l'état actuel de la collection et du porte-monnaie.
        void RefreshButtonLabels()
        {
            var item = current.Item;
            // « Équiper » : visible si l'objet n'est pas déjà équipé ; devient « Équipé ✓ ».
            bool equipped = Shop.IsEquipped(item);
            equipButton.text = equipped ? "Équipé ✓" : "Équiper";
            equipButton.SetEnabled(!equipped);
            // « Vendre (+X) » : seulement s'il y a au moins 2 exemplaires.
            bool canSell = Shop.CanSellDuplicate(item);
            sellButton.style.display = canSell ? DisplayStyle.Flex : DisplayStyle.None;
            sellButton.text = $"Vendre (+{Shop.SellValue(item)})";
            // « Rouvrir — 100 » : toujours, grisé si pas assez de pièces.
            reopenButton.text = $"Rouvrir — {current.Box.price}";
            reopenButton.SetEnabled(Shop.CanOpen(current.Box));
            // Badge : le nombre d'exemplaires peut changer après une vente.
            if (!current.IsNew) badge.text = $"Doublon — tu en as {Shop.CountOf(item)}";
        }

        /// Animation d'apparition des boutons (même que le HUD, sons-animations §3.10).
        void UpdateButtons()
        {
            for (int i = appearing.Count - 1; i >= 0; i--)
            {
                var (b, start) = appearing[i];
                float k = (t - start) / 0.25f;
                if (k < 0f) continue;
                if (k >= 1f)
                {
                    b.style.opacity = 1f;
                    b.style.scale = StyleKeyword.Null;
                    b.pickingMode = PickingMode.Position; // cliquable à la fin de l'animation
                    appearing.RemoveAt(i);
                    continue;
                }
                float s = Ease.Pop(k, 0.6f, 1.08f);
                b.style.opacity = Mathf.Clamp01(k / 0.7f);
                b.style.scale = new Scale(new Vector3(s, s, 1f));
            }
        }

        void OnEquip()
        {
            if (!Shop.TryEquip(current.Item)) return;
            if (avatar != null) avatar.ShowEquipped();
            if (avatarAnimator != null) avatarAnimator.WelcomeHop();
            RefreshButtonLabels();
        }

        void OnSell()
        {
            if (!Shop.TrySellDuplicate(current.Item)) return;
            Play(Sfx.Vente);
            RefreshButtonLabels();
        }

        /// « Rouvrir » : la règle est dans Shop.TryOpenBox ; si elle accepte, on recommence
        /// l'animation au temps 0 avec le nouveau résultat.
        void OnReopen()
        {
            if (!resultShown) return;
            if (Shop.TryOpenBox(current.Box, out var r)) Play(r);
        }

        /// « OK » (ou Échap) : on ferme l'écran, retour à l'onglet Boutique.
        public void Close()
        {
            if (!busy) return;
            busy = false;
            resultShown = false;
            closedFrame = Time.frameCount;
            root.style.display = DisplayStyle.None;
            Closed?.Invoke();
        }

        // =====================================================================
        // Outils
        // =====================================================================

        static void Play(Sfx id)
        {
            if (SoundManager.Instance != null) SoundManager.Instance.Play(id);
        }

        static Color Hue(float h) => Color.HSVToRGB(Mathf.Repeat(h, 1f), 0.7f, 1f);

        static Color WithAlpha(Color c, float a) { c.a = a; return c; }

        float Range(float min, float max) => min + (float)fxRandom.NextDouble() * (max - min);

        /// OUTIL DE TEST : simule un clic sur un bouton de l'écran de résultat.
        public void DebugClick(string button)
        {
            switch (button)
            {
                case "equip": OnEquip(); break;
                case "sell": OnSell(); break;
                case "reopen": OnReopen(); break;
                case "ok": Close(); break;
            }
        }
    }
}
