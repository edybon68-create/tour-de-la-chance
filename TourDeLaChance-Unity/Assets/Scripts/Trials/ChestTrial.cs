using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TourDeLaChance
{
    /// <summary>
    /// L'épreuve des 3 coffres : un seul est piégé, tiré au hasard à chaque étage.
    /// Le joueur clique sur un coffre (ou appuie sur 1, 2, 3).
    /// </summary>
    public class ChestTrial : MonoBehaviour
    {
        [SerializeField] Chest[] chests;

        /// Prévient quand le joueur a fait son choix (pour bloquer l'interface).
        public event Action ChoiceMade;
        /// Prévient quand l'épreuve est terminée : true = gagné, false = piégé.
        public event Action<bool> Finished;

        /// Quand c'est vrai, les coffres ignorent souris et clavier (ex. : shop ouvert).
        /// C'est le GameManager qui décide de bloquer ; le piège déjà tiré ne change pas.
        public bool InputLocked { get; set; }

        int trapIndex;
        bool waitingForChoice;

        /// Prépare un nouvel étage : coffres fermés, nouveau piège caché.
        public void Begin()
        {
            for (int i = 0; i < chests.Length; i++)
            {
                chests[i].Index = i;
                chests[i].ResetChest();
            }
            // LE HASARD : Random.Range(0, 3) donne 0, 1 ou 2 (le 3 est exclu).
            trapIndex = UnityEngine.Random.Range(0, chests.Length);
            waitingForChoice = true;
        }

        /// Referme tous les coffres (visuel uniquement : le piège ne change pas).
        /// Utile en fin de partie pour dégager la vue sur le personnage.
        public void CloseAllLids()
        {
            foreach (var chest in chests) chest.ResetChest();
        }

        public void Choose(int index)
        {
            if (InputLocked || !waitingForChoice || index < 0 || index >= chests.Length) return;
            waitingForChoice = false;
            ChoiceMade?.Invoke();
            StartCoroutine(Reveal(index));
        }

        IEnumerator Reveal(int picked)
        {
            bool won = picked != trapIndex;

            // 1. On ouvre le coffre choisi… suspense !
            yield return chests[picked].Open(isTrap: !won);
            yield return new WaitForSeconds(0.3f);

            // 2. On révèle les autres pour montrer où était le piège.
            for (int i = 0; i < chests.Length; i++)
                if (i != picked) StartCoroutine(chests[i].Open(isTrap: i == trapIndex));
            yield return new WaitForSeconds(0.7f);

            Finished?.Invoke(won);
        }

        void Update()
        {
            // Un panneau UI Toolkit n'arrête pas le rayon physique ni le clavier :
            // il faut donc ce blocage explicite.
            if (!waitingForChoice || InputLocked) return;

            // Raccourcis clavier 1, 2, 3
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.digit1Key.wasPressedThisFrame) { Choose(0); return; }
                if (kb.digit2Key.wasPressedThisFrame) { Choose(1); return; }
                if (kb.digit3Key.wasPressedThisFrame) { Choose(2); return; }
            }

            // Clic souris : on lance un "rayon" depuis la caméra vers le curseur
            // et on regarde quel objet 3D il touche.
            var mouse = Mouse.current;
            if (mouse == null || !mouse.leftButton.wasPressedThisFrame) return;

            Ray ray = Camera.main.ScreenPointToRay(mouse.position.ReadValue());
            if (Physics.Raycast(ray, out RaycastHit hit, 100f))
            {
                var chest = hit.collider.GetComponentInParent<Chest>();
                if (chest != null) Choose(chest.Index);
            }
        }
    }
}
