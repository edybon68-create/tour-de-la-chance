namespace TourDeLaChance
{
    /// La famille d'une boîte (spec v5 §6.6) : elle choisit la petite « touche thématique »
    /// jouée APRÈS la révélation d'un Secret (vague pour les Pirates, empreintes pour les
    /// Animaux). Pur affichage. Les numéros sont écrits dans le catalogue : ne pas les changer.
    public enum BoxFamily
    {
        Classique = 0, // boîtes n°1 : la cinématique Secret actuelle, rien de plus
        Pirates = 1,
        Animaux = 2
    }
}
