namespace TourDeLaChance
{
    /// <summary>
    /// La liste de tous les effets sonores du jeu (spec sons-animations §5.2).
    /// Un "enum" est une liste de noms fixes : écrire Sfx.UiClic évite les fautes de frappe
    /// qu'on ferait avec un texte "ui_clic". Chaque nom est relié à son fichier WAV dans
    /// la fiche SoundBank (Assets/Data/Sons.asset).
    /// </summary>
    public enum Sfx
    {
        // Interface
        UiSurvol,
        UiClic,
        UiJouer,
        UiEtageMonte,
        UiNouvellePartie,
        UiBoutiqueOuvre,
        UiBoutiqueFerme,
        Vente,
        // Coffres
        CoffreSurvol,
        CoffreTremble,
        CoffreOuvre,
        CoffreFerme,
        // Résultats
        ResultatTresor,
        ResultatPiege,
        ResultatEncaisse,
        // Pièces
        PiecesJaillissement,
        PiecesTic,
        PiecesAchat,
        // Boîtes à surprises (utilisés plus tard, avec les boîtes)
        BoiteSecoue,
        BoiteOuverture,
        RevelationCommun,
        RevelationPeuCommun,
        RevelationRare,
        RevelationSuperRare,
        RevelationLegendaire,
        RevelationSecret,
        // v3 (spec v3 §14). AJOUTÉS À LA FIN : la fiche Sons.asset range les sons par leur
        // numéro dans cette liste, insérer au milieu décalerait tous les suivants.
        AmeliorationAchat,
        AmeliorationMax,
        Record,
        CoffresPrecieux,
        CoffreEnPlus,
        CoffreEclatBronze,
        CoffreEclatArgent,
        CoffreEclatOr,
        CoffreEclatDiamant,
        CoffresRemelange,
        SecretCoupure,
        SecretBattement,
        SecretFissure,
        SecretEclat,
        SecretConfettis
    }
}
