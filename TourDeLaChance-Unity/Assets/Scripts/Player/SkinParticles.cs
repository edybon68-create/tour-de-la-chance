using UnityEngine;
using UnityEngine.Rendering;

namespace TourDeLaChance
{
    /// <summary>
    /// v7 (point 10, catalogue v5 rendu « P ») : les PARTICULES des skins rares. Ce n'est pas
    /// un MonoBehaviour : SkinFxPlayer en crée une quand le skin en a besoin et l'avance à
    /// chaque image. Une RÉSERVE de petites sphères est créée une seule fois (aucune création
    /// ni destruction pendant le jeu), puis réutilisée :
    ///   - Fumée (Or maudit) : 12 volutes vertes qui montent de 0,5 m en 1,5 s depuis le socle ;
    ///   - Bulles (Lagon) : 10 bulles claires, une toutes les 0,3 s, qui montent le long du corps ;
    ///   - Gouttes (Kraken) : 12 gouttes qui tombent de la tête, une toutes les 0,5 s ;
    ///   - Escarbilles (Lion solaire) : 30 étincelles orange qui montent de la crinière.
    /// En Graphismes « Faible », deux fois moins de particules (intervalle doublé).
    /// Pur affichage, avec son propre hasard : aucune chance, aucun gain ne dépend de ceci.
    /// </summary>
    public class SkinParticles
    {
        public enum Kind { Fumee, Bulles, Gouttes, Escarbilles }

        class P { public Transform t; public Vector3 start; public float age, size; public bool alive; }

        readonly Kind kind;
        readonly P[] pool;
        readonly Material material;
        readonly float interval, life;
        readonly System.Random random = new System.Random();
        float timer;

        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        /// La sorte de particules d'un skin (null = aucune).
        public static Kind? For(ShopItem item)
        {
            if (item == null) return null;
            switch (item.SkinFx)
            {
                case SkinFx.Fumee: return Kind.Fumee;
                case SkinFx.Caustiques:
                case SkinFx.Bulles: return Kind.Bulles;
                case SkinFx.Bioluminescence when item.Pattern == SkinPattern.Ventouses: return Kind.Gouttes;
                case SkinFx.Chaleur: return Kind.Escarbilles;
                default: return null;
            }
        }

        public SkinParticles(Kind kind, Transform parent, int layer)
        {
            this.kind = kind;
            int count; Color color; float glow;
            switch (kind)
            {
                case Kind.Fumee: count = 12; interval = 0.125f; life = 1.5f; color = new Color(0.24f, 1f, 0.6f, 1f); glow = 0.8f; break;   // #3CFF9A
                case Kind.Bulles: count = 10; interval = 0.3f; life = 2.0f; color = new Color(0.88f, 1f, 1f, 1f); glow = 0.5f; break;     // #E0FFFF
                case Kind.Escarbilles: count = 30; interval = 0.12f; life = 1.4f; color = new Color(1f, 0.55f, 0.15f, 1f); glow = 1.2f; break;  // orange
                default: count = 12; interval = 0.5f; life = 1.0f; color = new Color(0.55f, 0.9f, 1f, 1f); glow = 0.4f; break;
            }
            var template = GraphicsSettings.currentRenderPipeline != null ? GraphicsSettings.currentRenderPipeline.defaultMaterial : null;
            material = template != null ? new Material(template) : new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.name = "Particule " + kind;
            material.SetColor(BaseColorId, color);
            material.EnableKeyword("_EMISSION");
            material.SetColor(EmissionColorId, color * glow);
            pool = new P[count];
            for (int i = 0; i < count; i++)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                go.name = "Particule_" + kind + "_" + i;
                Object.Destroy(go.GetComponent<Collider>());
                go.transform.SetParent(parent, false);
                go.layer = layer; // même calque que le cheval (vitrine des pop-ups comprise)
                var r = go.GetComponent<Renderer>();
                r.sharedMaterial = material;
                r.shadowCastingMode = ShadowCastingMode.Off;
                r.receiveShadows = false;
                go.SetActive(false);
                pool[i] = new P { t = go.transform };
            }
        }

        /// Nombre de particules visibles (pour les tests).
        public int AliveCount { get { int n = 0; foreach (var p in pool) if (p.alive) n++; return n; } }

        public void Tick(float dt)
        {
            timer -= dt;
            if (timer <= 0f)
            {
                // Graphismes Faible : deux fois moins de particules.
                timer += GraphicsLevel.Current == GraphicsLevel.Low ? interval * 2f : interval;
                Spawn();
            }
            foreach (var p in pool)
            {
                if (!p.alive) continue;
                p.age += dt;
                if (p.age >= life) { p.alive = false; p.t.gameObject.SetActive(false); continue; }
                float k = p.age / life;
                switch (kind)
                {
                    case Kind.Fumee:
                        // La volute monte de 0,5 m, grossit puis disparaît (pas de transparence : elle rétrécit).
                        p.t.localPosition = p.start + new Vector3(0.05f * Mathf.Sin(k * 6f + p.size * 40f), 0.5f * Ease.Out(k), 0f);
                        p.t.localScale = Vector3.one * p.size * (0.6f + 0.8f * k) * (1f - Mathf.Pow(k, 3f));
                        break;
                    case Kind.Bulles:
                        // La bulle monte de 1,2 m en ondulant ; elle « éclate » à la fin.
                        p.t.localPosition = p.start + new Vector3(0.03f * Mathf.Sin(k * 12f), 1.2f * k, 0f);
                        p.t.localScale = Vector3.one * p.size * (k < 0.9f ? 1f : (1f - k) / 0.1f);
                        break;
                    case Kind.Escarbilles:
                        // L'étincelle monte de 0,7 m en tournoyant et s'éteint.
                        p.t.localPosition = p.start + new Vector3(0.12f * Mathf.Sin(k * 9f + p.size * 90f), 0.7f * k, 0.12f * Mathf.Cos(k * 9f + p.size * 90f));
                        p.t.localScale = Vector3.one * p.size * (1f - k);
                        break;
                    default:
                        // La goutte tombe de la tête jusqu'au socle (accélère).
                        p.t.localPosition = p.start + Vector3.down * (1.4f * k * k);
                        p.t.localScale = new Vector3(p.size, p.size * 1.4f, p.size);
                        break;
                }
            }
        }

        void Spawn()
        {
            P free = null;
            foreach (var p in pool) if (!p.alive) { free = p; break; }
            if (free == null) return;
            float angle = Range(-180f, 180f) * Mathf.Deg2Rad;
            switch (kind)
            {
                case Kind.Fumee:
                    free.start = new Vector3(Mathf.Sin(angle) * 0.42f, 0.15f, Mathf.Cos(angle) * 0.42f);
                    free.size = Range(0.06f, 0.10f);
                    break;
                case Kind.Bulles:
                    free.start = new Vector3(Mathf.Sin(angle) * 0.38f, Range(0.15f, 0.5f), Mathf.Cos(angle) * 0.38f);
                    free.size = Range(0.03f, 0.06f); // Ø 3 à 6 cm
                    break;
                case Kind.Escarbilles:
                    free.start = new Vector3(Mathf.Sin(angle) * 0.3f, Range(1.3f, 1.8f), Mathf.Cos(angle) * 0.3f);
                    free.size = Range(0.02f, 0.035f);
                    break;
                default:
                    free.start = new Vector3(Mathf.Sin(angle) * 0.3f, Range(1.4f, 1.7f), Mathf.Cos(angle) * 0.3f);
                    free.size = 0.035f;
                    break;
            }
            free.age = 0f;
            free.alive = true;
            free.t.localPosition = free.start;
            free.t.localScale = Vector3.zero;
            free.t.gameObject.SetActive(true);
        }

        /// Détruit la réserve (changement de skin).
        public void Dispose()
        {
            foreach (var p in pool) if (p.t != null) Object.Destroy(p.t.gameObject);
            if (material != null) Object.Destroy(material);
        }

        float Range(float a, float b) => a + (float)random.NextDouble() * (b - a);
    }
}
