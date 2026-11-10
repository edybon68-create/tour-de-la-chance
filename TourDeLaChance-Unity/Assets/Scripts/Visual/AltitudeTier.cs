using UnityEngine;

namespace TourDeLaChance
{
    /// <summary>
    /// La fiche d'un palier de décor (spec §3.14) : c'est du CONTENU, rangé dans un
    /// ScriptableObject (un fichier .asset qu'on règle dans l'Inspector, sans toucher au code).
    /// Les 4 fiches sont dans Assets/Data/Paliers/.
    /// Le décor est du pur affichage : il ne change ni les chances, ni les gains.
    /// </summary>
    [CreateAssetMenu(menuName = "Tour de la Chance/Palier de décor", fileName = "Palier")]
    public class AltitudeTier : ScriptableObject
    {
        [Tooltip("Nom affiché sous « Étage N » quand on entre dans ce palier.")]
        public string displayName = "Au pied des nuages";

        [Header("Fond de ciel")]
        [Tooltip("Image du fond (ciel_palierN.png). En attendant les vraies images, ciel_nuit teinté.")]
        public Texture2D background;
        [Tooltip("Teinte multipliée sur l'image (blanc = image telle quelle).")]
        public Color backgroundTint = Color.white;
        [Tooltip("v3 : couleur de fond de la caméra (visible seulement si l'image manque).")]
        public Color cameraColor = new Color(0.122f, 0.09f, 0.188f);

        [Header("Lumière principale (sa direction ne change pas)")]
        public Color lightColor = new Color(1f, 0.945f, 0.839f);
        public float lightIntensity = 1.2f;

        [Header("Lumière d'ambiance (ciel / horizon / sol)")]
        public Color ambientSky = new Color(0.549f, 0.478f, 0.722f);
        public Color ambientEquator = new Color(0.353f, 0.290f, 0.439f);
        public Color ambientGround = new Color(0.165f, 0.125f, 0.188f);

        [Header("Nuages 3D sous l'étage")]
        [Range(0, 12)] public int cloudGroups;
        public float cloudMinY = -2.5f;
        public float cloudMaxY = -1.5f;
        public Color cloudColor = Color.white;
    }
}
