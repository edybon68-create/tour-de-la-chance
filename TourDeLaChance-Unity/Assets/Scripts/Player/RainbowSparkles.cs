using UnityEngine;
using UnityEngine.Rendering;

namespace TourDeLaChance
{
    /// <summary>
    /// Les paillettes des skins brillants : « Arc-en-ciel » (spec v3 §8.1) et, depuis la v4,
    /// « Galaxie » (§7.5). De petites étoiles apparaissent sur la moitié avant du cheval,
    /// montent de 0,4 m en 0,8 s en grossissant puis en rétrécissant. Arc-en-ciel : une
    /// toutes les 0,25 s, teintées par la couleur du moment ; Galaxie : une toutes les 0,4 s,
    /// couleur fixe #CFE0FF (réglées par PlayerAvatar avec Configure).
    ///
    /// Une réserve de 10 étoiles est créée une fois (pas de création/destruction en boucle).
    /// Pur affichage, avec son propre générateur de hasard (sans lien avec le jeu).
    /// Les étoiles sont des enfants de `Joueur` : elles suivent ses sauts.
    /// </summary>
    public class RainbowSparkles : MonoBehaviour
    {
        [SerializeField] PlayerAvatar avatar;      // pour la couleur de l'arc-en-ciel du moment
        [SerializeField] Material emissiveTemplate; // URP Lit avec l'émission (M_SkinEmissif)

        const int PoolSize = 10;
        const float Life = 0.8f;       // elle vit 0,8 s
        const float Rise = 0.4f;       // et monte de 0,4 m
        const float Size = 0.06f;      // cube de 6 cm tourné à 45°
        float interval = 0.25f;        // une nouvelle étoile toutes les 0,25 s (arc-en-ciel)
        float intensity = 1.2f;        // lumière propre (sous le plafond de 1,5)
        Color? fixedColor;             // null = couleur de l'arc-en-ciel du moment

        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        class Star { public Transform t; public Renderer r; public Vector3 start; public float age; public bool alive; }
        readonly Star[] stars = new Star[PoolSize];
        readonly System.Random random = new System.Random();
        MaterialPropertyBlock block;
        Material material;
        bool active;
        float timer;

        void Awake()
        {
            block = new MaterialPropertyBlock();
            if (emissiveTemplate != null)
            {
                material = new Material(emissiveTemplate) { name = "Paillette (émission)" };
                material.EnableKeyword("_EMISSION");
            }
            for (int i = 0; i < PoolSize; i++)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = "Paillette_" + i;
                Destroy(go.GetComponent<Collider>()); // aucun collider
                go.transform.SetParent(transform, false);
                go.layer = gameObject.layer; // même calque que le cheval (vitrine des pop-ups)
                go.transform.localRotation = Quaternion.Euler(45f, 0f, 45f);
                var r = go.GetComponent<Renderer>();
                if (material != null) r.sharedMaterial = material;
                r.shadowCastingMode = ShadowCastingMode.Off; // sans ombre
                r.receiveShadows = false;
                go.SetActive(false);
                stars[i] = new Star { t = go.transform, r = r };
            }
        }

        void OnDestroy()
        {
            if (material != null) Destroy(material);
        }

        /// Règle les paillettes : couleur fixe (null = arc-en-ciel du moment), intervalle entre
        /// deux étoiles (s) et force de la lumière propre.
        public void Configure(Color? color, float spawnInterval, float glow)
        {
            fixedColor = color;
            interval = Mathf.Max(0.05f, spawnInterval);
            intensity = glow;
        }

        /// Allume ou éteint les paillettes (appelé par PlayerAvatar au changement de skin).
        public void SetActive(bool on)
        {
            active = on;
            if (on) return;
            foreach (var s in stars)
            {
                if (s == null) continue;
                s.alive = false;
                s.t.gameObject.SetActive(false);
            }
        }

        /// Combien d'étoiles sont visibles (pour les tests).
        public int AliveCount
        {
            get { int n = 0; foreach (var s in stars) if (s != null && s.alive) n++; return n; }
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (active)
            {
                timer -= dt;
                if (timer <= 0f) { timer += interval; Spawn(); }
            }

            Color tint = fixedColor ?? (avatar != null ? avatar.RainbowColorNow : Color.white);
            foreach (var s in stars)
            {
                if (s == null || !s.alive) continue;
                s.age += dt;
                if (s.age >= Life) { s.alive = false; s.t.gameObject.SetActive(false); continue; }
                float k = s.age / Life;
                s.t.localPosition = s.start + Vector3.up * Rise * Ease.Out(k);
                s.t.localScale = Vector3.one * Size * Ease.Sine(k); // 0 → 1 → 0
                // Arc-en-ciel : blanc teinté de la couleur du moment ; Galaxie : la couleur fixe.
                Color c = fixedColor ?? Color.Lerp(Color.white, tint, 0.5f);
                block.SetColor(BaseColorId, c);
                block.SetColor(EmissionColorId, c * intensity);
                s.r.SetPropertyBlock(block);
            }
        }

        /// Place une nouvelle étoile au hasard sur la moitié AVANT du corps (côté caméra),
        /// entre 0,4 et 1,9 m de haut, juste à la surface du cheval.
        void Spawn()
        {
            Star star = null;
            foreach (var s in stars) if (!s.alive) { star = s; break; }
            if (star == null) return;
            float h = Range(0.4f, 1.9f);
            float angle = Range(-80f, 80f) * Mathf.Deg2Rad; // 0 = droit devant (vers −z)
            float radius = RadiusAt(h) + 0.03f;
            star.start = new Vector3(Mathf.Sin(angle) * radius, h, -Mathf.Cos(angle) * radius);
            star.age = 0f;
            star.alive = true;
            star.t.localPosition = star.start;
            star.t.localScale = Vector3.zero;
            star.t.gameObject.SetActive(true);
        }

        /// Rayon approximatif du cheval selon la hauteur (socle, encolure, tête), en m.
        static float RadiusAt(float h)
        {
            if (h < 0.3f) return 0.5f;   // socle
            if (h < 0.8f) return 0.36f;  // poitrail
            if (h < 1.4f) return 0.28f;  // encolure
            return 0.3f;                 // tête
        }

        float Range(float min, float max) => min + (float)random.NextDouble() * (max - min);
    }
}
