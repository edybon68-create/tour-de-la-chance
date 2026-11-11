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
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class HorseShowcase : MonoBehaviour
    {
        const int Width = 512, Height = 640;

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
            Prepare();
            cam.enabled = false; // éteinte au départ
        }

        void Prepare()
        {
            if (cam == null) cam = GetComponent<Camera>();
            if (target != null) return;
            target = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32)
            {
                name = "Vitrine du cheval",
                antiAliasing = 4
            };
            target.Create();
            cam.targetTexture = target;
        }

        /// Allume (pop-up Inventaire / Index ouverte) ou éteint la caméra.
        public void SetActive(bool on)
        {
            Prepare();
            cam.enabled = on;
        }

        void OnDestroy()
        {
            if (cam != null) cam.targetTexture = null;
            if (target != null) { target.Release(); Destroy(target); }
        }
    }
}
