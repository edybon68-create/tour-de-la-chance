namespace TourDeLaChance
{
    /// Comment un skin est dessiné sur le corps (spec boites §5.3). Pur affichage.
    public enum SkinStyle
    {
        Uni,          // une couleur (mate, brillante ou métal)
        Rayures,      // sucre d'orge : bandes horizontales (texture fabriquée par le code)
        Pois,         // pois de fête : pois en quinconce (texture fabriquée par le code)
        Transparent,  // cristal : on voit à travers (opacité 0,55)
        ArcEnCiel,    // la couleur fait le tour de l'arc-en-ciel et brille (émission)
        Galaxie       // fond nuit + étoiles qui brillent et scintillent (émission)
    }
}
