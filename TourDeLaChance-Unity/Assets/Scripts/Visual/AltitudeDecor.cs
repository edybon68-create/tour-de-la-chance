using UnityEngine;
using UnityEngine.Rendering;

namespace TourDeLaChance
{
    /// <summary>
    /// Le décor qui change avec l'altitude (spec v3 §9 : du plein jour à la nuit étoilée) :
    /// fond de ciel, couleur de fond de la caméra, lumière principale,
    /// lumière d'ambiance et nuages 3D sous l'étage. Pur affichage, recalculé à partir de
    /// l'étage (GameManager.Tier) : rien à sauvegarder.
    ///
    /// Le changement se fait TOUJOURS pendant l'écran sombre (événement FloorPrepared du
    /// GameManager) : on ne voit jamais le décor changer, on le découvre en arrivant.
    /// </summary>
    public class AltitudeDecor : MonoBehaviour
    {
        [SerializeField] GameManager game;
        [Tooltip("Les 4 paliers, dans l'ordre : 1-3, 4-6, 7-9, 10 et plus.")]
        [SerializeField] AltitudeTier[] tiers = new AltitudeTier[4];
        [SerializeField] Renderer skyRenderer;  // le quad FondCiel
        [SerializeField] Light mainLight;       // Directional Light
        [SerializeField] Material cloudMaterial; // matériau de base des nuages (URP Lit opaque)

        const int GroupCount = 12;
        const float DriftSpeed = 0.25f; // m/s

        static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        Transform[] groups;
        float[] angles, radii; // position de chaque groupe sur l'anneau autour de la tour
        Material cloudInstance;     // copie du matériau dont on change la couleur
        MaterialPropertyBlock skyBlock;
        readonly System.Random random = new System.Random(); // hasard visuel, séparé du piège

        /// Le palier affiché en ce moment (1 à 4).
        public int CurrentTier { get; private set; }

        /// Nom d'un palier pour la bannière (« Dans les nuages ! »).
        public string TierName(int tier)
        {
            var data = Get(tier);
            return data != null ? data.displayName : "";
        }

        /// Combien de groupes de nuages sont visibles (pour les tests).
        public int ActiveCloudGroups
        {
            get
            {
                int n = 0;
                if (groups != null) foreach (var g in groups) if (g.gameObject.activeSelf) n++;
                return n;
            }
        }

        void Awake()
        {
            skyBlock = new MaterialPropertyBlock();
            if (cloudMaterial != null) cloudInstance = new Material(cloudMaterial) { name = "NuageDecor (instance)" };
            CreateCloudPool();
        }

        void OnEnable() => game.FloorPrepared += OnFloorPrepared;
        void OnDisable() => game.FloorPrepared -= OnFloorPrepared;

        void OnDestroy()
        {
            if (cloudInstance != null) Destroy(cloudInstance);
        }

        // Au lancement : décor du palier 1.
        void Start() => Apply(1);

        void OnFloorPrepared() => Apply(game.Tier);

        void Update()
        {
            // Les nuages dérivent lentement (0,25 m/s). Écart avec la spec, voulu : au lieu
            // d'aller tout droit vers +x (ils traverseraient la tour, large de 10 m), ils
            // tournent doucement autour d'elle sur leur anneau : ils ne la touchent jamais.
            if (groups == null) return;
            for (int i = 0; i < groups.Length; i++)
            {
                if (!groups[i].gameObject.activeSelf) continue;
                angles[i] += DriftSpeed / radii[i] * Time.deltaTime; // vitesse angulaire = vitesse / rayon
                SetRingPosition(i, groups[i].localPosition.y);
                groups[i].localScale = Vector3.one * HiddenBehindPlayerFactor(angles[i]);
            }
        }

        /// Applique un palier : fond, lumières, ambiance, nuages. Ne fait rien s'il est déjà affiché.
        public void Apply(int tier)
        {
            var data = Get(tier);
            if (data == null || tier == CurrentTier) return;
            CurrentTier = tier;

            // 1. Le fond : on change l'image (et sa teinte) avec un PropertyBlock, pour ne
            //    pas modifier le fichier du matériau sur le disque.
            if (skyRenderer != null)
            {
                skyRenderer.GetPropertyBlock(skyBlock);
                if (data.background != null) skyBlock.SetTexture(BaseMapId, data.background);
                skyBlock.SetColor(BaseColorId, data.backgroundTint);
                skyRenderer.SetPropertyBlock(skyBlock);
            }

            // 1 bis. La couleur de fond de la caméra (si l'image du ciel manquait).
            var cam = Camera.main;
            if (cam != null) cam.backgroundColor = data.cameraColor;

            // 2. La lumière principale (couleur et intensité seulement).
            if (mainLight != null)
            {
                mainLight.color = data.lightColor;
                mainLight.intensity = data.lightIntensity;
            }

            // 3. La lumière d'ambiance (mode « Gradient » : ciel / horizon / sol).
            RenderSettings.ambientSkyColor = data.ambientSky;
            RenderSettings.ambientEquatorColor = data.ambientEquator;
            RenderSettings.ambientGroundColor = data.ambientGround;

            // 4. Les nuages : on en active le nombre voulu et on les replace au hasard.
            if (cloudInstance != null) cloudInstance.SetColor(BaseColorId, data.cloudColor);
            for (int i = 0; i < GroupCount; i++)
            {
                bool on = i < data.cloudGroups;
                groups[i].gameObject.SetActive(on);
                if (on) PlaceGroup(i, data);
            }
        }

        AltitudeTier Get(int tier)
        {
            int index = Mathf.Clamp(tier, 1, tiers.Length) - 1;
            return tiers.Length > 0 ? tiers[index] : null;
        }

        /// Crée les 12 groupes une fois (3 à 5 sphères aplaties chacun), sans collider ni ombre.
        void CreateCloudPool()
        {
            groups = new Transform[GroupCount];
            angles = new float[GroupCount];
            radii = new float[GroupCount];
            for (int i = 0; i < GroupCount; i++)
            {
                var group = new GameObject("Nuage_" + i).transform;
                group.SetParent(transform, false);
                int puffs = random.Next(3, 6); // 3, 4 ou 5
                for (int j = 0; j < puffs; j++)
                {
                    var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    Destroy(sphere.GetComponent<Collider>());
                    sphere.transform.SetParent(group, false);
                    float d = Range(1.2f, 2.5f);
                    sphere.transform.localScale = new Vector3(d, d * 0.6f, d);
                    // Serrées les unes contre les autres, le long d'une ligne.
                    sphere.transform.localPosition = new Vector3((j - (puffs - 1) * 0.5f) * 0.9f + Range(-0.2f, 0.2f),
                                                                 Range(-0.15f, 0.25f), Range(-0.4f, 0.4f));
                    var r = sphere.GetComponent<Renderer>();
                    if (cloudInstance != null) r.sharedMaterial = cloudInstance;
                    r.shadowCastingMode = ShadowCastingMode.Off;
                    r.receiveShadows = false;
                }
                group.gameObject.SetActive(false);
                groups[i] = group;
            }
        }

        /// Place un groupe au hasard sur un anneau autour de la tour (6 à 14 m du centre),
        /// toujours SOUS le dessus du sol : il ne passe jamais devant les coffres.
        /// Vu de la caméra, un nuage placé juste DERRIÈRE la tour (côté +z) apparaît
        /// derrière la tête du personnage et brouille sa silhouette. Dans ce secteur
        /// (90° ± 20°, mesuré depuis l'axe x), le nuage rétrécit jusqu'à disparaître,
        /// puis regrossit en douceur en sortant (zone de transition de 15°).
        static float HiddenBehindPlayerFactor(float angle)
        {
            float degrees = Mathf.Repeat(angle * Mathf.Rad2Deg, 360f);
            float fromBehind = Mathf.Abs(degrees - 90f);
            return Mathf.SmoothStep(0f, 1f, (fromBehind - 20f) / 15f);
        }

        void PlaceGroup(int i, AltitudeTier data)
        {
            // On tire un angle hors du secteur caché derrière le personnage (60° à 120°).
            do angles[i] = Range(0f, Mathf.PI * 2f);
            while (HiddenBehindPlayerFactor(angles[i]) < 1f);
            radii[i] = Range(6f, 14f);
            SetRingPosition(i, Mathf.Min(Range(data.cloudMinY, data.cloudMaxY), -1.2f));
            groups[i].localScale = Vector3.one;
        }

        void SetRingPosition(int i, float y)
        {
            groups[i].localPosition = new Vector3(Mathf.Cos(angles[i]) * radii[i], y, Mathf.Sin(angles[i]) * radii[i]);
        }

        float Range(float min, float max) => min + (float)random.NextDouble() * (max - min);
    }
}
