using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TourDeLaChance
{
    /// <summary>
    /// Les petites animations des ACCESSOIRES v5 et des « extras » de skins (art bible §13,
    /// catalogue v5). Pur affichage, jamais avant une révélation : il ne réagit qu'aux moments
    /// déjà affichés (saut de joie du cheval).
    ///
    /// Il est posé automatiquement sur chaque accessoire affiché (cheval du joueur, cheval vierge,
    /// studio d'ouverture) et sur les extras des skins. Il cherche dans le modèle les pièces
    /// nommées par l'artiste 3D (l'origine de chaque pièce est sur sa charnière) et applique une
    /// RÈGLE selon le nom (table de ChooseRule) :
    ///   - Anim_* sans règle particulière : RESSORT (mouvement secondaire) : la pièce penche à
    ///     l'opposé du mouvement de la tête puis revient en rebondissant ;
    ///   - Orbite / Orbite_N / Anim_OrbitePoisson* : tournent sur leur Y local ;
    ///   - tentacules en chaîne (8 segments), flammes, roue du paon, baleine sur un 8, aiguille
    ///     de boussole, voiles, ailes, yeux qui clignent, respiration, éclats (Eclat_*)…
    ///   - matériaux lumineux (rubis, bioluminescence…) : émission qui oscille.
    /// Les anciens noms (AileGauche, Anneau, Perle_*, Orbite de la planète…) restent animés par
    /// AccessoryMotion. Les points Fx_* (particules) sont gardés pour plus tard.
    /// </summary>
    public class AccessoryRig : MonoBehaviour
    {
        static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        enum Kind
        {
            Spring, Sway, SpinY, SpinZ, Needle, Breath, Sniff, Blink, Sparkle, Tentacle, Flame,
            PaonFeather, Whale, JoyFlap, BatWing, Vibrate, Sail, HeadFollow, FlowerSpin, Flower,
            Heart, Rays, Moon, Coin, JoyRise, JoyPaw, Shiver, Wobble
        }

        class Part
        {
            public Transform t;
            public Kind kind;
            public Vector3 restPos, restScale, axis = Vector3.forward;
            public Quaternion restRot;
            public float amp, period, rate, phase, side = 1f;
            public int index;
            public Vector2 angle, speed;    // ressort
            public float nextEvent;         // clignement, frémissement
        }

        readonly List<Part> parts = new List<Part>();
        readonly List<(Renderer r, int index, Color color, float min, float max, float period, float phase)> glows =
            new List<(Renderer, int, Color, float, float, float, float)>();
        MaterialPropertyBlock block;
        Vector3 lastPos, lastVel;
        float time;
        string owner;                // l'identifiant de l'objet (« perroquet », « animal_paon_extras »…)
        PlayerAnimator animator;     // le cheval du joueur (saut de joie), null pour une vitrine
        Transform horseRoot;         // le personnage (pour la trajectoire en 8 de la baleine)
        bool wasJoy;
        float joyTime = -10f;
        readonly System.Random fxRandom = new System.Random(); // hasard VISUEL seulement

        /// Le son du saut de joie de certains objets (audio/README « Sons v5 »).
        static readonly Dictionary<string, Sfx> JoySounds = new Dictionary<string, Sfx>
        {
            { "perroquet", Sfx.PerroquetCri }, { "kraken_bebe", Sfx.KrakenBebe }, { "navire_fantome", Sfx.NavireCanon },
            { "animal_paon_extras", Sfx.PaonRoue }, { "animal_lion_soleil_extras", Sfx.LionRugit },
            { "poussin", Sfx.PoussinPiou }, { "chaton_sac", Sfx.ChatonMiaou }, { "baleine_celeste", Sfx.BaleineChant }
        };

        void Awake()
        {
            block = new MaterialPropertyBlock();
            owner = gameObject.name.Replace("(Clone)", "").Trim();
            animator = GetComponentInParent<PlayerAnimator>();
            var avatar = GetComponentInParent<PlayerAvatar>();
            horseRoot = avatar != null ? avatar.transform : transform.parent;
            int flower = 0, sparkle = 0;
            foreach (var t in GetComponentsInChildren<Transform>(true))
            {
                if (t == transform) continue;
                var p = ChooseRule(t, ref flower, ref sparkle);
                if (p == null) continue;
                p.t = t;
                p.restPos = t.localPosition;
                p.restRot = t.localRotation;
                p.restScale = t.localScale;
                p.nextEvent = Range(1f, 4f);
                parts.Add(p);
            }
            CollectGlows();
            lastPos = transform.position;
            if (parts.Count + glows.Count == 0) enabled = false;
        }

        float Range(float a, float b) => a + (float)fxRandom.NextDouble() * (b - a);

        static int TrailingNumber(string name)
        {
            int i = name.Length;
            while (i > 0 && char.IsDigit(name[i - 1])) i--;
            return i < name.Length && int.TryParse(name.Substring(i), out int n) ? n : 0;
        }

        /// LA TABLE DES RÈGLES : quel mouvement pour quelle pièce (d'après les en-têtes des
        /// scripts blender/pirate_*.py et animal_*.py).
        Part ChooseRule(Transform t, ref int flower, ref int sparkle)
        {
            string n = t.name;
            int num = TrailingNumber(n);
            bool left = n.Contains("Gauche");
            // Orbites : le navire fantôme (+51,4°/s = un tour en 7 s), la ruche, les poissons.
            if (n == "Orbite" && owner == "navire_fantome") return new Part { kind = Kind.SpinY, rate = 51.4f };
            if (n.StartsWith("Orbite_")) return new Part { kind = Kind.SpinY, rate = num == 1 ? 144f : num == 2 ? -120f : 90f };
            if (n.StartsWith("Anim_OrbitePoisson_")) return new Part { kind = Kind.SpinY, rate = num == 1 ? 40f : num == 2 ? -55f : 70f };
            if (n == "Anim_OrbitePoisson") return new Part { kind = Kind.SpinY, rate = 90f }; // bocal : un tour en 4 s
            if (n == "Anim_Aiguille") return new Part { kind = Kind.Needle, axis = new Vector3(0f, 0.33f, -0.94f).normalized };
            if (n == "Anim_Tangage") return new Part { kind = Kind.Sway, amp = 8f, period = 1.5f, axis = Vector3.forward };
            if (n.StartsWith("Anim_Voile")) return new Part { kind = Kind.Sail, period = 2f, phase = Range(0f, 1f) };
            if (n == "Anim_Flamme") return new Part { kind = Kind.Sway, amp = 10f, period = 0.9f, axis = Vector3.up };
            if (n.StartsWith("Anim_Flamme_")) return new Part { kind = Kind.Flame, period = 0.6f + 0.5f * ((num * 37) % 10) / 10f, phase = num * 0.37f };
            if (n == "Anim_DisqueSolaire") return new Part { kind = Kind.SpinZ, rate = 10f };
            // Plumes 01 à 13 → i = 0 à 12 : l'éventail va de +84° à −84°, bien symétrique.
            if (n.StartsWith("Anim_Plume_")) return new Part { kind = Kind.PaonFeather, index = num - 1 };
            if (n.StartsWith("Anim_Tentacule") && num > 0)
                return new Part { kind = Kind.Tentacle, index = num, amp = 15f, period = 2.2f, phase = (n.GetHashCode() & 0xFFFF) / 65535f * 6.28f };
            if (n.StartsWith("Anim_Tentacule")) return new Part { kind = Kind.Sway, amp = 15f, period = 1.8f, axis = Vector3.right, phase = left ? 0f : 0.9f };
            if (n == "Anim_Baleine") return new Part { kind = Kind.Whale };
            if (n == "Anim_Queue" && owner == "baleine_celeste") return new Part { kind = Kind.Sway, amp = 20f, period = 1.2f, axis = Vector3.up };
            if (n.StartsWith("Anim_Nageoire")) return new Part { kind = Kind.Sway, amp = 15f, period = 1.2f, axis = Vector3.forward, phase = left ? 0f : 0.3f };
            if (n == "Anim_QueuePoisson") return new Part { kind = Kind.Sway, amp = 20f, period = 0.8f, axis = Vector3.up };
            if (n == "Anim_Algues") return new Part { kind = Kind.Sway, amp = 6f, period = 2.6f, axis = Vector3.forward };
            if (n == "Anim_Eau") return new Part { kind = Kind.Wobble, amp = 0.03f, period = 2f };
            if (n.StartsWith("Anim_Oeil") || n.StartsWith("Anim_Yeux") || n.StartsWith("Anim_Paupiere")) return new Part { kind = Kind.Blink };
            if (n == "Anim_Corps") return new Part { kind = Kind.Breath, amp = 0.04f, period = 2f };
            if (n == "Anim_Truffe") return new Part { kind = Kind.Sniff, period = 4f };
            if (n.StartsWith("Anim_Moustaches")) return new Part { kind = Kind.Shiver, period = 3f, side = left ? 1f : -1f };
            if (n.StartsWith("Anim_Ailes_")) return new Part { kind = Kind.Vibrate };
            if (n.StartsWith("Anim_Abeille_")) return new Part { kind = Kind.Sway, amp = 10f, period = 0.8f, axis = Vector3.forward, phase = num * 0.3f };
            if (n.StartsWith("Anim_Aile"))
            {
                if (owner == "ailes_chauve_souris") return new Part { kind = Kind.BatWing, side = left ? 1f : -1f };
                return new Part { kind = Kind.JoyFlap, side = left ? 1f : -1f };
            }
            if (n.StartsWith("Anim_Fleur_"))
            {
                if (owner == "bois_cerf") return new Part { kind = Kind.FlowerSpin, rate = num % 2 == 0 ? -20f : 20f };
                return new Part { kind = Kind.Flower, phase = 0.12f * flower++ };
            }
            if (n.StartsWith("Anim_Coeur")) return new Part { kind = Kind.Heart, phase = n.EndsWith("Droit") ? 0.1f : 0f };
            if (n.StartsWith("Anim_Rayons")) return new Part { kind = Kind.Rays };
            if (n.StartsWith("Anim_OrbiteLune")) return new Part { kind = Kind.Moon };
            if (n == "Anim_Tete" || n == "Anim_TeteChaton") return new Part { kind = Kind.HeadFollow, amp = 25f };
            if (n == "Anim_Chaton" || n == "Anim_Poussin") return new Part { kind = Kind.JoyRise, amp = n == "Anim_Poussin" ? 0.1f : 0.06f };
            if (n == "Anim_PatteChaton") return new Part { kind = Kind.JoyPaw };
            if (n == "Anim_Piece") return new Part { kind = Kind.Coin };
            if (n.StartsWith("Anim_Sourcil")) return null; // ils se haussent au survol : plus tard
            if (n.StartsWith("Eclat_")) return new Part { kind = Kind.Sparkle, phase = 0.4f * sparkle++ };
            if (n.StartsWith("Anim_")) return new Part { kind = Kind.Spring };
            return null;
        }

        /// Les matériaux lumineux qui « respirent » : (début du nom, émission min, max, période).
        static readonly (string name, float min, float max, float period)[] GlowRules =
        {
            ("M5_Rubis", 0.2f, 0.5f, 2f), ("M5_TachePapillon", 0.2f, 0.5f, 2f),
            ("M5_PirBiolum", 0f, 1.3f, 3f), ("M5_PirKrakenLueur", 0.6f, 0.9f, 2f),
            ("M5_PirOrLuit", 0.3f, 0.6f, 1.6f), ("M5_PirAncreLuit", 0.5f, 0.8f, 2f)
        };

        void CollectGlows()
        {
            foreach (var r in GetComponentsInChildren<Renderer>(true))
            {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    if (mats[i] == null) continue;
                    foreach (var g in GlowRules)
                    {
                        if (!mats[i].name.StartsWith(g.name)) continue;
                        // (L'émission est activée dans le fichier du matériau : on ne modifie
                        // jamais un matériau partagé pendant le jeu.)
                        Color c = mats[i].HasProperty("_BaseColor") ? mats[i].GetColor("_BaseColor") : Color.white;
                        glows.Add((r, i, c, g.min, g.max, g.period, glows.Count * 0.7f));
                    }
                }
            }
        }

        void LateUpdate()
        {
            float dt = Mathf.Max(Time.deltaTime, 0.0001f);
            time += dt;

            // Le saut de joie du cheval (seulement le cheval du joueur) : départ des réactions.
            bool joy = animator != null && animator.IsJoyJumping;
            if (joy && !wasJoy)
            {
                joyTime = 0f;
                if (JoySounds.TryGetValue(owner, out var sfx) && SoundManager.Instance != null) SoundManager.Instance.Play(sfx);
            }
            wasJoy = joy;
            joyTime += dt;

            // Le ressort suit l'accélération de l'objet (la tête du cheval qui saute).
            Vector3 vel = (transform.position - lastPos) / dt;
            Vector3 accel = (vel - lastVel) / dt;
            lastPos = transform.position;
            lastVel = vel;
            Vector3 localAccel = transform.InverseTransformDirection(accel);

            foreach (var p in parts) Animate(p, dt, localAccel);

            foreach (var (r, index, color, min, max, period, phase) in glows)
            {
                float e = Mathf.Lerp(min, max, 0.5f + 0.5f * Mathf.Sin((time + phase) * Mathf.PI * 2f / period));
                r.GetPropertyBlock(block, index);
                block.SetColor(EmissionColorId, color * e);
                r.SetPropertyBlock(block, index);
            }
        }

        static float Wave(float t, float period) => Mathf.Sin(t * Mathf.PI * 2f / Mathf.Max(0.01f, period));

        void Animate(Part p, float dt, Vector3 localAccel)
        {
            float u = time + p.phase;
            switch (p.kind)
            {
                case Kind.Spring:
                {
                    Vector2 target = new Vector2(Mathf.Clamp(localAccel.z * 2.5f, -25f, 25f), Mathf.Clamp(-localAccel.x * 2.5f, -25f, 25f));
                    target += new Vector2(2f * Mathf.Sin(time * 2.1f), 1.5f * Mathf.Sin(time * 1.7f + 1f));
                    p.speed += (60f * (target - p.angle) - 7f * p.speed) * dt; // raideur 60, amortissement 7
                    p.angle += p.speed * dt;
                    p.t.localRotation = p.restRot * Quaternion.Euler(p.angle.x, 0f, p.angle.y);
                    break;
                }
                case Kind.Sway: p.t.localRotation = p.restRot * Quaternion.AngleAxis(p.amp * Wave(u, p.period), p.axis); break;
                case Kind.SpinY: p.t.localRotation = p.restRot * Quaternion.Euler(0f, p.rate * time, 0f); break;
                case Kind.SpinZ:
                {
                    // Le disque solaire fait un tour rapide au saut de joie (0,6 s).
                    float extra = joyTime < 0.6f ? 360f * Ease.InOut(joyTime / 0.6f) : 0f;
                    p.t.localRotation = p.restRot * Quaternion.Euler(0f, 0f, p.rate * time + extra);
                    break;
                }
                case Kind.Needle:
                    // L'aiguille cherche le nord : elle oscille, puis repart (axe donné par l'artiste).
                    p.t.localRotation = Quaternion.AngleAxis(25f * Wave(time, 2.3f) + 10f * Wave(time, 0.7f) + (Mathf.Repeat(time, 9f) < 1f ? 360f * Mathf.Repeat(time, 9f) : 0f), p.axis) * p.restRot;
                    break;
                case Kind.Breath: p.t.localScale = p.restScale * (1f + p.amp * (0.5f + 0.5f * Wave(time, p.period))); break;
                case Kind.Sniff:
                {
                    float k = Mathf.Repeat(time, p.period);
                    p.t.localScale = p.restScale * (k < 0.3f ? 1f + 0.06f * Mathf.Sin(k / 0.3f * Mathf.PI * 2f) * 0.5f + 0.03f : 1f);
                    break;
                }
                case Kind.Shiver:
                {
                    float k = Mathf.Repeat(time, p.period);
                    p.t.localRotation = p.restRot * Quaternion.Euler(0f, k < 0.3f ? p.side * 4f * Mathf.Sin(k * 60f) : 0f, 0f);
                    break;
                }
                case Kind.Blink:
                {
                    // Clignement : échelle Y 1 → 0,1 → 1 en 0,15 s, toutes les 3 à 5 s.
                    p.nextEvent -= dt;
                    float s = 1f;
                    if (p.nextEvent < 0.15f && p.nextEvent > 0f) s = Mathf.Lerp(0.1f, 1f, Mathf.Abs(p.nextEvent - 0.075f) / 0.075f);
                    if (p.nextEvent <= 0f) p.nextEvent = Range(3f, 5f);
                    p.t.localScale = new Vector3(p.restScale.x, p.restScale.y * s, p.restScale.z);
                    break;
                }
                case Kind.Sparkle:
                {
                    float k = Mathf.Repeat(u, 1.5f) / 1.5f;
                    p.t.localScale = p.restScale * (k < 0.25f ? Mathf.Sin(k / 0.25f * Mathf.PI) : 0.02f);
                    break;
                }
                case Kind.Tentacle:
                {
                    // Une vague qui part de la base vers la pointe : ±15° par segment, 2,2 s. Au
                    // saut de joie, les tentacules se lèvent ensemble (0,3 s).
                    float lift = joyTime < 1.2f ? -20f * Mathf.Sin(Mathf.Clamp01(joyTime / 1.2f) * Mathf.PI) / 8f : 0f;
                    float a = p.amp * Mathf.Sin(time * Mathf.PI * 2f / p.period - p.index * 0.7f + p.phase) + lift;
                    p.t.localRotation = p.restRot * Quaternion.Euler(a, 0f, 0.4f * a);
                    break;
                }
                case Kind.Flame:
                {
                    // Chaque mèche s'allonge (échelle Y 0,9 ↔ 1,15) et se tord (±12°) à sa période ;
                    // au saut de joie elle s'embrase (×1,3 pendant 0,5 s).
                    float boost = joyTime < 0.5f ? 1.3f : 1f;
                    float sy = Mathf.Lerp(0.9f, 1.15f, 0.5f + 0.5f * Wave(u, p.period)) * boost;
                    p.t.localScale = new Vector3(p.restScale.x * boost, p.restScale.y * sy, p.restScale.z * boost);
                    p.t.localRotation = p.restRot * Quaternion.Euler(0f, 12f * Wave(u + 0.3f, p.period * 1.3f), 0f);
                    break;
                }
                case Kind.PaonFeather:
                {
                    // Repliée = la pose du fichier ; au saut de joie, la roue se déploie (0,35 s,
                    // ease-out-back), reste 1,2 s, se replie (0,3 s). Plume i : Euler(0, 0, 84 − 14·i).
                    float open = joyTime < 0.35f ? Ease.OutBack(joyTime / 0.35f)
                               : joyTime < 1.55f ? 1f
                               : joyTime < 1.85f ? 1f - Ease.InOut((joyTime - 1.55f) / 0.3f) : 0f;
                    var deployed = Quaternion.Euler(0f, 0f, 84f - 14f * p.index);
                    var rest = p.restRot * Quaternion.Euler(0f, 0f, 2f * Wave(time + p.index * 0.1f, 3f)); // frémit au repos
                    p.t.localRotation = Quaternion.SlerpUnclamped(rest, deployed, open);
                    break;
                }
                case Kind.Whale: AnimateWhale(p); break;
                case Kind.JoyFlap:
                {
                    // 4 battements de ±60° en 0,8 s au saut de joie (perroquet, poussin).
                    float a = joyTime < 0.8f ? 60f * Mathf.Sin(joyTime / 0.8f * Mathf.PI * 8f) * (1f - joyTime / 0.8f) : 0f;
                    p.t.localRotation = p.restRot * Quaternion.Euler(0f, 0f, p.side * a);
                    break;
                }
                case Kind.BatWing:
                {
                    // Battement « saccadé » ±18° en 0,7 s, puis une petite pause.
                    float k = Mathf.Repeat(time, 1.0f);
                    float a = k < 0.7f ? 18f * Mathf.Sin(k / 0.7f * Mathf.PI * 2f) : 0f;
                    p.t.localRotation = p.restRot * Quaternion.Euler(0f, p.side * a, 0f);
                    break;
                }
                case Kind.Vibrate: p.t.localRotation = p.restRot * Quaternion.Euler(25f * Mathf.Sin(time * Mathf.PI * 2f * 25f), 0f, 0f); break;
                case Kind.Sail:
                    p.t.localScale = new Vector3(p.restScale.x, p.restScale.y, p.restScale.z * Mathf.Lerp(1f, 1.08f, 0.5f + 0.5f * Wave(u, p.period)));
                    break;
                case Kind.HeadFollow:
                {
                    // Le perroquet et le chaton suivent le curseur des yeux (±25°), gentiment.
                    float x = 0f;
                    if (Mouse.current != null && Screen.width > 0) x = Mathf.Clamp(Mouse.current.position.ReadValue().x / Screen.width * 2f - 1f, -1f, 1f);
                    p.angle.x = Mathf.Lerp(p.angle.x, -x * p.amp, dt * 4f);
                    p.t.localRotation = p.restRot * Quaternion.Euler(0f, p.angle.x, 0f);
                    break;
                }
                case Kind.FlowerSpin: p.t.localRotation = p.restRot * Quaternion.Euler(0f, p.rate * time, 0f); break;
                case Kind.Flower: p.t.localRotation = p.restRot * Quaternion.Euler(0f, 0f, 6f * Wave(u, 2.4f)); break;
                case Kind.Heart:
                {
                    float beat = Mathf.Repeat(u, 1.2f);
                    float s = 1f + (beat < 0.15f ? 0.12f * Mathf.Sin(beat / 0.15f * Mathf.PI) : beat > 0.25f && beat < 0.4f ? 0.08f * Mathf.Sin((beat - 0.25f) / 0.15f * Mathf.PI) : 0f);
                    p.t.localScale = p.restScale * s;
                    break;
                }
                case Kind.Rays: p.t.localRotation = p.restRot * Quaternion.Euler(0f, -50f * time, 0f); break;
                case Kind.Moon: p.t.localRotation = p.restRot * Quaternion.Euler(0f, 180f * time, 0f); break;
                case Kind.Coin:
                {
                    // La pièce du petit kraken : lancée et rattrapée au saut de joie (0,6 s).
                    float k = joyTime < 0.6f ? Mathf.Sin(joyTime / 0.6f * Mathf.PI) : 0f;
                    p.t.localPosition = p.restPos + Vector3.up * 0.25f * k;
                    p.t.localRotation = p.restRot * Quaternion.Euler(720f * Mathf.Clamp01(joyTime / 0.6f), 0f, 0f);
                    break;
                }
                case Kind.JoyRise:
                {
                    float k = joyTime < 0.6f ? Mathf.Sin(joyTime / 0.6f * Mathf.PI) : 0f;
                    p.t.localPosition = p.restPos + Vector3.up * p.amp * k;
                    break;
                }
                case Kind.JoyPaw:
                {
                    float k = joyTime < 0.8f ? Mathf.Sin(joyTime / 0.8f * Mathf.PI) : 0f;
                    p.t.localRotation = p.restRot * Quaternion.Euler(-40f * k, 0f, 0f);
                    break;
                }
                case Kind.Wobble: p.t.localScale = p.restScale * (1f + p.amp * Wave(time, p.period)); break;
            }
        }

        /// La baleine céleste nage sur un « 8 » de 2,2 m de large centré en (0 ; 1,75 ; 0,05) dans
        /// le repère du cheval, un tour en 9 s ; quand elle passe devant, elle passe AU-DESSUS de
        /// la tête (jamais devant les yeux). Elle regarde dans le sens de la nage.
        void AnimateWhale(Part p)
        {
            if (horseRoot == null) return;
            float th = time * Mathf.PI * 2f / 9f;
            Vector3 Pos(float a)
            {
                float x = 1.1f * Mathf.Sin(a);
                float z = 0.05f + 0.9f * Mathf.Sin(a) * Mathf.Cos(a);
                float y = 1.75f + 0.5f * Mathf.Clamp((0.05f - z) / 0.45f, -1f, 1f); // devant (z < 0) : plus haut
                return new Vector3(x, y, z);
            }
            Vector3 local = Pos(th), ahead = Pos(th + 0.05f);
            p.t.position = horseRoot.TransformPoint(local);
            Vector3 dir = horseRoot.TransformDirection(ahead - local);
            if (dir.sqrMagnitude > 1e-6f) p.t.rotation = Quaternion.LookRotation(dir, horseRoot.up);
        }
    }
}
