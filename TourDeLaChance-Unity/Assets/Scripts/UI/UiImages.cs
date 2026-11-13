using UnityEngine;

namespace TourDeLaChance
{
    /// <summary>
    /// Les IMAGES d'interface de la v6 (art bible §15.8), rangées dans un seul fichier de
    /// données (Assets/Resources/UiImages.asset) pour être chargées par leur nom, sans référence
    /// à brancher dans la scène. Chaque case porte le NOM DU FICHIER de l'image
    /// (Assets/Art/icones_detourees/icone_route.png → case « iconeRoute ») : l'outil d'éditeur
    /// IconAutoBinder la remplit tout seul quand l'image arrive dans le dossier.
    ///
    /// Une case vide n'est jamais une erreur : l'interface dessine alors un SUBSTITUT propre
    /// (pictogramme fait par le code). C'est de l'affichage seulement.
    /// </summary>
    [CreateAssetMenu(fileName = "UiImages", menuName = "Tour de la Chance/Images d'interface v6")]
    public class UiImages : ScriptableObject
    {
        [Tooltip("icone_boutique.png : l'échoppe de fête foraine (colonne et titre de la Boutique).")]
        public Sprite iconeBoutique;
        [Tooltip("icone_reglages.png : la roue dentée.")]
        public Sprite iconeReglages;
        [Tooltip("icone_graphismes.png : la rangée Graphismes des Réglages.")]
        public Sprite iconeGraphismes;
        [Tooltip("icone_rotation.png : l'indice « Glisse pour tourner » de la fiche agrandie.")]
        public Sprite iconeRotation;
        [Tooltip("icone_route.png : le titre de la Route des trophées.")]
        public Sprite iconeRoute;
        [Tooltip("drapeau_record.png : le drapeau doré du record sur la route.")]
        public Sprite drapeauRecord;
        [Tooltip("renaissance_heros.png : la grande illustration de la Renaissance.")]
        public Sprite renaissanceHeros;
        [Tooltip("macaron_or.png : le macaron « ×2 » de la Renaissance.")]
        public Sprite macaronOr;
        [Tooltip("piece.png : la pièce d'or (cartes d'échange).")]
        public Sprite piece;
        [Tooltip("diamant.png : le diamant.")]
        public Sprite diamant;

        static UiImages instance;
        static bool searched;

        /// Les images (null si le fichier de données manque : tout passe alors aux substituts).
        public static UiImages Get
        {
            get
            {
                if (!searched) { instance = Resources.Load<UiImages>("UiImages"); searched = true; }
                return instance;
            }
        }

        /// Raccourcis : l'image demandée, ou null (= dessiner le substitut).
        public static Sprite Boutique => Get != null ? Get.iconeBoutique : null;
        public static Sprite Reglages => Get != null ? Get.iconeReglages : null;
        public static Sprite Graphismes => Get != null ? Get.iconeGraphismes : null;
        public static Sprite Rotation => Get != null ? Get.iconeRotation : null;
        public static Sprite Route => Get != null ? Get.iconeRoute : null;
        public static Sprite Drapeau => Get != null ? Get.drapeauRecord : null;
        public static Sprite Heros => Get != null ? Get.renaissanceHeros : null;
        public static Sprite Macaron => Get != null ? Get.macaronOr : null;
        public static Sprite Piece => Get != null ? Get.piece : null;
        public static Sprite Diamant => Get != null ? Get.diamant : null;

        /// Le nom de la case qui correspond à un fichier (« icone_route » → « iconeRoute »),
        /// utilisé par l'outil d'éditeur.
        public static string FieldFor(string fileName)
        {
            switch (fileName)
            {
                case "icone_boutique": return nameof(iconeBoutique);
                case "icone_reglages": return nameof(iconeReglages);
                case "icone_graphismes": return nameof(iconeGraphismes);
                case "icone_rotation": return nameof(iconeRotation);
                case "icone_route": return nameof(iconeRoute);
                case "drapeau_record": return nameof(drapeauRecord);
                case "renaissance_heros": return nameof(renaissanceHeros);
                case "macaron_or": return nameof(macaronOr);
                case "piece": return nameof(piece);
                case "diamant": return nameof(diamant);
                default: return null;
            }
        }
    }
}
