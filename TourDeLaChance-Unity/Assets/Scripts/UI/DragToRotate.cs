using System;
using UnityEngine.UIElements;

namespace TourDeLaChance
{
    /// <summary>
    /// v6.5 §5 : un MANIPULATEUR UI Toolkit réutilisable : on l'accroche à n'importe quel élément
    /// d'interface (l'image d'un cheval, par exemple) et, quand on clique-glisse dessus, il
    /// transmet le glissement horizontal à un objet ISpinnable qui tourne.
    ///
    /// Un « manipulateur », c'est la façon prévue par UI Toolkit d'ajouter un comportement de
    /// souris à un élément sans écrire une classe d'élément à part :
    ///     element.AddManipulator(new DragToRotate(() => monCheval));
    ///
    /// Pur affichage : il ne décide de rien, il transmet seulement les mouvements de la souris.
    /// On passe une FONCTION qui rend la cible (et pas la cible elle-même) : ainsi la cible peut
    /// changer ou être absente (null = on ne fait rien), sans recréer le manipulateur.
    /// </summary>
    public class DragToRotate : PointerManipulator
    {
        readonly Func<ISpinnable> getTarget;
        ISpinnable current;   // la cible attrapée pendant ce glisser
        int pointerId = -1;   // le doigt / la souris qui glisse (−1 = personne)

        /// Vrai pendant qu'on glisse.
        public bool Dragging => pointerId >= 0;

        /// Prévenu au premier vrai glissement (pour cacher une indication, par exemple).
        public event Action Rotated;

        public DragToRotate(Func<ISpinnable> getTarget)
        {
            this.getTarget = getTarget;
            // Seulement le bouton gauche de la souris (ou un doigt sur écran tactile).
            activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse });
        }

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<PointerDownEvent>(OnPointerDown);
            target.RegisterCallback<PointerMoveEvent>(OnPointerMove);
            target.RegisterCallback<PointerUpEvent>(OnPointerUp);
            target.RegisterCallback<PointerCaptureOutEvent>(OnCaptureOut);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<PointerDownEvent>(OnPointerDown);
            target.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
            target.UnregisterCallback<PointerUpEvent>(OnPointerUp);
            target.UnregisterCallback<PointerCaptureOutEvent>(OnCaptureOut);
            End();
        }

        void OnPointerDown(PointerDownEvent e)
        {
            if (Dragging || !CanStartManipulation(e)) return;
            current = getTarget?.Invoke();
            if (current == null) return;
            pointerId = e.pointerId;
            // « Capturer » le pointeur : on continue de recevoir ses mouvements même s'il sort
            // de l'élément pendant le glisser.
            target.CapturePointer(pointerId);
            current.BeginDrag();
            e.StopPropagation();
        }

        void OnPointerMove(PointerMoveEvent e)
        {
            if (!Dragging || e.pointerId != pointerId) return;
            if (e.deltaPosition.x == 0f) return;
            current.Drag(e.deltaPosition.x);
            Rotated?.Invoke();
            e.StopPropagation();
        }

        void OnPointerUp(PointerUpEvent e)
        {
            if (!Dragging || e.pointerId != pointerId || !CanStopManipulation(e)) return;
            if (target.HasPointerCapture(pointerId)) target.ReleasePointer(pointerId);
            End();
            e.StopPropagation();
        }

        // La capture est perdue (fenêtre fermée, élément caché…) : on lâche proprement.
        void OnCaptureOut(PointerCaptureOutEvent e) => End();

        void End()
        {
            if (!Dragging) return;
            pointerId = -1;
            current?.EndDrag();
            current = null;
        }
    }
}
