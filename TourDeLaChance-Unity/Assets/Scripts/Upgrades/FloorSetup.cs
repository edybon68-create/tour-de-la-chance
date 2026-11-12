namespace TourDeLaChance
{
    /// <summary>
    /// Ce que vaut un étage, tiré une fois au moment où il est préparé (spec v3 §5.5) :
    /// le nombre de coffres, leur rareté et le gain d'un trésor. Ces valeurs sont FIGÉES
    /// pour l'étage : acheter une amélioration pendant le choix n'y change rien.
    /// "struct" = une petite boîte de valeurs, copiée telle quelle (pas partagée).
    ///
    /// v5 : + le tirage des diamants (spec v5 §10) et le multiplicateur de renaissance (§13).
    /// </summary>
    public struct FloorSetup
    {
        public int ChestCount;        // 3, 4, 5 ou 6 (v4)
        public int RarityRank;        // 0 = Bois … 4 = Cristal (identifiant « diamant »)
        public ChestRarityDef Rarity; // la fiche de la rareté (nom, multiplicateur, couleurs)
        public int Gain;              // pièces ajoutées à la cagnotte en cas de trésor
        /// v4 §9.3 : le bonus de tenue (en %) lu à la préparation de l'étage.
        /// Il est compris dans Gain. Équiper un objet pendant le choix ne le change pas.
        public int OutfitBonusPercent;

        // ---------- v5 §10 : les diamants ----------
        /// Le trésor de cet étage contient-il des diamants (à la place des pièces) ?
        /// SECRET jusqu'à l'ouverture : l'affichage ne le lit qu'après la révélation.
        public bool DiamondTreasure;
        /// Combien de diamants (renaissance comprise) si c'est un trésor en diamants.
        public int DiamondAmount;
        /// La chance (pour mille) qu'avait cet étage d'être en diamants : elle, on peut
        /// l'afficher avant le choix (« ◆ Diamants : 3 % »).
        public int DiamondChancePermille;

        /// v5 §13 : ×2 après la renaissance (×1 avant). Déjà compris dans Gain et DiamondAmount.
        public int RebirthMultiplier;
    }
}
