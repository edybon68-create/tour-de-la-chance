using UnityEngine;

namespace TourDeLaChance
{
    /// <summary>
    /// Un objet du shop (une couleur ou un chapeau), décrit comme une "fiche" de données.
    /// C'est un ScriptableObject : un fichier .asset qu'on crée et règle dans l'éditeur,
    /// sans écrire de code. Pour ajouter un objet, on crée une nouvelle fiche et on
    /// l'ajoute au catalogue (ShopCatalog).
    /// RÈGLE D'OR : ces fiches ne contiennent que du visuel, rien qui touche aux chances.
    /// </summary>
    [CreateAssetMenu(fileName = "NouvelObjet", menuName = "Tour de la Chance/Objet du shop")]
    public class ShopItem : ScriptableObject
    {
        [Tooltip("Identifiant unique, écrit dans la sauvegarde. Ne JAMAIS le changer une fois publié.")]
        [SerializeField] string id = "couleur_xxx";
        [SerializeField] string displayName = "Nouvel objet";
        [SerializeField] ShopCategory category;
        [Tooltip("Prix en pièces du porte-monnaie. 0 = gratuit, possédé d'office.")]
        [SerializeField, Min(0)] int price;

        [Header("Couleur (catégorie Couleur, et visuel de la carte)")]
        [SerializeField] Color color = Color.white;
        [SerializeField, Range(0f, 1f)] float metallic;
        [SerializeField, Range(0f, 1f)] float smoothness = 0.3f;

        [Header("Chapeau (catégorie Chapeau)")]
        [Tooltip("Le modèle posé sur PointChapeau. Vide = pas de chapeau.")]
        [SerializeField] GameObject hatPrefab;

        // Propriétés en lecture seule : le reste du code peut lire, mais pas modifier.
        public string Id => id;
        public string DisplayName => displayName;
        public ShopCategory Category => category;
        public int Price => price;
        public bool IsFree => price == 0;
        public Color Color => color;
        public float Metallic => metallic;
        public float Smoothness => smoothness;
        public GameObject HatPrefab => hatPrefab;
    }
}
