using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace TourDeLaChance
{
    /// <summary>
    /// L'interface (HUD) : affiche l'étage, la cagnotte, le porte-monnaie, un message,
    /// et les boutons Encaisser X / Rejouer. Elle ne décide de rien :
    /// elle montre l'état du GameManager et lui transmet les clics.
    /// (v3 : le bouton « Boutique » a disparu, remplacé par la colonne de boutons des
    /// panneaux ; les messages de piège et d'encaissement sont tirés au hasard, TextPicker.)
    ///
    /// Elle s'occupe aussi des animations de l'interface (spec sons-animations §3.8 à §3.10) :
    /// compteurs qui défilent, texte flottant « +20 », pièces qui filent dans le
    /// porte-monnaie, apparition des boutons, battement de la cagnotte.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class HudController : MonoBehaviour
    {
        [SerializeField] GameManager game;
        [SerializeField] ChestTrial chestTrial;
        [SerializeField] GameTexts texts; // les phrases (Assets/Data/Textes.asset)

        VisualElement hudRoot, fxLayer, potCounter, walletCounter, potCoin, walletCoin;
        Label potLabel, walletLabel, messageLabel, messageSub, cashOutText;
        Button cashOutButton, replayButton;
        VisualElement messageBox;

        // Les compteurs affichés, qui courent après les vraies valeurs.
        readonly RollingCounter pot = new RollingCounter();
        readonly RollingCounter wallet = new RollingCounter();

        // Ce qu'on a vu au dernier rafraîchissement (pour repérer les changements).
        GameState lastState = GameState.Title;
        bool lastPanelOpen;
        string endLine1 = "", endLine2 = ""; // message de fin de partie, tiré UNE fois par résultat

        // Effets en cours sur les compteurs
        float potPulseTime = -1f;     // grossissement après un gain
        float potTicTimer;            // un pieces_tic toutes les 0,06 s pendant le gain
        bool potLossEffect;           // rouge + tremblement pendant la perte
        float walletPulseTime = -1f;  // grossissement à chaque pièce volante
        float heartbeatTime;          // battement de la cagnotte pendant le choix

        // Boutons : qui est affiché, et les animations d'apparition en cours.
        readonly Dictionary<VisualElement, Coroutine> appearing = new Dictionary<VisualElement, Coroutine>();
        readonly HashSet<VisualElement> shown = new HashSet<VisualElement>();
        bool cashOutReady; // l'animation d'apparition d'« Encaisser » est finie
        Coroutine messageFade;

        static readonly Color PotColor = new Color(1f, 0.835f, 0.29f);    // #FFD54A
        static readonly Color LossColor = new Color(0.839f, 0.271f, 0.271f); // #D64545

        void OnEnable()
        {
            // On retrouve les éléments de Hud.uxml par leur "name".
            var root = GetComponent<UIDocument>().rootVisualElement;
            hudRoot = root.Q<VisualElement>("root");
            fxLayer = root.Q<VisualElement>("fx-layer");
            potCounter = root.Q<VisualElement>("pot-counter");
            walletCounter = root.Q<VisualElement>("wallet-counter");
            potCoin = root.Q<VisualElement>("pot-coin");
            walletCoin = root.Q<VisualElement>("wallet-coin");
            potLabel = root.Q<Label>("pot");
            walletLabel = root.Q<Label>("wallet");
            messageLabel = root.Q<Label>("message");
            messageSub = root.Q<Label>("message-sub");
            messageBox = root.Q<VisualElement>("message-box");
            cashOutText = root.Q<Label>("cashout-text");
            cashOutButton = root.Q<Button>("cashout");
            replayButton = root.Q<Button>("replay");
            UiSounds.Register(hudRoot);
            ButtonStyler.Register(hudRoot);
            TextPicker.Use(texts);

            cashOutButton.clicked += game.CashOut;
            replayButton.clicked += game.NewRun;
            game.Changed += Refresh;
            game.ClimbStarted += OnClimbStarted;
            game.ReshuffleStarted += OnClimbStarted; // le message s'efface aussi (0,1 s)
            Shop.Changed += Refresh;     // après un achat, le porte-monnaie change
            Upgrades.Changed += Refresh; // idem après une amélioration

            // Au départ, tout est caché ; les boutons apparaissent avec leur animation.
            foreach (var b in new VisualElement[] { cashOutButton, replayButton })
                b.style.display = DisplayStyle.None;
            pot.Snap(game.Pot);
            wallet.Snap(Wallet.Coins);
            Refresh();
        }

        void OnDisable()
        {
            cashOutButton.clicked -= game.CashOut;
            replayButton.clicked -= game.NewRun;
            game.Changed -= Refresh;
            game.ClimbStarted -= OnClimbStarted;
            game.ReshuffleStarted -= OnClimbStarted;
            Shop.Changed -= Refresh;
            Upgrades.Changed -= Refresh;
            UiSounds.Unregister(hudRoot);
        }

        // ---------- Rafraîchissement (à chaque changement du GameManager ou du Shop) ----------

        void Refresh()
        {
            // Écran titre : le HUD entier est caché (l'écran titre est affiché par-dessus).
            bool title = game.State == GameState.Title;
            hudRoot.style.display = title ? DisplayStyle.None : DisplayStyle.Flex;
            GameState state = game.State;
            bool stateChanged = state != lastState;
            if (title)
            {
                pot.Snap(game.Pot);
                wallet.Snap(Wallet.Coins);
                lastState = state;
                return;
            }

            // v4 : le numéro d'étage est dans le badge (FloorBadgeView).
            UpdateCounters(state, stateChanged);
            UpdateButtons(state, stateChanged);
            UpdateMessage(state, stateChanged);

            lastState = state;
            lastPanelOpen = game.IsPanelOpen;
        }

        /// Les compteurs : on lance le bon défilement selon l'événement (tableau §3.8).
        void UpdateCounters(GameState state, bool stateChanged)
        {
            int realPot = game.Pot, realWallet = Wallet.Coins;

            if (stateChanged && state == GameState.Climbing && realPot != pot.Target)
            {
                // Gain : 0,5 s ease-out, le texte grossit, des petits « tic ».
                pot.RollTo(realPot, 0.5f, Ease.Out);
                potPulseTime = 0f;
                potTicTimer = 0f;
                ShowFloatingGain(game.LastGain);
            }
            else if (stateChanged && state == GameState.Lost && realPot != pot.Target)
            {
                // Perte : 0,6 s ease-in, en rouge avec un tremblement.
                pot.RollTo(realPot, 0.6f, Ease.In);
                potLossEffect = true;
            }
            else if (stateChanged && state == GameState.CashedOut)
            {
                // Encaisser : les deux compteurs en même temps (1 s) + pièces volantes.
                int amount = game.LastGain;
                pot.RollTo(realPot, 1f, Ease.InOut);
                wallet.RollTo(realWallet, 1f, Ease.InOut);
                StartCoroutine(FlyingCoins(Mathf.Clamp(amount / 10, 3, 15)));
            }
            else
            {
                // Autre changement (achat au shop…) : le porte-monnaie défile en 0,4 s.
                if (realWallet != wallet.Target) wallet.RollTo(realWallet, 0.4f, Ease.Out);
                // Nouvelle partie (la cagnotte vaut déjà 0) ou outil de test : directement.
                if (realPot != pot.Target && !pot.Rolling) pot.Snap(realPot);
            }
            DrawCounters();
        }

        void DrawCounters()
        {
            potLabel.text = $"Cagnotte : {pot.Displayed}";
            walletLabel.text = $"Porte-monnaie : {wallet.Displayed}";
        }

        /// Les boutons : chacun apparaît avec son animation quand il devient utile (§3.10).
        void UpdateButtons(GameState state, bool stateChanged)
        {
            bool shop = game.IsPanelOpen;
            bool locked = game.TransitionLocked;
            bool over = state == GameState.Lost || state == GameState.CashedOut;

            // « Encaisser X » : pendant le choix, à partir de l'étage 2, panneau fermé.
            bool showCashOut = !shop && state == GameState.Choosing && game.Floor >= 2;
            cashOutText.text = $"Encaisser {game.Pot}";
            if (showCashOut && !shown.Contains(cashOutButton))
            {
                cashOutReady = false;
                ShowAnimated(cashOutButton, 0f, locked ? 0.6f : 1f, () => cashOutReady = true);
            }
            else if (!showCashOut && shown.Contains(cashOutButton))
            {
                // Dès qu'un coffre est choisi, il disparaît vite (0,1 s).
                HideAnimated(cashOutButton, 0.1f);
            }

            // Fin de partie : « Rejouer », 0,3 s après le résultat. Caché pendant la
            // redescente / le remélange et quand un panneau est ouvert.
            bool showReplay = over && !shop && !locked && !game.BoxOpeningActive;
            float delay = over && (stateChanged || lastPanelOpen != shop) ? 0.3f : 0f;

            if (showReplay && !shown.Contains(replayButton)) ShowAnimated(replayButton, delay, 1f, null);
            else if (!showReplay && shown.Contains(replayButton)) HideNow(replayButton);
        }

        void UpdateMessage(GameState state, bool stateChanged)
        {
            bool shop = game.IsPanelOpen;
            bool over = state == GameState.Lost || state == GameState.CashedOut;

            // Fin de partie : la phrase est tirée UNE fois, au moment du résultat (v3 §11).
            if (stateChanged && state == GameState.Lost)
            {
                endLine1 = TextPicker.TrapLine(game.Floor, game.LastLoss);
                endLine2 = TextPicker.LossLine(game.LastLoss);
            }
            else if (stateChanged && state == GameState.CashedOut)
            {
                endLine1 = TextPicker.CashOutLines(game.LastGain, out endLine2);
            }

            messageLabel.text = state switch
            {
                GameState.Choosing => game.Floor >= 2
                    ? $"Choisis un coffre pour monter… ou encaisse tes {game.Pot} pièces !"
                    : "Choisis un coffre ! Un seul est piégé…",
                GameState.Revealing => "…",
                GameState.Climbing => game.LastDiamonds > 0
                    ? $"+{game.LastDiamonds} ◆ ! On monte…"            // v5 §10.5 : trésor en diamants
                    : $"+{game.LastGain} pièces ! On monte…",
                GameState.Lost => endLine1,
                GameState.CashedOut => endLine1,
                _ => ""
            };
            messageSub.text = over ? endLine2 : "";
            messageSub.style.display = over && endLine2.Length > 0 ? DisplayStyle.Flex : DisplayStyle.None;

            // Panneau ouvert, ou redescente / remélange en cours : pas de message.
            // v5 §11 : pendant la reprise proposée, c'est sa fenêtre qui parle.
            bool visible = !shop && !(over && game.TransitionLocked) && state != GameState.ReviveOffer && !game.RebirthPlaying && !game.BoxOpeningActive;
            messageBox.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            if (!visible) return;

            if (stateChanged || lastPanelOpen != shop)
            {
                // Fin de partie : le message apparaît avec le premier bouton (0,3 s plus tard).
                // Début d'étage : il apparaît avec « Encaisser » (fin de la montée).
                FadeMessage(0f, 1f, 0.2f, over ? 0.3f : 0f);
            }
        }

        void OnClimbStarted() => FadeMessage(messageBox.resolvedStyle.opacity, 0f, 0.1f, 0f);

        // ---------- Animations de l'interface ----------

        void Update()
        {
            float dt = Time.deltaTime;
            bool potWasRolling = pot.Rolling;
            pot.Tick(dt);
            wallet.Tick(dt);
            DrawCounters();

            // Gain : un pieces_tic toutes les 0,06 s pendant le défilement.
            if (potWasRolling && potPulseTime >= 0f)
            {
                potTicTimer -= dt;
                if (potTicTimer <= 0f)
                {
                    potTicTimer = 0.06f; // au plus un « tic » par image, même si une image est lente
                    if (SoundManager.Instance != null) SoundManager.Instance.Play(Sfx.PiecesTic);
                }
            }

            // Échelle de la cagnotte : grossissement après un gain, ou battement pendant le choix.
            float potScale = 1f;
            if (potPulseTime >= 0f)
            {
                potPulseTime += dt;
                potScale = 1f + 0.25f * Ease.Sine(potPulseTime / 0.25f);
                if (potPulseTime >= 0.25f && !pot.Rolling) potPulseTime = -1f;
            }
            else if (game.State == GameState.Choosing && game.Pot > 0 && !game.IsPanelOpen)
            {
                // Battement : période 1,2 s à l'étage 2, 0,1 s plus rapide par étage, minimum 0,7 s.
                float period = Mathf.Max(0.7f, 1.2f - 0.1f * (game.Floor - 2));
                heartbeatTime += dt;
                potScale = 1f + 0.03f * (1f - Mathf.Cos(2f * Mathf.PI * heartbeatTime / period));
            }
            else heartbeatTime = 0f;
            potLabel.style.scale = new Scale(new Vector3(potScale, potScale, 1f));

            // Perte : rouge + tremblement horizontal ±4 px à 16 Hz pendant le défilement.
            if (potLossEffect && pot.Rolling)
            {
                potLabel.style.color = LossColor;
                float x = 4f * Mathf.Sin(2f * Mathf.PI * 16f * Time.time);
                potLabel.style.translate = new Translate(x, 0f);
            }
            else if (potLossEffect)
            {
                potLossEffect = false;
                potLabel.style.color = PotColor;
                potLabel.style.translate = new Translate(0f, 0f);
            }

            // Porte-monnaie : grossit 1 → 1,2 → 1 à chaque pièce qui arrive.
            float walletScale = 1f;
            if (walletPulseTime >= 0f)
            {
                walletPulseTime += dt;
                walletScale = 1f + 0.2f * Ease.Sine(walletPulseTime / 0.15f);
                if (walletPulseTime >= 0.15f) walletPulseTime = -1f;
            }
            walletLabel.style.scale = new Scale(new Vector3(walletScale, walletScale, 1f));

            // « Encaisser » : à opacité 0,6 pendant le verrou anti-clic, puis il « s'allume »
            // (→ 1 en 0,1 s) et devient cliquable.
            if (cashOutReady && shown.Contains(cashOutButton))
            {
                bool locked = game.TransitionLocked;
                float target = locked ? 0.6f : 1f;
                float current = cashOutButton.resolvedStyle.opacity;
                cashOutButton.style.opacity = Mathf.MoveTowards(current, target, dt * 0.4f / 0.1f);
                cashOutButton.pickingMode = locked ? PickingMode.Ignore : PickingMode.Position;
            }
        }

        /// Fait apparaître un bouton : opacité 0 → 1 et échelle 0,6 → 1,08 → 1 en 0,25 s.
        /// Il n'est cliquable qu'à la fin de son animation.
        void ShowAnimated(VisualElement element, float delay, float finalOpacity, System.Action onDone)
        {
            StopAnimation(element);
            shown.Add(element);
            appearing[element] = StartCoroutine(AppearRoutine(element, delay, finalOpacity, onDone));
        }

        IEnumerator AppearRoutine(VisualElement element, float delay, float finalOpacity, System.Action onDone)
        {
            element.style.display = DisplayStyle.Flex;
            element.pickingMode = PickingMode.Ignore;
            element.style.opacity = 0f;
            element.style.scale = new Scale(new Vector3(0.6f, 0.6f, 1f));
            if (delay > 0f) yield return new WaitForSeconds(delay);

            for (float t = 0f; t < 0.25f; t += Time.deltaTime)
            {
                float k = t / 0.25f;
                float s = Ease.Pop(k, 0.6f, 1.08f);
                element.style.opacity = Mathf.Lerp(0f, finalOpacity, Mathf.Clamp01(k / 0.7f));
                element.style.scale = new Scale(new Vector3(s, s, 1f));
                yield return null;
            }
            element.style.opacity = finalOpacity;
            // On efface l'échelle « en dur » : le survol du USS (:hover) reprend la main.
            element.style.scale = StyleKeyword.Null;
            element.pickingMode = PickingMode.Position;
            appearing.Remove(element);
            onDone?.Invoke();
        }

        void HideAnimated(VisualElement element, float duration)
        {
            StopAnimation(element);
            shown.Remove(element);
            element.pickingMode = PickingMode.Ignore; // plus cliquable dès maintenant
            appearing[element] = StartCoroutine(HideRoutine(element, duration));
        }

        IEnumerator HideRoutine(VisualElement element, float duration)
        {
            float start = element.resolvedStyle.opacity;
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                element.style.opacity = Mathf.Lerp(start, 0f, t / duration);
                yield return null;
            }
            element.style.display = DisplayStyle.None;
            appearing.Remove(element);
        }

        void HideNow(VisualElement element)
        {
            StopAnimation(element);
            shown.Remove(element);
            element.style.display = DisplayStyle.None;
        }

        void StopAnimation(VisualElement element)
        {
            if (appearing.TryGetValue(element, out var c) && c != null) StopCoroutine(c);
            appearing.Remove(element);
        }

        void FadeMessage(float from, float to, float duration, float delay)
        {
            if (messageFade != null) StopCoroutine(messageFade);
            messageFade = StartCoroutine(FadeRoutine(messageBox, from, to, duration, delay));
        }

        static IEnumerator FadeRoutine(VisualElement element, float from, float to, float duration, float delay)
        {
            element.style.opacity = from;
            if (delay > 0f) yield return new WaitForSeconds(delay);
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                element.style.opacity = Mathf.Lerp(from, to, t / duration);
                yield return null;
            }
            element.style.opacity = to;
        }

        /// Le texte « +20 » au-dessus du coffre choisi (§3.8) : il grossit, monte de 60 px
        /// en 0,9 s et s'efface pendant les 0,3 dernières secondes.
        void ShowFloatingGain(int gain)
        {
            if (chestTrial == null || chestTrial.LastPicked < 0 || Camera.main == null) return;
            Vector3 world = chestTrial.GetChest(chestTrial.LastPicked).transform.position + new Vector3(0f, 1.6f, 0f);
            // Position 3D → position dans le panneau d'interface → position dans le calque.
            Vector2 panelPos = RuntimePanelUtils.CameraTransformWorldToPanel(fxLayer.panel, world, Camera.main);
            Vector2 local = fxLayer.WorldToLocal(panelPos);

            var label = new Label($"+{gain}");
            label.AddToClassList("float-text");
            // v3 §5.3.2 : le texte grossit avec la rareté du coffre (48 / 54 / 60 / 66 / 72).
            label.style.fontSize = 48 + 6 * game.Setup.RarityRank;
            label.pickingMode = PickingMode.Ignore;
            label.style.left = local.x;
            label.style.top = local.y;
            fxLayer.Add(label);
            StartCoroutine(FloatRoutine(label));
        }

        static IEnumerator FloatRoutine(Label label)
        {
            const float duration = 0.9f;
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                float s = t < 0.2f ? Ease.Pop(t / 0.2f, 0.5f, 1.2f) : 1f;
                label.style.scale = new Scale(new Vector3(s, s, 1f));
                label.style.translate = new Translate(0f, -60f * Ease.Out(t / duration));
                label.style.opacity = t < duration - 0.3f ? 1f : (duration - t) / 0.3f;
                yield return null;
            }
            label.RemoveFromHierarchy();
        }

        /// Encaisser (§3.9) : des pièces filent de la cagnotte au porte-monnaie, une toutes
        /// les 0,05 s, chacune en 0,5 s sur une courbe vers la gauche.
        IEnumerator FlyingCoins(int count)
        {
            // On attend une image pour que les positions de l'interface soient calculées.
            yield return null;
            Vector2 from = fxLayer.WorldToLocal(potCoin.worldBound.center);
            Vector2 to = fxLayer.WorldToLocal(walletCoin.worldBound.center);
            for (int i = 0; i < count; i++)
            {
                StartCoroutine(OneFlyingCoin(from, to));
                yield return new WaitForSeconds(0.05f);
            }
        }

        IEnumerator OneFlyingCoin(Vector2 from, Vector2 to)
        {
            var coin = new VisualElement();
            coin.AddToClassList("flying-coin");
            coin.pickingMode = PickingMode.Ignore;
            fxLayer.Add(coin);
            // Courbe de Bézier : un point de contrôle 80 px à gauche du milieu du trajet.
            Vector2 control = (from + to) * 0.5f + new Vector2(-80f, 0f);
            for (float t = 0f; t < 0.5f; t += Time.deltaTime)
            {
                float k = Ease.InOut(t / 0.5f);
                Vector2 p = (1 - k) * (1 - k) * from + 2 * (1 - k) * k * control + k * k * to;
                coin.style.left = p.x;
                coin.style.top = p.y;
                yield return null;
            }
            coin.RemoveFromHierarchy();
            // Arrivée : un « tic » (hauteur ±5 %) et le porte-monnaie grossit.
            if (SoundManager.Instance != null) SoundManager.Instance.Play(Sfx.PiecesTic, 1f, 1f, 0.05f);
            walletPulseTime = 0f;
        }
    }
}
