using System;
using System.Collections;
using UnityEngine;

namespace TourDeLaChance
{
    /// Les différents moments d'une partie (une "machine à états").
    public enum GameState
    {
        Title,      // écran titre : la partie n'a pas encore commencé (placé en premier,
                    // c'est donc l'état par défaut dès le chargement de la scène)
        Choosing,   // le joueur choisit un coffre… ou encaisse (à partir de l'étage 2)
        Revealing,  // animation d'ouverture en cours
        Climbing,   // gagné : on monte tout seul à l'étage suivant (transition)
        Lost,       // piégé : la cagnotte est perdue
        CashedOut,  // encaissé : la cagnotte va dans le porte-monnaie
        ReviveOffer // v5 §11 : piégé, mais la reprise (1 diamant) est proposée ; la partie
                    // n'est PAS encore finie (ajouté à la fin : Title doit rester le premier)
    }

    /// Les pop-ups (spec v3 §4.1, v4 §5.6). None = aucune pop-up ouverte.
    /// v4 : + Classement (colonne de droite) et Reglages (roue dentée). AJOUTÉS À LA FIN :
    /// les outils de test utilisent les numéros (Boutique = 1 … Ameliorations = 4).
    /// v5 : + Renaissance (colonne de droite, sous le Classement), ajoutée à la fin.
    public enum PanelId { None, Boutique, Inventaire, Index, Ameliorations, Classement, Reglages, Renaissance }

    /// <summary>
    /// Le chef d'orchestre : il connaît l'étage, la cagnotte, l'état de la partie,
    /// et applique les règles du GDD (quitte ou double).
    ///
    /// v3 : il PRÉPARE chaque étage (spec v3 §5.5) : il lit les niveaux d'amélioration à cet
    /// instant, tire le nombre de coffres, leur rareté, calcule le gain, puis demande au
    /// ChestTrial de tirer le piège. Le résultat (FloorSetup) est figé pour l'étage.
    /// Il décide aussi QUAND un panneau peut s'ouvrir (CanOpenPanel) et lequel est ouvert,
    /// et gère « Rejouer » (redescente, ou remélange sur place à l'étage 1), le record et la
    /// cagnotte perdue (LastLoss).
    ///
    /// v5 : il tire aussi « ce trésor contient-il des diamants ? » à la préparation de l'étage
    /// (§10), propose la REPRISE après un piège (§11 : état ReviveOffer, AcceptRevive,
    /// DeclineRevive) et lance la nouvelle partie après la RENAISSANCE (§13, RequestRebirth).
    ///
    /// C'est ce script qui mène la danse (le temps de la montée, le verrou anti-clic) :
    /// les scripts d'animation se contentent de suivre ses événements. Si une animation
    /// manque, la partie avance quand même.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        // ---------- Les durées du flux (lues aussi par l'affichage) ----------
        // "const" = une valeur fixe, connue de tous (GameManager.ClimbDelay).

        /// Après un trésor, on attend un peu avant de monter (voir le trésor et le « +20 »).
        public const float ClimbDelay = 0.6f;
        /// Moment de la montée où l'écran est sombre : on change d'étage à ce moment-là.
        public const float ClimbDarkTime = 0.5f;
        /// Durée totale de la montée (caméra + voile sombre).
        public const float ClimbDuration = 1.05f;
        /// Verrou anti-clic après une transition : évite qu'un clic trop rapide ouvre un coffre.
        public const float AntiClickLock = 0.5f;
        /// « Rejouer » : moment sombre de la redescente, puis durée totale.
        public const float DescentDarkTime = 0.25f;
        public const float DescentDuration = 0.55f;
        /// « Rejouer » à l'étage 1 (spec v3 §12) : moment du nouveau tirage, puis durée totale.
        public const float ReshuffleSwapTime = 0.3f;
        public const float ReshuffleDuration = 0.55f;
        /// Durée du fondu de l'écran titre (les coffres attendent la fin).
        public const float TitleFadeDuration = 0.4f;
        /// v5 §11.2 : la reprise rejoue l'étage par un remélange sur place (mêmes durées que
        /// « Rejouer » à l'étage 1, plus la spirale bleutée de 0,4 s au début).
        public const float ReviveSwapTime = 0.45f;
        public const float ReviveDuration = 0.7f;
        /// v5 §13.4 : durée de l'animation de renaissance (passable après 1 s).
        public const float RebirthDuration = 4.2f;
        public const float RebirthSkippableAfter = 1f;

        /// Clé de sauvegarde du plus haut étage atteint (spec v3 §17).
        public const string RecordKey = "record_etage";
        /// « Nouveau record ! » ne s'affiche que si l'ancien record vaut au moins ceci (§11.1).
        public const int RecordBannerMinimum = 3;

        [SerializeField] ChestTrial chestTrial;
        [SerializeField] int coinsPerFloor = 10;

        public int Floor { get; private set; }
        public int Pot { get; private set; }        // la cagnotte de la partie en cours
        public int LastGain { get; private set; }
        /// La cagnotte perdue au dernier piège (pour le message « Cagnotte perdue : X »).
        public int LastLoss { get; private set; }
        public GameState State { get; private set; }

        /// Ce que vaut l'étage en cours (nombre de coffres, rareté, gain), figé à sa préparation.
        public FloorSetup Setup { get; private set; }

        /// Le panneau ouvert "par-dessus" l'état courant (PAS un nouvel état) : à la fermeture,
        /// on retrouve le même étage, le même état et le même piège.
        public PanelId PanelOpen { get; private set; }
        public bool IsPanelOpen => PanelOpen != PanelId.None;

        /// Une ouverture de boîte est en cours (prévenu par l'écran d'ouverture).
        public bool BoxOpeningActive { get; private set; }

        /// Verrou de transition (montée, redescente, remélange, fondu du titre, anti-clic).
        /// C'est un drapeau SÉPARÉ des panneaux : fermer un panneau pendant une transition
        /// ne doit pas débloquer les coffres trop tôt.
        public bool TransitionLocked { get; private set; }

        /// Le palier de décor (1 à 4) correspondant à l'étage actuel.
        public int Tier => TierForFloor(Floor);

        /// Le plus haut étage atteint (sauvegardé).
        public int Record => Mathf.Max(0, PlayerPrefs.GetInt(RecordKey, 0));

        /// Vrai si l'étage qu'on vient de préparer bat le record pour la première fois de la
        /// partie (et que l'ancien record valait au moins 3) : la bannière l'annonce.
        public bool NewRecordThisFloor { get; private set; }

        /// Peut-on ouvrir une pop-up (Boutique, Inventaire, Index, Améliorations, Classement) ?
        /// (spec v4 §4.4, remplace v3 §4.2) À tous les moments calmes : choix d'un coffre
        /// (verrou retiré), écrans Perdu / Encaissé. PLUS sur l'écran titre (v4 : il reste
        /// propre). Jamais pendant une épreuve, une transition ou une ouverture de boîte.
        public bool CanOpenPanel =>
            !TransitionLocked && !BoxOpeningActive &&
            (State == GameState.Choosing || State == GameState.Lost || State == GameState.CashedOut);

        /// Peut-on ouvrir les Réglages (roue dentée) ? Comme les autres pop-ups, ET sur l'écran
        /// titre (pour couper la musique avant de jouer). Jamais pendant une ouverture de boîte.
        public bool CanOpenSettings =>
            !BoxOpeningActive && ((State == GameState.Title && !TransitionLocked) || CanOpenPanel ||
                                  (State == GameState.ReviveOffer && !TransitionLocked));

        /// Le record d'étage au début de la partie en cours (pour le badge « RECORD »).
        public int RecordAtRunStart => recordAtRunStart;

        /// Vrai pendant toute la partie dès que l'étage a dépassé le record de départ (et que
        /// ce record valait au moins 3, comme la bannière « Nouveau record ! ») : le badge
        /// d'étage passe en or avec l'étiquette « RECORD » (spec v4 §6.1).
        public bool RecordBeatenThisRun => recordAnnounced;

        /// Peut-on encaisser ? Pendant le choix, à partir de l'étage 2 (avant, la cagnotte
        /// vaut 0), panneau fermé, hors verrou de transition.
        public bool CanCashOut =>
            !IsPanelOpen && !TransitionLocked &&
            State == GameState.Choosing && Floor >= 2 && Pot > 0;

        // ---------- v5 : diamants, reprise, renaissance ----------

        /// Les diamants gagnés au dernier trésor en diamants (0 sinon). Ils sont DÉJÀ dans le
        /// compteur (et sauvegardés) quand l'événement DiamondsWon arrive.
        public int LastDiamonds { get; private set; }
        /// La reprise a-t-elle déjà servi dans cette partie ? (jamais sauvegardé : une partie
        /// en cours ne l'est jamais)
        public bool ReviveUsed { get; private set; }
        /// Vrai pour l'étage rejoué juste après une reprise (la carte d'étage le dit).
        public bool RevivedThisFloor { get; private set; }
        /// La renaissance est en cours d'animation (avant la nouvelle partie).
        public bool RebirthPlaying { get; private set; }

        /// Coût de la reprise (1 diamant), lu dans le catalogue de l'économie.
        public static int ReviveCost => EconomyCatalog.Instance.ReviveCost;

        /// Peut-on proposer la reprise après ce piège ? (§11.1) Cagnotte > 0 (donc à partir de
        /// l'étage 2), au moins 1 diamant, reprise pas encore utilisée dans la partie.
        /// Sans diamant : RIEN n'est proposé (on ne propose jamais d'acheter).
        bool ReviveAllowed(int pot) =>
            pot > 0 && !ReviveUsed && EconomyCatalog.Instance.MaxRevivesPerRun > 0 && Diamonds.Count >= ReviveCost;

        /// v5 §13.2 : est-ce un moment calme, SANS cagnotte, pour renaître ? Perdu, Encaissé,
        /// ou choix à l'étage 1. Jamais pendant une épreuve, une transition, une ouverture de
        /// boîte ou une reprise proposée. (Les pièces sont vérifiées par Rebirth.)
        public bool CanRebirthNow =>
            !TransitionLocked && !BoxOpeningActive && !RebirthPlaying &&
            (State == GameState.Lost || State == GameState.CashedOut || (State == GameState.Choosing && Floor <= 1 && Pot == 0));

        /// L'interface s'abonne à cet événement pour se mettre à jour.
        public event Action Changed;
        /// La montée commence (0,6 s après un trésor) : caméra, voile, son…
        public event Action ClimbStarted;
        /// « Rejouer » depuis l'étage 2 ou plus : la redescente vers l'étage 1 commence.
        public event Action DescentStarted;
        /// « Rejouer » à l'étage 1 : remélange sur place (ni caméra, ni voile).
        public event Action ReshuffleStarted;
        /// Le nouvel étage est prêt (étage, nombre de coffres, rareté, piège, palier).
        public event Action FloorPrepared;
        /// Le nouvel étage se montre : fin de la montée / de la redescente / du remélange
        /// (le message et l'étiquette d'étage apparaissent, les coffres précieux sautent).
        public event Action FloorShown;
        /// v4 §12 : une partie vient de se terminer (Perdu ou Encaissé). Le GameManager
        /// prévient seulement : les statistiques et le classement s'en occupent.
        public event Action<RunResult> RunEnded;
        /// v5 §10.5 : un trésor en diamants vient d'être ouvert (montant déjà ajouté et sauvegardé).
        public event Action<int> DiamondsWon;
        /// v5 §11 : la reprise est proposée (état ReviveOffer).
        public event Action ReviveOffered;
        /// v5 §11.2 : reprise acceptée : la spirale « retour en arrière » et le remélange commencent.
        public event Action ReviveStarted;
        /// v5 §13.4 : la renaissance vient d'être faite, son animation commence.
        public event Action RebirthStarted;

        Coroutine transition;
        // Le générateur des RÈGLES (nombre de coffres, rareté, piège) : créé une fois, séparé
        // de celui des effets visuels et de celui des boîtes.
        readonly System.Random rulesRandom = new System.Random();

        // Record : sa valeur au début de la partie, et s'il a déjà été annoncé.
        int recordAtRunStart;
        bool recordAnnounced;

        // Outils de test : forcer le nombre de coffres / la rareté du prochain étage (−1 = non).
        int forcedCount = -1, forcedRarity = -1;
        bool forcedDiamonds;       // outil de test : le prochain étage est « en diamants »
        int savedPot;              // la cagnotte mise de côté pendant la reprise proposée
        bool skipRebirthRequested; // clic pendant l'animation de renaissance

        void OnEnable()
        {
            chestTrial.ChoiceMade += OnChoiceMade;
            chestTrial.Finished += OnTrialFinished;
        }

        void OnDisable()
        {
            chestTrial.ChoiceMade -= OnChoiceMade;
            chestTrial.Finished -= OnTrialFinished;
        }

        // Au lancement, on reste sur l'écran titre : aucun coffre n'est préparé, donc ils ne
        // sont pas cliquables. v4 : seule la pop-up Réglages est permise sur l'écran titre.
        void Start() => SetState(GameState.Title);

        /// Le palier de décor d'un étage : 1-3 → 1, 4-6 → 2, 7-9 → 3, 10 et plus → 4.
        public static int TierForFloor(int floor)
        {
            if (floor <= 3) return 1;
            if (floor <= 6) return 2;
            if (floor <= 9) return 3;
            return 4;
        }

        /// Le bouton « Jouer » de l'écran titre : lance la toute première partie.
        /// Ignoré si on a déjà quitté l'écran titre ou si un panneau est ouvert.
        public void StartGame()
        {
            if (State != GameState.Title || IsPanelOpen) return;
            BeginRun();
            PrepareFloor();
            FloorPrepared?.Invoke();
            SetLock(true);
            SetState(GameState.Choosing);
            RunTransition(StartRoutine());
        }

        /// « Rejouer » : nouvelle partie. Depuis l'étage 2 ou plus : la petite redescente.
        /// Depuis l'étage 1 (piège au premier coffre) : remélange sur place (spec v3 §12).
        public void NewRun()
        {
            if (IsPanelOpen || TransitionLocked) return; // on ferme d'abord le panneau
            if (State != GameState.Lost && State != GameState.CashedOut) return;
            RunTransition(Floor <= 1 ? ReshuffleRoutine() : DescentRoutine());
        }

        /// Le joueur sécurise sa cagnotte (bouton « Encaisser X », pendant le choix).
        /// Les coffres de l'étage restent fermés et le piège n'est pas montré.
        /// Un double-clic n'encaisse qu'une fois : au 2e clic, l'état n'est plus Choosing.
        public void CashOut()
        {
            if (!CanCashOut) return;
            chestTrial.CancelChoice(); // plus aucun coffre ne peut être choisi
            LastGain = Pot;
            Wallet.Add(Pot);
            Pot = 0;
            SetState(GameState.CashedOut);
            RunEnded?.Invoke(new RunResult { Floor = Floor, CashedOut = LastGain, Lost = false, UsedRevive = ReviveUsed });
        }

        // =====================================================================
        // Les panneaux (spec v3 §4.2)
        // =====================================================================

        /// Peut-on ouvrir CETTE pop-up maintenant ? (Réglages a sa propre règle, §4.4.)
        public bool CanOpen(PanelId panel) => panel == PanelId.Reglages ? CanOpenSettings : CanOpenPanel;

        /// Ouvre un panneau (s'il est permis) ; s'il y en avait un autre, il est remplacé.
        /// Les coffres sont bloqués tant qu'un panneau est ouvert. On n'appelle JAMAIS
        /// chestTrial.Begin() ici : le coffre piégé de l'étage reste le même.
        public void OpenPanel(PanelId panel)
        {
            if (panel == PanelId.None) { ClosePanel(); return; }
            if (panel == PanelOpen || !CanOpen(panel)) return;
            bool wasOpen = IsPanelOpen;
            PanelOpen = panel;
            ApplyInputLock();
            // En fin de partie, les couvercles ouverts cachent le cheval : on les referme
            // (en douceur) pour qu'il serve bien d'aperçu.
            if (!wasOpen && (State == GameState.Lost || State == GameState.CashedOut))
                chestTrial.CloseAllLids(animated: true);
            Changed?.Invoke();
        }

        /// Clic sur le bouton d'un panneau : l'ouvre, ou le ferme s'il est déjà ouvert.
        public void TogglePanel(PanelId panel)
        {
            if (panel == PanelOpen) ClosePanel();
            else OpenPanel(panel);
        }

        /// Ferme le panneau ouvert (« × », Échap, ou re-clic sur son bouton), sauf pendant une
        /// ouverture de boîte. Les coffres sont débloqués (s'il n'y a pas de transition).
        public void ClosePanel()
        {
            if (!IsPanelOpen || BoxOpeningActive) return;
            PanelOpen = PanelId.None;
            ApplyInputLock();
            Changed?.Invoke();
        }

        /// L'écran d'ouverture de boîte prévient qu'il commence (true) ou se ferme (false).
        public void SetBoxOpening(bool active)
        {
            if (BoxOpeningActive == active) return;
            BoxOpeningActive = active;
            Changed?.Invoke();
        }

        // =====================================================================
        // Préparer un étage (spec v3 §5.5)
        // =====================================================================

        /// Dans cet ordre : 1. lire les niveaux d'amélioration ET le bonus de tenue MAINTENANT ;
        /// 2. tirer le nombre de coffres ; 3. tirer la rareté ; 4. calculer le gain ; 5. préparer
        /// les coffres (qui tirent le piège). Tout est figé pour l'étage (v4 §9.3 : équiper un
        /// objet pendant le choix ne change pas l'étage affiché).
        /// RÈGLE D'OR : le bonus de tenue n'entre QUE dans le calcul du gain (étape 4) ; les
        /// tirages des étapes 2, 3 et 5 ne le connaissent pas.
        void PrepareFloor()
        {
            int gainsLevel = Upgrades.Level(Upgrades_.Gains);
            int chestsLevel = Upgrades.Level(Upgrades_.Chests);
            int trapsLevel = Upgrades.Level(Upgrades_.Traps);
            int outfitBonus = Shop.OutfitBonusPercent;

            int count = forcedCount > 0 ? forcedCount : Upgrades.DrawChestCount(trapsLevel, rulesRandom);
            int rank = forcedRarity >= 0 ? forcedRarity : Upgrades.DrawChestRarity(chestsLevel, rulesRandom);
            forcedCount = forcedRarity = -1; // un forçage ne vaut que pour UN étage
            count = Mathf.Clamp(count, 3, ChestTrial.MaxChests);

            // v5 §13 : ×2 après la renaissance (lu MAINTENANT, comme les niveaux).
            int rebirth = Rebirth.CoinMultiplier;
            var rarity = Upgrades.Rarity(rank);

            // v5 §10 : « ce trésor contient-il des diamants ? » Tiré APRÈS le gain et AVANT le
            // piège, avec le générateur des règles ; chacun des tirages reste indépendant.
            int chance = rarity != null ? Mathf.Clamp(rarity.diamondChancePermille, 0, 1000) : 0;
            bool diamonds = rulesRandom.Next(0, 1000) < chance;
            if (forcedDiamonds) { diamonds = true; forcedDiamonds = false; }
            int diamondAmount = EconomyCatalog.Instance.DiamondsForFloor(Floor) * Rebirth.DiamondMultiplier;

            Setup = new FloorSetup
            {
                ChestCount = count,
                RarityRank = rank,
                Rarity = rarity,
                Gain = Upgrades.FloorGain(coinsPerFloor, Floor, gainsLevel, rank, outfitBonus, rebirth),
                OutfitBonusPercent = outfitBonus,
                DiamondTreasure = diamonds,
                DiamondAmount = diamondAmount,
                DiamondChancePermille = chance,
                RebirthMultiplier = rebirth
            };
            chestTrial.Begin(Floor, count, rank, rulesRandom);
            UpdateRecord();
        }

        void BeginRun()
        {
            Floor = 1;
            Pot = 0;
            LastLoss = 0;
            LastDiamonds = 0;
            ReviveUsed = false;
            RevivedThisFloor = false;
            recordAtRunStart = Record;
            recordAnnounced = false;
        }

        /// Le record est mis à jour à chaque étage préparé, s'il est battu (spec v3 §17).
        void UpdateRecord()
        {
            NewRecordThisFloor = false;
            if (Floor <= Record) return;
            if (!recordAnnounced && Floor > recordAtRunStart && recordAtRunStart >= RecordBannerMinimum)
            {
                NewRecordThisFloor = true;
                recordAnnounced = true; // une seule fois par partie
            }
            PlayerPrefs.SetInt(RecordKey, Floor);
            PlayerPrefs.Save();
        }

        // =====================================================================
        // Outils de test (depuis `unity command script eval`)
        // =====================================================================

        /// OUTIL DE TEST : fixe l'étage puis déclenche une montée, comme après un trésor,
        /// pour vérifier chaque palier de décor sans dépendre de la chance.
        public void DebugClimbFrom(int floor)
        {
            if (IsPanelOpen || TransitionLocked || State != GameState.Choosing) return;
            chestTrial.CancelChoice();
            Floor = Mathf.Max(1, floor);
            // La cagnotte qu'on aurait gagnée jusqu'ici : 10 × (1 + 2 + … + étage).
            Pot = coinsPerFloor * Floor * (Floor + 1) / 2;
            LastGain = coinsPerFloor * Floor;
            SetLock(true);
            SetState(GameState.Climbing);
            RunTransition(ClimbRoutine());
        }

        /// OUTIL DE TEST : force le nombre de coffres (3 à 6, 0 = tirage normal) et la rareté
        /// (0 à 4, −1 = tirage normal) du PROCHAIN étage préparé seulement.
        public void DebugForceNextFloor(int count, int rarityRank)
        {
            forcedCount = count;
            forcedRarity = rarityRank;
        }

        /// OUTIL DE TEST (v5) : le PROCHAIN étage préparé sera « en diamants ».
        public void DebugForceDiamondTreasure() => forcedDiamonds = true;

        /// OUTIL DE TEST (v5) : choisit le coffre piégé de l'étage (pour tester la reprise).
        public void DebugPickTrap() => chestTrial.Choose(chestTrial.DebugTrapIndex);

        /// OUTIL DE TEST (v5) : choisit un coffre qui n'est PAS piégé.
        public void DebugPickTreasure() => chestTrial.Choose(chestTrial.DebugTrapIndex == 0 ? 1 : 0);

        /// OUTIL DE TEST (v5 critère 26) : simule n étages préparés avec un générateur à part
        /// (rien n'est sauvegardé) et renvoie la part d'étages « en diamants » par rareté.
        public static string DebugSimulateDiamonds(int floors, int chestsLevel, int seed)
        {
            var random = new System.Random(seed);
            int n = Upgrades.RarityCount;
            var total = new int[n];
            var withDiamonds = new int[n];
            for (int i = 0; i < floors; i++)
            {
                Upgrades.DrawChestCount(0, random);                 // même ordre que le jeu
                int rank = Upgrades.DrawChestRarity(chestsLevel, random);
                int chance = Upgrades.Rarity(rank).diamondChancePermille;
                total[rank]++;
                if (random.Next(0, 1000) < chance) withDiamonds[rank]++;
                ChestTrial.DrawTrap(3, random);
            }
            var sb = new System.Text.StringBuilder($"{floors} étages : ");
            for (int r = 0; r < n; r++)
                if (total[r] > 0) sb.Append($"{Upgrades.Rarity(r).shortName} {100.0 * withDiamonds[r] / total[r]:0.00} % ({total[r]}) ; ");
            return sb.ToString();
        }

        /// OUTIL DE TEST : le coffre piégé de l'étage (critère 2 : il ne change pas quand on
        /// ouvre et ferme un panneau).
        public int DebugTrapIndex => chestTrial.DebugTrapIndex;

        void OnChoiceMade() => SetState(GameState.Revealing);

        void OnTrialFinished(bool won)
        {
            RevivedThisFloor = false;
            if (won)
            {
                if (Setup.DiamondTreasure && Setup.DiamondAmount > 0)
                {
                    // v5 §10.1 : trésor en diamants. Les diamants vont DIRECTEMENT dans le
                    // compteur (sauvegardé tout de suite) ; la cagnotte ne bouge pas.
                    LastGain = 0;
                    LastDiamonds = Setup.DiamondAmount;
                    Diamonds.Add(LastDiamonds, fromTreasure: true);
                    DiamondsWon?.Invoke(LastDiamonds);
                }
                else
                {
                    LastGain = Setup.Gain; // le gain FIGÉ à la préparation de l'étage
                    LastDiamonds = 0;
                    Pot += LastGain;
                }
                SetLock(true); // coffres et « Encaisser » ignorés jusqu'à la fin du verrou
                SetState(GameState.Climbing);
                RunTransition(ClimbRoutine());
            }
            else
            {
                LastGain = 0;
                LastDiamonds = 0;
                LastLoss = Pot;
                if (ReviveAllowed(Pot))
                {
                    // v5 §11.2 : la cagnotte est mise de côté (pas encore perdue) et la partie
                    // n'est pas terminée : pas de RunEnded. Aucun compte à rebours.
                    savedPot = Pot;
                    SetState(GameState.ReviveOffer);
                    ReviveOffered?.Invoke();
                    return;
                }
                EndRunLost();
            }
        }

        /// La partie est perdue pour de bon (piège sans reprise, ou « Non merci »).
        void EndRunLost()
        {
            Pot = 0;
            savedPot = 0;
            SetState(GameState.Lost);
            RunEnded?.Invoke(new RunResult { Floor = Floor, CashedOut = 0, Lost = true, UsedRevive = ReviveUsed });
        }

        // =====================================================================
        // v5 §11 : la reprise
        // =====================================================================

        /// « Continuer — 1 ◆ » : la logique relit tout (un double-clic ne dépense qu'un
        /// diamant : au 2e clic, on n'est plus en ReviveOffer). Dépense et sauvegarde le
        /// diamant, marque la reprise, rend la cagnotte et REJOUE l'étage avec un tirage neuf.
        public bool AcceptRevive()
        {
            if (State != GameState.ReviveOffer || TransitionLocked) return false;
            if (!ReviveAllowed(savedPot) || !Diamonds.TrySpend(ReviveCost))
            {
                DeclineRevive();
                return false;
            }
            Diamonds.CountRevive();
            ReviveUsed = true;
            Pot = savedPot;
            LastLoss = 0;
            RunTransition(ReviveRoutine());
            return true;
        }

        /// « Non merci » (ou Échap) : la partie est perdue comme avant.
        public void DeclineRevive()
        {
            if (State != GameState.ReviveOffer) return;
            LastLoss = savedPot;
            EndRunLost();
        }

        /// Le remélange de la reprise (comme v3 §12, mais SANS nouvelle partie) : même étage,
        /// nouveau tirage complet (coffres, rareté, gain, diamants, piège) avec les niveaux et
        /// la tenue du moment.
        IEnumerator ReviveRoutine()
        {
            SetLock(true);
            ReviveStarted?.Invoke();
            Changed?.Invoke();

            yield return new WaitForSeconds(ReviveSwapTime);
            PrepareFloor();
            RevivedThisFloor = true;
            FloorPrepared?.Invoke();
            Changed?.Invoke();

            yield return new WaitForSeconds(ReviveDuration - ReviveSwapTime);
            SetState(GameState.Choosing);
            FloorShown?.Invoke();

            yield return new WaitForSeconds(AntiClickLock);
            SetLock(false);
            Changed?.Invoke();
        }

        // =====================================================================
        // v5 §13 : la renaissance
        // =====================================================================

        /// Clic sur « Oui, renaître ! » : vérifie le moment (calme, sans cagnotte) puis la
        /// sauvegarde (Rebirth.TryRebirth fait tout en une fois et sauvegarde), ferme la pop-up
        /// et lance l'animation ; à la fin, une NOUVELLE PARTIE (le 1er étage a le ×2).
        public bool RequestRebirth()
        {
            if (!CanRebirthNow) return false;
            if (!Rebirth.TryRebirth()) return false;
            PanelOpen = PanelId.None;
            RebirthPlaying = true;
            skipRebirthRequested = false;
            chestTrial.CancelChoice();
            SetLock(true);
            RebirthStarted?.Invoke();
            Changed?.Invoke();
            RunTransition(RebirthRoutine());
            return true;
        }

        /// L'animation peut être passée après 1 s (clic, Espace, Échap).
        public void SkipRebirthAnimation() => skipRebirthRequested = true;

        IEnumerator RebirthRoutine()
        {
            for (float t = 0f; t < RebirthDuration; t += Time.deltaTime)
            {
                if (skipRebirthRequested && t >= RebirthSkippableAfter) break;
                yield return null;
            }
            RebirthPlaying = false;
            // Nouvelle partie : remélange sur place à l'étage 1, redescente sinon.
            yield return Floor <= 1 ? ReshuffleRoutine() : DescentRoutine();
        }

        // =====================================================================
        // Les transitions (coroutines : "yield return" = attendre)
        // =====================================================================

        IEnumerator StartRoutine()
        {
            yield return new WaitForSeconds(TitleFadeDuration);
            FloorShown?.Invoke();
            SetLock(false);
            Changed?.Invoke();
        }

        /// La montée automatique, étape par étape.
        IEnumerator ClimbRoutine()
        {
            yield return new WaitForSeconds(ClimbDelay);
            ClimbStarted?.Invoke();

            yield return new WaitForSeconds(ClimbDarkTime);
            // Écran sombre : on change d'étage et on prépare le nouveau (coffres, rareté, piège).
            Floor++;
            PrepareFloor();
            FloorPrepared?.Invoke();
            Changed?.Invoke(); // le compteur « Étage » change pendant le noir

            yield return new WaitForSeconds(ClimbDuration - ClimbDarkTime);
            SetState(GameState.Choosing); // le verrou reste posé…
            FloorShown?.Invoke();

            yield return new WaitForSeconds(AntiClickLock);
            SetLock(false); // …et il est retiré ici : coffres et « Encaisser » actifs
            Changed?.Invoke();
        }

        /// « Rejouer » depuis l'étage 2 ou plus : on redescend à l'étage 1.
        IEnumerator DescentRoutine()
        {
            SetLock(true);
            DescentStarted?.Invoke();
            Changed?.Invoke();

            yield return new WaitForSeconds(DescentDarkTime);
            BeginRun();
            PrepareFloor();
            FloorPrepared?.Invoke();
            Changed?.Invoke();

            yield return new WaitForSeconds(DescentDuration - DescentDarkTime);
            SetState(GameState.Choosing);
            FloorShown?.Invoke();

            yield return new WaitForSeconds(AntiClickLock);
            SetLock(false);
            Changed?.Invoke();
        }

        /// « Rejouer » à l'étage 1 (spec v3 §12) : on est déjà en bas, pas de redescente.
        /// Les couvercles se ferment, les coffres font « pouf », et à 0,3 s le nouvel étage est
        /// préparé (nouveau nombre, nouvelle rareté, nouveau piège) ; ils réapparaissent.
        IEnumerator ReshuffleRoutine()
        {
            SetLock(true);
            ReshuffleStarted?.Invoke();
            Changed?.Invoke();

            yield return new WaitForSeconds(ReshuffleSwapTime);
            BeginRun();
            PrepareFloor();
            FloorPrepared?.Invoke();
            Changed?.Invoke();

            yield return new WaitForSeconds(ReshuffleDuration - ReshuffleSwapTime);
            SetState(GameState.Choosing);
            FloorShown?.Invoke();

            yield return new WaitForSeconds(AntiClickLock);
            SetLock(false);
            Changed?.Invoke();
        }

        /// Lance une transition (en arrêtant celle d'avant, par sécurité).
        void RunTransition(IEnumerator routine)
        {
            if (transition != null) StopCoroutine(transition);
            transition = StartCoroutine(routine);
        }

        void SetLock(bool locked)
        {
            TransitionLocked = locked;
            ApplyInputLock();
        }

        /// Les coffres sont bloqués si un panneau est ouvert OU si une transition est en cours.
        void ApplyInputLock() => chestTrial.InputLocked = IsPanelOpen || TransitionLocked;

        void SetState(GameState state)
        {
            State = state;
            Changed?.Invoke();
        }
    }
}
