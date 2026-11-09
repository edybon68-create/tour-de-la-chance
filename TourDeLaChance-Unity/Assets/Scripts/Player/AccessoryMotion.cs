using System.Collections.Generic;
using UnityEngine;

namespace TourDeLaChance
{
    /// <summary>
    /// Les animations propres d'un accessoire porté (spec boites §5.6) : battement d'ailes,
    /// auréole qui flotte et tourne, planète en orbite. Ajouté par PlayerAvatar sur le
    /// modèle qu'il vient de poser. Pur affichage : il ne touche à rien d'autre.
    ///
    /// Les pièces à animer sont retrouvées PAR LEUR NOM dans le modèle Blender
    /// (AileGauche, AileDroite, Anneau, Orbite, Planete). Il allume aussi la lumière propre
    /// (émission) des matériaux qui en ont besoin : M_Aureole (1,5) et M_AnneauPlanete (0,5).
    /// L'accessoire étant un enfant de `Joueur`, il suit tout seul les sauts du personnage.
    /// </summary>
    public class AccessoryMotion : MonoBehaviour
    {
        static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        AccessoryAnimation mode;
        Transform left, right, ring, orbit, planet;
        Quaternion leftRest, rightRest, ringRest;
        float time;
        readonly List<Material> ownMaterials = new List<Material>(); // copies à détruire

        /// Prépare l'animation (appelé une fois par PlayerAvatar juste après la création).
        public void Setup(AccessoryAnimation animation)
        {
            mode = animation;
            left = FindChild("AileGauche");
            right = FindChild("AileDroite");
            ring = FindChild("Anneau");
            orbit = FindChild("Orbite");
            planet = FindChild("Planete");
            if (left != null) leftRest = left.localRotation;
            if (right != null) rightRest = right.localRotation;
            if (ring != null) ringRest = ring.localRotation;

            // Émission (art-bible §4.1) : réservée au Légendaire et au Secret.
            EnableEmission("M_Aureole", 1.5f);
            EnableEmission("M_AnneauPlanete", 0.5f);
            Tick(0f);
        }

        void Update()
        {
            time += Time.deltaTime;
            Tick(time);
        }

        void Tick(float t)
        {
            switch (mode)
            {
                case AccessoryAnimation.AilesAnge:
                    Flap(t, 8f, 1.6f);
                    break;
                case AccessoryAnimation.AilesPapillon:
                    Flap(t, 20f, 0.9f);
                    break;
                case AccessoryAnimation.Aureole:
                    if (ring != null)
                    {
                        // Monte et descend de ±4 cm (période 2 s) et tourne à 30°/s.
                        ring.localPosition = new Vector3(0f, 0.30f + 0.04f * Mathf.Sin(2f * Mathf.PI * t / 2f), 0f);
                        ring.localRotation = ringRest * Quaternion.Euler(0f, 30f * t, 0f);
                    }
                    break;
                case AccessoryAnimation.Planete:
                    // L'orbite tourne (un tour en 6 s), la planète tourne sur elle-même.
                    // On ne touche pas à « Inclinaison » : c'est elle qui penche l'orbite de 15°.
                    if (orbit != null) orbit.localRotation = Quaternion.Euler(0f, 60f * t, 0f);
                    if (planet != null) planet.localRotation = Quaternion.Euler(0f, 90f * t, 0f);
                    break;
            }
        }

        /// Battement en miroir : l'aile gauche tourne de +a, la droite de −a autour de l'axe
        /// vertical de leur attache, avec a = amplitude × sin(2πt / période).
        void Flap(float t, float amplitude, float period)
        {
            float a = amplitude * Mathf.Sin(2f * Mathf.PI * t / period);
            if (left != null) left.localRotation = leftRest * Quaternion.Euler(0f, a, 0f);
            if (right != null) right.localRotation = rightRest * Quaternion.Euler(0f, -a, 0f);
        }

        /// Allume l'émission (lumière propre) des matériaux de ce nom, sur des COPIES
        /// (les matériaux importés du modèle restent intacts). Couleur émise = couleur de
        /// base × intensité ; avec le Bloom (seuil 1,0), cela donne une lueur douce.
        void EnableEmission(string materialName, float intensity)
        {
            foreach (var r in GetComponentsInChildren<Renderer>(true))
            {
                var shared = r.sharedMaterials;
                bool found = false;
                for (int i = 0; i < shared.Length; i++)
                {
                    if (shared[i] == null || !shared[i].name.StartsWith(materialName)) continue;
                    // On compare le nom exact (sans « (Instance) ») pour ne pas confondre
                    // M_Aureole avec un autre nom qui commencerait pareil.
                    string clean = shared[i].name.Replace(" (Instance)", "");
                    if (clean != materialName) continue;
                    var copy = new Material(shared[i]) { name = materialName + " (émission)" };
                    copy.EnableKeyword("_EMISSION");
                    copy.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                    Color baseColor = copy.HasProperty(BaseColorId) ? copy.GetColor(BaseColorId) : Color.white;
                    copy.SetColor(EmissionColorId, baseColor * intensity);
                    shared[i] = copy;
                    ownMaterials.Add(copy);
                    found = true;
                }
                if (found) r.sharedMaterials = shared;
            }
        }

        Transform FindChild(string childName)
        {
            foreach (var t in GetComponentsInChildren<Transform>(true))
                if (t.name == childName) return t;
            return null;
        }

        void OnDestroy()
        {
            foreach (var m in ownMaterials) if (m != null) Destroy(m);
        }
    }
}
