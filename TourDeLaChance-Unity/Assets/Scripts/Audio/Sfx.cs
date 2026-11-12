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
        SecretConfettis,
        // v5 (spec v5 §17.4, audio/README.md « Sons v5 »). Toujours AJOUTÉS À LA FIN.
        // Jeu
        DiamantGain1,       // trésor en diamants, étages 1 à 5
        DiamantGain2,       // étages 6 à 9
        DiamantGain3,       // étage 10 et plus
        DiamantCompteur,
        RepriseOk,
        Renaissance,
        RenaissanceTampon,
        PirateEclair,
        PerroquetCri,
        KrakenBebe,
        NavireCanon,
        PaonRoue,
        LionRugit,
        PoussinPiou,
        ChatonMiaou,
        BaleineChant,
        // Interface
        RepriseOffre,
        RecapEtage,
        UiBoutonRond,
        UiInfobulle,
        UiOnglet,
        UiRefus,
        // Ouverture 3D (couches calées sur art-bible §14.4, identiques pour toutes les raretés)
        Boite3dAtterrit,
        Boite3dSaut,
        Boite3dCraque,
        Boite3dNoeud,
        Boite3dCouvercle,
        Boite3dFaisceau,
        // Touches thématiques des Secrets (§6.6)
        PirateVague,
        AnimalPattes,
        PirateSecretKraken,
        PirateSecretAbysses,
        AnimalSecretLion,
        AnimalSecretBaleine
    }
}
