namespace TourDeLaChance
{
    /// <summary>
    /// Ce que vaut un étage, tiré une fois au moment où il est préparé (spec v3 §5.5) :
    /// le nombre de coffres, leur rareté et le gain d'un trésor. Ces valeurs sont FIGÉES
    /// pour l'étage : acheter une amélioration pendant le choix n'y change rien.
    /// "struct" = une petite boîte de valeurs, copiée telle quelle (pas partagée).
    /// </summary>
    public struct FloorSetup
    {
        public int ChestCount;        // 3, 4, 5 ou 6 (v4)
        public int RarityRank;        // 0 = Bois … 4 = Diamant
        public ChestRarityDef Rarity; // la fiche de la rareté (nom, multiplicateur, couleurs)
        public int Gain;              // pièces ajoutées à la cagnotte en cas de trésor
        /// v4 §9.3 : le bonus de tenue (en %, déjà plafonné) lu à la préparation de l'étage.
        /// Il est compris dans Gain. Équiper un objet pendant le choix ne le change pas.
        public int OutfitBonusPercent;
    }
}
