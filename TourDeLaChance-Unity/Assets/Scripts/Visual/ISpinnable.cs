namespace TourDeLaChance
{
    /// <summary>
    /// v6.5 §5 : « quelque chose qu'on peut faire tourner en le glissant » (un cheval, un objet…).
    /// C'est un CONTRAT (une interface) : il dit seulement quelles méthodes existent, pas comment
    /// elles marchent. Le manipulateur d'interface DragToRotate parle à n'importe quel objet qui
    /// respecte ce contrat, sans savoir si c'est le cheval vierge de l'Index (ShowcaseHorse) ou
    /// le cheval du joueur dans l'Inventaire (PlayerSpinner). Le futur Inventaire refait
    /// pourra donc brancher son grand cheval 3D de la même façon.
    /// </summary>
    public interface ISpinnable
    {
        /// Le joueur attrape l'objet (bouton de la souris enfoncé dessus).
        void BeginDrag();

        /// Le joueur glisse de dx pixels (vers la droite = positif).
        void Drag(float dx);

        /// Le joueur lâche : l'objet peut garder un peu d'élan.
        void EndDrag();
    }
}
