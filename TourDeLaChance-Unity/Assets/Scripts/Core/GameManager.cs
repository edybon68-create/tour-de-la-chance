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

    /// <summary>
    /// Le chef d'orchestre : il connaît l'étage, la cagnotte, l'état de la partie,
    /// et applique les règles du GDD (gain = 10 × étage, quitte ou double).
    ///
    /// Nouveau flux (spec sons-animations §2.1) : après un trésor, on monte TOUT SEUL à
    /// l'étage suivant ; le bouton « Encaisser X » est proposé pendant le choix du coffre.
    /// C'est ce script qui mène la danse (le temps de la montée, le verrou anti-clic) :
    /// les scripts d'animation se contentent de suivre ses événements. Si une animation
    /// manque, la partie avance quand même.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        // ---------- Les durées du nouveau flux (lues aussi par l'affichage) ----------
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
        /// Durée du fondu de l'écran titre (les coffres attendent la fin).
        public const float TitleFadeDuration = 0.4f;

        [SerializeField] ChestTrial chestTrial;
        [SerializeField] int coinsPerFloor = 10;

        public int Floor { get; private set; }
        public int Pot { get; private set; }        // la cagnotte de la partie en cours
        public int LastGain { get; private set; }
        public GameState State { get; private set; }

        /// Le shop est un panneau ouvert "par-dessus" l'état courant, PAS un nouvel état :
        /// à la fermeture, on retrouve le même étage, le même état et le même piège.
        public bool ShopOpen { get; private set; }

        /// Verrou de transition (montée, redescente, fondu du titre, anti-clic).
        /// C'est un drapeau SÉPARÉ de ShopOpen : fermer le shop pendant une transition
        /// ne doit pas débloquer les coffres trop tôt.
        public bool TransitionLocked { get; private set; }

        /// Le palier de décor (1 à 4) correspondant à l'étage actuel (§3.14).
        public int Tier => TierForFloor(Floor);

        /// Peut-on ouvrir le shop ? (spec shop §3.1) Seulement au début de partie
        /// (étage 1, aucun coffre choisi), sur l'écran Perdu ou sur l'écran Encaissé.
        /// Jamais pendant une épreuve ni pendant une transition.
        public bool CanOpenShop =>
            !ShopOpen && !TransitionLocked &&
            ((Floor == 1 && State == GameState.Choosing) ||
             State == GameState.Lost ||
             State == GameState.CashedOut);

        /// Peut-on encaisser ? Pendant le choix, à partir de l'étage 2 (avant, la cagnotte
        /// vaut 0), shop fermé, hors verrou de transition.
        public bool CanCashOut =>
            !ShopOpen && !TransitionLocked &&
            State == GameState.Choosing && Floor >= 2 && Pot > 0;

        /// L'interface s'abonne à cet événement pour se mettre à jour.
        public event Action Changed;
        /// La montée commence (0,6 s après un trésor) : caméra, voile, son…
        public event Action ClimbStarted;
        /// « Rejouer » : la redescente vers l'étage 1 commence.
        public event Action DescentStarted;
        /// Pendant l'écran sombre : le nouvel étage est prêt (étage, piège, palier).
        public event Action FloorPrepared;

        Coroutine transition;

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

        // Au lancement, on reste sur l'écran titre : aucun coffre n'est préparé
        // (chestTrial.Begin() n'est pas appelé), donc ils ne sont pas cliquables,
        // et CanOpenShop est faux (l'état n'est ni Choosing, ni Lost, ni CashedOut).
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
        /// Ignoré si on a déjà quitté l'écran titre. Les coffres restent bloqués pendant
        /// le fondu de l'écran titre.
        public void StartGame()
        {
            if (State != GameState.Title) return;
            Floor = 1;
            Pot = 0;
            chestTrial.Begin(Floor);
            SetLock(true);
            SetState(GameState.Choosing);
            RunTransition(UnlockAfter(TitleFadeDuration));
        }

        /// « Rejouer » : nouvelle partie, retour à l'étage 1 avec la petite redescente.
        public void NewRun()
        {
            if (ShopOpen || TransitionLocked) return; // on ferme d'abord le shop
            if (State != GameState.Lost && State != GameState.CashedOut) return;
            RunTransition(DescentRoutine());
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
        }

        /// Ouvre le shop (si c'est permis) et bloque les coffres.
        public void OpenShop()
        {
            if (!CanOpenShop) return;
            ShopOpen = true;
            ApplyInputLock();
            // En fin de partie, les couvercles ouverts cachent le personnage :
            // on les referme (en douceur) pour qu'il serve bien d'aperçu.
            if (State == GameState.Lost || State == GameState.CashedOut)
                chestTrial.CloseAllLids(animated: true);
            Changed?.Invoke();
        }

        /// Ferme le shop et débloque les coffres. On ne relance PAS chestTrial.Begin() :
        /// le coffre piégé reste le même.
        public void CloseShop()
        {
            if (!ShopOpen) return;
            ShopOpen = false;
            ApplyInputLock();
            Changed?.Invoke();
        }

        /// OUTIL DE TEST (spec §8, critère 43) : fixe l'étage puis déclenche une montée,
        /// comme après un trésor, pour vérifier chaque palier de décor sans dépendre de la chance.
        /// À appeler depuis `unity command script eval`, pendant le choix d'un coffre.
        public void DebugClimbFrom(int floor)
        {
            if (ShopOpen || TransitionLocked || State != GameState.Choosing) return;
            chestTrial.CancelChoice();
            Floor = Mathf.Max(1, floor);
            // La cagnotte qu'on aurait gagnée jusqu'ici : 10 × (1 + 2 + … + étage).
            Pot = coinsPerFloor * Floor * (Floor + 1) / 2;
            LastGain = coinsPerFloor * Floor;
            SetLock(true);
            SetState(GameState.Climbing);
            RunTransition(ClimbRoutine());
        }

        void OnChoiceMade() => SetState(GameState.Revealing);

        void OnTrialFinished(bool won)
        {
            if (won)
            {
                LastGain = coinsPerFloor * Floor;
                Pot += LastGain;
                SetLock(true); // coffres et « Encaisser » ignorés jusqu'à la fin du verrou
                SetState(GameState.Climbing);
                RunTransition(ClimbRoutine());
            }
            else
            {
                LastGain = 0;
                Pot = 0;
                SetState(GameState.Lost);
            }
        }

        /// La montée automatique, étape par étape (§2.1). "yield return" = attendre.
        IEnumerator ClimbRoutine()
        {
            yield return new WaitForSeconds(ClimbDelay);
            ClimbStarted?.Invoke();

            yield return new WaitForSeconds(ClimbDarkTime);
            // Écran sombre : on change d'étage et on tire le nouveau piège.
            Floor++;
            chestTrial.Begin(Floor);
            FloorPrepared?.Invoke();
            Changed?.Invoke(); // le compteur « Étage » change pendant le noir

            yield return new WaitForSeconds(ClimbDuration - ClimbDarkTime);
            SetState(GameState.Choosing); // le verrou reste posé…

            yield return new WaitForSeconds(AntiClickLock);
            SetLock(false); // …et il est retiré ici : coffres et « Encaisser » actifs
            Changed?.Invoke();
        }

        /// « Rejouer » : on redescend à l'étage 1 (version courte de la montée, à l'envers).
        IEnumerator DescentRoutine()
        {
            SetLock(true);
            DescentStarted?.Invoke();
            Changed?.Invoke();

            yield return new WaitForSeconds(DescentDarkTime);
            Floor = 1;
            Pot = 0;
            chestTrial.Begin(Floor);
            FloorPrepared?.Invoke();
            Changed?.Invoke();

            yield return new WaitForSeconds(DescentDuration - DescentDarkTime);
            SetState(GameState.Choosing);

            yield return new WaitForSeconds(AntiClickLock);
            SetLock(false);
            Changed?.Invoke();
        }

        IEnumerator UnlockAfter(float seconds)
        {
            yield return new WaitForSeconds(seconds);
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

        /// Les coffres sont bloqués si le shop est ouvert OU si une transition est en cours.
        void ApplyInputLock() => chestTrial.InputLocked = ShopOpen || TransitionLocked;

        void SetState(GameState state)
        {
            State = state;
            Changed?.Invoke();
        }
    }
}
