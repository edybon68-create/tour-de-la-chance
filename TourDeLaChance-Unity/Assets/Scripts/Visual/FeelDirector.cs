using System.Collections;
using UnityEngine;

namespace TourDeLaChance
{
    /// <summary>
    /// Le « chef d'orchestre de l'affichage » (spec §6.3) : il ÉCOUTE le GameManager et le
    /// ChestTrial, et déclenche les animations 3D et les sons au bon moment.
    /// Il ne change jamais l'étage, la cagnotte, le porte-monnaie ni le piège :
    /// on pourrait l'enlever, la partie marcherait quand même (sans spectacle).
    /// (Les animations de l'interface, elles, sont dans HudController et TitleController.)
    ///
    /// v3 : sons d'ouverture / fermeture des 4 panneaux, remélange de l'étage 1 (§12),
    /// arrivée des coffres en plus (« pop ») et des coffres précieux (petit saut + son),
    /// plus de pièces pour un coffre précieux (§5.3).
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
        [Tooltip("v5 §10.5 : les diamants 3D d'un trésor en diamants.")]
        [SerializeField] DiamondBurst diamondBurst;

        // Pour repérer les changements (avant / après) à chaque événement Changed.
        GameState lastState;
        PanelId lastPanel;
        bool lastLocked;
        bool reshuffling; // « Rejouer » à l'étage 1 en cours (les coffres font « pouf »)

        // Invitation si le joueur hésite (§3.2) : 3 s sans survol ni choix, puis toutes les 5 s.
        float idleTime;
        float nextInvite = 3f;

        void OnEnable()
        {
            game.Changed += OnGameChanged;
            game.ClimbStarted += OnClimbStarted;
            game.DescentStarted += OnDescentStarted;
            game.ReshuffleStarted += OnReshuffleStarted;
            game.FloorPrepared += OnFloorPrepared;
            game.FloorShown += OnFloorShown;
            game.ReviveStarted += OnReviveStarted;
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
            game.ReshuffleStarted -= OnReshuffleStarted;
            game.FloorPrepared -= OnFloorPrepared;
            game.FloorShown -= OnFloorShown;
            game.ReviveStarted -= OnReviveStarted;
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

            // Panneau ouvert / fermé / changé : clochette (les mêmes sons pour les 4 panneaux),
            // et le cheval sort de la « tête basse ». Passer de l'un à l'autre : ui_clic.
            if (game.PanelOpen != lastPanel)
            {
                if (lastPanel == PanelId.None)
                {
                    // v4 §14 : les Réglages (petite fenêtre) s'ouvrent avec un son plus léger.
                    Play(Sfx.UiBoutiqueOuvre, 1f, game.PanelOpen == PanelId.Reglages ? 1.12f : 0f);
                    if (player != null) player.ReturnToRest(0.3f);
                }
                else if (game.PanelOpen == PanelId.None) Play(Sfx.UiBoutiqueFerme);
                else Play(Sfx.UiClic);
            }

            // Fin du verrou de transition à l'étage 1 (après « Jouer » ou « Rejouer ») :
            // petit saut d'accueil.
            if (lastLocked && !game.TransitionLocked && state == GameState.Choosing && game.Floor == 1)
                if (player != null) player.WelcomeHop();

            lastState = state;
            lastPanel = game.PanelOpen;
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

        /// « Rejouer » à l'étage 1 (v3 §12) : les couvercles se referment, le cheval sort de la
        /// tête basse, puis les coffres rétrécissent (à 0,2 s, en 0,1 s). Ni caméra, ni voile.
        void OnReshuffleStarted()
        {
            reshuffling = true;
            Play(Sfx.CoffresRemelange);
            chestTrial.CloseAllLids(animated: true, silent: true);
            if (player != null) player.ReturnToRest(0.3f);
            StartCoroutine(ShrinkChestsLater(0.2f, 0.1f));
            SetTension(0f);
        }

        /// v5 §11.2 : reprise acceptée : son « rembobinage », les couvercles se referment, le
        /// cheval relève la tête, les coffres font « pouf » puis réapparaissent (remélange).
        /// (La spirale bleutée est dessinée par l'interface, ReviveOfferView.)
        void OnReviveStarted()
        {
            reshuffling = true;
            Play(Sfx.RepriseOk);
            chestTrial.CloseAllLids(animated: true, silent: true);
            if (player != null) player.ReturnToRest(0.3f);
            StartCoroutine(ShrinkChestsLater(0.3f, 0.12f));
            SetTension(MusicPlayer.TensionLevelFor(game.Floor));
        }

        IEnumerator ShrinkChestsLater(float delay, float duration)
        {
            yield return new WaitForSeconds(delay);
            for (int i = 0; i < chestTrial.ChestCount; i++) chestTrial.GetChest(i).ShrinkOut(duration);
        }

        // Le nouvel étage est prêt (dans le noir, ou au milieu du remélange).
        void OnFloorPrepared()
        {
            if (reshuffling)
            {
                // Les coffres réapparaissent TOUS ENSEMBLE : 0 → 1,08 → 1 (0,2 s) (v5 §5.2).
                for (int i = 0; i < chestTrial.ChestCount; i++) chestTrial.GetChest(i).PopIn(0f, 0.2f);
                return;
            }
            if (player != null) player.SnapToRest();
            // v5 §5.2 : plus de coffres en plus cachés pendant la montée (ils « poussaient »
            // après) : tous les coffres sont visibles dès le noir de la montée.
        }

        /// Fin de la montée (ou du remélange, de la redescente, de « Jouer ») : le nouvel étage
        /// se montre. v5 §5.2 : TOUS les coffres font en même temps la même petite arrivée
        /// (0,9 → 1,05 → 1 en 0,2 s) ; le son coffre_en_plus joue une fois s'il y a plus de 3
        /// coffres (sans retard visuel). Coffres précieux : chacun fait ensuite un petit saut
        /// (0,1 m, 0,25 s, 0,08 s de décalage) et le son coffres_precieux. Pur affichage.
        void OnFloorShown()
        {
            bool afterReshuffle = reshuffling;
            reshuffling = false;
            int count = chestTrial.ChestCount;
            if (!afterReshuffle)
                for (int i = 0; i < count; i++) chestTrial.GetChest(i).Arrive(0.2f);
            if (count > 3) Play(Sfx.CoffreEnPlus);
            int rank = chestTrial.RarityRank;
            if (rank >= 1)
            {
                float start = 0.2f; // après l'arrivée commune (ou le « pouf » du remélange)
                for (int i = 0; i < count; i++) chestTrial.GetChest(i).Hop(0.1f, 0.25f, start + 0.08f * i);
                float[] pitches = { 1f, 1.06f, 1.12f, 1.19f };
                Play(Sfx.CoffresPrecieux, 1f, pitches[Mathf.Clamp(rank - 1, 0, 3)]);
            }
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
                Play(Sfx.ResultatTresor);
                var setup = game.Setup;
                if (setup.DiamondTreasure && setup.DiamondAmount > 0)
                {
                    // v5 §10.5 : des diamants à la place des pièces (révélés APRÈS l'ouverture,
                    // comme les pièces). Le son dépend du palier d'étage (1-5, 6-9, 10+).
                    if (diamondBurst != null) diamondBurst.Burst(position, setup.DiamondAmount);
                    Play(game.Floor >= 10 ? Sfx.DiamantGain3 : game.Floor >= 6 ? Sfx.DiamantGain2 : Sfx.DiamantGain1);
                }
                else
                {
                    // v3 §5.3 : 4 pièces de plus par rang de rareté de coffre, au plus 40.
                    int coins = Mathf.Min(CoinBurst.MaxCoins, CoinBurst.CountForFloor(game.Floor) + 4 * chestTrial.RarityRank);
                    if (coinBurst != null) coinBurst.Burst(position, coins);
                    Play(Sfx.PiecesJaillissement);
                }
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
                // Les coffres présents sautent, de gauche à droite, avec 0,1 s de décalage. Pas de son.
                for (int i = 0; i < chestTrial.ChestCount; i++)
                    chestTrial.GetChest(i).Hop(0.08f, 0.25f, 0.1f * i);
                nextInvite += 5f;
            }
        }
    }
}
