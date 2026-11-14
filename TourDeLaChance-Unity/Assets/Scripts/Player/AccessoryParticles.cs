using UnityEngine;
using UnityEngine.Rendering;

namespace TourDeLaChance
{
    /// <summary>
    /// v7 (point 10, catalogue v5) : les PARTICULES des accessoires rares, sur le modèle de
    /// SkinParticles. PlayerAvatar l'ajoute sur l'objet posé quand l'accessoire en a besoin :
    /// comme il vit SUR l'accessoire, il disparaît avec lui (arrêt propre quand on l'enlève).
    ///   - Baleine céleste : traînée de 30 petites étoiles qui s'effacent en 1,2 s ; au saut de
    ///     joie, un jet de 20 étoiles par l'évent ;
    ///   - Navire fantôme : 20 petits nuages de brume qui suivent le navire ; au saut de joie, une
    ///     salve de 12 confettis dorés ;
    ///   - Bocal : 8 bulles qui montent dans l'eau ;
    ///   - Bébé kraken : au saut de joie, un petit nuage d'encre violette.
    /// Une RÉSERVE de petites sphères est créée une fois ; deux fois moins de particules en
    /// Graphismes « Faible ». Pur affichage, avec son propre hasard : aucune chance, aucun gain.
    /// </summary>
    public class AccessoryParticles : MonoBehaviour
    {
        enum Motion { Trail, Rise, Burst }

        class P { public Transform t; public Vector3 pos, vel; public float age, life, size; public bool alive; public Motion motion; }

        // Réglages de l'effet de cet accessoire.
        int poolSize, burstCount;
        float interval, life;
        Color color, burstColor;
        Motion motion;
        bool hasStream;

        P[] pool;
        Material material, burstMaterial;
        Transform space;           // les particules vivent dans l'espace du point d'accroche
        Renderer source;           // le plus gros morceau de l'accessoire (d'où elles partent)
        PlayerAnimator animator;   // pour savoir quand le cheval fait son saut de joie
        bool wasJoy;
        float timer;
        readonly System.Random random = new System.Random();

        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        /// Cet accessoire a-t-il des particules ?
        public static bool Needs(ShopItem item) =>
            item != null && (item.Id == "baleine_celeste" || item.Id == "navire_fantome" || item.Id == "bocal_poisson" || item.Id == "kraken_bebe");

        /// Nombre de particules visibles (pour les tests).
        public int AliveCount { get { int n = 0; if (pool != null) foreach (var p in pool) if (p.alive) n++; return n; } }

        public void Setup(ShopItem item)
        {
            switch (item.Id)
            {
                case "baleine_celeste":
                    hasStream = true; poolSize = 30 + 20; interval = 0.04f; life = 1.2f; motion = Motion.Trail;
                    color = new Color(1f, 0.945f, 0.84f); burstCount = 20; burstColor = new Color(0.8f, 0.9f, 1f); break;
                case "navire_fantome":
                    hasStream = true; poolSize = 20 + 12; interval = 0.075f; life = 1.5f; motion = Motion.Trail;
                    color = new Color(0.75f, 0.96f, 0.9f); burstCount = 12; burstColor = new Color(1f, 0.83f, 0.29f); break;
                case "bocal_poisson":
                    hasStream = true; poolSize = 8; interval = 0.5f; life = 1.6f; motion = Motion.Rise;
                    color = new Color(0.88f, 1f, 1f); burstCount = 0; break;
                default: // kraken_bebe
                    hasStream = false; poolSize = 10; burstCount = 10; burstColor = new Color(0.6f, 0.3f, 0.85f); break;
            }
            space = transform.parent != null ? transform.parent : transform;
            animator = GetComponentInParent<PlayerAnimator>();
            // La source : le plus gros rendu de l'accessoire (le navire, la baleine, le bocal…).
            float best = -1f;
            foreach (var r in GetComponentsInChildren<Renderer>())
            {
                float s = r.bounds.size.sqrMagnitude;
                if (s > best) { best = s; source = r; }
            }
            material = NewMaterial(color, 0.8f);
            burstMaterial = burstCount > 0 ? NewMaterial(burstColor, 1f) : null;
            pool = new P[poolSize];
            for (int i = 0; i < poolSize; i++)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                go.name = "ParticuleAcc_" + i;
                Destroy(go.GetComponent<Collider>());
                // Enfant de CET objet (détruit avec lui), mais placé dans l'espace du point d'accroche.
                go.transform.SetParent(transform, false);
                go.layer = gameObject.layer;
                var rend = go.GetComponent<Renderer>();
                rend.sharedMaterial = material;
                rend.shadowCastingMode = ShadowCastingMode.Off;
                rend.receiveShadows = false;
                go.SetActive(false);
                pool[i] = new P { t = go.transform };
            }
        }

        static Material NewMaterial(Color c, float glow)
        {
            var template = GraphicsSettings.currentRenderPipeline != null ? GraphicsSettings.currentRenderPipeline.defaultMaterial : null;
            var m = template != null ? new Material(template) : new Material(Shader.Find("Universal Render Pipeline/Lit"));
            m.SetColor(BaseColorId, c);
            m.EnableKeyword("_EMISSION");
            m.SetColor(EmissionColorId, c * glow);
            return m;
        }

        void OnDestroy()
        {
            if (material != null) Destroy(material);
            if (burstMaterial != null) Destroy(burstMaterial);
        }

        /// Le point de départ des particules, dans l'espace du point d'accroche.
        Vector3 SourcePoint() => space.InverseTransformPoint(source != null ? source.bounds.center : transform.position);

        void Update()
        {
            if (pool == null) return;
            float dt = Time.deltaTime;
            bool low = GraphicsLevel.Current == GraphicsLevel.Low;
            if (hasStream)
            {
                timer -= dt;
                if (timer <= 0f) { timer += low ? interval * 2f : interval; Spawn(motion, life, Range(0.03f, 0.06f), Vector3.zero, false); }
            }
            // Le saut de joie : une salve (moitié moins en Faible).
            bool joy = animator != null && animator.IsJoyJumping;
            if (joy && !wasJoy && burstCount > 0)
                for (int i = 0; i < (low ? burstCount / 2 : burstCount); i++)
                {
                    var v = new Vector3(Range(-0.6f, 0.6f), Range(0.8f, 1.6f), Range(-0.6f, 0.6f));
                    Spawn(Motion.Burst, 1.0f, Range(0.03f, 0.05f), v, true);
                }
            wasJoy = joy;

            foreach (var p in pool)
            {
                if (!p.alive) continue;
                p.age += dt;
                if (p.age >= p.life) { p.alive = false; p.t.gameObject.SetActive(false); continue; }
                float k = p.age / p.life;
                switch (p.motion)
                {
                    case Motion.Trail: // reste là où il est né, dérive à peine, rétrécit
                        p.pos += Vector3.up * 0.05f * dt;
                        p.t.localScale = Vector3.one * p.size * (1f - k);
                        break;
                    case Motion.Rise: // la bulle monte de 0,3 m en ondulant
                        p.pos += new Vector3(0.02f * Mathf.Sin(p.age * 8f), 0.2f, 0f) * dt;
                        p.t.localScale = Vector3.one * p.size * (k < 0.9f ? 1f : (1f - k) / 0.1f);
                        break;
                    default: // la salve : vitesse + gravité
                        p.vel += Vector3.down * 3f * dt;
                        p.pos += p.vel * dt;
                        p.t.localScale = Vector3.one * p.size * (1f - k * k);
                        break;
                }
                p.t.position = space.TransformPoint(p.pos);
            }
        }

        void Spawn(Motion m, float lifetime, float size, Vector3 velocity, bool burst)
        {
            P free = null;
            foreach (var p in pool) if (!p.alive) { free = p; break; }
            if (free == null) return;
            free.motion = m;
            free.life = lifetime;
            free.size = m == Motion.Rise ? Range(0.02f, 0.035f) : size;
            free.pos = SourcePoint() + new Vector3(Range(-0.05f, 0.05f), Range(-0.03f, 0.05f), Range(-0.05f, 0.05f));
            free.vel = velocity;
            free.age = 0f;
            free.alive = true;
            free.t.GetComponent<Renderer>().sharedMaterial = burst ? burstMaterial : material;
            free.t.position = space.TransformPoint(free.pos);
            free.t.localScale = Vector3.zero;
            free.t.gameObject.SetActive(true);
        }

        float Range(float a, float b) => a + (float)random.NextDouble() * (b - a);
    }
}
