using System.Collections.Generic;
using UnityEngine;

namespace TourDeLaChance
{
    /// <summary>
    /// Les petites animations des ACCESSOIRES MODERNISÉS v5 (art bible §13, spec v5 §4.1). Pur
    /// affichage. Il est posé automatiquement sur chaque accessoire affiché (cheval du joueur,
    /// cheval vierge, studio d'ouverture) et cherche dans le modèle les pièces nommées par
    /// l'artiste 3D (l'origine de chaque pièce « Anim_* » est sur sa charnière) :
    ///
    ///   - Anim_Pompon, Anim_Pan*, Anim_Pointe, Anim_Plume… : RESSORT (mouvement secondaire) :
    ///     la pièce penche à l'opposé du mouvement de la tête puis revient en rebondissant ;
    ///   - Anim_Fleur_1..8 : petite boucle douce, chacune avec 0,12 s de décalage ;
    ///   - Anim_Coeur* : battement en échelle ;
    ///   - Anim_Rayons : tourne à −50°/s ; Anim_OrbiteLune : un tour en 2 s ;
    ///   - Eclat_1..3 : scintillement (échelle qui pulse, chacun à son tour) ;
    ///   - matériaux M5_Rubis et M5_TachePapillon : émission qui oscille (0,2 ↔ 0,5).
    /// Les anciens noms (AileGauche, Anneau, Perle_*, Orbite…) restent animés par AccessoryMotion.
    /// (Fx_Emetteur, le point d'où partent des particules, est gardé pour la partie 2.)
    /// </summary>
    public class AccessoryRig : MonoBehaviour
    {
        static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        class Spring { public Transform t; public Quaternion rest; public Vector2 angle, speed; }
        class Loop { public Transform t; public Quaternion restRot; public Vector3 restScale; public float phase; public int kind; }

        readonly List<Spring> springs = new List<Spring>();
        readonly List<Loop> loops = new List<Loop>();
        readonly List<(Renderer r, int index, Color baseEmission, float phase)> glows = new List<(Renderer, int, Color, float)>();
        MaterialPropertyBlock block;
        Vector3 lastPos, lastVel;
        float time;
        bool hasAnything;

        const int KindFlower = 0, KindHeart = 1, KindRays = 2, KindMoon = 3, KindSparkle = 4;

        void Awake()
        {
            block = new MaterialPropertyBlock();
            int flower = 0, sparkle = 0;
            foreach (var t in GetComponentsInChildren<Transform>(true))
            {
                string n = t.name;
                if (n.StartsWith("Anim_Fleur")) loops.Add(NewLoop(t, KindFlower, 0.12f * flower++));
                else if (n.StartsWith("Anim_Coeur")) loops.Add(NewLoop(t, KindHeart, n.EndsWith("Droit") ? 0.1f : 0f));
                else if (n.StartsWith("Anim_Rayons")) loops.Add(NewLoop(t, KindRays, 0f));
                else if (n.StartsWith("Anim_OrbiteLune")) loops.Add(NewLoop(t, KindMoon, 0f));
                else if (n.StartsWith("Eclat_")) loops.Add(NewLoop(t, KindSparkle, 0.4f * sparkle++));
                else if (n.StartsWith("Anim_")) springs.Add(new Spring { t = t, rest = t.localRotation });
            }
            foreach (var r in GetComponentsInChildren<Renderer>(true))
            {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    if (mats[i] == null) continue;
                    string n = mats[i].name;
                    if (n.StartsWith("M5_Rubis") || n.StartsWith("M5_TachePapillon"))
                    {
                        // (L'émission est activée dans le fichier du matériau : on ne modifie
                        // jamais un matériau partagé pendant le jeu.)
                        Color c = mats[i].HasProperty("_BaseColor") ? mats[i].GetColor("_BaseColor") : Color.white;
                        glows.Add((r, i, c, glows.Count * 0.7f));
                    }
                }
            }
            hasAnything = springs.Count + loops.Count + glows.Count > 0;
            lastPos = transform.position;
        }

        static Loop NewLoop(Transform t, int kind, float phase) =>
            new Loop { t = t, restRot = t.localRotation, restScale = t.localScale, phase = phase, kind = kind };

        void LateUpdate()
        {
            if (!hasAnything) { enabled = false; return; }
            float dt = Mathf.Max(Time.deltaTime, 0.0001f);
            time += dt;

            // RESSORT : l'accélération de la tête fait pencher les pièces dans l'autre sens.
            Vector3 vel = (transform.position - lastPos) / dt;
            Vector3 accel = (vel - lastVel) / dt;
            lastPos = transform.position;
            lastVel = vel;
            Vector3 localAccel = transform.InverseTransformDirection(accel);
            foreach (var s in springs)
            {
                // Cible : penché à l'opposé de l'accélération (au plus 25°), plus un léger balancement.
                Vector2 target = new Vector2(Mathf.Clamp(localAccel.z * 2.5f, -25f, 25f), Mathf.Clamp(-localAccel.x * 2.5f, -25f, 25f));
                target += new Vector2(2f * Mathf.Sin(time * 2.1f), 1.5f * Mathf.Sin(time * 1.7f + 1f));
                // Ressort amorti (raideur 60, amortissement 7) : retard puis petit rebond.
                s.speed += (60f * (target - s.angle) - 7f * s.speed) * dt;
                s.angle += s.speed * dt;
                s.t.localRotation = s.rest * Quaternion.Euler(s.angle.x, 0f, s.angle.y);
            }

            foreach (var l in loops)
            {
                float u = time - l.phase;
                switch (l.kind)
                {
                    case KindFlower: l.t.localRotation = l.restRot * Quaternion.Euler(0f, 0f, 6f * Mathf.Sin(u * Mathf.PI * 2f / 2.4f)); break;
                    case KindHeart:
                    {
                        float beat = Mathf.Repeat(u, 1.2f);
                        float s = 1f + (beat < 0.15f ? 0.12f * Mathf.Sin(beat / 0.15f * Mathf.PI) : beat > 0.25f && beat < 0.4f ? 0.08f * Mathf.Sin((beat - 0.25f) / 0.15f * Mathf.PI) : 0f);
                        l.t.localScale = l.restScale * s;
                        break;
                    }
                    case KindRays: l.t.localRotation = l.restRot * Quaternion.Euler(0f, -50f * time, 0f); break;
                    case KindMoon: l.t.localRotation = l.restRot * Quaternion.Euler(0f, 180f * time, 0f); break;
                    case KindSparkle:
                    {
                        float k = Mathf.Repeat(u, 1.2f) / 1.2f;
                        l.t.localScale = l.restScale * (k < 0.3f ? Mathf.Sin(k / 0.3f * Mathf.PI) : 0.05f);
                        break;
                    }
                }
            }

            foreach (var (r, index, baseColor, phase) in glows)
            {
                float e = Mathf.Lerp(0.2f, 0.5f, 0.5f + 0.5f * Mathf.Sin((time + phase) * Mathf.PI * 2f / 2f));
                r.GetPropertyBlock(block, index);
                block.SetColor(EmissionColorId, baseColor * e);
                r.SetPropertyBlock(block, index);
            }
        }
    }
}
