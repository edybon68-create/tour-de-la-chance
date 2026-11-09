namespace TourDeLaChance
{
    /// L'animation propre d'un accessoire (spec boites §5.6). Pur affichage.
    public enum AccessoryAnimation
    {
        Aucune,
        AilesAnge,      // battement lent ±8°, période 1,6 s
        AilesPapillon,  // battement rapide ±20°, période 0,9 s
        Aureole,        // flotte (±4 cm, 2 s) et tourne (30°/s), émission 1,5
        Planete         // orbite autour de la tête (60°/s), tourne sur elle-même (90°/s)
    }
}
