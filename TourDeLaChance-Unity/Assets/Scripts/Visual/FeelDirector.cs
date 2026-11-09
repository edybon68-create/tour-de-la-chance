using UnityEngine;

namespace TourDeLaChance
{
    /// <summary>
    /// Le « chef d'orchestre de l'affichage » (spec §6.3) : il ÉCOUTE le GameManager et le
    /// ChestTrial, et déclenche les animations 3D et les sons au bon moment.
    /// Il ne change jamais l'étage, la cagnotte, le porte-monnaie ni le piège :
    /// on pourrait l'enlever, la partie marcherait quand même (sans spectacle).
    /// (Les animations de l'interface, elles, sont dans HudController et TitleController.)
    /// </summary>
    public class FeelDirector : MonoBehaviour
    {
        [SerializeField] GameManager game;
        [SerializeField] ChestTrial chestTrial;
        [SerializeField] PlayerAnimator player;
        [SerializeField] CameraRig cameraRig;
        [SerializeField] CoinBurst coinBurst;
        [SerializeField] TrapCloud trapCloud;
        [SerializeField] MusicPlayer music;

        // Pour repérer les changements (avant / après) à chaque événement Changed.
        GameState lastState;
        bool lastShopOpen, lastLocked;

        // Invitation si le joueur hésite (§3.2) : 3 s sans survol ni choix, puis toutes les 5 s.
        float idleTime;
        float nextInvite = 3f;

        void OnEnable()
        {
            game.Changed += OnGameChanged;
            game.ClimbStarted += OnClimbStarted;
            game.DescentStarted += OnDescentStarted;
            game.FloorPrepared += OnFloorPrepared;
            chestTrial.HoverChanged += OnHoverChanged;
            chestTrial.SuspenseStarted += OnSuspenseStarted;
            chestTrial.PickedLidOpening += OnPickedLidOpening;
            chestTrial.OthersLidsOpening += OnOthersLidsOpening;
            chestTrial.ChestRevealed += OnChestRevealed;
            chestTrial.LidsClosing += OnLidsClosing;
            lastState = game.State;
        }

        void OnDisable()
        {
            game.Changed -= OnGameChanged;
            game.ClimbStarted -= OnClimbStarted;
            game.DescentStarted -= OnDescentStarted;
            game.FloorPrepared -= OnFloorPrepared;
            chestTrial.HoverChanged -= OnHoverChanged;
            chestTrial.SuspenseStarted -= OnSuspenseStarted;
            chestTrial.PickedLidOpening -= OnPickedLidOpening;
            chestTrial.OthersLidsOpening -= OnOthersLidsOpening;
            chestTrial.ChestRevealed -= OnChestRevealed;
            chestTrial.LidsClosing -= OnLidsClosing;
        }

        static void Play(Sfx id, float volume = 1f, float pitch = 0f)
        {
            if (SoundManager.Instance != null) SoundManager.Instance.Play(id, volume, pitch);
        }

        // ---------- GameManager ----------

        void OnGameChanged()
        {
            GameState state = game.State;
            bool stateChanged = state != lastState;

            if (stateChanged)
            {
                switch (state)
                {
                    case GameState.Choosing:
                        SetTension(MusicPlayer.TensionLevelFor(game.Floor));
                        break;
                    case GameState.Lost:
                        SetTension(0f); // la couche tension s'éteint en 1 s
                        break;
                    case GameState.CashedOut:
                        Play(Sfx.ResultatEncaisse);
                        if (player != null) player.CashOutCelebration();
                        SetTension(0f);
                        break;
                }
            }

            // Shop ouvert / fermé : clochette, et le personnage sort de la « tête basse ».
            if (game.ShopOpen != lastShopOpen)
            {
                if (game.ShopOpen)
                {
                    Play(Sfx.UiBoutiqueOuvre);
                    if (player != null) player.ReturnToRest(0.3f);
                }
                else Play(Sfx.UiBoutiqueFerme);
            }

            // Fin du verrou de transition à l'étage 1 (après « Jouer » ou « Rejouer ») :
            // petit saut d'accueil.
            if (lastLocked && !game.TransitionLocked && state == GameState.Choosing && game.Floor == 1)
                if (player != null) player.WelcomeHop();

            lastState = state;
            lastShopOpen = game.ShopOpen;
            lastLocked = game.TransitionLocked;
        }

        void OnClimbStarted()
        {
            Play(Sfx.UiEtageMonte);
            if (player != null) player.ClimbHop();
            if (cameraRig != null) cameraRig.PlayClimb();
            // La couche tension monte au niveau du nouvel étage pendant la transition.
            SetTension(MusicPlayer.TensionLevelFor(game.Floor + 1));
        }

        void OnDescentStarted()
        {
            Play(Sfx.UiNouvellePartie);
            if (cameraRig != null) cameraRig.PlayDescent();
            SetTension(0f);
        }

        // Dans le noir : le personnage est remis au repos (les coffres l'ont été par Begin).
        void OnFloorPrepared()
        {
            if (player != null) player.SnapToRest();
        }

        void SetTension(float level)
        {
            if (music != null) music.SetTension(level);
        }

        // ---------- ChestTrial ----------

        void OnHoverChanged(int index)
        {
            idleTime = 0f;
            nextInvite = 3f;
            if (index >= 0)
            {
                Play(Sfx.CoffreSurvol); // une seule fois, à l'entrée du survol
                if (player != null) player.LookAtX(chestTrial.GetChest(index).transform.position.x);
            }
            else if (player != null && game.State == GameState.Choosing)
            {
                player.LookAtX(null);
            }
        }

        void OnSuspenseStarted(int index, float duration)
        {
            Play(Sfx.UiClic);
            Play(Sfx.CoffreTremble);
            if (player != null) player.HoldBreath();
        }

        void OnPickedLidOpening(int index)
        {
            if (SoundManager.Instance != null) SoundManager.Instance.Stop(Sfx.CoffreTremble, 0.03f);
            Play(Sfx.CoffreOuvre);
        }

        // Les deux autres coffres : un seul son, plus doux et un peu plus aigu.
        void OnOthersLidsOpening() => Play(Sfx.CoffreOuvre, 0.5f, 1.1f);

        void OnChestRevealed(int index, bool trap, bool picked)
        {
            if (!picked) return; // on ne fête que le coffre choisi par le joueur
            Vector3 position = chestTrial.GetChest(index).transform.position;
            if (trap)
            {
                if (trapCloud != null) trapCloud.Play(position);
                if (cameraRig != null) cameraRig.Shake();
                Play(Sfx.ResultatPiege);
                if (player != null) player.TrapSquash();
            }
            else
            {
                if (coinBurst != null) coinBurst.Burst(position, CoinBurst.CountForFloor(game.Floor));
                Play(Sfx.ResultatTresor);
                Play(Sfx.PiecesJaillissement);
                if (player != null) player.JumpOfJoy();
            }
        }

        void OnLidsClosing() => Play(Sfx.CoffreFerme);

        // ---------- Invitation si le joueur hésite ----------

        void Update()
        {
            bool waiting = game.State == GameState.Choosing && chestTrial.AcceptsChoice && chestTrial.HoveredIndex < 0;
            if (!waiting)
            {
                idleTime = 0f;
                nextInvite = 3f;
                return;
            }
            idleTime += Time.deltaTime;
            if (idleTime >= nextInvite)
            {
                // Les trois coffres sautent, de gauche à droite, avec 0,1 s de décalage. Pas de son.
                for (int i = 0; i < chestTrial.ChestCount; i++)
                    chestTrial.GetChest(i).Hop(0.08f, 0.25f, 0.1f * i);
                nextInvite += 5f;
            }
        }
    }
}
