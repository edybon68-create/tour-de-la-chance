using UnityEngine;

namespace TourDeLaChance
{
    /// <summary>
    /// Le ciel de nuit derrière la scène : un simple quad (rectangle plat) avec l'image
    /// du ciel, accroché à la caméra et placé tout au fond, juste avant la limite de vue
    /// (farClipPlane). Tout objet 3D est plus proche, donc dessiné devant lui.
    ///
    /// Pourquoi pas une skybox ? Une skybox attend une image panoramique (360°) ;
    /// une image 16:9 y serait déformée. Le quad, lui, montre l'image telle quelle.
    ///
    /// Ce script recalcule la taille du quad à chaque image pour qu'il couvre toujours
    /// tout l'écran (même si on redimensionne la fenêtre), sans déformer l'image :
    /// comme "background-size: cover" en CSS, quitte à rogner un peu les bords.
    /// </summary>
    [RequireComponent(typeof(MeshRenderer))]
    public class SkyBackdrop : MonoBehaviour
    {
        [SerializeField] Camera targetCamera;
        [Tooltip("Proportion de la distance max de la caméra (0.95 = presque tout au fond).")]
        [SerializeField, Range(0.5f, 0.99f)] float depthRatio = 0.95f;
        [Tooltip("Largeur / hauteur de l'image du ciel (1376 / 768).")]
        [SerializeField] float imageAspect = 1376f / 768f;

        void LateUpdate()
        {
            if (targetCamera == null) return;

            // 1. On se place droit devant la caméra, au fond.
            float distance = targetCamera.farClipPlane * depthRatio;
            transform.localPosition = new Vector3(0f, 0f, distance);
            transform.localRotation = Quaternion.identity;

            // 2. Taille visible à cette distance (trigonométrie : tan(angle/2) = demi-hauteur / distance).
            float viewHeight = 2f * distance * Mathf.Tan(targetCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
            float viewWidth = viewHeight * targetCamera.aspect;

            // 3. "Cover" : on agrandit l'image jusqu'à remplir les deux côtés.
            float width = viewWidth;
            float height = viewWidth / imageAspect;
            if (height < viewHeight)
            {
                height = viewHeight;
                width = viewHeight * imageAspect;
            }
            transform.localScale = new Vector3(width, height, 1f);
        }
    }
}
