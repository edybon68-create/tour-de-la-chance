using System.Collections;
using UnityEngine;

namespace TourDeLaChance
{
    /// <summary>
    /// Le CHEVAL VIERGE (spec v5 §8.5) : un 2e cheval, couleur « plâtre », placé LOIN de la tour
    /// (200 ; 0 ; 0), sur le calque « Vitrine » que la caméra principale ne voit pas, avec sa
    /// propre caméra (image 512 × 680) et sa lumière. Il sert à la fiche agrandie de l'Index et
    /// aux cartes « Contenu et taux » : on y voit UN objet seul (un skin, ou un accessoire sur le
    /// cheval plâtre), sans jamais toucher au cheval du joueur ni à la sauvegarde.
    ///
    /// Caméra et lumière ne sont allumées que pendant qu'une fiche est ouverte (économie). Le
    /// cheval tourne lentement (25°/s) et fait un petit saut de joie quand on lui montre un objet.
    /// Pur affichage.
    /// </summary>
    public class ShowcaseHorse : MonoBehaviour
    {
        const int Width = 512, Height = 680;
        const float TurnSpeed = 25f; // °/s

        [SerializeField] PlayerAvatar avatar;   // en mode vitrine (showcaseMode)
        [SerializeField] Transform turntable;   // ce qui tourne (le cheval)
        [SerializeField] Camera cam;            // filme seulement le calque « Vitrine »
        [SerializeField] Light lamp;            // sa lumière à lui

        RenderTexture target;
        Vector3 restPosition;
        Coroutine hop;

        /// L'image filmée (à poser en fond d'un élément d'interface).
        public RenderTexture Texture { get { Prepare(); return target; } }
        public bool IsActive => cam != null && cam.enabled;
        public PlayerAvatar Avatar => avatar;
        /// L'objet montré en ce moment (pour les tests).
        public ShopItem Shown { get; private set; }

        void Awake()
        {
            Prepare();
            if (turntable != null) restPosition = turntable.localPosition;
            SetActive(false);
        }

        void Prepare()
        {
            if (target != null || cam == null) return;
            target = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32) { name = "Cheval vierge", antiAliasing = 4 };
            target.Create();
            cam.targetTexture = target;
        }

        /// Allume ou éteint la caméra et la lumière.
        public void SetActive(bool on)
        {
            Prepare();
            if (cam != null) cam.enabled = on;
            if (lamp != null) lamp.enabled = on;
        }

        /// Montre un objet seul sur le cheval vierge (null = plâtre, rien).
        public void Show(ShopItem item)
        {
            Shown = item;
            if (avatar != null) avatar.ShowOnly(item);
            if (turntable != null) turntable.localRotation = Quaternion.Euler(0f, 200f, 0f); // de trois-quarts
            if (hop != null) StopCoroutine(hop);
            if (isActiveAndEnabled) hop = StartCoroutine(JoyHop());
        }

        void Update()
        {
            if (turntable != null && IsActive) turntable.Rotate(0f, TurnSpeed * Time.deltaTime, 0f, Space.Self);
        }

        /// Petit saut de joie (0,2 m, 0,35 s) à l'ouverture de la fiche.
        IEnumerator JoyHop()
        {
            for (float t = 0f; t < 0.35f; t += Time.deltaTime)
            {
                if (turntable != null) turntable.localPosition = restPosition + Vector3.up * 0.2f * Mathf.Sin(t / 0.35f * Mathf.PI);
                yield return null;
            }
            if (turntable != null) turntable.localPosition = restPosition;
            hop = null;
        }

        void OnDestroy()
        {
            if (cam != null) cam.targetTexture = null;
            if (target != null) { target.Release(); Destroy(target); }
        }
    }
}
