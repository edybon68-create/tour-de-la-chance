using UnityEngine;

namespace TourDeLaChance
{
    /// <summary>
    /// v6.5 §5 : fait tourner le cheval DU JOUEUR quand on le glisse dans la vitrine de
    /// l'Inventaire (même sensation que la fiche agrandie de l'Index : 0,5° par pixel, élan au
    /// relâché qui s'amortit, vitesse divisée par 2 toutes les 0,1 s). Aucune indication à
    /// l'écran : c'est un bonus pour qui le découvre.
    ///
    /// Le cheval du joueur est posé à chaque image par PlayerAnimator, qui additionne des
    /// « calques » (saut, regard, penché…). On ne touche donc PAS directement à sa rotation :
    /// on règle seulement un calque de plus, « l'angle de vue » (PlayerAnimator.ViewYaw). Ainsi
    /// les autres animations continuent de marcher, et remettre l'angle à 0 rend exactement la
    /// pose de jeu.
    ///
    /// PanelsController remet l'angle à 0 à l'ouverture et à la fermeture de l'Inventaire :
    /// en jeu, le cheval fait toujours face aux coffres. Pur affichage, rien n'est sauvegardé.
    /// </summary>
    [RequireComponent(typeof(PlayerAnimator))]
    public class PlayerSpinner : MonoBehaviour, ISpinnable
    {
        const float HalfLife = 0.1f; // l'élan perd la moitié de sa vitesse toutes les 0,1 s

        PlayerAnimator animator;
        bool dragging;
        float spinVelocity;            // °/s de l'élan après le relâché
        float lastDragDelta, lastDragTime;

        /// L'angle de vue actuel (pour les tests).
        public float Angle => animator != null ? animator.ViewYaw : 0f;

        void Awake() => animator = GetComponent<PlayerAnimator>();

        void Update()
        {
            if (dragging || Mathf.Abs(spinVelocity) <= 0.5f) { if (!dragging) spinVelocity = 0f; return; }
            float dt = Time.unscaledDeltaTime;
            animator.ViewYaw = Mathf.Repeat(animator.ViewYaw + spinVelocity * dt, 360f);
            spinVelocity *= Mathf.Pow(0.5f, dt / HalfLife);
        }

        public void BeginDrag()
        {
            dragging = true;
            spinVelocity = 0f;
            lastDragDelta = 0f;
            lastDragTime = Time.unscaledTime;
        }

        public void Drag(float dx)
        {
            // Même sens que l'Index : glisser à droite fait tourner la face vers la droite.
            float deg = -dx * ShowcaseHorse.DegreesPerPixel;
            animator.ViewYaw = Mathf.Repeat(animator.ViewYaw + deg, 360f);
            float now = Time.unscaledTime;
            lastDragDelta = deg / Mathf.Max(0.001f, now - lastDragTime); // vitesse du dernier glisser (°/s)
            lastDragTime = now;
        }

        public void EndDrag()
        {
            if (!dragging) return;
            dragging = false;
            // L'élan ne part que si on bougeait encore juste avant de lâcher (comme l'Index).
            bool recent = Time.unscaledTime - lastDragTime < 0.1f;
            spinVelocity = recent ? Mathf.Clamp(lastDragDelta * 0.5f, -360f, 360f) : 0f;
        }

        /// Remet le cheval de face, tout de suite (ouverture / fermeture de l'Inventaire).
        public void ResetView()
        {
            dragging = false;
            spinVelocity = 0f;
            if (animator != null) animator.ViewYaw = 0f;
        }

        /// OUTIL DE TEST : simule un glisser de dx pixels en plusieurs pas.
        public void DebugDrag(float dx, int steps = 10)
        {
            BeginDrag();
            for (int i = 0; i < steps; i++) Drag(dx / steps);
            dragging = false; // sans élan, pour un angle prévisible
        }
    }
}
