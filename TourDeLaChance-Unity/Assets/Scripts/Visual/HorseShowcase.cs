using UnityEngine;

namespace TourDeLaChance
{
    /// <summary>
    /// La VITRINE du cheval (spec v4 §5.6) : puisque les pop-ups Inventaire et Index cachent
    /// maintenant le cheval, une 2e caméra (« CameraVitrine », 35° de champ, à 4 m devant le
    /// cheval, à 1,1 m de haut) filme SEULEMENT le calque « Cheval » (le corps, les
    /// accessoires, les paillettes) dans une image (RenderTexture 512 × 640), que la pop-up
    /// affiche dans un cadre. Pur affichage.
    /// La caméra n'est allumée que pendant que l'une de ces deux pop-ups est ouverte : le reste
    /// du temps, elle ne coûte rien.
    /// v6 §12 : la taille de l'image suit le niveau graphique (×0,75 en Faible, ×1,5 en Élevé).
    /// v6.5 (art bible §16.1.7) : l'image fait 440 × 500, comme la scène de l'Inventaire
    /// « Casier », et son fond est TRANSPARENT : on voit le dégradé bleu de la vitrine derrière.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class HorseShowcase : MonoBehaviour
    {
        const int Width = 440, Height = 500;

        Camera cam;
        RenderTexture target;

        /// L'image filmée (à poser en fond d'un élément d'interface).
        public RenderTexture Texture
        {
            get { Prepare(); return target; }
        }

        public bool IsActive => cam != null && cam.enabled;

        void Awake()
        {
            GraphicsLevel.Changed += OnGraphicsChanged;
            Prepare();
            cam.enabled = false; // éteinte au départ
        }

        void Prepare()
        {
            if (cam == null) cam = GetComponent<Camera>();
            if (target != null) return;
            var size = GraphicsLevel.ShowcaseSize(Width, Height);
            target = new RenderTexture(size.x, size.y, 24, RenderTextureFormat.ARGB32)
            {
                name = "Vitrine du cheval",
                antiAliasing = 4
            };
            target.Create();
            cam.targetTexture = target;
            // Fond transparent (alpha 0) : seule la couleur du cheval s'imprime dans l'image.
            cam.clearFlags = CameraClearFlags.SolidColor;
            var bg = cam.backgroundColor; bg.a = 0f; cam.backgroundColor = bg;
        }

        /// Allume (pop-up Inventaire / Index ouverte) ou éteint la caméra.
        public void SetActive(bool on)
        {
            Prepare();
            cam.enabled = on;
        }

        /// Nouveau niveau graphique : l'image est refaite (la pop-up la reprend à sa prochaine ouverture).
        void OnGraphicsChanged()
        {
            if (target == null) return;
            if (cam != null) cam.targetTexture = null;
            target.Release();
            Destroy(target);
            target = null;
            Prepare();
        }

        void OnDestroy()
        {
            GraphicsLevel.Changed -= OnGraphicsChanged;
            if (cam != null) cam.targetTexture = null;
            if (target != null) { target.Release(); Destroy(target); }
        }
    }
}
