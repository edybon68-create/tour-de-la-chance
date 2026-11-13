using System.Collections.Generic;
using UnityEngine;

namespace TourDeLaChance
{
    /// <summary>
    /// Le STUDIO 3D de l'ouverture des boîtes (spec v5 §6, art bible §14). Un petit décor HORS
    /// de la tour (0 ; −100 ; 0) : un podium violet à bord doré, un fond dégradé violet nuit, 3
    /// lumières et sa caméra « CameraBoite », qui filme TOUT l'écran pendant l'ouverture (la
    /// caméra principale est coupée pendant ce temps, puis rallumée). L'interface (textes,
    /// halo, cinématique du Secret, boutons) se dessine par-dessus, comme avant.
    ///
    /// Il ne décide RIEN : BoxOpeningController lui donne l'horloge « t » (secondes depuis le
    /// clic sur « Ouvrir ») et le résultat DÉJÀ tiré et sauvegardé, et il place tout.
    ///
    /// ÉQUITÉ (bible §14.1) : jusqu'à R = 1,75 s, la boîte, ses mouvements, ses sons et sa
    /// lumière (BLANCHE neutre) sont IDENTIQUES pour toutes les raretés ; les petites variations
    /// (trajectoire du nœud et du couvercle) viennent d'un générateur visuel SÉPARÉ, tiré au
    /// clic ; l'objet gagné est désactivé et invisible avant R.
    ///
    /// Le décor est fabriqué par le code au démarrage (primitives + matériaux URP Lit), et tout
    /// est dessiné une fois par une caméra cachée (préchargement §6.7).
    /// </summary>
    public class BoxStudio : MonoBehaviour
    {
        // ---------- La chronologie commune (bible §14.4), en secondes depuis le clic ----------
        public const float DropStart = 0.10f, DropLand = 0.24f, LandSettle = 0.36f;
        public const float ShakeStart = 0.30f, ShakeEnd = 1.50f;
        public static readonly float[] HopTimes = { 0.55f, 0.95f, 1.35f };
        public const float CrackTime = 1.20f;
        public const float KnotTime = 1.58f, LidTime = 1.62f;
        public const float Reveal = 1.75f; // R

        static readonly Color NeutralWhite = new Color(1f, 0.965f, 0.91f); // #FFF6E8

        [Tooltip("Matériau URP Lit « Transparent » (le même que le Cristal) pour le faisceau.")]
        [SerializeField] Material transparentTemplate;
        [Tooltip("Petit cheval vierge (mode vitrine, échelle 0,5) pour la révélation des skins.")]
        [SerializeField] PlayerAvatar miniHorse;
        [Tooltip("Modèle de secours si une boîte n'a pas de modèle d'ouverture.")]
        [SerializeField] GameObject genericBoxPrefab;

        Camera studioCam, mainCam;
        readonly List<Light> lights = new List<Light>();
        Light innerLight;
        Transform boxRoot;      // ce qui saute et tremble (contient le modèle)
        Transform rewardPivot;  // l'objet gagné (désactivé avant R)
        GameObject beam;
        Material beamMaterial;
        GameObject placeholderToken;
        Material tokenMaterial;
        bool built;

        // Un modèle d'ouverture préparé par boîte (créé une fois au démarrage).
        class BoxModel
        {
            public GameObject root;
            public Transform lid, knot;
            public Vector3 lidRestPos, knotRestPos;
            public Quaternion lidRestRot, knotRestRot;
            public Renderer body;
            public int glowIndex = -1;
            public readonly List<(Renderer r, int index)> tinted = new List<(Renderer, int)>();
        }
        readonly Dictionary<GameObject, BoxModel> models = new Dictionary<GameObject, BoxModel>();
        BoxModel current;
        GameObject rewardInstance;
        MaterialPropertyBlock block;

        // Le résultat raconté et ses réglages d'affichage (tout est connu AVANT t = 0).
        Color rarityColor = Color.white;
        bool legendary, secret, skin;
        Vector3 knotVelocity, knotSpinAxis, lidVelocity;
        float lidSpinZ;
        readonly System.Random fxRandom = new System.Random(); // hasard VISUEL séparé (jamais le tirage)

        public bool Active => studioCam != null && studioCam.enabled;
        public Camera Camera => studioCam;

        // v6 (QA n°35) : le cadrage de la caméra du studio. Avant : (0 ; 2,1 ; −6,2) visant 1,45 m,
        // ce qui coupait le socle en bas et faisait passer le cartouche du résultat sur la boîte.
        static readonly Vector3 CameraPosition = new Vector3(0f, 1.75f, -7.4f);
        const float CameraAimHeight = 0.55f;
        /// La position 3D de l'objet gagné (pour centrer les effets de l'interface dessus).
        public Vector3 RewardWorldPosition => rewardPivot != null ? rewardPivot.position : transform.position + Vector3.up * 1.55f;

        void Awake() => Build();

        void Start()
        {
            // Préchargement (§6.7) : modèles de toutes les boîtes, podium, faisceau, petit cheval.
            var objects = new List<GameObject>();
            foreach (var m in models.Values) objects.Add(m.root);
            objects.Add(beam);
            if (placeholderToken != null) objects.Add(placeholderToken);
            if (miniHorse != null) objects.Add(miniHorse.gameObject);
            RenderPreloader.RenderOnce(objects);
            SetActive(false);
        }

        void OnDestroy()
        {
            if (beamMaterial != null) Destroy(beamMaterial);
            if (tokenMaterial != null) Destroy(tokenMaterial);
        }

        static Material NewLit(Color color, float smoothness = 0.4f, float metallic = 0f)
        {
            var pipe = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline;
            var m = pipe != null && pipe.defaultMaterial != null ? new Material(pipe.defaultMaterial) : new Material(Shader.Find("Universal Render Pipeline/Lit"));
            m.SetColor("_BaseColor", color);
            m.SetFloat("_Smoothness", smoothness);
            m.SetFloat("_Metallic", metallic);
            return m;
        }

        static GameObject Primitive(PrimitiveType type, string name, Transform parent)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            var col = go.GetComponent<Collider>();
            if (col != null) Destroy(col); // aucun collider : rien ne doit gêner les clics
            return go;
        }

        /// Fabrique le décor (une seule fois).
        void Build()
        {
            if (built) return;
            built = true;
            block = new MaterialPropertyBlock();

            // Le podium : Ø 2 m, bois verni violet #5B2D82, bord doré.
            var podium = Primitive(PrimitiveType.Cylinder, "Podium", transform);
            podium.transform.localPosition = new Vector3(0f, -0.125f, 0f);
            podium.transform.localScale = new Vector3(2f, 0.125f, 2f);
            podium.GetComponent<Renderer>().sharedMaterial = NewLit(new Color(0.357f, 0.176f, 0.51f), 0.7f);
            var rim = Primitive(PrimitiveType.Cylinder, "BordDore", transform);
            rim.transform.localPosition = new Vector3(0f, -0.14f, 0f);
            rim.transform.localScale = new Vector3(2.08f, 0.11f, 2.08f);
            rim.GetComponent<Renderer>().sharedMaterial = NewLit(new Color(0.91f, 0.72f, 0.19f), 0.7f, 1f);

            // Le fond : une grande sphère vue de l'intérieur, dégradé violet nuit (#1F1730 → #3A2858).
            var back = Primitive(PrimitiveType.Sphere, "Fond", transform);
            back.transform.localPosition = new Vector3(0f, 2f, 0f);
            back.transform.localScale = Vector3.one * 30f;
            var backMat = NewLit(Color.black, 0f);
            backMat.SetFloat("_Cull", 1f); // on voit l'intérieur de la sphère
            backMat.EnableKeyword("_EMISSION");
            backMat.SetTexture("_EmissionMap", UiFxTextures.VerticalGradient(new Color(0.227f, 0.157f, 0.345f), new Color(0.122f, 0.09f, 0.188f)));
            backMat.SetColor("_EmissionColor", Color.white);
            back.GetComponent<Renderer>().sharedMaterial = backMat;
            back.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            // La caméra « CameraBoite » (bible §14.2 : (0 ; 1,9 ; −4,4), 34°), reculée en capture
            // à (0 ; 2,1 ; −6,2) et visant 1,45 m : la boîte ouverte, l'objet qui flotte au-dessus
            // et les textes du résultat tiennent ensemble à l'écran.
            var camGo = new GameObject("CameraBoite");
            camGo.transform.SetParent(transform, false);
            // v6 (QA n°35) : la caméra recule et vise plus bas (0,55 m) : toute la scène (socle,
            // boîte, objet qui flotte) tient dans les deux tiers HAUTS de l'écran, et le cartouche
            // du résultat se place dessous, sans rien recouvrir ni couper le socle.
            camGo.transform.localPosition = CameraPosition;
            camGo.transform.LookAt(transform.position + Vector3.up * CameraAimHeight);
            studioCam = camGo.AddComponent<Camera>();
            studioCam.fieldOfView = 34f;
            studioCam.clearFlags = CameraClearFlags.SolidColor;
            studioCam.backgroundColor = new Color(0.122f, 0.09f, 0.188f);
            studioCam.depth = 10f;
            studioCam.nearClipPlane = 0.1f;
            studioCam.farClipPlane = 60f;
            var urp = camGo.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            urp.renderPostProcessing = true;
            studioCam.enabled = false;

            // 3 lumières (principale chaude, contre-jour lavande, appoint doux) + la lumière
            // intérieure de la boîte (blanche avant R).
            lights.Add(NewLight("LumierePrincipale", LightType.Spot, new Color(1f, 0.945f, 0.84f), 4f, new Vector3(-1.5f, 4f, -2f), 8f, 50f));
            lights.Add(NewLight("ContreJour", LightType.Point, new Color(0.7f, 0.62f, 1f), 2f, new Vector3(1.2f, 2.5f, 2f), 6f, 0f));
            lights.Add(NewLight("Appoint", LightType.Point, new Color(1f, 0.95f, 0.9f), 0.8f, new Vector3(2f, 1.5f, -2.5f), 7f, 0f));
            innerLight = NewLight("LumiereInterieure", LightType.Point, NeutralWhite, 0f, new Vector3(0f, 0.6f, 0f), 3f, 0f);
            lights.Add(innerLight);

            // La boîte (racine animée) et l'objet gagné.
            boxRoot = new GameObject("Boite").transform;
            boxRoot.SetParent(transform, false);
            rewardPivot = new GameObject("ObjetGagne").transform;
            rewardPivot.SetParent(transform, false);

            // Un modèle par boîte du catalogue (même prefab partagé : un seul exemplaire).
            foreach (var box in Shop.Boxes)
            {
                var prefab = box.openingPrefab != null ? box.openingPrefab : genericBoxPrefab;
                if (prefab == null || models.ContainsKey(prefab)) continue;
                models[prefab] = PrepareModel(prefab);
            }

            // Le faisceau : un cône ouvert transparent (rayon bas 0,42, haut 0,9, 3,5 m).
            beam = new GameObject("Faisceau");
            beam.transform.SetParent(transform, false);
            beam.AddComponent<MeshFilter>().sharedMesh = BuildCone(0.42f, 0.9f, 1f, 24);
            beamMaterial = transparentTemplate != null ? new Material(transparentTemplate) : NewLit(Color.white);
            beamMaterial.name = "Faisceau (ouverture)";
            beamMaterial.SetFloat("_Cull", 0f);
            beamMaterial.EnableKeyword("_EMISSION");
            var beamRenderer = beam.AddComponent<MeshRenderer>();
            beamRenderer.sharedMaterial = beamMaterial;
            beamRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            beam.SetActive(false);

            // Le jeton de secours (accessoire sans modèle : art de la partie 2).
            placeholderToken = Primitive(PrimitiveType.Cylinder, "JetonDeSecours", rewardPivot);
            placeholderToken.transform.localScale = new Vector3(0.6f, 0.06f, 0.6f);
            placeholderToken.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            tokenMaterial = NewLit(Color.white, 0.8f, 0.3f);
            placeholderToken.GetComponent<Renderer>().sharedMaterial = tokenMaterial;
            placeholderToken.SetActive(false);

            if (miniHorse != null)
            {
                miniHorse.transform.SetParent(rewardPivot, false);
                miniHorse.transform.localPosition = new Vector3(0f, -0.55f, 0f);
                miniHorse.transform.localScale = Vector3.one * 0.5f;
                miniHorse.transform.localRotation = Quaternion.identity;
                miniHorse.gameObject.SetActive(false);
            }
        }

        Light NewLight(string name, LightType type, Color color, float intensity, Vector3 pos, float range, float angle)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = pos;
            go.transform.LookAt(transform.position + Vector3.up * 0.6f);
            var l = go.AddComponent<Light>();
            l.type = type;
            l.color = color;
            l.intensity = intensity;
            l.range = range;
            if (type == LightType.Spot) { l.spotAngle = angle; l.shadows = LightShadows.Soft; }
            l.enabled = false;
            return l;
        }

        BoxModel PrepareModel(GameObject prefab)
        {
            var root = Instantiate(prefab, boxRoot, false);
            root.name = prefab.name;
            root.transform.localScale = Vector3.one * 1.25f; // ≈ 1 m de large (le modèle fait 0,8 m)
            foreach (var c in root.GetComponentsInChildren<Collider>()) Destroy(c);
            var m = new BoxModel { root = root };
            m.lid = FindDeep(root.transform, "Couvercle");
            m.knot = FindDeep(root.transform, "Noeud"); // peut manquer (boîtes « coffre »)
            if (m.lid != null) { m.lidRestPos = m.lid.localPosition; m.lidRestRot = m.lid.localRotation; }
            if (m.knot != null) { m.knotRestPos = m.knot.localPosition; m.knotRestRot = m.knot.localRotation; }
            var corps = FindDeep(root.transform, "Corps");
            m.body = corps != null ? corps.GetComponent<Renderer>() : null;
            // Les matériaux « Corps » / « Bord » (teintables) et « Lueur » (émission pilotée).
            foreach (var r in root.GetComponentsInChildren<Renderer>())
            {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    if (mats[i] == null) continue;
                    string n = mats[i].name;
                    if (n.EndsWith("_Corps") || n.EndsWith("_Bord")) m.tinted.Add((r, i));
                    if (r == m.body && n.EndsWith("_Lueur")) { m.glowIndex = i; mats[i].EnableKeyword("_EMISSION"); }
                }
            }
            root.SetActive(false);
            return m;
        }

        static Transform FindDeep(Transform t, string name)
        {
            if (t.name == name) return t;
            foreach (Transform c in t) { var f = FindDeep(c, name); if (f != null) return f; }
            return null;
        }

        /// Un cône ouvert (sans fonds) de hauteur 1 : la hauteur est réglée par l'échelle Y.
        static Mesh BuildCone(float bottomRadius, float topRadius, float height, int sides)
        {
            var verts = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();
            for (int i = 0; i <= sides; i++)
            {
                float a = i * Mathf.PI * 2f / sides;
                var dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                verts.Add(dir * bottomRadius); uvs.Add(new Vector2(i / (float)sides, 0f));
                verts.Add(dir * topRadius + Vector3.up * height); uvs.Add(new Vector2(i / (float)sides, 1f));
                if (i < sides)
                {
                    int b = i * 2;
                    tris.Add(b); tris.Add(b + 1); tris.Add(b + 2);
                    tris.Add(b + 1); tris.Add(b + 3); tris.Add(b + 2);
                }
            }
            var mesh = new Mesh { name = "Faisceau (cône)" };
            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            return mesh;
        }

        // =====================================================================
        // Ouverture : début, horloge, fin
        // =====================================================================

        /// Allume le studio (et éteint la caméra principale) ou l'inverse.
        public void SetActive(bool on)
        {
            Build();
            if (on)
            {
                mainCam = Camera.main != null && Camera.main != studioCam ? Camera.main : mainCam;
                if (mainCam != null) mainCam.enabled = false; // économie (§6.2)
            }
            else if (mainCam != null) mainCam.enabled = true;
            studioCam.enabled = on;
            foreach (var l in lights) l.enabled = on;
            if (!on)
            {
                foreach (var m in models.Values) m.root.SetActive(false);
                beam.SetActive(false);
                HideReward();
            }
        }

        /// Prépare une ouverture : la boîte fermée (nœud et couvercle à leur place), l'objet
        /// gagné préparé mais CACHÉ, et les trajectoires « vivantes » tirées au hasard visuel.
        public void Begin(BoxResult result)
        {
            SetActive(true);
            var prefab = result.Box != null && result.Box.openingPrefab != null ? result.Box.openingPrefab : genericBoxPrefab;
            foreach (var m in models.Values) m.root.SetActive(false);
            current = prefab != null && models.TryGetValue(prefab, out var found) ? found : null;
            if (current != null)
            {
                current.root.SetActive(true);
                if (current.lid != null) { current.lid.gameObject.SetActive(true); current.lid.localPosition = current.lidRestPos; current.lid.localRotation = current.lidRestRot; }
                if (current.knot != null) { current.knot.gameObject.SetActive(true); current.knot.localPosition = current.knotRestPos; current.knot.localRotation = current.knotRestRot; current.knot.localScale = Vector3.one; }
                // La teinte de la boîte (Pirates, Animaux : modèle réutilisé teinté). Connue
                // AVANT d'acheter : elle ne dit rien du résultat.
                Color tint = result.Box != null ? result.Box.openingTint : Color.white;
                foreach (var (r, i) in current.tinted)
                {
                    r.GetPropertyBlock(block, i);
                    var baseColor = r.sharedMaterials[i].GetColor("_BaseColor");
                    block.SetColor("_BaseColor", tint == Color.white ? baseColor : Color.Lerp(baseColor, tint, 0.85f));
                    block.SetColor("_EmissionColor", Color.black);
                    r.SetPropertyBlock(block, i);
                }
            }

            rarityColor = result.Rarity != null ? result.Rarity.color : Color.white;
            legendary = result.Rarity != null && result.Rarity.id == "legendaire";
            secret = result.Rarity != null && result.Rarity.id == "secret";
            skin = result.Item != null && result.Item.Category == ShopCategory.Skin;

            // Trajectoires du nœud et du couvercle : générateur VISUEL séparé (bible §14.1 n°4).
            knotVelocity = new Vector3(Range(-0.8f, 0.8f), 5f, Range(-0.3f, 0.3f));
            knotSpinAxis = new Vector3(Range(-1f, 1f), Range(-1f, 1f), Range(-1f, 1f)).normalized;
            lidVelocity = new Vector3(Range(-0.3f, 0.3f), 6f, 1.5f);
            lidSpinZ = Range(-40f, 40f);

            PrepareReward(result.Item);
            beam.SetActive(false);
        }

        /// Prépare l'objet gagné : DÉSACTIVÉ jusqu'à R (on ne peut pas le deviner par la fente).
        void PrepareReward(ShopItem item)
        {
            HideReward();
            if (item == null) return;
            if (item.Category == ShopCategory.Skin)
            {
                if (miniHorse != null) miniHorse.ShowOnly(item);
            }
            else if (item.AccessoryPrefab != null)
            {
                rewardInstance = Instantiate(item.AccessoryPrefab, rewardPivot, false);
                rewardInstance.transform.localPosition = Vector3.zero;
                rewardInstance.transform.localScale = Vector3.one * 1.1f; // spec : 1,5 ; réduit en capture
                foreach (var c in rewardInstance.GetComponentsInChildren<Collider>()) Destroy(c);
                // La Planète tourne sur place (sans orbite) : les autres gardent leur animation.
                if (item.Animation != AccessoryAnimation.Aucune && item.Animation != AccessoryAnimation.Planete)
                    rewardInstance.AddComponent<AccessoryMotion>().Setup(item.Animation);
                rewardInstance.AddComponent<AccessoryRig>();
                rewardInstance.SetActive(false);
            }
            else
            {
                tokenMaterial.SetColor("_BaseColor", rarityColor);
            }
            rewardPivot.gameObject.SetActive(false);
        }

        void HideReward()
        {
            if (rewardInstance != null) Destroy(rewardInstance);
            rewardInstance = null;
            if (placeholderToken != null) placeholderToken.SetActive(false);
            if (miniHorse != null) miniHorse.gameObject.SetActive(false);
            if (rewardPivot != null) rewardPivot.gameObject.SetActive(false);
        }

        void ShowReward(bool on)
        {
            rewardPivot.gameObject.SetActive(on);
            if (!on) return;
            if (skin) { if (miniHorse != null) miniHorse.gameObject.SetActive(true); }
            else if (rewardInstance != null) rewardInstance.SetActive(true);
            else placeholderToken.SetActive(true);
        }

        public void End() => SetActive(false);

        float Range(float a, float b) => a + (float)fxRandom.NextDouble() * (b - a);

        /// Place tout selon l'horloge. cardStart = arrivée de l'objet (R, ou R + 2,4 s pour le
        /// Secret) ; revealEnd = fin de la révélation ; skipped = animation passée (état final).
        public void Render(float t, float cardStart, float revealEnd, bool skipped)
        {
            if (!Active || current == null) return;
            var box = boxRoot;

            if (skipped)
            {
                // État final direct : boîte ouverte sans couvercle ni nœud, faisceau à 60 %,
                // objet en place qui tourne et flotte.
                box.localPosition = Vector3.zero; box.localRotation = Quaternion.identity; box.localScale = Vector3.one;
                if (current.lid != null) current.lid.gameObject.SetActive(false);
                if (current.knot != null) current.knot.gameObject.SetActive(false);
                SetBeam(3.5f, secret ? Rainbow(t) : rarityColor, 0.6f);
                SetGlow(legendary ? rarityColor * 0.6f : NeutralWhite * 0.6f);
                innerLight.color = secret ? Rainbow(t) : rarityColor;
                innerLight.intensity = 0.8f;
                ShowReward(true);
                PlaceRewardFloating(t, 1.55f, 1f);
                return;
            }

            // --- 1. Chute et atterrissage (0,10 → 0,36) ---
            float y = 0f, rotZ = 0f, rotY = 0f;
            Vector3 scale = Vector3.one;
            if (t < DropStart) y = 2.2f;
            else if (t < DropLand) y = Mathf.Lerp(2.2f, 0f, Ease.In((t - DropStart) / (DropLand - DropStart)));
            else if (t < LandSettle)
            {
                float k = (t - DropLand) / (LandSettle - DropLand);
                scale = k < 0.5f ? Vector3.Lerp(new Vector3(1.18f, 0.78f, 1.18f), new Vector3(0.94f, 1.08f, 0.94f), k * 2f)
                                 : Vector3.Lerp(new Vector3(0.94f, 1.08f, 0.94f), Vector3.one, (k - 0.5f) * 2f);
            }

            // --- 2. Secousse (0,30 → 1,50) : rotation Z 10 allers-retours/s, 2° → 10°, rotation
            //        Y ±4° à 2,5 Hz, 3 petits sauts de 0,10 m. IDENTIQUE pour toutes les raretés.
            float lidLift = 0f;
            if (t >= ShakeStart && t < ShakeEnd)
            {
                float s = t - ShakeStart;
                float amp = Mathf.Lerp(2f, 10f, s / (ShakeEnd - ShakeStart));
                rotZ = amp * Mathf.Sin(2f * Mathf.PI * 10f * s);
                rotY = 4f * Mathf.Sin(2f * Mathf.PI * 2.5f * s);
                foreach (float hop in HopTimes)
                {
                    float h = t - hop;
                    if (h >= 0f && h < 0.2f) y += 0.10f * Mathf.Sin(h / 0.2f * Mathf.PI);
                    if (h >= 0.2f && h < 0.26f) scale = new Vector3(1.08f, 0.90f, 1.08f);
                    float hl = t - hop - 0.05f; // le couvercle suit avec 0,05 s de retard
                    if (hl >= 0f && hl < 0.2f) lidLift = 0.03f * Mathf.Sin(hl / 0.2f * Mathf.PI);
                }
            }
            // --- 3. Anticipation (1,50 → 1,58) puis détente (1,62 → 1,80) ---
            if (t >= ShakeEnd && t < KnotTime) scale = Vector3.Lerp(Vector3.one, new Vector3(1.12f, 0.82f, 1.12f), Ease.In((t - ShakeEnd) / (KnotTime - ShakeEnd)));
            else if (t >= KnotTime && t < LidTime) scale = new Vector3(1.12f, 0.82f, 1.12f);
            else if (t >= LidTime && t < 1.80f)
            {
                float k = (t - LidTime) / (1.80f - LidTime);
                scale = Vector3.LerpUnclamped(new Vector3(0.92f, 1.15f, 0.92f), Vector3.one, Ease.OutBack(k));
            }
            // Secret : la boîte s'écrase à l'impact (R + 3,4 s).
            if (secret)
            {
                float imp = t - Reveal - SecretCinematic.ImpactStart;
                if (imp >= 0f && imp < 0.2f) scale = Vector3.Lerp(new Vector3(1.15f, 0.85f, 1.15f), Vector3.one, imp / 0.2f);
            }
            box.localPosition = new Vector3(0f, y, 0f);
            box.localRotation = Quaternion.Euler(0f, rotY, rotZ);
            box.localScale = scale;

            // --- Le couvercle : se soulève aux sauts, puis SAUTE à 1,62 (vers le haut et
            //     l'arrière, bascule en arrière = rotation X locale positive), disparaît à 2,30.
            if (current.lid != null)
            {
                float since = t - LidTime;
                if (since < 0f)
                {
                    current.lid.gameObject.SetActive(true);
                    current.lid.localPosition = current.lidRestPos + Vector3.up * lidLift;
                    current.lid.localRotation = current.lidRestRot;
                }
                else
                {
                    current.lid.gameObject.SetActive(t < 2.30f);
                    var p = lidVelocity * since + 0.5f * new Vector3(0f, -9f, 0f) * since * since;
                    current.lid.localPosition = current.lidRestPos + p / 1.25f;
                    current.lid.localRotation = current.lidRestRot * Quaternion.Euler(360f * since, 0f, lidSpinZ * since);
                }
            }
            // --- Le nœud s'envole à 1,58 (vitesse 5 m/s vers le haut), rétrécit de 1,90 à 2,10.
            if (current.knot != null)
            {
                float since = t - KnotTime;
                if (since < 0f)
                {
                    current.knot.gameObject.SetActive(true);
                    current.knot.localPosition = current.knotRestPos;
                    current.knot.localRotation = current.knotRestRot;
                    current.knot.localScale = Vector3.one;
                }
                else
                {
                    current.knot.gameObject.SetActive(t < 2.10f);
                    var p = knotVelocity * since + 0.5f * new Vector3(0f, -9f, 0f) * since * since;
                    current.knot.localPosition = current.knotRestPos + p / 1.25f;
                    current.knot.localRotation = current.knotRestRot * Quaternion.AngleAxis(900f * since, knotSpinAxis);
                    current.knot.localScale = Vector3.one * (t < 1.90f ? 1f : Mathf.Clamp01(1f - (t - 1.90f) / 0.2f));
                }
            }

            // --- La lueur et la lumière intérieure : BLANCHES jusqu'à R (aucune couleur) ---
            float glow = t < ShakeStart ? 0f : t < ShakeEnd ? Mathf.Lerp(0.3f, 1f, (t - ShakeStart) / (ShakeEnd - ShakeStart)) : t < LidTime ? 1.2f : 0.6f;
            Color glowColor = NeutralWhite * glow;
            Color lightColor = NeutralWhite;
            float lightIntensity = t < ShakeStart ? 0f : t < ShakeEnd ? Mathf.Lerp(0f, 0.8f, (t - ShakeStart) / (ShakeEnd - ShakeStart)) : 0.8f;
            if (t >= Reveal && !secret)
            {
                float k = Mathf.Clamp01((t - Reveal) / 0.15f);
                lightColor = Color.Lerp(NeutralWhite, rarityColor, k);
                lightIntensity = Mathf.Lerp(0.8f, legendary ? 2f : 1.2f, k);
                if (legendary) glowColor = Color.Lerp(glowColor, rarityColor * 0.9f, k); // la boîte entière luit
            }
            if (secret && t >= Reveal)
            {
                // Pendant le noir du Secret, tout s'éteint ; à l'éclat (R + 2,2), arc-en-ciel.
                bool lit = t - Reveal >= SecretCinematic.ShatterStart;
                lightIntensity = lit ? 1.5f : 0f;
                lightColor = Rainbow(t);
                glowColor = lit ? Rainbow(t) * 0.6f : Color.black;
            }
            SetGlow(glowColor);
            innerLight.color = lightColor;
            innerLight.intensity = lightIntensity;

            // --- Le faisceau : blanc à 1,62 (0 → 3,5 m en 0,13 s), puis couleur de rareté ---
            if (t < LidTime) beam.SetActive(false);
            else
            {
                float h = Mathf.Lerp(0f, 3.5f, Mathf.Clamp01((t - LidTime) / 0.13f));
                Color c = NeutralWhite;
                float strength = 1f;
                if (t >= Reveal)
                {
                    float k = Mathf.Clamp01((t - Reveal) / 0.15f);
                    c = secret ? Rainbow(t) : Color.Lerp(NeutralWhite, rarityColor, k);
                    if (t > revealEnd) strength = 0.6f;
                    if (secret && t - Reveal < SecretCinematic.ShatterStart) strength = 0f;
                }
                SetBeam(h, c, strength);
            }

            // --- L'objet gagné : invisible avant R ; il sort du PointSortie et monte ---
            float since2 = t - cardStart;
            if (since2 < (secret ? 0f : 0.05f)) ShowReward(false);
            else
            {
                ShowReward(true);
                if (secret)
                {
                    // Il tombe de 4 m à 1,55 m en 1 s en faisant 3 demi-tours qui ralentissent.
                    float k = Mathf.Clamp01(since2 / 1.0f);
                    float yy = Mathf.LerpUnclamped(4f, 1.55f, Ease.OutBack(k));
                    if (k < 1f)
                    {
                        rewardPivot.localPosition = new Vector3(0f, yy, 0f);
                        rewardPivot.localRotation = Quaternion.Euler(0f, 540f * Ease.Out(k), 0f);
                        rewardPivot.localScale = Vector3.one;
                    }
                    else PlaceRewardFloating(t, 1.55f, 1f);
                }
                else
                {
                    float s = since2 - 0.05f;
                    float k = Mathf.Clamp01(s / 0.45f);
                    float top = legendary ? 1.75f : 1.55f;
                    float big = legendary ? 1.3f : 1f;
                    if (s < 0.45f)
                    {
                        rewardPivot.localPosition = new Vector3(0f, Mathf.LerpUnclamped(0.4f, top, Ease.OutBack(k)), 0f);
                        rewardPivot.localScale = Vector3.one * Mathf.Lerp(0.3f, big, k);
                        rewardPivot.localRotation = Quaternion.Euler(0f, 360f * s, 0f);
                    }
                    else if (legendary && s < 0.85f)
                    {
                        float k2 = (s - 0.45f) / 0.4f;
                        rewardPivot.localPosition = new Vector3(0f, Mathf.Lerp(1.75f, 1.55f, Ease.InOut(k2)), 0f);
                        rewardPivot.localScale = Vector3.one * Mathf.Lerp(1.3f, 1f, Ease.InOut(k2));
                        rewardPivot.localRotation = Quaternion.Euler(0f, 162f + 200f * s, 0f);
                    }
                    else PlaceRewardFloating(t, 1.55f, 1f);
                }
            }
        }

        /// L'objet flotte (±0,05 m, période 2 s) et tourne doucement (45°/s).
        void PlaceRewardFloating(float t, float height, float scale)
        {
            rewardPivot.localPosition = new Vector3(0f, height + 0.05f * Mathf.Sin(t * Mathf.PI), 0f);
            rewardPivot.localRotation = Quaternion.Euler(0f, 45f * t, 0f);
            rewardPivot.localScale = Vector3.one * scale;
        }

        void SetBeam(float height, Color color, float strength)
        {
            beam.SetActive(height > 0.01f && strength > 0.01f);
            beam.transform.localPosition = new Vector3(0f, 0.55f, 0f);
            beam.transform.localScale = new Vector3(1f, Mathf.Max(0.01f, height), 1f);
            var c = color; c.a = 0.45f * strength;
            beamMaterial.SetColor("_BaseColor", c);
            beamMaterial.SetColor("_EmissionColor", color * Mathf.Min(1.2f, 1.2f * strength)); // plafond 1,5
        }

        void SetGlow(Color c)
        {
            if (current == null || current.body == null || current.glowIndex < 0) return;
            current.body.GetPropertyBlock(block, current.glowIndex);
            block.SetColor("_EmissionColor", c);
            current.body.SetPropertyBlock(block, current.glowIndex);
        }

        static Color Rainbow(float t) => Color.HSVToRGB(Mathf.Repeat(t / 2f, 1f), 0.75f, 1f);
    }
}
