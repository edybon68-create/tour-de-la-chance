using UnityEngine;

namespace TourDeLaChance
{
    /// <summary>
    /// Le « pouf » cartoon d'un piège (spec §3.6) : 10 petites sphères lavande qui
    /// s'écartent en montant, gonflent puis disparaissent. Rien d'effrayant.
    /// Comme les pièces : une réserve de 10 sphères créées une fois, sans collider.
    /// </summary>
    public class TrapCloud : MonoBehaviour
    {
        [SerializeField] Material cloudMaterial; // lavande grisée #B9B4C8, smoothness 0,1
        const int PuffCount = 10;
        const float Duration = 0.6f;

        readonly Transform[] puffs = new Transform[PuffCount];
        readonly Vector3[] directions = new Vector3[PuffCount];
        Vector3 origin;
        float age = -1f; // < 0 : pas de nuage en cours

        // Hasard visuel séparé de celui du piège.
        readonly System.Random random = new System.Random();

        public bool IsPlaying => age >= 0f;

        void Awake()
        {
            for (int i = 0; i < PuffCount; i++)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                go.name = "Pouf_" + i;
                Destroy(go.GetComponent<Collider>());
                go.transform.SetParent(transform, false);
                if (cloudMaterial != null) go.GetComponent<Renderer>().sharedMaterial = cloudMaterial;
                go.SetActive(false);
                puffs[i] = go.transform;
            }
        }

        /// Lance le nuage depuis le centre d'un coffre.
        public void Play(Vector3 chestPosition)
        {
            origin = new Vector3(chestPosition.x, 0.9f, chestPosition.z);
            int shown = GraphicsLevel.Particles(PuffCount); // v6 §12.2 : moitié des bouffées en Faible
            for (int i = 0; i < PuffCount; i++)
            {
                if (i >= shown) { puffs[i].gameObject.SetActive(false); continue; }
                // Directions réparties en cercle, avec un petit décalage au hasard.
                float angle = (i + (float)random.NextDouble() * 0.6f) / PuffCount * Mathf.PI * 2f;
                directions[i] = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                puffs[i].gameObject.SetActive(true);
            }
            age = 0f;
            Update();
        }

        void Update()
        {
            if (age < 0f) return;
            float t = Mathf.Clamp01(age / Duration);
            float move = Ease.Out(t);
            // Diamètre : 0,25 → 0,6 pendant les 0,3 premières secondes, puis → 0.
            float size = age < 0.3f
                ? Mathf.Lerp(0.25f, 0.6f, age / 0.3f)
                : Mathf.Lerp(0.6f, 0f, (age - 0.3f) / (Duration - 0.3f));

            for (int i = 0; i < PuffCount; i++)
            {
                puffs[i].position = origin + directions[i] * (0.8f * move) + Vector3.up * (0.5f * move);
                puffs[i].localScale = Vector3.one * Mathf.Max(0f, size);
            }

            if (age >= Duration)
            {
                foreach (var p in puffs) p.gameObject.SetActive(false);
                age = -1f;
                return;
            }
            age += Time.deltaTime;
        }
    }
}
