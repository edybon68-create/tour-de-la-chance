using System;
using System.Collections;
using UnityEngine;

namespace TourDeLaChance
{
    /// <summary>
    /// Les animations du personnage (spec sons-animations §3.1, §3.2, §3.7) : respiration,
    /// regard vers le coffre survolé, penché du suspense, saut de joie, écrasement du piège,
    /// fête de l'encaissement, petits sauts.
    ///
    /// Comment ça marche : chaque animation règle quelques "calques" (hauteur, échelle,
    /// penché, tour sur soi-même, regard, respiration) et LateUpdate les additionne pour
    /// poser l'objet `Joueur`. Comme on part TOUJOURS de la pose de repos mémorisée au
    /// démarrage, le personnage ne peut pas « dériver » même après 50 parties.
    /// L'origine de `Joueur` est à ses pieds : l'écrasement les garde au sol, et le
    /// chapeau (un enfant) suit tout seul.
    /// </summary>
    public class PlayerAnimator : MonoBehaviour
    {
        // Pose de repos
        Vector3 restPosition, restScale;
        Quaternion restRotation;

        // Calques réglés par les animations
        float offsetY;                       // hauteur du saut (m)
        Vector3 actionScale = Vector3.one;   // écrasement / étirement
        float leanX;                         // penché en avant (°)
        float spinY;                         // tour sur soi-même (°)
        float lookY, lookFrom, lookTo, lookTime = 1f; // regard vers un coffre (°)

        // Respiration
        bool breathing = true;
        float breathWeight = 1f;
        float breathTime;
        bool headDown; // après un piège : respiration ralentie

        Coroutine action;

        void Awake()
        {
            restPosition = transform.localPosition;
            restRotation = transform.localRotation;
            restScale = transform.localScale;
        }

        void LateUpdate()
        {
            // Le regard tourne en 0,2 s (ease-out).
            lookTime += Time.deltaTime / 0.2f;
            lookY = Mathf.Lerp(lookFrom, lookTo, Ease.Out(lookTime));

            // Respiration : elle s'efface en douceur pendant une autre animation.
            breathWeight = Mathf.MoveTowards(breathWeight, breathing ? 1f : 0f, Time.deltaTime / 0.2f);
            float period = headDown ? 3.6f : 2.4f;
            float amplitude = headDown ? 0.5f : 1f;
            breathTime += Time.deltaTime;
            // (1 − cos) / 2 va doucement de 0 à 1 puis revient à 0 : une inspiration.
            float breath = (1f - Mathf.Cos(2f * Mathf.PI * breathTime / period)) * 0.5f * amplitude * breathWeight;
            Vector3 breathScale = new Vector3(1f - 0.015f * breath, 1f + 0.03f * breath, 1f - 0.015f * breath);
            // Petit balancement deux fois plus lent : ±1,5°.
            float swayZ = 1.5f * amplitude * breathWeight * Mathf.Sin(2f * Mathf.PI * breathTime / (period * 2f));

            transform.localPosition = restPosition + Vector3.up * offsetY;
            transform.localRotation = restRotation * Quaternion.Euler(leanX, lookY + spinY, swayZ);
            transform.localScale = Vector3.Scale(restScale, Vector3.Scale(actionScale, breathScale));
        }

        // ---------- Ce que l'affichage peut demander ----------

        /// Regarde vers un point (position X du coffre survolé dans le monde), ou de face (null).
        /// Le personnage nous fait face (il regarde vers −Z) : tourner de +15° autour de Y
        /// déplace ses yeux vers la gauche de l'écran.
        public void LookAtX(float? worldX)
        {
            float target = 0f;
            if (worldX.HasValue && worldX.Value < transform.position.x - 0.5f) target = 15f;
            else if (worldX.HasValue && worldX.Value > transform.position.x + 0.5f) target = -15f;
            if (Mathf.Approximately(target, lookTo)) return;
            lookFrom = lookY;
            lookTo = target;
            lookTime = 0f;
        }

        /// Suspense : il se penche en avant de 8° (0,2 s) et retient son souffle.
        public void HoldBreath()
        {
            LookAtX(null);
            StartAction(HoldBreathRoutine());
        }

        /// Trésor choisi : saut de joie (+0,6 m, 0,7 s).
        public void JumpOfJoy()
        {
            joyUntil = Time.time + 0.7f;
            StartAction(JumpRoutine(0.6f, 0.08f, 0.20f, 0.20f, 0.10f, 0.12f, false, true));
        }

        float joyUntil = -1f;
        /// Vrai pendant le saut de joie (l'arc-en-ciel s'y « excite » : il défile 3 × plus vite).
        public bool IsJoyJumping => Time.time < joyUntil;

        /// Encaisser : deux sauts à la suite (1,2 s), le second plus haut avec un tour complet.
        public void CashOutCelebration() => StartAction(CelebrationRoutine());

        /// Piège choisi : écrasement, puis tête basse jusqu'à ReturnToRest().
        public void TrapSquash() => StartAction(SquashRoutine());

        /// Petit saut d'accueil (début de partie, aperçu dans le shop) : 0,15 m, 0,25 s.
        public void WelcomeHop() => StartAction(SmallHopRoutine(0.15f, 0.25f));

        /// Début de la montée : préparation + saut de 0,4 m (0,25 s au total).
        public void ClimbHop() => StartAction(JumpRoutine(0.4f, 0.05f, 0.10f, 0.10f, 0f, 0f, false, true));

        /// Revient au repos en douceur (0,3 s, ease-out) : sortie de la « tête basse ».
        public void ReturnToRest(float duration = 0.3f) => StartAction(ReturnRoutine(duration));

        /// Revient au repos instantanément (pendant l'écran sombre d'une transition).
        public void SnapToRest()
        {
            if (action != null) StopCoroutine(action);
            action = null;
            offsetY = 0f;
            actionScale = Vector3.one;
            leanX = 0f;
            spinY = 0f;
            headDown = false;
            breathing = true;
            lookFrom = lookTo = lookY = 0f;
        }

        /// Pour les tests : la pose de repos mémorisée.
        public Vector3 RestPosition => restPosition;

        // ---------- Les animations (coroutines) ----------

        void StartAction(IEnumerator routine)
        {
            if (action != null) StopCoroutine(action);
            action = StartCoroutine(routine);
        }

        /// Fait évoluer une valeur pendant "duration" secondes. "apply" reçoit t (0 → 1)
        /// déjà passé dans la courbe "ease".
        static IEnumerator Phase(float duration, Func<float, float> ease, Action<float> apply)
        {
            if (duration <= 0f) { apply(1f); yield break; }
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                apply(ease(t / duration));
                yield return null;
            }
            apply(1f);
        }

        static float Linear(float t) => Mathf.Clamp01(t);

        IEnumerator HoldBreathRoutine()
        {
            breathing = false;
            headDown = false;
            float startLean = leanX;
            yield return Phase(0.2f, Ease.Out, k => leanX = Mathf.Lerp(startLean, 8f, k));
        }

        /// Un saut complet. Les durées de chaque phase sont données (0 = phase sautée).
        IEnumerator JumpRoutine(float height, float prep, float rise, float fall, float land, float back, bool spin, bool resumeBreath)
        {
            breathing = false;
            headDown = false;
            Vector3 s0 = actionScale;
            float lean0 = leanX;
            float spin0 = spinY;
            Vector3 crouch = new Vector3(1.10f, 0.85f, 1.10f);
            Vector3 stretch = new Vector3(0.95f, 1.10f, 0.95f);
            Vector3 landing = new Vector3(1.08f, 0.88f, 1.08f);

            // Préparation : il se tasse et se redresse (le penché revient à 0°).
            yield return Phase(prep, Linear, k => { actionScale = Vector3.Lerp(s0, crouch, k); leanX = Mathf.Lerp(lean0, 0f, k); });
            leanX = 0f;
            Vector3 s1 = actionScale;
            // Montée : il s'étire. (Si spin : la moitié du tour pendant la montée.)
            yield return Phase(rise, Ease.Out, k =>
            {
                offsetY = height * k;
                actionScale = Vector3.Lerp(s1, stretch, k);
                if (spin) spinY = Mathf.Lerp(spin0, spin0 + 180f, k);
            });
            Vector3 s2 = actionScale;
            // Descente : retour au sol, échelle normale. (L'autre moitié du tour.)
            yield return Phase(fall, Ease.In, k =>
            {
                offsetY = height * (1f - k);
                actionScale = Vector3.Lerp(s2, Vector3.one, k);
                if (spin) spinY = Mathf.Lerp(spin0 + 180f, spin0 + 360f, k);
            });
            offsetY = 0f;
            spinY = 0f; // 360° = la même chose que 0° : on revient exactement au repos
            // Atterrissage : petit écrasement, puis retour.
            yield return Phase(land, Linear, k => actionScale = Vector3.Lerp(Vector3.one, landing, k));
            Vector3 s3 = actionScale;
            yield return Phase(back, Ease.Out, k => actionScale = Vector3.Lerp(s3, Vector3.one, k));
            actionScale = Vector3.one;
            if (resumeBreath) breathing = true;
            action = null;
        }

        IEnumerator CelebrationRoutine()
        {
            // 1er saut (un peu plus court), puis le 2e, plus haut, avec un tour complet.
            // Total : 0,46 s + 0,74 s = 1,2 s (§3.7).
            yield return JumpRoutine(0.6f, 0.06f, 0.16f, 0.16f, 0.08f, 0f, false, false);
            yield return JumpRoutine(0.8f, 0.06f, 0.24f, 0.24f, 0.08f, 0.12f, true, true);
        }

        IEnumerator SquashRoutine()
        {
            breathing = false;
            headDown = false;
            Vector3 s0 = actionScale;
            float lean0 = leanX;
            Vector3 flat = new Vector3(1.15f, 0.75f, 1.15f);
            Vector3 sad = new Vector3(1.04f, 0.92f, 1.04f);

            // 1. Écrasement (0,12 s). Le penché du suspense revient à 0.
            yield return Phase(0.12f, Ease.Out, k => { actionScale = Vector3.Lerp(s0, flat, k); leanX = Mathf.Lerp(lean0, 0f, k); });
            // 2. Maintien 0,25 s.
            yield return new WaitForSeconds(0.25f);
            // 3. Tête basse (0,4 s) : penché de 12°, respiration ralentie.
            headDown = true;
            breathing = true;
            yield return Phase(0.4f, Ease.InOut, k => { actionScale = Vector3.Lerp(flat, sad, k); leanX = Mathf.Lerp(0f, 12f, k); });
            action = null; // il reste ainsi jusqu'à ReturnToRest()
        }

        /// v5 §13.4 : la RENAISSANCE. À 0,3 s le cheval s'élève de 0,5 m en faisant 2 tours
        /// (1,2 s), plane, puis atterrit à 2,1 s avec un écrasement. Pur affichage.
        public void RebirthRise() => StartAction(RebirthRoutine());

        IEnumerator RebirthRoutine()
        {
            headDown = false;
            breathing = false;
            leanX = 0f;
            actionScale = Vector3.one;
            yield return new WaitForSeconds(0.3f);
            yield return Phase(1.2f, Ease.InOut, k => { offsetY = 0.5f * k; spinY = 720f * k; });
            spinY = 0f; // 720° = deux tours : on revient pile de face
            yield return new WaitForSeconds(0.4f);
            yield return Phase(0.2f, Ease.In, k => offsetY = 0.5f * (1f - k));
            yield return Phase(0.25f, Linear, k =>
            {
                float squash = Ease.Sine(k);
                actionScale = new Vector3(1f + 0.12f * squash, 1f - 0.15f * squash, 1f + 0.12f * squash);
            });
            actionScale = Vector3.one;
            offsetY = 0f;
            breathing = true;
            action = null;
        }

        /// Remet tout de suite le cheval au repos (animation de renaissance passée).
        public void StopRebirth()
        {
            if (action != null) StopCoroutine(action);
            action = null;
            offsetY = 0f; spinY = 0f; leanX = 0f;
            actionScale = Vector3.one;
            breathing = true;
        }

        IEnumerator SmallHopRoutine(float height, float duration)
        {
            Vector3 s0 = actionScale;
            float lean0 = leanX;
            headDown = false;
            breathing = true;
            yield return Phase(duration, Linear, k =>
            {
                offsetY = height * Ease.Sine(k);
                actionScale = Vector3.Lerp(s0, Vector3.one, k * 4f);
                leanX = Mathf.Lerp(lean0, 0f, k * 4f);
            });
            offsetY = 0f;
            action = null;
        }

        IEnumerator ReturnRoutine(float duration)
        {
            Vector3 s0 = actionScale;
            float lean0 = leanX, y0 = offsetY;
            headDown = false;
            breathing = true;
            yield return Phase(duration, Ease.Out, k =>
            {
                actionScale = Vector3.Lerp(s0, Vector3.one, k);
                leanX = Mathf.Lerp(lean0, 0f, k);
                offsetY = Mathf.Lerp(y0, 0f, k);
            });
            spinY = 0f;
            action = null;
        }
    }
}
