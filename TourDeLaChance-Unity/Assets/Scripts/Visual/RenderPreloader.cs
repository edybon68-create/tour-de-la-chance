using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace TourDeLaChance
{
    /// <summary>
    /// Le PRÉCHARGEMENT de l'affichage (spec v5 §5.2 et §6.7). Pur affichage.
    ///
    /// Pourquoi ? Un objet désactivé depuis le lancement n'a jamais été dessiné : la première
    /// fois qu'il apparaît, Unity doit envoyer ses maillages à la carte graphique et préparer
    /// (compiler) les variantes de shaders de ses matériaux. Cela peut figer l'image, ou, dans
    /// l'éditeur, laisser l'objet invisible le temps de la compilation (c'était le « retard »
    /// des coffres 4 à 6 et des coffres précieux).
    ///
    /// La solution : au démarrage, on dessine UNE fois tous ces objets, d'un coup, avec une
    /// petite caméra cachée qui dessine dans une image de 64 × 64 que personne ne voit. Les
    /// objets sont activés juste avant ce dessin et remis comme avant juste après, dans la
    /// MÊME fonction : aucune image du jeu ne les montre.
    /// </summary>
    public static class RenderPreloader
    {
        /// Dessine une fois les objets donnés (même désactivés), puis remet chacun dans son état
        /// d'activation d'origine. La caméra cachée cadre automatiquement tous ces objets.
        public static void RenderOnce(IList<GameObject> objects)
        {
            if (objects == null || objects.Count == 0) return;

            // 1. On retient l'état de chaque objet et on active tout (parents compris).
            var states = new List<(GameObject go, bool active)>();
            foreach (var go in objects)
            {
                if (go == null) continue;
                for (var t = go.transform; t != null; t = t.parent)
                    states.Add((t.gameObject, t.gameObject.activeSelf));
            }
            foreach (var (go, _) in states) go.SetActive(true);
            var bounds = BoundsOf(objects); // calculée APRÈS l'activation (sinon elle est vide)

            // 2. Une caméra temporaire qui regarde la zone, et une petite image cible.
            var camGo = new GameObject("PrechargementCamera") { hideFlags = HideFlags.HideAndDontSave };
            var cam = camGo.AddComponent<Camera>();
            cam.enabled = false; // on la déclenche nous-mêmes, une seule fois
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            cam.cullingMask = ~0;
            float radius = Mathf.Max(0.5f, bounds.extents.magnitude);
            cam.fieldOfView = 60f;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = radius * 6f + 10f;
            camGo.transform.position = bounds.center + new Vector3(0f, radius * 0.5f, -radius * 2.2f);
            camGo.transform.LookAt(bounds.center);
            var rt = RenderTexture.GetTemporary(64, 64, 24);
            cam.targetTexture = rt;

            // 3. Le dessin, tout de suite (pas à la prochaine image).
            var request = new RenderPipeline.StandardRequest { destination = rt };
            if (RenderPipeline.SupportsRenderRequest(cam, request)) RenderPipeline.SubmitRenderRequest(cam, request);
            else cam.Render();

            // 4. On range tout comme avant (dans l'ordre inverse : les enfants avant les parents).
            cam.targetTexture = null;
            RenderTexture.ReleaseTemporary(rt);
            Object.DestroyImmediate(camGo);
            for (int i = states.Count - 1; i >= 0; i--) states[i].go.SetActive(states[i].active);
        }

        /// La boîte englobante de tous les objets dessinables (pour cadrer la caméra cachée).
        public static Bounds BoundsOf(IList<GameObject> objects)
        {
            bool any = false;
            var b = new Bounds();
            foreach (var go in objects)
            {
                if (go == null) continue;
                foreach (var r in go.GetComponentsInChildren<Renderer>(true))
                {
                    if (!any) { b = r.bounds; any = true; }
                    else b.Encapsulate(r.bounds);
                }
                if (!any) { b = new Bounds(go.transform.position, Vector3.one); any = true; }
            }
            return b;
        }
    }
}
