using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace TourDeLaChance
{
    /// <summary>
    /// L'ANIMATION de la renaissance (spec v5 §13.4), 4,2 s, passable après 1 s (clic, Espace,
    /// Échap). Pur affichage : la renaissance est DÉJÀ faite et sauvegardée (Rebirth) quand
    /// elle commence ; à la fin, le GameManager lance la nouvelle partie.
    ///
    ///   0,0  l'écran s'assombrit (0,3 s), son « renaissance »
    ///   0,3  3 anneaux dorés montent du socle autour du cheval ; il s'élève en faisant 2 tours
    ///   1,5  flash blanc
    ///   2,1  le cheval atterrit : onde de choc dorée au sol, pluie d'étincelles dorées
    ///   2,4  « RENAISSANCE ! » (lettres une à une, or) puis le tampon « ×2 » s'écrase sur le
    ///        compteur de pièces (le badge y reste), son « renaissance_tampon »
    ///   3,6  fondu de retour ; 4,2 nouvelle partie
    /// Tout est calculé à partir d'une seule horloge (comme l'ouverture des boîtes).
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class RebirthCinematic : MonoBehaviour
    {
        [SerializeField] GameManager game;
        [SerializeField] PlayerAnimator player;
        [SerializeField] Transform playerRoot; // pour placer les anneaux autour du cheval

        static readonly Color Gold = new Color(1f, 0.79f, 0.24f);
        const string Word = "RENAISSANCE !";
        const int SparkCount = 40;

        VisualElement overlay, dark, flash, wordRow, badge;
        readonly List<Label> letters = new List<Label>();
        readonly List<(VisualElement e, Vector2 pos, Vector2 vel)> sparks = new List<(VisualElement, Vector2, Vector2)>();
        readonly LineRenderer[] rings = new LineRenderer[3];
        LineRenderer shock;
        Material ringMaterial;
        readonly System.Random fxRandom = new System.Random(); // hasard visuel seulement

        float t = -1f;
        bool playedStamp, sparksDone;

        public bool Playing => t >= 0f;

        void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            badge = root.Q<VisualElement>("rebirth-badge");
            overlay = new VisualElement { name = "rebirth-fx", pickingMode = PickingMode.Ignore };
            overlay.AddToClassList("rebirth-fx");
            dark = new VisualElement { pickingMode = PickingMode.Ignore };
            dark.AddToClassList("rebirth-fx__noir");
            overlay.Add(dark);
            wordRow = new VisualElement { pickingMode = PickingMode.Ignore };
            wordRow.AddToClassList("rebirth-fx__mot");
            foreach (char c in Word)
            {
                var l = new Label(c == ' ' ? " " : c.ToString()) { pickingMode = PickingMode.Ignore };
                l.AddToClassList("rebirth-fx__lettre");
                if (c == ' ') l.style.width = 32; // une espace seule ne prend aucune place
                wordRow.Add(l);
                letters.Add(l);
            }
            overlay.Add(wordRow);
            for (int i = 0; i < SparkCount; i++)
            {
                var e = new VisualElement { pickingMode = PickingMode.Ignore };
                e.AddToClassList("rebirth-fx__etincelle");
                e.style.backgroundImage = new StyleBackground(UiFxTextures.Sparkle);
                overlay.Add(e);
                sparks.Add((e, Vector2.zero, Vector2.zero));
            }
            flash = new VisualElement { pickingMode = PickingMode.Ignore };
            flash.AddToClassList("rebirth-fx__flash");
            overlay.Add(flash);
            root.Add(overlay);
            overlay.style.display = DisplayStyle.None;
            game.RebirthStarted += OnRebirthStarted;
        }

        void OnDisable()
        {
            game.RebirthStarted -= OnRebirthStarted;
            overlay?.RemoveFromHierarchy();
        }

        void OnDestroy()
        {
            if (ringMaterial != null) Destroy(ringMaterial);
        }

        void OnRebirthStarted()
        {
            t = 0f;
            playedStamp = sparksDone = false;
            overlay.style.display = DisplayStyle.Flex;
            if (badge != null) badge.style.visibility = Visibility.Hidden; // le tampon arrive à 2,4 s
            if (SoundManager.Instance != null) SoundManager.Instance.Play(Sfx.Renaissance);
            if (player != null) player.RebirthRise();
            BuildRings();
            Render();
        }

        /// 3 anneaux dorés (et l'onde de choc), dessinés par des LineRenderer (aucun modèle).
        void BuildRings()
        {
            if (ringMaterial == null)
            {
                var baseMat = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline != null
                    ? UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline.defaultMaterial : null;
                ringMaterial = baseMat != null ? new Material(baseMat) : new Material(Shader.Find("Universal Render Pipeline/Lit"));
                ringMaterial.name = "Anneaux de renaissance";
                ringMaterial.SetColor("_BaseColor", Gold);
                ringMaterial.EnableKeyword("_EMISSION");
                ringMaterial.SetColor("_EmissionColor", Gold * 1.3f); // sous le plafond de 1,5
            }
            for (int i = 0; i < rings.Length; i++) rings[i] = rings[i] != null ? rings[i] : NewRing("AnneauRenaissance_" + i);
            if (shock == null) shock = NewRing("OndeRenaissance");
        }

        LineRenderer NewRing(string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var lr = go.AddComponent<LineRenderer>();
            lr.sharedMaterial = ringMaterial;
            lr.loop = true;
            lr.positionCount = 48;
            lr.useWorldSpace = true;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.enabled = false;
            return lr;
        }

        static void PlaceRing(LineRenderer lr, Vector3 center, float radius, float width)
        {
            lr.widthMultiplier = width;
            for (int i = 0; i < lr.positionCount; i++)
            {
                float a = i * Mathf.PI * 2f / lr.positionCount;
                lr.SetPosition(i, center + new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius));
            }
        }

        void Update()
        {
            if (t < 0f) return;
            t += Time.deltaTime;
            // Passer l'animation : après 1 s, un clic, Espace ou Échap.
            var kb = Keyboard.current;
            var mouse = Mouse.current;
            bool skip = (kb != null && (kb.spaceKey.wasPressedThisFrame || kb.escapeKey.wasPressedThisFrame)) ||
                        (mouse != null && mouse.leftButton.wasPressedThisFrame);
            if (skip && t >= GameManager.RebirthSkippableAfter) { game.SkipRebirthAnimation(); Finish(); return; }
            if (t >= GameManager.RebirthDuration || !game.RebirthPlaying) { Finish(); return; }
            Render();
        }

        void Render()
        {
            // Le voile sombre : 0 → 0,55 en 0,3 s, retour de 3,6 à 4,2 s.
            float d = t < 0.3f ? t / 0.3f : t < 3.6f ? 1f : 1f - (t - 3.6f) / 0.6f;
            dark.style.opacity = 0.55f * Mathf.Clamp01(d);
            // Flash blanc à 1,5 s (0 → 0,8 → 0 en 0,3 s).
            float f = (t - 1.5f) / 0.3f;
            flash.style.opacity = f > 0f && f < 1f ? 0.8f * Mathf.Sin(f * Mathf.PI) : 0f;

            // Les 3 anneaux montent du socle (0,3 → 1,5 s) autour du cheval, puis s'effacent.
            Vector3 c = playerRoot != null ? playerRoot.position : Vector3.zero;
            for (int i = 0; i < rings.Length; i++)
            {
                if (rings[i] == null) continue;
                float k = Mathf.Clamp01((t - 0.3f - 0.15f * i) / 1.2f);
                bool on = t >= 0.3f && t < 2.2f;
                rings[i].enabled = on;
                if (on) PlaceRing(rings[i], c + Vector3.up * (0.1f + 1.8f * Ease.Out(k) * (0.6f + 0.2f * i)), 0.9f - 0.15f * i, 0.06f * (1f - Mathf.Clamp01((t - 1.8f) / 0.4f)));
            }
            // L'onde de choc dorée au sol à l'atterrissage (2,1 → 2,6 s).
            if (shock != null)
            {
                float k = (t - 2.1f) / 0.5f;
                shock.enabled = k > 0f && k < 1f;
                if (shock.enabled) PlaceRing(shock, c + Vector3.up * 0.05f, Mathf.Lerp(0.4f, 3f, Ease.Out(k)), 0.12f * (1f - k));
            }
            if (!sparksDone && t >= 2.1f) { sparksDone = true; EmitSparks(); }
            UpdateSparks();

            // « RENAISSANCE ! » : lettres une à une à partir de 2,4 s (0,05 s d'écart), avec un rebond.
            for (int i = 0; i < letters.Count; i++)
            {
                float k = (t - 2.4f - i * 0.05f) / 0.25f;
                letters[i].style.opacity = k <= 0f ? 0f : (t > 3.6f ? 1f - (t - 3.6f) / 0.6f : 1f);
                float s = k <= 0f ? 0f : Ease.Pop(Mathf.Clamp01(k), 0.3f, 1.25f);
                letters[i].style.scale = new Scale(new Vector3(s, s, 1f));
            }

            // Le tampon « ×2 » s'écrase sur le compteur de pièces (2,4 s : 3 → 1 en 0,2 s).
            if (badge != null && t >= 2.4f)
            {
                if (!playedStamp)
                {
                    playedStamp = true;
                    badge.style.visibility = Visibility.Visible;
                    if (SoundManager.Instance != null) SoundManager.Instance.Play(Sfx.RenaissanceTampon);
                }
                float k = Mathf.Clamp01((t - 2.4f) / 0.2f);
                float s = Mathf.Lerp(3f, 1f, Ease.In(k));
                badge.style.scale = new Scale(new Vector3(s, s, 1f));
            }
        }

        void EmitSparks()
        {
            var r = overlay.contentRect;
            Vector2 center = new Vector2(r.width * 0.5f, r.height * 0.62f);
            for (int i = 0; i < sparks.Count; i++)
            {
                float a = (float)(fxRandom.NextDouble() * Mathf.PI * 2f);
                float v = 300f + (float)fxRandom.NextDouble() * 500f;
                sparks[i] = (sparks[i].e, center, new Vector2(Mathf.Cos(a), Mathf.Sin(a) - 0.6f) * v);
                sparks[i].e.style.display = DisplayStyle.Flex;
                sparks[i].e.style.unityBackgroundImageTintColor = Gold;
            }
        }

        void UpdateSparks()
        {
            float dt = Time.deltaTime;
            float life = Mathf.Clamp01((t - 2.1f) / 1.3f);
            for (int i = 0; i < sparks.Count; i++)
            {
                var (e, pos, vel) = sparks[i];
                if (!sparksDone || life >= 1f) { e.style.display = DisplayStyle.None; continue; }
                vel.y += 900f * dt;
                pos += vel * dt;
                sparks[i] = (e, pos, vel);
                e.style.left = pos.x - 14f;
                e.style.top = pos.y - 14f;
                e.style.opacity = 1f - life;
            }
        }

        void Finish()
        {
            t = -1f;
            overlay.style.display = DisplayStyle.None;
            foreach (var r in rings) if (r != null) r.enabled = false;
            if (shock != null) shock.enabled = false;
            if (badge != null) { badge.style.visibility = Visibility.Visible; badge.style.scale = StyleKeyword.Null; }
            if (player != null) player.StopRebirth();
        }
    }
}
