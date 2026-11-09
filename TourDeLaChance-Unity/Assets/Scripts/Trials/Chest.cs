using System.Collections;
using UnityEngine;

namespace TourDeLaChance
{
    /// <summary>
    /// Un coffre. Il sait se refermer, s'ouvrir avec une petite animation,
    /// et changer de couleur selon son contenu : or = trésor, rouge = piège.
    /// </summary>
    public class Chest : MonoBehaviour
    {
        [SerializeField] Transform lid;     // le couvercle (son pivot est à l'arrière, comme une charnière)
        [SerializeField] Renderer body;     // le corps du coffre, qui change de couleur
        [SerializeField] Color closedColor = new Color(0.55f, 0.35f, 0.2f);
        [SerializeField] Color treasureColor = new Color(1f, 0.8f, 0.1f);
        [SerializeField] Color trapColor = new Color(0.85f, 0.1f, 0.1f);

        public int Index { get; set; }

        // "_BaseColor" est le nom de la couleur dans le shader URP/Lit.
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        MaterialPropertyBlock block;

        void Awake() => block = new MaterialPropertyBlock();

        /// Remet le coffre fermé, couleur bois.
        public void ResetChest()
        {
            StopAllCoroutines();
            lid.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            SetColor(closedColor);
        }

        /// Ouvre le coffre. C'est une "coroutine" : une fonction qui s'étale sur
        /// plusieurs images (frames) grâce à "yield return null" = "attends l'image suivante".
        public IEnumerator Open(bool isTrap, float duration = 0.4f)
        {
            Quaternion closed = Quaternion.identity;
            Quaternion open = Quaternion.Euler(110f, 0f, 0f);

            for (float t = 0f; t < 1f; t += Time.deltaTime / duration)
            {
                float eased = 1f - (1f - t) * (1f - t); // démarre vite, finit en douceur
                lid.localRotation = Quaternion.Slerp(closed, open, eased);
                yield return null;
            }
            lid.localRotation = open;
            SetColor(isTrap ? trapColor : treasureColor);

            // Petit "pop" : le coffre grossit puis revient à sa taille.
            for (float t = 0f; t < 1f; t += Time.deltaTime / 0.2f)
            {
                transform.localScale = Vector3.one * (1f + 0.15f * Mathf.Sin(t * Mathf.PI));
                yield return null;
            }
            transform.localScale = Vector3.one;
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
