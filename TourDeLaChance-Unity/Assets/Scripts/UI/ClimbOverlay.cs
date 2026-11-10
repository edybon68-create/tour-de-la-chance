using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

namespace TourDeLaChance
{
    /// <summary>
    /// Le voile sombre et la bannière « Étage N » des transitions d'étage (spec §3.11).
    /// Pur affichage : il suit les événements ClimbStarted / DescentStarted du GameManager
    /// et ses durées (GameManager.ClimbDarkTime…), pour rester synchronisé avec la caméra.
    /// Il utilise les éléments "veil" et "banner" de Hud.uxml (même UIDocument que le HUD).
    ///
    /// v3 §11 : 2e ligne = nom du palier (étages 4, 7, 10), sinon texte spécial de l'étage,
    /// sinon une phrase de montée au hasard (TextPicker). 3e ligne « ★ Nouveau record ! ★ »
    /// quand le GameManager l'annonce. Le remélange de l'étage 1 n'a ni voile ni bannière.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class ClimbOverlay : MonoBehaviour
    {
        [SerializeField] GameManager game;
        [SerializeField] AltitudeDecor decor; // pour le nom du palier (2e ligne de la bannière)
        [SerializeField] GameTexts texts;     // les phrases (Assets/Data/Textes.asset)

        // Moments de la montée, d'après le tableau §3.11.
        const float VeilStart = 0.10f;
        const float ReappearTime = GameManager.ClimbDarkTime + 0.05f;

        VisualElement veil, banner;
        Label bannerFloor, bannerTier, bannerRecord;
        Coroutine veilRoutine, bannerRoutine;

        void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            veil = root.Q<VisualElement>("veil");
            banner = root.Q<VisualElement>("banner");
            bannerFloor = root.Q<Label>("banner-floor");
            bannerTier = root.Q<Label>("banner-tier");
            bannerRecord = root.Q<Label>("banner-record");
            TextPicker.Use(texts);
            veil.style.opacity = 0f;
            banner.style.opacity = 0f;
            game.ClimbStarted += OnClimbStarted;
            game.DescentStarted += OnDescentStarted;
        }

        void OnDisable()
        {
            game.ClimbStarted -= OnClimbStarted;
            game.DescentStarted -= OnDescentStarted;
        }

        void OnClimbStarted()
        {
            Restart(ref veilRoutine, ClimbVeil());
            Restart(ref bannerRoutine, Banner());
        }

        void OnDescentStarted() => Restart(ref veilRoutine, DescentVeil());

        /// Pour les tests : la dernière 2e ligne affichée par la bannière.
        public string LastBannerLine { get; private set; } = "";

        void Restart(ref Coroutine slot, IEnumerator routine)
        {
            if (slot != null) StopCoroutine(slot);
            slot = StartCoroutine(routine);
        }

        /// Voile de la montée : 0 → 1 (0,10 → 0,50 s, ease-in), noir jusqu'à 0,55 s,
        /// puis 1 → 0 jusqu'à la fin (ease-out).
        IEnumerator ClimbVeil()
        {
            float end = GameManager.ClimbDuration;
            for (float t = 0f; t < end; t += Time.deltaTime)
            {
                float o;
                if (t < VeilStart) o = 0f;
                else if (t < GameManager.ClimbDarkTime) o = Ease.In((t - VeilStart) / (GameManager.ClimbDarkTime - VeilStart));
                else if (t < ReappearTime) o = 1f;
                else o = 1f - Ease.Out((t - ReappearTime) / (end - ReappearTime));
                veil.style.opacity = o;
                yield return null;
            }
            veil.style.opacity = 0f;
        }

        /// Voile de « Rejouer » : 0 → 1 en 0,25 s (ease-in), puis 1 → 0 en 0,3 s (ease-out).
        IEnumerator DescentVeil()
        {
            float dark = GameManager.DescentDarkTime, end = GameManager.DescentDuration;
            for (float t = 0f; t < end; t += Time.deltaTime)
            {
                veil.style.opacity = t < dark ? Ease.In(t / dark) : 1f - Ease.Out((t - dark) / (end - dark));
                yield return null;
            }
            veil.style.opacity = 0f;
        }

        /// Bannière : apparaît quand la caméra ressort du noir (0,55 s), échelle 0,5 → 1,1 → 1
        /// (0,25 s), reste 0,55 s, s'efface en 0,3 s. 2e ligne si on entre dans un nouveau palier.
        IEnumerator Banner()
        {
            yield return new WaitForSeconds(ReappearTime);
            int floor = game.Floor; // l'étage a déjà changé (pendant le noir)
            bool newTier = GameManager.TierForFloor(floor) != GameManager.TierForFloor(floor - 1);
            bannerFloor.text = $"Étage {floor}";
            string tierName = newTier && decor != null ? decor.TierName(GameManager.TierForFloor(floor)) : "";
            bannerTier.text = TextPicker.ClimbLine(floor, tierName, game.Pot, game.LastGain);
            bannerTier.style.display = string.IsNullOrEmpty(bannerTier.text) ? DisplayStyle.None : DisplayStyle.Flex;
            bool record = game.NewRecordThisFloor;
            bannerRecord.text = record && TextPicker.Texts != null ? TextPicker.Texts.recordLine : "";
            bannerRecord.style.display = record ? DisplayStyle.Flex : DisplayStyle.None;
            if (record && SoundManager.Instance != null) SoundManager.Instance.Play(Sfx.Record);
            LastBannerLine = bannerTier.text;

            for (float t = 0f; t < 1.1f; t += Time.deltaTime)
            {
                float s = t < 0.25f ? Ease.Pop(t / 0.25f, 0.5f, 1.1f) : 1f;
                float o = t < 0.1f ? t / 0.1f : (t > 0.8f ? 1f - (t - 0.8f) / 0.3f : 1f);
                banner.style.scale = new Scale(new Vector3(s, s, 1f));
                banner.style.opacity = o;
                yield return null;
            }
            banner.style.opacity = 0f;
        }
    }
}
