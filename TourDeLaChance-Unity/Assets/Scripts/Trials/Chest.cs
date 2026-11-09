using System;
using System.Collections;
using UnityEngine;

namespace TourDeLaChance
{
    /// <summary>
    /// Un coffre : il sait se refermer, s'animer (survol, suspense, ouverture avec rebond)
    /// et changer de couleur selon son contenu : or = trésor, rouge = piège.
    ///
    /// C'est de l'AFFICHAGE : le coffre ne sait pas lui-même s'il est piégé, c'est
    /// ChestTrial qui le lui dit au moment de la révélation.
    ///
    /// Les animations bougent le modèle 3D (l'enfant "coffre"), pas l'objet racine :
    /// ainsi le collider qui reçoit le clic reste immobile.
    /// Règle : on mémorise la pose de repos au démarrage et on y revient exactement.
    /// </summary>
    public class Chest : MonoBehaviour
    {
        [SerializeField] Transform lid;     // le couvercle (son pivot est à l'arrière, comme une charnière)
        [SerializeField] Renderer body;     // le corps du coffre, qui change de couleur
        [SerializeField] Transform model;   // le modèle 3D animé (enfant "coffre")
        [SerializeField] Color closedColor = new Color(0.55f, 0.35f, 0.2f);
        [SerializeField] Color treasureColor = new Color(1f, 0.784f, 0.227f);  // #FFC83A
        [SerializeField] Color trapColor = new Color(0.851f, 0.102f, 0.102f);  // #D91A1A

        public int Index { get; set; }

        /// Le couvercle est-il (au moins un peu) ouvert ? (hors petit entrouvert du survol)
        public bool IsLidOpen => !idle && lidAngle > 20f;

        // "_BaseColor" est le nom de la couleur dans le shader URP/Lit.
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        MaterialPropertyBlock block;

        // Pose de repos du modèle, mémorisée une fois pour toutes.
        Vector3 restPosition, restScale;
        Quaternion restRotation;

        // "idle" = le coffre est au repos ou survolé : c'est Update qui le pose.
        // Sinon, une coroutine (suspense, ouverture…) a la main sur le modèle.
        bool idle = true;
        float lidAngle;

        // Survol
        bool hovered;
        float hoverProgress; // de 0 (repos) à 1 (survol complet), avance en 0,12 s
        float hoverTime;     // depuis combien de temps on survole

        // Petit saut d'invitation (joueur qui hésite)
        float hopStart = -10f, hopHeight, hopDuration = 1f;

        void Awake()
        {
            block = new MaterialPropertyBlock();
            if (model == null) model = transform; // secours si le champ n'est pas rempli
            restPosition = model.localPosition;
            restRotation = model.localRotation;
            restScale = model.localScale;
        }

        // ---------- Repos et survol (pilotés image par image dans Update) ----------

        void Update()
        {
            if (!idle) return;

            // La progression du survol avance (ou recule) à vitesse constante…
            hoverProgress = Mathf.MoveTowards(hoverProgress, hovered ? 1f : 0f, Time.deltaTime / 0.12f);
            // …et on l'adoucit avec une courbe ease-out dans les deux sens.
            float w = hovered ? Ease.Out(hoverProgress) : 1f - Ease.Out(1f - hoverProgress);
            if (hovered) hoverTime += Time.deltaTime;

            // Couvercle : entrouvert à 8°, puis il « respire » entre 5° et 11° (période 0,5 s).
            lidAngle = w * (8f + 3f * Mathf.Sin(2f * Mathf.PI * hoverTime / 0.5f));

            // Petits sauts : un toutes les 0,8 s pendant le survol (0,05 m, 0,2 s).
            float y = 0f;
            if (hovered)
            {
                float phase = hoverTime % 0.8f;
                if (phase < 0.2f) y += w * 0.05f * Mathf.Sin(phase / 0.2f * Mathf.PI);
            }
            // Saut d'invitation demandé par l'affichage (joueur qui hésite).
            float hopT = (Time.time - hopStart) / hopDuration;
            if (hopT >= 0f && hopT <= 1f) y += hopHeight * Mathf.Sin(hopT * Mathf.PI);

            ApplyPose(Vector3.one * (1f + 0.06f * w), 0f, y);
        }

        /// Le curseur entre (true) ou sort (false) du coffre. Appelé par ChestTrial.
        public void SetHovered(bool value)
        {
            if (value && !hovered) hoverTime = 0f;
            hovered = value;
        }

        /// Petit saut d'invitation (hauteur en m, durée en s, après un délai en s).
        public void Hop(float height, float duration, float delay)
        {
            hopHeight = height;
            hopDuration = duration;
            hopStart = Time.time + delay;
        }

        /// Remet le coffre fermé, couleur bois, exactement dans sa pose de repos.
        public void ResetChest()
        {
            StopAllCoroutines();
            idle = true;
            hovered = false;
            hoverProgress = 0f;
            hopStart = -10f;
            lidAngle = 0f;
            ApplyPose(Vector3.one, 0f, 0f);
            SetColor(closedColor);
        }

        // ---------- Les animations de l'épreuve (coroutines) ----------
        // Une coroutine est une fonction qui s'étale sur plusieurs images grâce à
        // "yield return null" = "attends l'image suivante".

        /// Suspense (§3.3) : tassement 0,08 s puis tremblement jusqu'à la fin de la durée.
        /// Strictement le même pour un trésor et pour un piège.
        public IEnumerator Suspense(float duration)
        {
            idle = false;
            Vector3 startScale = model.localScale;
            startScale = new Vector3(startScale.x / restScale.x, startScale.y / restScale.y, startScale.z / restScale.z);
            float startLid = lidAngle;
            Vector3 squashed = new Vector3(1.04f, 0.92f, 1.04f);

            // Une seule boucle sur toute la durée : chaque image calcule la pose d'après le
            // temps écoulé. Ainsi la durée totale est exacte (à une image près), et
            // strictement la même pour un trésor et pour un piège.
            const float squashTime = 0.08f;
            float shakeTime = Mathf.Max(0.01f, duration - squashTime);
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                if (t < squashTime)
                {
                    // 1. Tassement : il « inspire ».
                    float k = t / squashTime;
                    lidAngle = Mathf.Lerp(startLid, 0f, k);
                    ApplyPose(Vector3.Lerp(startScale, squashed, k), 0f, 0f);
                }
                else
                {
                    // 2. Tremblement : 16 allers-retours par seconde, amplitude de 1° à 5°,
                    //    couvercle qui claque de 0° à 6° à 12 Hz, échelle qui revient à 1.
                    float s = t - squashTime;
                    float amplitude = Mathf.Lerp(1f, 5f, s / shakeTime);
                    float rotZ = amplitude * Mathf.Sin(2f * Mathf.PI * 16f * s);
                    lidAngle = 3f - 3f * Mathf.Cos(2f * Mathf.PI * 12f * s);
                    ApplyPose(Vector3.Lerp(squashed, Vector3.one, s / 0.1f), rotZ, 0f);
                }
                yield return null;
            }
        }

        /// Les coffres non choisis s'effacent : échelle 1 → 0,95 en 0,15 s.
        public void StepBack()
        {
            StopAllCoroutines();
            StartCoroutine(StepBackRoutine());
        }

        IEnumerator StepBackRoutine()
        {
            idle = false;
            float start = model.localScale.x / restScale.x;
            float startLid = lidAngle;
            for (float t = 0f; t < 0.15f; t += Time.deltaTime)
            {
                float k = Ease.Out(t / 0.15f);
                lidAngle = Mathf.Lerp(startLid, 0f, k);
                ApplyPose(Vector3.one * Mathf.Lerp(start, 0.95f, k), 0f, 0f);
                yield return null;
            }
            lidAngle = 0f;
            ApplyPose(Vector3.one * 0.95f, 0f, 0f);
        }

        /// Ouverture du coffre choisi (§3.4) : 0° → 120° en 0,22 s, révélation (couleur
        /// + pop), puis rebond 120° → 104° → 110°.
        public IEnumerator OpenPicked(bool isTrap, Action onRevealed)
        {
            idle = false;
            for (float t = 0f; t < 0.22f; t += Time.deltaTime)
            {
                lidAngle = Mathf.Lerp(0f, 120f, Ease.Out(t / 0.22f));
                ApplyPose(Vector3.one, 0f, 0f);
                yield return null;
            }
            lidAngle = 120f;
            SetColor(isTrap ? trapColor : treasureColor);
            onRevealed?.Invoke();

            // Rebond du couvercle (0,25 s) et « pop » du coffre (0,2 s) en même temps.
            for (float t = 0f; t < 0.25f; t += Time.deltaTime)
            {
                lidAngle = t < 0.12f
                    ? Mathf.Lerp(120f, 104f, Ease.Out(t / 0.12f))
                    : Mathf.Lerp(104f, 110f, Ease.InOut((t - 0.12f) / 0.13f));
                float pop = t < 0.2f ? 1f + 0.15f * Mathf.Sin(t / 0.2f * Mathf.PI) : 1f;
                ApplyPose(Vector3.one * pop, 0f, 0f);
                yield return null;
            }
            lidAngle = 110f;
            ApplyPose(Vector3.one, 0f, 0f);
        }

        /// Ouverture d'un coffre non choisi, sans suspense : 0° → 115° (0,3 s), révélation,
        /// 115° → 110° (0,1 s), petit pop à 1,08. L'échelle revient de 0,95 à 1.
        public IEnumerator OpenOther(bool isTrap, Action onRevealed)
        {
            idle = false;
            float startScale = model.localScale.x / restScale.x;
            for (float t = 0f; t < 0.3f; t += Time.deltaTime)
            {
                float k = Ease.Out(t / 0.3f);
                lidAngle = Mathf.Lerp(0f, 115f, k);
                ApplyPose(Vector3.one * Mathf.Lerp(startScale, 1f, k), 0f, 0f);
                yield return null;
            }
            lidAngle = 115f;
            SetColor(isTrap ? trapColor : treasureColor);
            onRevealed?.Invoke();

            for (float t = 0f; t < 0.2f; t += Time.deltaTime)
            {
                lidAngle = Mathf.Lerp(115f, 110f, Mathf.Clamp01(t / 0.1f));
                ApplyPose(Vector3.one * (1f + 0.08f * Mathf.Sin(t / 0.2f * Mathf.PI)), 0f, 0f);
                yield return null;
            }
            lidAngle = 110f;
            ApplyPose(Vector3.one, 0f, 0f);
        }

        /// Referme le couvercle en douceur (110° → 0° en 0,2 s, ease-in), puis couleur bois.
        public void CloseAnimated()
        {
            StopAllCoroutines();
            StartCoroutine(CloseRoutine());
        }

        IEnumerator CloseRoutine()
        {
            idle = false;
            float start = lidAngle;
            for (float t = 0f; t < 0.2f; t += Time.deltaTime)
            {
                lidAngle = Mathf.Lerp(start, 0f, Ease.In(t / 0.2f));
                ApplyPose(Vector3.one, 0f, 0f);
                yield return null;
            }
            ResetChest();
        }

        // ---------- Outils ----------

        /// Pose le modèle : échelle (relative au repos), inclinaison Z, petit saut Y,
        /// et l'angle courant du couvercle.
        void ApplyPose(Vector3 scale, float rotZ, float y)
        {
            model.localPosition = restPosition + Vector3.up * y;
            model.localRotation = restRotation * Quaternion.Euler(0f, 0f, rotZ);
            model.localScale = Vector3.Scale(restScale, scale);
            lid.localRotation = Quaternion.Euler(lidAngle, 0f, 0f);
        }

        void SetColor(Color color)
        {
            // Un PropertyBlock change la couleur de CE coffre sans modifier
            // le matériau partagé par les autres coffres.
            body.GetPropertyBlock(block);
            block.SetColor(BaseColorId, color);
            body.SetPropertyBlock(block);
        }
    }
}
