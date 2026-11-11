using UnityEngine;

namespace TourDeLaChance
{
    /// <summary>
    /// Le personnage joueur : il AFFICHE un skin et 4 accessoires (Tête, Visage, Cou, Dos),
    /// rien d'autre (spec boites §5.3 à §5.6). Au démarrage il montre les objets équipés.
    /// Le shop peut lui demander un aperçu (Show) d'un objet non équipé ; ShowEquipped()
    /// le remet comme dans la sauvegarde.
    ///
    /// Les skins spéciaux (cristal transparent, arc-en-ciel et galaxie qui brillent) ont
    /// chacun leur matériau de départ : on passe de l'un à l'autre au changement de skin,
    /// et on remet TOUT à zéro à chaque fois, pour qu'un skin ne « bave » jamais sur le
    /// suivant (plus de transparence ni de lumière propre après un Cristal ou une Galaxie).
    /// Il n'a aucun collider : il ne gêne pas le clic sur les coffres.
    ///
    /// v3 : le personnage est un CHEVAL (modèle avec ses propres UV, que l'on garde). Chaque
    /// accessoire est posé sur son point avec son décalage et son échelle (fiche de l'objet).
    /// L'arc-en-ciel devient un dégradé qui défile vers le haut, avec des paillettes.
    /// </summary>
    public class PlayerAvatar : MonoBehaviour
    {
        [SerializeField] Renderer body;      // le "Corps" du cheval (seul à changer de skin)

        [Header("Points d'accroche (dans le modèle du cheval)")]
        [SerializeField] Transform hatPoint;   // PointChapeau (0 ; 1,86 ; 0,03)
        [SerializeField] Transform facePoint;  // PointVisage  (0 ; 1,68 ; −0,253)
        [SerializeField] Transform neckPoint;  // PointCou     (0 ; 1,05 ; 0,01)
        [SerializeField] Transform backPoint;  // PointDos     (0 ; 0,95 ; 0,296)

        [Header("Effets des Légendaires (facultatif)")]
        [SerializeField] RainbowSparkles sparkles;   // paillettes de l'arc-en-ciel
        [SerializeField] PlayerAnimator playerAnimator; // pour accélérer pendant le saut de joie

        [Header("Matériaux de départ des skins spéciaux (URP Lit)")]
        [Tooltip("Matériau URP Lit réglé en « Transparent » (pour le Cristal).")]
        [SerializeField] Material transparentTemplate;
        [Tooltip("Matériau URP Lit avec l'émission activée (pour l'Arc-en-ciel et la Galaxie).")]
        [SerializeField] Material emissiveTemplate;

        // Noms des réglages dans le shader URP/Lit.
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
        static readonly int MetallicId = Shader.PropertyToID("_Metallic");
        static readonly int SmoothnessId = Shader.PropertyToID("_Smoothness");
        static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        static readonly int EmissionMapId = Shader.PropertyToID("_EmissionMap");

        // v4 §7.5 : la Galaxie (vernis brillant, lumière propre ×1,4 : sous le plafond de 1,5
        // de l'art bible puisque les texels de la carte d'émission ne dépassent jamais 1).
        const float GalaxyEmission = 1.4f;
        const float GalaxySmoothness = 0.85f;
        const float GalaxyMetallic = 0.1f;
        const float GalaxyStep = 0.05f;                 // recalcul 20 fois par seconde
        static readonly Vector2 GalaxyTiling = new Vector2(2f, 1.5f);
        static readonly Color GalaxySparkleColor = new Color(0.812f, 0.878f, 1f); // #CFE0FF

        // Une copie de chaque matériau de départ, propre à ce personnage.
        Material opaqueMat, transparentMat, emissiveMat;
        ShopItem shownSkin;
        float skinTime;
        GalaxyTexture galaxy;     // créée au premier affichage de la Galaxie
        float galaxyTimer;        // temps accumulé depuis le dernier recalcul

        // Un accessoire affiché par emplacement (index = AccessorySlot).
        readonly ShopItem[] shownAccessories = new ShopItem[4];
        readonly GameObject[] accessoryInstances = new GameObject[4];

        bool ready;

        void Awake() => Prepare();

        void Start() => ShowEquipped();

        /// Prépare les matériaux et les UV du corps (une seule fois).
        void Prepare()
        {
            if (ready || body == null) return;
            ready = true;

            // Le cheval a ses propres UV (cylindriques, couture à l'arrière) : on les garde.
            // Un modèle SANS coordonnées de texture recevrait des UV cylindriques fabriquées
            // sur une copie du maillage (sinon les motifs ne pourraient pas s'afficher).
            var filter = body.GetComponent<MeshFilter>();
            if (filter != null) filter.sharedMesh = SkinTextures.AddCylindricalUVs(filter.sharedMesh);

            body.SetPropertyBlock(null); // la v1 utilisait un PropertyBlock : on l'efface
            opaqueMat = new Material(body.sharedMaterial) { name = "Corps (opaque)" };
            if (transparentTemplate != null) transparentMat = new Material(transparentTemplate) { name = "Corps (transparent)" };
            if (emissiveTemplate != null) emissiveMat = new Material(emissiveTemplate) { name = "Corps (émission)" };
            body.sharedMaterial = opaqueMat;
        }

        void OnDestroy()
        {
            if (opaqueMat != null) Destroy(opaqueMat);
            if (transparentMat != null) Destroy(transparentMat);
            if (emissiveMat != null) Destroy(emissiveMat);
            galaxy?.Dispose();
        }

        /// La texture animée de la Galaxie (null tant qu'elle n'a jamais été affichée), pour les tests.
        public GalaxyTexture Galaxy => galaxy;

        /// Affiche les objets équipés (ceux de la sauvegarde) : le skin et les 4 emplacements.
        public void ShowEquipped()
        {
            Show(Shop.GetEquippedSkin());
            for (int s = 0; s < 4; s++) Show(Shop.GetEquippedAccessory((AccessorySlot)s));
        }

        /// Affiche un objet (aperçu ou équipé) sur le personnage, sans rien sauvegarder.
        /// Seule sa place change : des lunettes ne touchent ni au chapeau, ni au skin.
        public void Show(ShopItem item)
        {
            if (item == null) return;
            Prepare();
            if (item.Category == ShopCategory.Skin) ApplySkin(item);
            else ApplyAccessory(item);
        }

        /// Le skin affiché en ce moment (pour les tests).
        public ShopItem ShownSkin => shownSkin;
        /// L'accessoire affiché sur un emplacement (pour les tests).
        public ShopItem ShownAccessory(AccessorySlot slot) => shownAccessories[(int)slot];
        public GameObject AccessoryInstance(AccessorySlot slot) => accessoryInstances[(int)slot];

        // =====================================================================
        // Skins
        // =====================================================================

        void ApplySkin(ShopItem item)
        {
            if (item == shownSkin) return;
            shownSkin = item;
            skinTime = 0f;

            // 1. On choisit le matériau de départ selon le style…
            Material mat = item.SkinStyle switch
            {
                SkinStyle.Transparent => transparentMat ?? opaqueMat,
                SkinStyle.ArcEnCiel => emissiveMat ?? opaqueMat,
                SkinStyle.Galaxie => emissiveMat ?? opaqueMat,
                _ => opaqueMat
            };

            // 2. …et on remet TOUS ses réglages à zéro avant d'appliquer le skin
            //    (aucune texture, aucune émission résiduelle).
            mat.SetTexture(BaseMapId, Texture2D.whiteTexture);
            mat.SetTextureScale(BaseMapId, Vector2.one);
            mat.SetTextureOffset(BaseMapId, Vector2.zero);
            rainbowOffset = 0f;
            mat.SetColor(EmissionColorId, Color.black);
            if (mat.HasProperty(EmissionMapId)) mat.SetTexture(EmissionMapId, null);
            mat.SetFloat(MetallicId, item.Metallic);
            mat.SetFloat(SmoothnessId, item.Smoothness);
            mat.SetColor(BaseColorId, item.Color);

            switch (item.SkinStyle)
            {
                case SkinStyle.Rayures:
                    // Les couleurs sont dans la texture : la couleur de base reste blanche.
                    mat.SetColor(BaseColorId, Color.white);
                    mat.SetTexture(BaseMapId, SkinTextures.Stripes(item.Color, item.SecondaryColor));
                    mat.SetTextureScale(BaseMapId, new Vector2(1f, 4f)); // 4 tuiles × 2 bandes = 8 bandes
                    break;
                case SkinStyle.Pois:
                    mat.SetColor(BaseColorId, Color.white);
                    mat.SetTexture(BaseMapId, SkinTextures.Dots(item.Color, item.SecondaryColor));
                    // Tour du corps ≈ 3,1 m, hauteur 2 m : tuiles de ~30 cm → pois de ~12 cm.
                    mat.SetTextureScale(BaseMapId, new Vector2(10f, 7f));
                    break;
                case SkinStyle.Transparent:
                    // Alpha de la couleur = opacité (0,55 dans la fiche, jamais moins : art-bible §4.1).
                    var c = item.Color;
                    c.a = Mathf.Max(0.55f, c.a);
                    mat.SetColor(BaseColorId, c);
                    break;
                case SkinStyle.Galaxie:
                    // v4 §7 : nébuleuse animée calculée par le code (couleur ET émission), vernis
                    // très brillant. La couleur de base reste blanche (tout est dans la texture).
                    if (galaxy == null) galaxy = new GalaxyTexture();
                    galaxyTimer = 0f;
                    mat.SetColor(BaseColorId, Color.white);
                    mat.SetFloat(SmoothnessId, GalaxySmoothness);
                    mat.SetFloat(MetallicId, GalaxyMetallic);
                    mat.SetTexture(BaseMapId, galaxy.BaseMap);
                    mat.SetTexture(EmissionMapId, galaxy.EmissionMap);
                    mat.SetColor(EmissionColorId, Color.white * GalaxyEmission);
                    mat.SetTextureScale(BaseMapId, GalaxyTiling); // URP applique ce tiling à toutes les cartes
                    break;
                case SkinStyle.ArcEnCiel:
                    // v3 §8.1 : un dégradé qui fait un tour complet des couleurs, utilisé comme
                    // image ET comme lumière propre. On en voit la moitié à la fois (tiling 0,5).
                    mat.SetColor(BaseColorId, Color.white);
                    mat.SetTexture(BaseMapId, SkinTextures.RainbowGradient());
                    mat.SetTexture(EmissionMapId, SkinTextures.RainbowGradient());
                    mat.SetColor(EmissionColorId, Color.white * RainbowEmission);
                    mat.SetTextureScale(BaseMapId, new Vector2(1f, 0.5f));
                    break;
            }

            body.sharedMaterial = mat;
            // Paillettes : arc-en-ciel (couleur du moment, toutes les 0,25 s) ou Galaxie
            // (petites étoiles #CFE0FF, toutes les 0,4 s). Coupées pour tout autre skin.
            if (sparkles != null)
            {
                if (item.SkinStyle == SkinStyle.ArcEnCiel) sparkles.Configure(null, 0.25f, 1.2f);
                else if (item.SkinStyle == SkinStyle.Galaxie) sparkles.Configure(GalaxySparkleColor, 0.4f, 1.2f);
                sparkles.SetActive(item.SkinStyle == SkinStyle.ArcEnCiel || item.SkinStyle == SkinStyle.Galaxie);
            }
            UpdateAnimatedSkin(0f);
        }

        void Update()
        {
            if (shownSkin == null) return;
            skinTime += Time.deltaTime;
            UpdateAnimatedSkin(skinTime);
        }

        /// Arc-en-ciel v3 : le dégradé monte de 0,25 par seconde (un tour en 4 s), trois fois
        /// plus vite pendant le saut de joie (il « s'excite »). Lumière propre × 1,0 (sous le
        /// plafond de 1,5 de la bible).
        const float RainbowEmission = 1f;
        float rainbowOffset;

        /// La couleur de l'arc-en-ciel « du moment » (pour teinter les paillettes).
        public Color RainbowColorNow => Color.HSVToRGB(Mathf.Repeat(-rainbowOffset + 0.5f, 1f), 0.75f, 1f);

        /// Couleurs animées : arc-en-ciel qui défile, et nébuleuse de la Galaxie recalculée
        /// toutes les 0,05 s (v4 §7.4). Pur affichage : cela continue pendant les épreuves.
        /// Le recalcul ne tourne QUE si la Galaxie est affichée.
        void UpdateAnimatedSkin(float t)
        {
            var mat = body.sharedMaterial;
            switch (shownSkin.SkinStyle)
            {
                case SkinStyle.ArcEnCiel:
                    bool excited = playerAnimator != null && playerAnimator.IsJoyJumping;
                    rainbowOffset = Mathf.Repeat(rainbowOffset + 0.25f * (excited ? 3f : 1f) * Time.deltaTime, 1f);
                    // Décalage vers le bas de l'image = les bandes de couleur MONTENT le long du cheval.
                    mat.SetTextureOffset(BaseMapId, new Vector2(0f, -rainbowOffset));
                    break;
                case SkinStyle.Galaxie:
                    galaxyTimer += Time.deltaTime;
                    if (galaxy != null && galaxyTimer >= GalaxyStep)
                    {
                        // Pendant le saut de joie, la nébuleuse va 3 fois plus vite (comme l'arc-en-ciel).
                        bool joy = playerAnimator != null && playerAnimator.IsJoyJumping;
                        galaxy.Step(galaxyTimer, joy ? 3f : 1f);
                        galaxyTimer = 0f;
                    }
                    break;
            }
        }

        // =====================================================================
        // Accessoires
        // =====================================================================

        void ApplyAccessory(ShopItem item)
        {
            int index = (int)item.Slot;
            if (item == shownAccessories[index]) return;
            shownAccessories[index] = item;

            // On enlève l'ancien accessoire de cet emplacement…
            if (accessoryInstances[index] != null) Destroy(accessoryInstances[index]);
            accessoryInstances[index] = null;

            // …et on pose le nouveau (s'il y en a un : « Rien » n'a pas de modèle), comme
            // enfant de son point d'accroche, position et rotation locales à zéro.
            Transform point = PointFor(item.Slot);
            if (item.AccessoryPrefab == null || point == null) return;
            var instance = Instantiate(item.AccessoryPrefab, point, false);
            instance.name = item.Id;
            // v3 : enfant du point, décalage et échelle propres à l'objet, rotation nulle.
            instance.transform.localPosition = item.AttachOffset;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one * item.AttachScale;
            foreach (var c in instance.GetComponentsInChildren<Collider>()) Destroy(c); // aucun collider
            // v4 §5.6 : l'accessoire prend le calque du corps (« Cheval ») pour être filmé par
            // la caméra de la vitrine des pop-ups Inventaire et Index.
            SetLayer(instance.transform, body != null ? body.gameObject.layer : gameObject.layer);
            if (item.Animation != AccessoryAnimation.Aucune)
                instance.AddComponent<AccessoryMotion>().Setup(item.Animation);
            accessoryInstances[index] = instance;
        }

        static void SetLayer(Transform t, int layer)
        {
            t.gameObject.layer = layer;
            foreach (Transform child in t) SetLayer(child, layer);
        }

        Transform PointFor(AccessorySlot slot) => slot switch
        {
            AccessorySlot.Tete => hatPoint,
            AccessorySlot.Visage => facePoint,
            AccessorySlot.Cou => neckPoint,
            _ => backPoint
        };
    }
}
