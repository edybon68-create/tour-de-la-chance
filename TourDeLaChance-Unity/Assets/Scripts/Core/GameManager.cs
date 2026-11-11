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
        CashedOut   // encaissé : la cagnotte va dans le porte-monnaie
    }

    /// Les pop-ups (spec v3 §4.1, v4 §5.6). None = aucune pop-up ouverte.
    /// v4 : + Classement (colonne de droite) et Reglages (roue dentée). AJOUTÉS À LA FIN :
    /// les outils de test utilisent les numéros (Boutique = 1 … Ameliorations = 4).
    public enum PanelId { None, Boutique, Inventaire, Index, Ameliorations, Classement, Reglages }

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
            !BoxOpeningActive && ((State == GameState.Title && !TransitionLocked) || CanOpenPanel);

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

        Coroutine transition;
        // Le générateur des RÈGLES (nombre de coffres, rareté, piège) : créé une fois, séparé
        // de celui des effets visuels et de celui des boîtes.
        readonly System.Random rulesRandom = new System.Random();

        // Record : sa valeur au début de la partie, et s'il a déjà été annoncé.
        int recordAtRunStart;
        bool recordAnnounced;

        // Outils de test : forcer le nombre de coffres / la rareté du prochain étage (−1 = non).
        int forcedCount = -1, forcedRarity = -1;

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
            RunEnded?.Invoke(new RunResult { Floor = Floor, CashedOut = LastGain, Lost = false });
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

            Setup = new FloorSetup
            {
                ChestCount = count,
                RarityRank = rank,
                Rarity = Upgrades.Rarity(rank),
                Gain = Upgrades.FloorGain(coinsPerFloor, Floor, gainsLevel, rank, outfitBonus),
                OutfitBonusPercent = outfitBonus
            };
            chestTrial.Begin(Floor, count, rank, rulesRandom);
            UpdateRecord();
        }

        void BeginRun()
        {
            Floor = 1;
            Pot = 0;
            LastLoss = 0;
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

        /// OUTIL DE TEST : le coffre piégé de l'étage (critère 2 : il ne change pas quand on
        /// ouvre et ferme un panneau).
        public int DebugTrapIndex => chestTrial.DebugTrapIndex;

        void OnChoiceMade() => SetState(GameState.Revealing);

        void OnTrialFinished(bool won)
        {
            if (won)
            {
                LastGain = Setup.Gain; // le gain FIGÉ à la préparation de l'étage
                Pot += LastGain;
                SetLock(true); // coffres et « Encaisser » ignorés jusqu'à la fin du verrou
                SetState(GameState.Climbing);
                RunTransition(ClimbRoutine());
            }
            else
            {
                LastGain = 0;
                LastLoss = Pot;
                Pot = 0;
                SetState(GameState.Lost);
                RunEnded?.Invoke(new RunResult { Floor = Floor, CashedOut = 0, Lost = true });
            }
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
