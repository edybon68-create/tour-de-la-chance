namespace TourDeLaChance
{
    /// Les 4 emplacements du corps où l'on porte un accessoire (spec boites §5.4).
    /// On peut porter un accessoire par emplacement, tous en même temps.
    public enum AccessorySlot
    {
        Tete = 0,   // PointChapeau : chapeaux, couronne, auréole
        Visage = 1, // PointVisage : lunettes, masque
        Cou = 2,    // PointCou : nœud, écharpe, collier
        Dos = 3     // PointDos : ailes, planète
    }
}
