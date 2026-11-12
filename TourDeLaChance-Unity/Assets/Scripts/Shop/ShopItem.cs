using UnityEngine;
using UnityEngine.Serialization;

namespace TourDeLaChance
{
    /// <summary>
    /// Un objet cosmétique (un skin ou un accessoire), décrit comme une "fiche" de données.
    /// C'est un ScriptableObject : un fichier .asset qu'on crée et règle dans l'éditeur,
    /// sans écrire de code. Pour l'ajouter au jeu, on le met dans une boîte du catalogue
    /// (ShopCatalog), ou dans la liste des objets gratuits.
    /// RÈGLE D'OR : ces fiches ne contiennent que du visuel, rien qui touche aux chances du jeu.
    /// </summary>
    [CreateAssetMenu(fileName = "NouvelObjet", menuName = "Tour de la Chance/Objet cosmétique")]
    public class ShopItem : ScriptableObject
    {
        [Tooltip("Identifiant unique, écrit dans la sauvegarde. Ne JAMAIS le changer une fois publié.")]
        [SerializeField] string id = "couleur_xxx";
        [SerializeField] string displayName = "Nouvel objet";
        [SerializeField] ShopCategory category;
        [Tooltip("Pour un accessoire : l'emplacement où il se porte.")]
        [SerializeField] AccessorySlot slot;
        [Tooltip("Identifiant de la rareté (commun, rare…), comme dans le catalogue. " +
                 "Vide pour un objet gratuit.")]
        [SerializeField] string rarity = "";
        [Tooltip("Objet gratuit, possédé d'office (Bleu ciel, « Rien sur la tête »…). " +
                 "Jamais dans une boîte, jamais vendable.")]
        [SerializeField] bool free;

        [Header("Carte (Inventaire, Index, résultat)")]
        [Tooltip("L'icône détourée (réglée en Sprite). Vide = symbole de secours dessiné par l'interface.")]
        [SerializeField] Sprite icon;

        [Header("Skin : rendu du corps")]
        [SerializeField] SkinStyle skinStyle = SkinStyle.Uni;
        [Tooltip("Couleur principale (fond du motif, couleur unie…).")]
        [SerializeField] Color color = Color.white;
        [Tooltip("Deuxième couleur : bandes, pois, étoiles.")]
        [SerializeField] Color secondaryColor = Color.white;
        [SerializeField, Range(0f, 1f)] float metallic;
        [SerializeField, Range(0f, 1f)] float smoothness = 0.3f;

        [Header("Accessoire : modèle et animation")]
        [Tooltip("Le modèle posé sur le point d'accroche de l'emplacement. Vide = rien (« Aucun »).")]
        [FormerlySerializedAs("hatPrefab")] // ancien nom (v1) : les fiches existantes restent branchées
        [SerializeField] GameObject accessoryPrefab;
        [SerializeField] AccessoryAnimation animation = AccessoryAnimation.Aucune;
        [Tooltip("v3 (cheval) : décalage LOCAL du modèle par rapport à son point d'accroche, en m " +
                 "(tableau REGLAGES de blender/cheval_accessoires.py).")]
        [SerializeField] Vector3 attachOffset = Vector3.zero;
        [Tooltip("v3 (cheval) : échelle uniforme du modèle sur son point d'accroche (1 = taille d'origine).")]
        [SerializeField, Min(0.01f)] float attachScale = 1f;

        [Header("v5 (catalogue §0.3 à §0.5)")]
        [Tooltip("La phrase de la fiche agrandie de l'Index (en italique).")]
        [SerializeField, TextArea] string flavorText = "";
        [Tooltip("Skin « T » : texture répétable 512 × 512 posée sur le corps (art de la partie 2).")]
        [SerializeField] Texture2D baseTexture;
        [Tooltip("Répétition de la texture sur le corps (tiling).")]
        [SerializeField] Vector2 textureTiling = Vector2.one;
        [Tooltip("Effet animé du skin (fumée, éclairs…). Pur affichage, arrive avec l'art.")]
        [SerializeField] SkinFx skinFx = SkinFx.Aucun;
        [Tooltip("Pièces 3D ajoutées au cheval tant que le skin est équipé (tentacules…).")]
        [SerializeField] GameObject skinExtrasPrefab;

        // Propriétés en lecture seule : le reste du code peut lire, mais pas modifier.
        public string Id => id;
        public string DisplayName => displayName;
        public ShopCategory Category => category;
        public AccessorySlot Slot => slot;
        public string RarityId => rarity;
        public bool IsFree => free;
        public Sprite Icon => icon;
        public SkinStyle SkinStyle => skinStyle;
        public Color Color => color;
        public Color SecondaryColor => secondaryColor;
        public float Metallic => metallic;
        public float Smoothness => smoothness;
        public GameObject AccessoryPrefab => accessoryPrefab;
        public AccessoryAnimation Animation => animation;
        public Vector3 AttachOffset => attachOffset;
        public float AttachScale => attachScale;
        public string FlavorText => flavorText;
        public Texture2D BaseTexture => baseTexture;
        public Vector2 TextureTiling => textureTiling;
        public SkinFx SkinFx => skinFx;
        public GameObject SkinExtrasPrefab => skinExtrasPrefab;

        /// v5 §8.5 : fabrique en mémoire (jamais sauvegardé, jamais dans une boîte) un skin uni,
        /// par exemple la couleur « plâtre » du cheval vierge de l'Index.
        public static ShopItem CreateRuntimeSkin(string id, string name, Color color, float smoothness)
        {
            var item = CreateInstance<ShopItem>();
            item.id = id;
            item.displayName = name;
            item.category = ShopCategory.Skin;
            item.free = true;
            item.skinStyle = SkinStyle.Uni;
            item.color = color;
            item.smoothness = smoothness;
            item.hideFlags = HideFlags.DontSave;
            return item;
        }
    }
}
