using System.Collections.Generic;
using UnityEngine;

namespace TourDeLaChance
{
    /// <summary>
    /// Les animations propres d'un accessoire porté (spec boites §5.6) : battement d'ailes,
    /// auréole qui flotte et tourne, planète en orbite. Ajouté par PlayerAvatar sur le
    /// modèle qu'il vient de poser. Pur affichage : il ne touche à rien d'autre.
    ///
    /// Les pièces à animer sont retrouvées PAR LEUR NOM dans le modèle Blender
    /// (AileGauche, AileDroite, Anneau, Orbite, Planete). Il allume aussi la lumière propre
    /// (émission) des matériaux qui en ont besoin : M_Aureole (1,5) et M_AnneauPlanete (0,5).
    /// L'accessoire étant un enfant de `Joueur`, il suit tout seul les sauts du personnage.
    ///
    /// v3 (spec v3 §8.2) : l'auréole porte 8 perles qui s'allument l'une après l'autre
    /// (« chenillard »), une petite étoile scintille sur une perle toutes les 1,5 s, et elle
    /// suit la tête avec un léger RETARD (comme un ressort) : sur un saut, elle traîne puis
    /// rebondit. Les perles sont cherchées dans le modèle (Perle_0 à Perle_7) ; tant que le
    /// modèle n'en a pas, le code crée 8 petites sphères.
    /// </summary>
    public class AccessoryMotion : MonoBehaviour
    {
        static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        AccessoryAnimation mode;
        Transform left, right, ring, orbit, planet;
        Quaternion leftRest, rightRest, ringRest;
        float time;
        readonly List<Material> ownMaterials = new List<Material>(); // copies à détruire

        // ---------- Auréole v3 ----------
        const int PearlCount = 8;
        const float RingRadius = 0.35f;     // tore Ø 0,70
        const float PearlSize = 0.07f;      // perles Ø 7 cm
        const float RingHeight = 0.30f;     // hauteur de l'anneau au-dessus du point (modèle)
        const float FollowSharpness = 12f;  // lissage du « ressort » : 12 par seconde
        static readonly Color PearlColor = new Color(1f, 0.945f, 0.659f); // #FFF1A8
        readonly Renderer[] pearls = new Renderer[PearlCount];
        Transform star;
        Renderer starRenderer;
        float nextStar = 1.5f, starAge = -1f;
        Vector3 smoothedRing;
        bool hasSmoothed;
        MaterialPropertyBlock block;
        readonly System.Random random = new System.Random(); // hasard visuel seulement

        /// Prépare l'animation (appelé une fois par PlayerAvatar juste après la création).
        public void Setup(AccessoryAnimation animation)
        {
            mode = animation;
            left = FindChild("AileGauche");
            right = FindChild("AileDroite");
            ring = FindChild("Anneau");
            orbit = FindChild("Orbite");
            planet = FindChild("Planete");
            if (left != null) leftRest = left.localRotation;
            if (right != null) rightRest = right.localRotation;
            if (ring != null) ringRest = ring.localRotation;

            // Émission (art-bible §4.1) : réservée au Légendaire et au Secret.
            EnableEmission("M_Aureole", 1.2f); // v3 : anneau à 1,2, les perles montent à 1,5
            EnableEmission("M_AnneauPlanete", 0.5f);
            if (mode == AccessoryAnimation.Aureole && ring != null) BuildPearls();
            Tick(0f);
        }

        /// Les 8 perles de la guirlande (et l'étoile qui scintille), enfants de l'anneau.
        void BuildPearls()
        {
            block = new MaterialPropertyBlock();
            Material emissive = null;
            var ringRenderer = ring.GetComponent<Renderer>();
            if (ringRenderer != null) emissive = ringRenderer.sharedMaterial; // copie émissive faite ci-dessus
            for (int i = 0; i < PearlCount; i++)
            {
                var found = FindChild("Perle_" + i);
                if (found == null)
                {
                    var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    Destroy(go.GetComponent<Collider>());
                    go.name = "Perle_" + i;
                    found = go.transform;
                    found.SetParent(ring, false);
                    float a = i * Mathf.PI * 2f / PearlCount;
                    found.localPosition = new Vector3(Mathf.Cos(a) * RingRadius, 0f, Mathf.Sin(a) * RingRadius);
                    found.localScale = Vector3.one * PearlSize;
                    var r = go.GetComponent<Renderer>();
                    if (emissive != null) r.sharedMaterial = emissive;
                    r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                }
                pearls[i] = found.GetComponent<Renderer>();
            }
            var s = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(s.GetComponent<Collider>());
            s.name = "EtoilePerle";
            star = s.transform;
            star.SetParent(ring, false);
            star.localRotation = Quaternion.Euler(45f, 0f, 45f);
            star.localScale = Vector3.zero;
            starRenderer = s.GetComponent<Renderer>();
            if (emissive != null) starRenderer.sharedMaterial = emissive;
            starRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        void Update()
        {
            time += Time.deltaTime;
            Tick(time);
        }

        void Tick(float t)
        {
            switch (mode)
            {
                case AccessoryAnimation.AilesAnge:
                    Flap(t, 8f, 1.6f);
                    break;
                case AccessoryAnimation.AilesPapillon:
                    Flap(t, 20f, 0.9f);
                    break;
                case AccessoryAnimation.Aureole:
                    if (ring != null) TickHalo(t);
                    break;
                case AccessoryAnimation.Planete:
                    // L'orbite tourne (un tour en 6 s), la planète tourne sur elle-même.
                    // On ne touche pas à « Inclinaison » : c'est elle qui penche l'orbite de 15°.
                    if (orbit != null) orbit.localRotation = Quaternion.Euler(0f, 60f * t, 0f);
                    if (planet != null) planet.localRotation = Quaternion.Euler(0f, 90f * t, 0f);
                    break;
            }
        }

        /// Auréole v3 : flotte (±4 cm, 2 s), tourne (30°/s), suit la tête avec un retard,
        /// chenillard des perles (0,4 → 1,5 → 0,4, cycle 0,8 s, décalage 0,1 s) et étoile.
        void TickHalo(float t)
        {
            // 1. Où l'anneau DEVRAIT être (au-dessus du point d'accroche, qui suit la tête)…
            Vector3 local = new Vector3(0f, RingHeight + 0.04f * Mathf.Sin(2f * Mathf.PI * t / 2f), 0f);
            Vector3 target = transform.TransformPoint(local);
            // …2. et il y va avec un lissage (ressort) : sur un saut, il traîne puis rattrape.
            if (!hasSmoothed || Time.deltaTime <= 0f) { smoothedRing = target; hasSmoothed = true; }
            else smoothedRing = Vector3.Lerp(smoothedRing, target, 1f - Mathf.Exp(-FollowSharpness * Time.deltaTime));
            ring.position = smoothedRing;
            ring.localRotation = ringRest * Quaternion.Euler(0f, 30f * t, 0f);

            if (block == null) return;
            for (int i = 0; i < PearlCount; i++)
            {
                if (pearls[i] == null) continue;
                float phase = (t - 0.1f * i) / 0.8f;
                float k = 0.5f * (1f - Mathf.Cos(2f * Mathf.PI * phase)); // 0 → 1 → 0
                block.SetColor(BaseColorId, PearlColor);
                block.SetColor(EmissionColorId, PearlColor * Mathf.Lerp(0.4f, 1.5f, k));
                pearls[i].SetPropertyBlock(block);
            }

            // Une petite étoile sur une perle au hasard toutes les 1,5 s : 0 → 0,12 m → 0 en 0,3 s.
            if (t >= nextStar && star != null)
            {
                nextStar = t + 1.5f;
                starAge = 0f;
                var p = pearls[random.Next(PearlCount)];
                if (p != null) star.localPosition = p.transform.localPosition + Vector3.up * 0.03f;
            }
            if (starAge >= 0f && star != null)
            {
                starAge += Time.deltaTime;
                float k = Mathf.Clamp01(starAge / 0.3f);
                star.localScale = Vector3.one * 0.12f * Ease.Sine(k);
                block.SetColor(BaseColorId, Color.white);
                block.SetColor(EmissionColorId, Color.white * 1.5f);
                starRenderer.SetPropertyBlock(block);
                if (k >= 1f) { starAge = -1f; star.localScale = Vector3.zero; }
            }
        }

        /// Pour les tests : l'écart actuel entre l'anneau et sa place « idéale » (le retard).
        public float HaloLag => ring == null ? 0f : Vector3.Distance(ring.position,
            transform.TransformPoint(new Vector3(0f, RingHeight + 0.04f * Mathf.Sin(2f * Mathf.PI * time / 2f), 0f)));

        /// Battement en miroir : l'aile gauche tourne de +a, la droite de −a autour de l'axe
        /// vertical de leur attache, avec a = amplitude × sin(2πt / période).
        void Flap(float t, float amplitude, float period)
        {
            float a = amplitude * Mathf.Sin(2f * Mathf.PI * t / period);
            if (left != null) left.localRotation = leftRest * Quaternion.Euler(0f, a, 0f);
            if (right != null) right.localRotation = rightRest * Quaternion.Euler(0f, -a, 0f);
        }

        /// Allume l'émission (lumière propre) des matériaux de ce nom, sur des COPIES
        /// (les matériaux importés du modèle restent intacts). Couleur émise = couleur de
        /// base × intensité ; avec le Bloom (seuil 1,0), cela donne une lueur douce.
        void EnableEmission(string materialName, float intensity)
        {
            foreach (var r in GetComponentsInChildren<Renderer>(true))
            {
                var shared = r.sharedMaterials;
                bool found = false;
                for (int i = 0; i < shared.Length; i++)
                {
                    if (shared[i] == null || !shared[i].name.StartsWith(materialName)) continue;
                    // On compare le nom exact (sans « (Instance) ») pour ne pas confondre
                    // M_Aureole avec un autre nom qui commencerait pareil.
                    string clean = shared[i].name.Replace(" (Instance)", "");
                    if (clean != materialName) continue;
                    var copy = new Material(shared[i]) { name = materialName + " (émission)" };
                    copy.EnableKeyword("_EMISSION");
                    copy.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                    Color baseColor = copy.HasProperty(BaseColorId) ? copy.GetColor(BaseColorId) : Color.white;
                    copy.SetColor(EmissionColorId, baseColor * intensity);
                    shared[i] = copy;
                    ownMaterials.Add(copy);
                    found = true;
                }
                if (found) r.sharedMaterials = shared;
            }
        }

        Transform FindChild(string childName)
        {
            foreach (var t in GetComponentsInChildren<Transform>(true))
                if (t.name == childName) return t;
            return null;
        }

        void OnDestroy()
        {
            foreach (var m in ownMaterials) if (m != null) Destroy(m);
        }
    }
}
