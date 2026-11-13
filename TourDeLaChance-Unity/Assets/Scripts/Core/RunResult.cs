namespace TourDeLaChance
{
    /// <summary>
    /// Le résumé d'une partie qui vient de se terminer (spec v4 §12.4) : l'étage atteint et
    /// les pièces encaissées (0 si la partie est perdue). Le GameManager l'envoie dans son
    /// événement RunEnded ; il ne sait pas qui l'écoute (statistiques, classement…).
    /// "struct" = une petite boîte de valeurs, copiée telle quelle.
    /// </summary>
    public struct RunResult
    {
        public int Floor;      // l'étage où l'on a pu choisir un coffre (perdu ou encaissé)
        public int CashedOut;  // les pièces encaissées (0 pour une partie perdue)
        public bool Lost;      // vrai si la partie s'est finie sur un piège
        /// v6 §9.4 : le nombre de reprises utilisées dans la partie (0 à 5). Le classement local
        /// marque la ligne d'un ◆ (« ◆×3 » s'il y en a plusieurs). Remplace UsedRevive (v5).
        public int RevivesUsed;
        /// Ancien nom (v5) : vrai si au moins une reprise a servi.
        public bool UsedRevive => RevivesUsed > 0;
    }
}
