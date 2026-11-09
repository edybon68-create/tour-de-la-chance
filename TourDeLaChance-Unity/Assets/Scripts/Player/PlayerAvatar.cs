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
    /// </summary>
    public class PlayerAvatar : MonoBehaviour
    {
        [SerializeField] Renderer body;      // la capsule "Corps" (seule à changer de skin)

        [Header("Points d'accroche (enfants de Joueur)")]
        [SerializeField] Transform hatPoint;   // PointChapeau (0 ; 2,0 ; 0)
        [SerializeField] Transform facePoint;  // PointVisage  (0 ; 1,6 ; −0,49)
        [SerializeField] Transform neckPoint;  // PointCou     (0 ; 1,3 ; 0)
        [SerializeField] Transform backPoint;  // PointDos     (0 ; 1,3 ; 0,5)

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

        // Couleur de l'étoile de la galaxie (#FFF1D6), multipliée par l'intensité qui scintille.
        static readonly Color StarColor = new Color(1f, 0.945f, 0.839f);

        // Une copie de chaque matériau de départ, propre à ce personnage.
        Material opaqueMat, transparentMat, emissiveMat;
        ShopItem shownSkin;
        float skinTime;

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

            // Le modèle personnage.fbx n'a pas de coordonnées de texture : on fabrique des UV
            // cylindriques sur une copie du maillage, pour que les motifs puissent s'afficher.
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
        }

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
                    mat.SetTexture(EmissionMapId, SkinTextures.GalaxyStars());
                    mat.SetTextureScale(BaseMapId, new Vector2(4f, 3f)); // URP applique ce tiling à toutes les cartes
                    break;
            }

            body.sharedMaterial = mat;
            UpdateAnimatedSkin(0f);
        }

        void Update()
        {
            if (shownSkin == null) return;
            skinTime += Time.deltaTime;
            UpdateAnimatedSkin(skinTime);
        }

        /// Couleurs animées : arc-en-ciel (tour complet des teintes en 6 s, émission 0,8) et
        /// scintillement des étoiles de la galaxie (intensité 0,6 ↔ 1,2, période 2 s).
        /// Pur affichage : cela continue pendant les épreuves.
        void UpdateAnimatedSkin(float t)
        {
            var mat = body.sharedMaterial;
            switch (shownSkin.SkinStyle)
            {
                case SkinStyle.ArcEnCiel:
                    Color rainbow = Color.HSVToRGB(Mathf.Repeat(t / 6f, 1f), 0.65f, 1f);
                    mat.SetColor(BaseColorId, rainbow);
                    mat.SetColor(EmissionColorId, rainbow * 0.8f);
                    break;
                case SkinStyle.Galaxie:
                    float intensity = 0.9f + 0.3f * Mathf.Sin(2f * Mathf.PI * t / 2f);
                    mat.SetColor(EmissionColorId, StarColor * intensity);
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
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            foreach (var c in instance.GetComponentsInChildren<Collider>()) Destroy(c); // aucun collider
            if (item.Animation != AccessoryAnimation.Aucune)
                instance.AddComponent<AccessoryMotion>().Setup(item.Animation);
            accessoryInstances[index] = instance;
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
