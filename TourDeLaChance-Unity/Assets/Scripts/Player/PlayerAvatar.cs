using UnityEngine;

namespace TourDeLaChance
{
    /// <summary>
    /// Le personnage joueur : il AFFICHE une couleur et un chapeau, rien d'autre.
    /// Au démarrage il montre les objets équipés. Le shop peut lui demander un aperçu
    /// (Preview) d'un objet non équipé ; ShowEquipped() le remet comme dans la sauvegarde.
    /// Il n'a aucun collider : il ne gêne pas le clic sur les coffres.
    /// </summary>
    public class PlayerAvatar : MonoBehaviour
    {
        [SerializeField] Renderer body;      // la capsule "Corps" (seule à changer de couleur)
        [SerializeField] Transform hatPoint; // "PointChapeau", le sommet de la tête

        // Noms des réglages dans le shader URP/Lit.
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int MetallicId = Shader.PropertyToID("_Metallic");
        static readonly int SmoothnessId = Shader.PropertyToID("_Smoothness");

        MaterialPropertyBlock block;
        ShopItem shownHat;     // le chapeau actuellement affiché (pour ne pas le recréer inutilement)
        GameObject hatInstance;

        void Awake() => block = new MaterialPropertyBlock();

        void Start() => ShowEquipped();

        /// Affiche les objets équipés (ceux de la sauvegarde).
        public void ShowEquipped()
        {
            Show(Shop.GetEquipped(ShopCategory.Couleur));
            Show(Shop.GetEquipped(ShopCategory.Chapeau));
        }

        /// Affiche un objet (aperçu ou équipé) sur le personnage, sans rien sauvegarder.
        /// Seule la catégorie de l'objet change : un chapeau ne touche pas à la couleur.
        public void Show(ShopItem item)
        {
            if (item == null) return;
            if (block == null) block = new MaterialPropertyBlock();

            if (item.Category == ShopCategory.Couleur) ApplyColor(item);
            else ApplyHat(item);
        }

        void ApplyColor(ShopItem item)
        {
            // Un PropertyBlock change l'apparence de CE renderer sans modifier le matériau
            // partagé (même technique que les coffres).
            body.GetPropertyBlock(block);
            block.SetColor(BaseColorId, item.Color);
            block.SetFloat(MetallicId, item.Metallic);
            block.SetFloat(SmoothnessId, item.Smoothness);
            body.SetPropertyBlock(block);
        }

        void ApplyHat(ShopItem item)
        {
            if (item == shownHat) return;
            shownHat = item;

            // On enlève l'ancien chapeau…
            if (hatInstance != null) Destroy(hatInstance);
            hatInstance = null;

            // …et on pose le nouveau (s'il y en a un : "Aucun chapeau" n'a pas de modèle).
            if (item.HatPrefab != null)
                hatInstance = Instantiate(item.HatPrefab, hatPoint, false);
        }
    }
}
