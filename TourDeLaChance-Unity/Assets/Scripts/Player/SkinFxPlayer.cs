using UnityEngine;

namespace TourDeLaChance
{
    /// <summary>
    /// Les EFFETS ANIMÉS des skins Pirates / Animaux (catalogue v5, rendus « A » et « E »).
    /// Pur affichage : PlayerAvatar en crée un quand il affiche un skin qui a un effet (champ
    /// skinFx de la fiche), et l'avance à chaque image. Il dessine une petite image de lumière
    /// (128 × 128, recalculée 15 fois par seconde) ou change doucement la couleur du matériau :
    ///   - Bioluminescence : Kraken (taches qui s'allument comme une vague qui remonte, toutes
    ///     les 3 s) ; Abysses (les lignes « respirent » : émission 0,4 ↔ 1,2 en 2 s) ;
    ///   - Chaleur : Lion solaire (stries claires qui montent le long du corps) ;
    ///   - Eclairs : Tempête (un éclair toutes les 2 à 4 s, 0,15 s, avec le son pirate_eclair) ;
    ///   - Caustiques : Lagon (reflets de lumière dansants) ;
    ///   - BandeLumineuse : Fantôme (une bande monte toutes les 2,5 s, le corps flotte ±3 cm) ;
    ///   - ChangementCouleur : Caméléon (vert → turquoise → jaune → orange en 10 s) ;
    ///   - Irisation : Paon (la teinte glisse entre deux bleus) ; Pulsation : Méduse.
    /// Le hasard (moment des éclairs) est un générateur VISUEL séparé. v7 : les particules
    /// (fumée de l'Or maudit, bulles du Lagon, gouttes du Kraken) sont dans SkinParticles.
    /// </summary>
    public class SkinFxPlayer
    {
        const int N = 128;
        const float Step = 1f / 15f;
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        static readonly int EmissionMapId = Shader.PropertyToID("_EmissionMap");

        readonly ShopItem item;
        readonly Material mat;
        readonly SkinPatterns.Result pattern;
        readonly Transform body;
        readonly Vector3 bodyRest;
        readonly bool allowSound;
        readonly System.Random fxRandom = new System.Random();
        Texture2D tex;
        Color32[] buf;
        float time, timer, nextFlash = 2f, flashTime = -1f;
        Vector2[] bolt;
        SkinParticles particles; // v7 : null si le skin n'a pas de particules

        /// Ce skin a-t-il un effet animé ?
        public static bool Needs(ShopItem item) => item != null && item.SkinFx != SkinFx.Aucun;

        /// L'effet a-t-il besoin de sa propre image de lumière ?
        static bool DrawsTexture(ShopItem item) =>
            (item.SkinFx == SkinFx.Bioluminescence && item.Pattern == SkinPattern.Ventouses) ||
            item.SkinFx == SkinFx.Chaleur || item.SkinFx == SkinFx.Eclairs ||
            item.SkinFx == SkinFx.Caustiques || item.SkinFx == SkinFx.BandeLumineuse;

        /// Un éclair est-il en train de briller ? (pour les tests)
        public bool Flashing => flashTime >= 0f;

        public SkinFxPlayer(ShopItem item, Material mat, SkinPatterns.Result pattern, Transform body, bool allowSound)
        {
            this.item = item;
            this.mat = mat;
            this.pattern = pattern;
            this.body = body;
            this.allowSound = allowSound;
            if (body != null) bodyRest = body.localPosition;
            if (DrawsTexture(item))
            {
                tex = new Texture2D(N, N, TextureFormat.RGBA32, false)
                { name = "Effet " + item.Id, wrapMode = TextureWrapMode.Repeat, hideFlags = HideFlags.DontSave };
                buf = new Color32[N * N];
                mat.EnableKeyword("_EMISSION");
                mat.SetTexture(EmissionMapId, tex);
                mat.SetColor(EmissionColorId, Color.white * Mathf.Max(0.01f, item.EmissionStrength));
            }
            nextFlash = Range(2f, 4f);
            var kind = SkinParticles.For(item);
            if (kind.HasValue && body != null && body.parent != null)
                particles = new SkinParticles(kind.Value, body.parent, body.gameObject.layer);
            Redraw();
        }

        /// Les particules du skin (null s'il n'en a pas), pour les tests.
        public SkinParticles Particles => particles;

        float Range(float a, float b) => a + (float)fxRandom.NextDouble() * (b - a);

        /// Remet tout comme avant (changement de skin).
        public void Dispose()
        {
            if (body != null) body.localPosition = bodyRest;
            if (tex != null) Object.Destroy(tex);
            tex = null;
            particles?.Dispose();
            particles = null;
        }

        /// Avance l'effet. joy = le cheval fait son saut de joie (certains effets s'excitent).
        public void Tick(float dt, bool joy)
        {
            time += dt * (joy ? 2f : 1f);
            particles?.Tick(dt);
            switch (item.SkinFx)
            {
                case SkinFx.Bioluminescence when item.Pattern != SkinPattern.Ventouses:
                    // Abysses : les lignes bioluminescentes respirent (0,4 ↔ 1,2, période 2 s).
                    mat.SetColor(EmissionColorId, Color.white * Mathf.Lerp(0.4f, 1.2f, 0.5f + 0.5f * Mathf.Sin(time * Mathf.PI)));
                    break;
                case SkinFx.ChangementCouleur:
                {
                    // Vert → turquoise → jaune → orange → retour, un cycle en 10 s.
                    Color[] steps = { new Color(0.3f, 0.76f, 0.42f), new Color(0.17f, 0.72f, 0.77f), new Color(1f, 0.85f, 0.3f), new Color(0.95f, 0.55f, 0.22f) };
                    float k = Mathf.Repeat(time / 10f, 1f) * steps.Length;
                    int i = Mathf.FloorToInt(k);
                    Color hue = Color.Lerp(steps[i % steps.Length], steps[(i + 1) % steps.Length], Mathf.SmoothStep(0f, 1f, k - i));
                    // v7 : avec la texture dessinée (couleurs déjà dans l'image), on ne fait que
                    // TEINTER légèrement (30 %) : le cheval garde ses couleurs au repos.
                    if (item.BaseTexture != null) hue = Color.Lerp(Color.white, hue, 0.3f);
                    mat.SetColor(BaseColorId, hue);
                    break;
                }
                case SkinFx.Irisation:
                {
                    float yaw = body != null ? body.eulerAngles.y * Mathf.Deg2Rad : 0f;
                    mat.SetColor(BaseColorId, Color.Lerp(Color.white, new Color(0.75f, 1f, 0.95f), 0.5f + 0.5f * Mathf.Sin(time * 0.8f + yaw)));
                    break;
                }
                case SkinFx.Pulsation:
                {
                    var c = item.Color;
                    float s = 1f + 0.1f * Mathf.Sin(time * Mathf.PI);
                    mat.SetColor(BaseColorId, new Color(c.r * s, c.g * s, c.b * s, Mathf.Max(0.55f, c.a)));
                    break;
                }
                case SkinFx.BandeLumineuse:
                    if (body != null) body.localPosition = bodyRest + Vector3.up * 0.03f * Mathf.Sin(time * Mathf.PI); // flotte ±3 cm (2 s)
                    break;
                case SkinFx.Eclairs:
                    // Un éclair toutes les 2 à 4 s (hasard visuel), qui brille 0,15 s.
                    nextFlash -= dt;
                    if (nextFlash <= 0f)
                    {
                        nextFlash = Range(2f, 4f);
                        flashTime = 0f;
                        bolt = NewBolt();
                        if (allowSound && SoundManager.Instance != null) SoundManager.Instance.Play(Sfx.PirateEclair);
                    }
                    if (flashTime >= 0f)
                    {
                        flashTime += dt;
                        mat.SetColor(EmissionColorId, Color.white * (flashTime < 0.15f ? 1.4f : 0f));
                        if (flashTime >= 0.15f) flashTime = -1f;
                    }
                    break;
            }
            if (tex == null) return;
            timer += dt;
            if (timer < Step) return;
            timer = 0f;
            Redraw();
        }

        /// Un zigzag qui parcourt le corps de haut en bas (positions u, v).
        Vector2[] NewBolt()
        {
            var pts = new Vector2[9];
            float u = Range(0.25f, 0.75f);
            for (int i = 0; i < pts.Length; i++)
            {
                pts[i] = new Vector2(u, 0.95f - i * 0.1f);
                u += Range(-0.05f, 0.05f);
            }
            return pts;
        }

        /// Redessine l'image de lumière (128 × 128) selon l'effet et le temps.
        void Redraw()
        {
            if (tex == null) return;
            Color glow = item.GlowColor;
            for (int y = 0; y < N; y++)
            {
                float v = (y + 0.5f) / N;
                for (int x = 0; x < N; x++)
                {
                    float u = (x + 0.5f) / N;
                    float b = 0f;
                    Color col = glow;
                    switch (item.SkinFx)
                    {
                        case SkinFx.Chaleur:
                        {
                            // Stries claires qui montent lentement, comme l'air qui tremble.
                            float s = Mathf.Sin((v * 9f - time * 0.35f + 0.25f * Mathf.Sin(u * Mathf.PI * 2f * 4f + time)) * Mathf.PI * 2f);
                            b = Mathf.Pow(Mathf.Max(0f, s), 6f);
                            break;
                        }
                        case SkinFx.Caustiques:
                        {
                            float a = Mathf.Sin(u * Mathf.PI * 2f * 7f + time * 1.3f + Mathf.Sin(v * 11f + time));
                            float c = Mathf.Sin(v * 13f - time * 0.9f + Mathf.Sin(u * Mathf.PI * 2f * 5f - time * 0.7f));
                            b = Mathf.Pow(1f - Mathf.Abs(a * c), 10f);
                            col = item.AccentColor;
                            break;
                        }
                        case SkinFx.BandeLumineuse:
                        {
                            float band = Mathf.Repeat(time / 2.5f, 1f) * 1.3f - 0.15f;
                            float d = (v - band) / 0.06f;
                            b = Mathf.Exp(-d * d);
                            col = item.Color;
                            break;
                        }
                    }
                    col *= b;
                    buf[y * N + x] = new Color32((byte)(Mathf.Clamp01(col.r) * 255), (byte)(Mathf.Clamp01(col.g) * 255), (byte)(Mathf.Clamp01(col.b) * 255), 255);
                }
            }
            // Les taches du Kraken : elles s'allument tour à tour, une vague qui remonte (3 s).
            if (item.SkinFx == SkinFx.Bioluminescence && pattern != null && pattern.spots != null)
            {
                System.Array.Clear(buf, 0, buf.Length);
                foreach (var s in pattern.spots)
                {
                    float wave = Mathf.Pow(Mathf.Max(0f, Mathf.Sin((time / 3f - s.w * 1.2f) * Mathf.PI * 2f)), 3f);
                    if (wave < 0.02f) continue;
                    DrawDisc(s.x, s.y, s.z, glow * wave);
                }
            }
            // L'éclair : un zigzag blanc-bleu (visible seulement pendant le flash).
            if (item.SkinFx == SkinFx.Eclairs)
            {
                System.Array.Clear(buf, 0, buf.Length);
                if (bolt != null && flashTime >= 0f)
                    for (int i = 0; i + 1 < bolt.Length; i++)
                        for (float k = 0f; k <= 1f; k += 0.05f)
                            DrawDisc(Mathf.Lerp(bolt[i].x, bolt[i + 1].x, k), Mathf.Lerp(bolt[i].y, bolt[i + 1].y, k), 0.012f, glow);
            }
            tex.SetPixels32(buf);
            tex.Apply(false);
        }

        void DrawDisc(float cu, float cv, float radiusMeters, Color c)
        {
            int r = Mathf.CeilToInt(radiusMeters / 3.1f * N) + 1;
            int cx = Mathf.RoundToInt(cu * N), cy = Mathf.RoundToInt(cv * N);
            var c32 = new Color32((byte)(Mathf.Clamp01(c.r) * 255), (byte)(Mathf.Clamp01(c.g) * 255), (byte)(Mathf.Clamp01(c.b) * 255), 255);
            for (int dy = -r * 2; dy <= r * 2; dy++)
                for (int dx = -r; dx <= r; dx++)
                {
                    int x = ((cx + dx) % N + N) % N, y = cy + dy;
                    if (y < 0 || y >= N) continue;
                    if (SkinPatterns.Dist((x + 0.5f) / N, (y + 0.5f) / N, cu, cv) <= radiusMeters) buf[y * N + x] = c32;
                }
        }
    }
}
