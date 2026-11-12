using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace TourDeLaChance
{
    /// <summary>
    /// Le spectacle d'un TRÉSOR EN DIAMANTS (spec v5 §10.5) : à la place des pièces, des
    /// diamants 3D bleus jaillissent du coffre (3 + 2 × nombre, au plus 12), tournent, puis
    /// VOLENT vers le compteur de diamants du HUD ; un texte « +2 ◆ » bleu flotte au-dessus du
    /// coffre. Pur affichage : il arrive APRÈS la révélation (le coffre est déjà ouvert), et
    /// les diamants sont déjà comptés par le GameManager.
    ///
    /// Le diamant est un petit modèle « taillé » FABRIQUÉ PAR LE CODE (16 facettes) en attendant
    /// le modèle de l'artiste (spec §17.1). Une réserve de 12 diamants est créée au démarrage
    /// et dessinée une fois (préchargement §5.2) : rien n'est créé pendant le jeu.
    /// </summary>
    public class DiamondBurst : MonoBehaviour
    {
        public const int MaxDiamonds = 12;

        [Tooltip("Modèle du diamant (facultatif : sinon un diamant taillé fabriqué par le code).")]
        [SerializeField] GameObject diamondPrefab;
        [Tooltip("Le HUD : pour le texte « +2 ◆ » et la position du compteur de diamants.")]
        [SerializeField] UIDocument hud;
        [SerializeField] DiamondCounterView counter;

        static readonly Color DiamondBlue = new Color(0.31f, 0.765f, 1f);   // #4FC3FF
        const float JumpTime = 0.7f;   // jaillissement et rotation au-dessus du coffre
        const float FlyTime = 0.55f;   // vol vers le compteur

        class Piece { public Transform t; public Vector3 velocity, spinAxis; public float spin; }
        readonly List<Piece> pool = new List<Piece>();
        readonly System.Random fxRandom = new System.Random(); // hasard VISUEL, sans lien avec les règles
        Material material;

        void Awake()
        {
            material = new Material(UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline != null
                ? UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline.defaultMaterial
                : new Material(Shader.Find("Universal Render Pipeline/Lit"))) { name = "Diamant (monnaie)" };
            material.SetColor("_BaseColor", DiamondBlue);
            material.SetFloat("_Smoothness", 0.95f);
            material.SetFloat("_Metallic", 0.1f);
            material.SetFloat("_Cull", 0f); // les deux faces : aucune facette ne disparaît
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", DiamondBlue * 0.35f); // un léger éclat (< 1,5)
            var mesh = diamondPrefab == null ? BuildDiamondMesh() : null;
            for (int i = 0; i < MaxDiamonds; i++)
            {
                GameObject go;
                if (diamondPrefab != null) go = Instantiate(diamondPrefab, transform);
                else
                {
                    go = new GameObject("Diamant_" + i);
                    go.transform.SetParent(transform, false);
                    go.AddComponent<MeshFilter>().sharedMesh = mesh;
                    go.AddComponent<MeshRenderer>().sharedMaterial = material;
                }
                go.transform.localScale = Vector3.one * 0.2f;
                go.SetActive(false);
                pool.Add(new Piece { t = go.transform });
            }
        }

        // Préchargement (§5.2) : la réserve est dessinée une fois par une caméra cachée.
        void Start()
        {
            var objects = new List<GameObject>();
            foreach (var p in pool) objects.Add(p.t.gameObject);
            RenderPreloader.RenderOnce(objects);
        }

        void OnDestroy()
        {
            if (material != null) Destroy(material);
        }

        /// Le nombre de diamants 3D pour un gain : 3 + 2 × nombre, au plus 12.
        public static int CountFor(int amount) => Mathf.Min(MaxDiamonds, 3 + 2 * Mathf.Max(1, amount));

        /// Lance le jaillissement au-dessus du coffre (et le texte « +N ◆ »).
        public void Burst(Vector3 chestPosition, int amount)
        {
            StopAllCoroutines();
            int count = CountFor(amount);
            for (int i = 0; i < pool.Count; i++)
            {
                var p = pool[i];
                bool on = i < count;
                p.t.gameObject.SetActive(on);
                if (!on) continue;
                p.t.position = chestPosition + Vector3.up * 0.6f;
                float angle = Range(0f, Mathf.PI * 2f);
                p.velocity = new Vector3(Mathf.Cos(angle) * Range(0.6f, 1.6f), Range(3.5f, 5f), Mathf.Sin(angle) * Range(0.2f, 0.8f) - 0.6f);
                p.spinAxis = Random.onUnitSphere;
                p.spin = Range(360f, 720f);
                StartCoroutine(Fly(p, Range(0f, 0.12f)));
            }
            ShowFloatingText(chestPosition, amount);
        }

        IEnumerator Fly(Piece p, float extraDelay)
        {
            // 1. Jaillissement : il monte, tourne, retombe un peu (gravité douce).
            for (float t = 0f; t < JumpTime + extraDelay; t += Time.deltaTime)
            {
                p.velocity += Vector3.down * 7f * Time.deltaTime;
                p.t.position += p.velocity * Time.deltaTime;
                p.t.Rotate(p.spinAxis, p.spin * Time.deltaTime, Space.World);
                yield return null;
            }
            // 2. Vol vers le compteur de diamants (courbe), en rapetissant.
            Vector3 start = p.t.position;
            Vector3 target = CounterWorldPosition(start);
            Vector3 control = (start + target) * 0.5f + Vector3.up * 1.5f;
            for (float t = 0f; t < FlyTime; t += Time.deltaTime)
            {
                float k = Ease.In(t / FlyTime);
                Vector3 a = Vector3.Lerp(start, control, k), b = Vector3.Lerp(control, target, k);
                p.t.position = Vector3.Lerp(a, b, k);
                p.t.localScale = Vector3.one * Mathf.Lerp(0.2f, 0.08f, k);
                p.t.Rotate(p.spinAxis, p.spin * Time.deltaTime, Space.World);
                yield return null;
            }
            p.t.localScale = Vector3.one * 0.2f;
            p.t.gameObject.SetActive(false);
        }

        /// La position 3D (devant la caméra) qui correspond à l'icône du compteur du HUD.
        Vector3 CounterWorldPosition(Vector3 from)
        {
            var cam = Camera.main;
            if (cam == null || hud == null || counter == null) return from + Vector3.up * 6f;
            var panel = hud.rootVisualElement.panel;
            Vector2 panelPos = counter.IconPanelPosition;
            float panelW = hud.rootVisualElement.layout.width, panelH = hud.rootVisualElement.layout.height;
            if (panel == null || float.IsNaN(panelPos.x) || panelW <= 0f || panelPos == Vector2.zero) return from + new Vector3(4f, 5f, 0f);
            // Panneau (origine en haut à gauche) → écran (origine en bas à gauche).
            float sx = panelPos.x / panelW * Screen.width;
            float sy = (1f - panelPos.y / panelH) * Screen.height;
            float depth = Vector3.Dot(from - cam.transform.position, cam.transform.forward);
            return cam.ScreenToWorldPoint(new Vector3(sx, sy, Mathf.Max(2f, depth * 0.6f)));
        }

        /// « +2 ◆ » en bleu diamant au-dessus du coffre (comme le « +20 » des pièces).
        void ShowFloatingText(Vector3 chestPosition, int amount)
        {
            if (hud == null || Camera.main == null) return;
            var fx = hud.rootVisualElement.Q<VisualElement>("fx-layer");
            if (fx == null || fx.panel == null) return;
            Vector2 panelPos = RuntimePanelUtils.CameraTransformWorldToPanel(fx.panel, chestPosition + new Vector3(0f, 1.6f, 0f), Camera.main);
            Vector2 local = fx.WorldToLocal(panelPos);
            var label = new Label($"+{amount} ◆") { pickingMode = PickingMode.Ignore };
            label.AddToClassList("float-text");
            label.AddToClassList("float-text--diamants");
            label.style.left = local.x;
            label.style.top = local.y;
            fx.Add(label);
            StartCoroutine(FloatRoutine(label));
        }

        static IEnumerator FloatRoutine(Label label)
        {
            const float duration = 1.1f;
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                float s = t < 0.2f ? Ease.Pop(t / 0.2f, 0.5f, 1.25f) : 1f;
                label.style.scale = new Scale(new Vector3(s, s, 1f));
                label.style.translate = new Translate(0f, -70f * Ease.Out(t / duration));
                label.style.opacity = t < duration - 0.3f ? 1f : (duration - t) / 0.3f;
                yield return null;
            }
            label.RemoveFromHierarchy();
        }

        float Range(float min, float max) => min + (float)fxRandom.NextDouble() * (max - min);

        /// Un diamant « taillé » low-poly : une table (dessus plat), une couronne, une ceinture
        /// et une pointe, 8 côtés, faces plates (chaque triangle a ses propres sommets, pour que
        /// la lumière accroche chaque facette). Hauteur ≈ 1 (mis à l'échelle 0,2 → 20 cm).
        public static Mesh BuildDiamondMesh()
        {
            const int sides = 8;
            float tableR = 0.5f, tableY = 0.35f, girdleR = 1f, girdleY = 0.12f, tipY = -0.75f;
            var verts = new List<Vector3>();
            var tris = new List<int>();
            void Tri(Vector3 a, Vector3 b, Vector3 c) { int i = verts.Count; verts.Add(a); verts.Add(b); verts.Add(c); tris.Add(i); tris.Add(i + 1); tris.Add(i + 2); }
            Vector3 Ring(float r, float y, int k) { float a = k * Mathf.PI * 2f / sides; return new Vector3(Mathf.Cos(a) * r, y, Mathf.Sin(a) * r); }
            var top = new Vector3(0f, tableY, 0f);
            var tip = new Vector3(0f, tipY, 0f);
            for (int k = 0; k < sides; k++)
            {
                Vector3 t0 = Ring(tableR, tableY, k), t1 = Ring(tableR, tableY, k + 1);
                Vector3 g0 = Ring(girdleR, girdleY, k), g1 = Ring(girdleR, girdleY, k + 1);
                Tri(top, t1, t0);       // la table
                Tri(t0, t1, g1);        // la couronne (2 triangles)
                Tri(t0, g1, g0);
                Tri(g0, g1, tip);       // le pavillon (vers la pointe)
            }
            var mesh = new Mesh { name = "Diamant taillé (code)" };
            mesh.SetVertices(verts);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
