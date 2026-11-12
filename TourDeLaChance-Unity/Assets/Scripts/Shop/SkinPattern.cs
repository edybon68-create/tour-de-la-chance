namespace TourDeLaChance
{
    /// v5 (catalogue §0.3, rendus « C » et « T ») : le MOTIF calculé par le code pour un skin
    /// Pirates / Animaux, tant que le directeur artistique n'a pas livré d'image (champ
    /// baseTexture de la fiche, qui passe alors devant). Pur affichage. Les couleurs viennent de
    /// la fiche (couleur, 2e couleur, accent, lueur). Ajouter seulement à la FIN.
    public enum SkinPattern
    {
        Aucun = 0,
        Bandes,          // bandes horizontales (Marinière, Abeille) : patternCount bandes
        BandesOndulees,  // rayures ondulées qui suivent la forme (Zèbre, Tigre, Chat roux)
        Taches,          // petites taches rondes (Dalmatien)
        GrandesTaches,   // grandes taches irrégulières (Vache)
        Points,          // quelques gros points placés (Coccinelle : 7)
        Rosettes,        // anneaux à centre clair (Léopard)
        Reseau,          // polygones séparés par des lignes claires (Girafe)
        Ecailles,        // écailles en quinconce, dégradé de bas en haut (Écailles, Caméléon)
        Bois,            // bois délavé, veines et coquillages (Bois flotté)
        Alveoles,        // petites alvéoles claires (Corail)
        Patine,          // cuivre avec plaques vert-de-gris
        Parchemin,       // parchemin, chemin en pointillés et croix (Carte au trésor)
        Panda,           // zones noires : bas, oreilles, tour des yeux
        Degrade,         // dégradé vertical (2e couleur en bas → couleur en haut)
        Plumes,          // plumes en écailles allongées (Flamant, Paon)
        Ventouses,       // Kraken : peau, ventouses, taches lumineuses
        LignesAbyssales, // Abysses : lignes lumineuses de méduse et plancton
        Marbre,          // nuages marbrés (Tempête)
        Veines           // or parcouru de veines lumineuses (Or maudit)
    }
}
