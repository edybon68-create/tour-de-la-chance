using System.Collections;
using UnityEngine;

namespace TourDeLaChance
{
    /// <summary>
    /// Les mouvements de la caméra (spec §3.6 et §3.11). La caméra est FIXE pendant le jeu :
    /// elle ne bouge que pour la secousse du piège et pendant les transitions d'étage.
    /// Elle revient toujours EXACTEMENT à sa place de départ, (0 ; 5 ; −7), mémorisée au
    /// démarrage. Sa rotation ne change jamais (seulement sa hauteur).
    /// </summary>
    public class CameraRig : MonoBehaviour
    {
        // Moments de la montée (en secondes depuis son début), d'après le tableau §3.11.
        const float RiseStart = 0.10f;
        const float ReappearTime = GameManager.ClimbDarkTime + 0.05f;

        Vector3 restPosition;
        Quaternion restRotation;
        float heightOffset;   // décalage vertical des transitions
        Vector3 shakeOffset;  // décalage de la secousse
        Coroutine move, shake;
        readonly System.Random random = new System.Random(); // hasard visuel seulement

        public bool IsMoving => move != null;

        void Awake()
        {
            restPosition = transform.position;
            restRotation = transform.rotation;
        }

        void LateUpdate()
        {
            transform.position = restPosition + Vector3.up * heightOffset + shakeOffset;
            transform.rotation = restRotation;
        }

        /// Secousse du piège : 0,25 s, nouvelle direction toutes les 0,033 s,
        /// amplitude 0,06 m qui décroît jusqu'à 0.
        public void Shake() => Shake(0.06f, 0.25f);

        /// Secousse réglable (v3 §5.3.2 : coffre d'or 0,03 m / 0,2 s, diamant 0,05 m / 0,3 s).
        public void Shake(float amplitude, float duration)
        {
            if (shake != null) StopCoroutine(shake);
            shake = StartCoroutine(ShakeRoutine(duration, amplitude));
        }

        IEnumerator ShakeRoutine(float duration, float amplitude)
        {
            float nextChange = 0f;
            Vector3 direction = Vector3.zero;
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                if (t >= nextChange)
                {
                    direction = new Vector3((float)random.NextDouble() * 2f - 1f,
                                            (float)random.NextDouble() * 2f - 1f,
                                            (float)random.NextDouble() * 2f - 1f).normalized;
                    nextChange += 0.033f;
                }
                shakeOffset = direction * amplitude * (1f - t / duration);
                yield return null;
            }
            shakeOffset = Vector3.zero;
            shake = null;
        }

        /// La montée : la caméra file vers le haut (+3 m), l'écran s'assombrit, puis elle
        /// arrive par en dessous (−3 m → sa place). Le mouvement est toujours vers le haut.
        public void PlayClimb() => StartMove(ClimbRoutine());

        /// « Rejouer » : on redescend (5 → 3), puis on arrive par au-dessus (7 → 5).
        public void PlayDescent() => StartMove(DescentRoutine());

        void StartMove(IEnumerator routine)
        {
            if (move != null) StopCoroutine(move);
            move = StartCoroutine(routine);
        }

        IEnumerator ClimbRoutine()
        {
            float t = 0f;
            while (t < GameManager.ClimbDuration)
            {
                if (t < RiseStart) heightOffset = 0f;
                else if (t < GameManager.ClimbDarkTime)
                    heightOffset = 3f * Ease.In((t - RiseStart) / (GameManager.ClimbDarkTime - RiseStart));
                else if (t < ReappearTime) heightOffset = -3f; // dans le noir : 3 m SOUS sa place
                else heightOffset = Mathf.Lerp(-3f, 0f, Ease.Out((t - ReappearTime) / (GameManager.ClimbDuration - ReappearTime)));
                yield return null;
                t += Time.deltaTime;
            }
            heightOffset = 0f;
            move = null;
        }

        IEnumerator DescentRoutine()
        {
            float dark = GameManager.DescentDarkTime;
            float end = GameManager.DescentDuration;
            float t = 0f;
            while (t < end)
            {
                if (t < dark) heightOffset = -2f * Ease.In(t / dark);
                else heightOffset = Mathf.Lerp(2f, 0f, Ease.Out((t - dark) / (end - dark)));
                yield return null;
                t += Time.deltaTime;
            }
            heightOffset = 0f;
            move = null;
        }
    }
}
