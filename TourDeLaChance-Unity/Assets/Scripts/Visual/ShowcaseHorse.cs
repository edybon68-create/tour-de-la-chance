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
    /// v6 §6.2 : le joueur peut le FAIRE TOURNER en glissant (0,5° par pixel, autour de l'axe
    /// vertical seulement). Au relâché, un élan qui s'amortit (vitesse divisée par 2 toutes les
    /// 0,1 s). La rotation automatique s'arrête dès qu'on l'attrape et reprend en douceur (0,5 s)
    /// 2 s après le dernier glisser. v6 §12 : la taille de l'image suit le niveau graphique.
    ///
    /// Caméra et lumière ne sont allumées que pendant qu'une fiche est ouverte (économie). Le
    /// cheval tourne lentement (25°/s) et fait un petit saut de joie quand on lui montre un objet.
    /// Pur affichage.
    /// </summary>
    public class ShowcaseHorse : MonoBehaviour
    {
        const int Width = 512, Height = 680;   // taille « Moyen » (v6 §12.2)
        const float TurnSpeed = 25f; // °/s
        public const float DegreesPerPixel = 0.5f;
        const float ResumeDelay = 2f, ResumeRamp = 0.5f, HalfLife = 0.1f;

        // La rotation à la main (v6 §6.2).
        bool dragging;
        float spinVelocity;      // °/s de l'élan après le relâché
        float idleTime = 99f;    // temps depuis le dernier glisser
        float autoFactor = 1f;   // 0 = rotation automatique arrêtée, 1 = pleine vitesse
        float lastDragDelta, lastDragTime;

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
        /// L'angle du plateau autour de l'axe vertical (pour les tests).
        public float Angle => turntable != null ? turntable.localEulerAngles.y : 0f;
        public bool AutoTurning => !dragging && autoFactor > 0f;

        void Awake()
        {
            GraphicsLevel.Changed += OnGraphicsChanged;
            Prepare();
            if (turntable != null) restPosition = turntable.localPosition;
            SetActive(false);
        }

        void Prepare()
        {
            if (target != null || cam == null) return;
            var size = GraphicsLevel.ShowcaseSize(Width, Height);
            target = new RenderTexture(size.x, size.y, 24, RenderTextureFormat.ARGB32) { name = "Cheval vierge", antiAliasing = 4 };
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
            // v6 §6.2 : changer d'objet remet l'angle de départ et la rotation automatique.
            dragging = false; spinVelocity = 0f; autoFactor = 1f; idleTime = 99f;
            if (hop != null) StopCoroutine(hop);
            if (isActiveAndEnabled) hop = StartCoroutine(JoyHop());
        }

        void Update()
        {
            if (turntable == null || !IsActive) return;
            float dt = Time.deltaTime;
            if (dragging) return; // c'est la main du joueur qui tourne le plateau
            // L'élan : la vitesse est divisée par 2 toutes les 0,1 s (il s'arrête en ~0,4 s).
            if (Mathf.Abs(spinVelocity) > 0.5f)
            {
                turntable.Rotate(0f, spinVelocity * dt, 0f, Space.Self);
                spinVelocity *= Mathf.Pow(0.5f, dt / HalfLife);
            }
            else spinVelocity = 0f;
            // La rotation automatique reprend 2 s après le dernier glisser, en 0,5 s.
            idleTime += dt;
            if (idleTime >= ResumeDelay) autoFactor = Mathf.Min(1f, autoFactor + dt / ResumeRamp);
            turntable.Rotate(0f, TurnSpeed * autoFactor * dt, 0f, Space.Self);
        }

        /// Le joueur attrape le cheval : la rotation automatique s'arrête.
        public void BeginDrag()
        {
            dragging = true;
            spinVelocity = 0f;
            autoFactor = 0f;
            lastDragDelta = 0f;
            lastDragTime = Time.unscaledTime;
        }

        /// Le joueur glisse de dx pixels (vers la droite = positif) : 0,5° par pixel.
        public void Drag(float dx)
        {
            if (turntable == null) return;
            float deg = -dx * DegreesPerPixel; // glisser à droite fait tourner la face vers la droite
            turntable.Rotate(0f, deg, 0f, Space.Self);
            float now = Time.unscaledTime;
            float dt = Mathf.Max(0.001f, now - lastDragTime);
            lastDragDelta = deg / dt;   // vitesse du dernier glisser (°/s)
            lastDragTime = now;
            idleTime = 0f;
        }

        /// Relâché : l'élan part avec la vitesse du dernier glisser (divisée par 2, §6.2).
        public void EndDrag()
        {
            if (!dragging) return;
            dragging = false;
            bool recent = Time.unscaledTime - lastDragTime < 0.1f;
            spinVelocity = recent ? Mathf.Clamp(lastDragDelta * 0.5f, -360f, 360f) : 0f;
            idleTime = 0f;
        }

        /// v6 §12 : nouveau niveau graphique : on refait l'image à la bonne taille.
        void OnGraphicsChanged()
        {
            if (target == null) return;
            if (cam != null) cam.targetTexture = null;
            target.Release();
            Destroy(target);
            target = null;
            Prepare();
            TextureChanged?.Invoke();
        }

        /// Prévient l'interface que l'image a été refaite (elle doit reposer le fond).
        public event System.Action TextureChanged;

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
            GraphicsLevel.Changed -= OnGraphicsChanged;
            if (cam != null) cam.targetTexture = null;
            if (target != null) { target.Release(); Destroy(target); }
        }
    }
}
