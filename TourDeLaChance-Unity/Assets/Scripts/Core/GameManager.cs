using System;
using UnityEngine;

namespace TourDeLaChance
{
    /// Les différents moments d'une partie (une "machine à états").
    public enum GameState
    {
        Choosing,   // le joueur choisit un coffre
        Revealing,  // animation d'ouverture en cours
        Decision,   // gagné : encaisser ou continuer ?
        Lost,       // piégé : la cagnotte est perdue
        CashedOut   // encaissé : la cagnotte va dans le porte-monnaie
    }

    /// <summary>
    /// Le chef d'orchestre : il connaît l'étage, la cagnotte, l'état de la partie,
    /// et applique les règles du GDD (gain = 10 × étage, quitte ou double).
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        [SerializeField] ChestTrial chestTrial;
        [SerializeField] int coinsPerFloor = 10;

        public int Floor { get; private set; }
        public int Pot { get; private set; }        // la cagnotte de la partie en cours
        public int LastGain { get; private set; }
        public GameState State { get; private set; }

        /// L'interface s'abonne à cet événement pour se mettre à jour.
        public event Action Changed;

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

        void Start() => NewRun();

        /// Nouvelle partie : retour à l'étage 1, cagnotte vide.
        public void NewRun()
        {
            Floor = 1;
            Pot = 0;
            StartFloor();
        }

        /// Le joueur prend le risque de monter d'un étage.
        public void Continue()
        {
            if (State != GameState.Decision) return;
            Floor++;
            StartFloor();
        }

        /// Le joueur sécurise sa cagnotte.
        public void CashOut()
        {
            if (State != GameState.Decision) return;
            LastGain = Pot;
            Wallet.Add(Pot);
            Pot = 0;
            SetState(GameState.CashedOut);
        }

        void StartFloor()
        {
            chestTrial.Begin();
            SetState(GameState.Choosing);
        }

        void OnChoiceMade() => SetState(GameState.Revealing);

        void OnTrialFinished(bool won)
        {
            if (won)
            {
                LastGain = coinsPerFloor * Floor;
                Pot += LastGain;
                SetState(GameState.Decision);
            }
            else
            {
                LastGain = 0;
                Pot = 0;
                SetState(GameState.Lost);
            }
        }

        void SetState(GameState state)
        {
            State = state;
            Changed?.Invoke();
        }
    }
}
