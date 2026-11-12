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
        /// v5 §11.4 : la partie a utilisé une reprise (le classement local la marque d'un ◆).
        public bool UsedRevive;
    }
}
