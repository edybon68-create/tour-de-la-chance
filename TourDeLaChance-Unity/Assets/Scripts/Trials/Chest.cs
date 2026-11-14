using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TourDeLaChance
{
    /// <summary>
    /// Un coffre : il sait se refermer, s'animer (survol, suspense, ouverture avec rebond)
    /// et changer de couleur selon son contenu : or = trésor, rouge = piège.
    ///
    /// v3 : il a CINQ modèles (Bois, Bronze, Argent, Or, Diamant), tous de même taille, même
    /// hiérarchie et mêmes pivots. SetRarity() montre le bon et cache les autres. Le Corps
    /// reste UN seul matériau, repeint en or ou en rouge à la révélation, puis remis à la
    /// couleur de sa rareté au prochain étage.
    ///
    /// C'est de l'AFFICHAGE : le coffre ne sait pas lui-même s'il est piégé, c'est
    /// ChestTrial qui le lui dit au moment de la révélation.
    ///
    /// Les animations bougent le modèle 3D (l'enfant), pas l'objet racine : ainsi le collider
    /// qui reçoit le clic reste immobile. On mémorise la pose de repos et on y revient exactement.
    /// </summary>
    public class Chest : MonoBehaviour
    {
        /// Les pièces d'un des 5 modèles (une par rareté de coffre).
        [Serializable]
        public class Look
        {
            public Transform model;     // la racine du modèle (animée)
            public Transform lid;       // Charniere : le pivot du couvercle
            public Renderer body;       // Corps : la seule partie repeinte
            public Transform[] gems;    // Diamant : Gemme_0 (couvercle) puis Gemme_1..4 (coins)
        }

        [Tooltip("Les 5 modèles, dans l'ordre des raretés : Bois, Bronze, Argent, Or, Diamant.")]
        [SerializeField] Look[] looks = new Look[0];
        [SerializeField] Color closedColor = new Color(0.55f, 0.35f, 0.2f);
        [SerializeField] Color treasureColor = new Color(1f, 0.784f, 0.227f);  // #FFC83A
        [SerializeField] Color trapColor = new Color(0.851f, 0.102f, 0.102f);  // #D91A1A

        public int Index { get; set; }
        /// La rareté affichée (0 = Bois … 4 = Diamant).
        public int RarityRank => rank;

        /// Le couvercle est-il (au moins un peu) ouvert ? (hors petit entrouvert du survol)
        public bool IsLidOpen => !idle && lidAngle > 20f;

        // "_BaseColor" est le nom de la couleur dans le shader URP/Lit.
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        MaterialPropertyBlock block;

        // Le modèle visible en ce moment, son couvercle et son corps.
        Transform model, lid;
        Renderer body;
        int rank;

        // Pose de repos du modèle (la même pour les 5 modèles), mémorisée une fois pour toutes.
        Vector3 restPosition, restScale = Vector3.one;
        Quaternion restRotation = Quaternion.identity;
        Quaternion[] gemRest;
        bool initialized;

        // "idle" = le coffre est au repos ou survolé : c'est Update qui le pose.
        // Sinon, une coroutine (suspense, ouverture…) a la main sur le modèle.
        bool idle = true;
        float lidAngle;
        float extraScale = 1f; // « pouf » du remélange et apparition des coffres en plus

        // Survol
        bool hovered;
        float hoverProgress; // de 0 (repos) à 1 (survol complet), avance en 0,12 s
        float hoverTime;     // depuis combien de temps on survole

        // Petit saut d'invitation (joueur qui hésite) ou d'arrivée (coffres précieux)
        float hopStart = -10f, hopHeight, hopDuration = 1f;
        float gemTime;

        void Awake() => Init();

        /// Prépare le coffre (une seule fois). Appelé aussi par SetRarity/ResetChest : un coffre
        /// désactivé (4e ou 5e) n'a pas encore reçu Awake quand ChestTrial le prépare.
        void Init()
        {
            if (initialized) return;
            initialized = true;
            block = new MaterialPropertyBlock();
            if (looks.Length > 0 && looks[0].model != null)
            {
                restPosition = looks[0].model.localPosition;
                restRotation = looks[0].model.localRotation;
                restScale = looks[0].model.localScale;
            }
            ShowLook(0);
        }

        void ShowLook(int r)
        {
            rank = Mathf.Clamp(r, 0, Mathf.Max(0, looks.Length - 1));
            for (int i = 0; i < looks.Length; i++)
                if (looks[i].model != null) looks[i].model.gameObject.SetActive(i == rank);
            var look = looks.Length > 0 ? looks[rank] : null;
            model = look != null && look.model != null ? look.model : transform;
            lid = look?.lid;
            body = look?.body;
            gemRest = null;
            if (look?.gems != null && look.gems.Length > 0)
            {
                gemRest = new Quaternion[look.gems.Length];
                for (int i = 0; i < look.gems.Length; i++) if (look.gems[i] != null) gemRest[i] = look.gems[i].localRotation;
            }
        }

        /// Montre le modèle d'une rareté de coffre (et retient la couleur du Corps fermé).
        public void SetRarity(int rarityRank, Color? bodyColor)
        {
            Init();
            if (rarityRank != rank) ShowLook(rarityRank);
            if (bodyColor.HasValue) closedColor = bodyColor.Value;
        }

        // ---------- Repos et survol (pilotés image par image dans Update) ----------

        void Update()
        {
            AnimateGems();
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
            // Saut demandé par l'affichage (joueur qui hésite, arrivée d'un étage précieux).
            float hopT = (Time.time - hopStart) / hopDuration;
            if (hopT >= 0f && hopT <= 1f) y += hopHeight * Mathf.Sin(hopT * Mathf.PI);

            ApplyPose(Vector3.one * (1f + 0.06f * w), 0f, y);
        }

        /// Diamant : la grosse gemme du couvercle tourne sur elle-même, celles des coins
        /// « respirent » chacune à leur tour. Pur affichage, aucune émission.
        void AnimateGems()
        {
            var look = looks.Length > 0 ? looks[rank] : null;
            if (look?.gems == null || gemRest == null) return;
            gemTime += Time.deltaTime;
            for (int i = 0; i < look.gems.Length; i++)
            {
                var g = look.gems[i];
                if (g == null) continue;
                if (i == 0) g.localRotation = gemRest[0] * Quaternion.Euler(0f, 60f * gemTime, 0f);
                else
                {
                    float s = 1f + 0.08f * Mathf.Sin(2f * Mathf.PI * (gemTime / 1.6f + i * 0.25f));
                    g.localScale = Vector3.one * s;
                    g.localRotation = gemRest[i] * Quaternion.Euler(0f, 0f, 20f * Mathf.Sin(2f * Mathf.PI * gemTime / 2.4f + i));
                }
            }
        }

        /// Le curseur entre (true) ou sort (false) du coffre. Appelé par ChestTrial.
        public void SetHovered(bool value)
        {
            if (value && !hovered) hoverTime = 0f;
            hovered = value;
        }

        /// Petit saut (hauteur en m, durée en s, après un délai en s).
        public void Hop(float height, float duration, float delay)
        {
            hopHeight = height;
            hopDuration = duration;
            hopStart = Time.time + delay;
        }

        /// Remet le coffre fermé, couleur de sa rareté, exactement dans sa pose de repos.
        public void ResetChest()
        {
            Init();
            StopAllCoroutines();
            idle = true;
            hovered = false;
            hoverProgress = 0f;
            hopStart = -10f;
            lidAngle = 0f;
            extraScale = 1f;
            ApplyPose(Vector3.one, 0f, 0f);
            SetColor(closedColor);
        }

        // ---------- Les animations de l'épreuve (coroutines) ----------
        // Une coroutine est une fonction qui s'étale sur plusieurs images grâce à
        // "yield return null" = "attends l'image suivante".

        /// Suspense (§3.3) : tassement 0,08 s puis tremblement jusqu'à la fin de la durée.
        /// Strictement le même pour un trésor et pour un piège de la même rareté.
        /// maxAmplitude : tremblement final (5° + 1° par rang). hopAtEnd : le Diamant saute de
        /// 0,15 m pendant les 0,25 dernières secondes (DANS la durée : elle ne change pas).
        public IEnumerator Suspense(float duration, float maxAmplitude = 5f, bool hopAtEnd = false)
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
            const float hopTime = 0.25f;
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
                    // 2. Tremblement : 16 allers-retours par seconde, amplitude de 1° au maximum,
                    //    couvercle qui claque de 0° à 6° à 12 Hz, échelle qui revient à 1.
                    float s = t - squashTime;
                    float amplitude = Mathf.Lerp(1f, maxAmplitude, s / shakeTime);
                    float rotZ = amplitude * Mathf.Sin(2f * Mathf.PI * 16f * s);
                    lidAngle = 3f - 3f * Mathf.Cos(2f * Mathf.PI * 12f * s);
                    float y = 0f;
                    if (hopAtEnd && t > duration - hopTime) y = 0.15f * Mathf.Sin((t - (duration - hopTime)) / hopTime * Mathf.PI);
                    ApplyPose(Vector3.Lerp(squashed, Vector3.one, s / 0.1f), rotZ, y);
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
            SetColor(RevealColor(isTrap));
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
            SetColor(RevealColor(isTrap));
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

        /// Referme le couvercle en douceur (110° → 0° en 0,2 s, ease-in), puis couleur de repos.
        public void CloseAnimated()
        {
            StopAllCoroutines();
            StartCoroutine(CloseRoutine());
        }

        IEnumerator CloseRoutine()
        {
            idle = false;
            float start = lidAngle;
            float startScale = model.localScale.x / restScale.x / Mathf.Max(0.001f, extraScale);
            for (float t = 0f; t < 0.2f; t += Time.deltaTime)
            {
                lidAngle = Mathf.Lerp(start, 0f, Ease.In(t / 0.2f));
                ApplyPose(Vector3.one * Mathf.Lerp(startScale, 1f, t / 0.2f), 0f, 0f);
                yield return null;
            }
            ResetChest();
        }

        /// Remélange (spec v3 §12) : le coffre rétrécit jusqu'à 0 (0,1 s, ease-in).
        public void ShrinkOut(float duration)
        {
            StopAllCoroutines();
            StartCoroutine(ShrinkRoutine(duration));
        }

        IEnumerator ShrinkRoutine(float duration)
        {
            idle = false;
            float start = extraScale;
            float startLid = lidAngle;
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                float k = Ease.In(t / duration);
                extraScale = Mathf.Lerp(start, 0f, k);
                lidAngle = Mathf.Lerp(startLid, 0f, k);
                ApplyPose(Vector3.one, 0f, 0f);
                yield return null;
            }
            extraScale = 0f;
            ApplyPose(Vector3.one, 0f, 0f);
        }

        /// Le coffre « pousse » : échelle 0 → 1,08 → 1 (durée donnée), après un délai.
        /// v5 : ne sert plus qu'au remélange (« Rejouer » à l'étage 1, reprise) : les coffres
        /// en plus ne « poussent » plus après la montée (bug v5 §5).
        public void PopIn(float delay, float duration)
        {
            StopAllCoroutines();
            StartCoroutine(PopRoutine(delay, duration));
        }

        IEnumerator PopRoutine(float delay, float duration)
        {
            idle = false;
            extraScale = 0f;
            ApplyPose(Vector3.one, 0f, 0f);
            if (delay > 0f) yield return new WaitForSeconds(delay);
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                extraScale = Ease.Pop(t / duration, 0f, 1.08f);
                ApplyPose(Vector3.one, 0f, 0f);
                yield return null;
            }
            extraScale = 1f;
            idle = true; // Update reprend la main (survol, sauts)
            ApplyPose(Vector3.one, 0f, 0f);
        }

        /// v5 §5.2 : la petite ARRIVÉE commune à tous les coffres d'un étage (3 à 6), tous en
        /// même temps : échelle 0,9 → 1,05 → 1 en 0,2 s, pendant le verrou anti-clic. Le coffre
        /// est déjà visible (jamais caché) : c'est un simple « rebond » d'accueil.
        public void Arrive(float duration)
        {
            StopAllCoroutines();
            StartCoroutine(ArriveRoutine(duration));
        }

        IEnumerator ArriveRoutine(float duration)
        {
            idle = false;
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                extraScale = Ease.Pop(t / duration, 0.9f, 1.05f);
                ApplyPose(Vector3.one, 0f, 0f);
                yield return null;
            }
            extraScale = 1f;
            idle = true;
            ApplyPose(Vector3.one, 0f, 0f);
        }

        /// v5 §5.2 : les 5 modèles de rareté de ce coffre (pour le préchargement).
        public IEnumerable<GameObject> AllLookObjects()
        {
            foreach (var look in looks)
                if (look.model != null) yield return look.model.gameObject;
        }

        // ---------- Outils ----------

        /// Pose le modèle : échelle (relative au repos), inclinaison Z, petit saut Y,
        /// et l'angle courant du couvercle.
        void ApplyPose(Vector3 scale, float rotZ, float y)
        {
            model.localPosition = restPosition + Vector3.up * y;
            model.localRotation = restRotation * Quaternion.Euler(0f, 0f, rotZ);
            model.localScale = Vector3.Scale(restScale, scale) * extraScale;
            if (lid != null) lid.localRotation = Quaternion.Euler(lidAngle, 0f, 0f);
        }

        /// v7 (QA phase 2 n°13) : la couleur du Corps à la révélation. Piège : rouge, comme avant.
        /// Trésor : sur un coffre en bois, l'or habituel ; sur un coffre de rareté (bronze, argent,
        /// or, cristal), le Corps GARDE sa couleur avec une teinte d'or légère (25 %) : un coffre
        /// d'argent ne paraît plus doré une fois ouvert. Le moment et l'animation de la révélation
        /// ne changent pas : même suspense pour un trésor et un piège.
        Color RevealColor(bool isTrap)
        {
            if (isTrap) return trapColor;
            return rank >= 1 ? Color.Lerp(closedColor, treasureColor, 0.25f) : treasureColor;
        }

        void SetColor(Color color)
        {
            if (body == null) return;
            // Un PropertyBlock change la couleur de CE coffre sans modifier
            // le matériau partagé par les autres coffres.
            body.GetPropertyBlock(block);
            block.SetColor(BaseColorId, color);
            body.SetPropertyBlock(block);
        }
    }
}
