namespace TourDeLaChance
{
    /// Les catégories d'objets (spec boites §3).
    /// Les numéros (0 et 1) sont écrits dans les fiches .asset : on garde les mêmes qu'en v1
    /// (0 = couleur, 1 = chapeau) pour que les anciennes fiches restent valables.
    public enum ShopCategory
    {
        Skin = 0,       // l'apparence du corps (couleur ou matière) — un seul à la fois
        Accessoire = 1  // un objet porté, un par emplacement (Tête, Visage, Cou, Dos)
    }
}
