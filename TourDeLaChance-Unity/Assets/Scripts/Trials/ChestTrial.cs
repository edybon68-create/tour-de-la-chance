using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TourDeLaChance
{
    /// <summary>
    /// L'épreuve des 3 coffres : un seul est piégé, tiré au hasard à chaque étage.
    /// Le joueur clique sur un coffre (ou appuie sur 1, 2, 3).
    ///
    /// Elle déroule aussi la chronologie de l'ouverture (suspense, ouverture, révélation),
    /// toujours IDENTIQUE pour un trésor et pour un piège (« le spectacle ne triche jamais ») :
    /// la seule différence est la couleur révélée, et elle arrive à la même seconde.
    /// Elle prévient l'affichage par des événements (survol, suspense, révélation…).
    /// </summary>
    public class ChestTrial : MonoBehaviour
    {
        [SerializeField] Chest[] chests;

        /// Prévient quand le joueur a fait son choix (pour bloquer l'interface).
        public event Action ChoiceMade;
        /// Prévient quand l'épreuve est terminée : true = gagné, false = piégé.
        public event Action<bool> Finished;

        // ---------- Événements d'affichage (ne changent rien aux règles) ----------
        /// Le coffre survolé a changé (index 0, 1, 2, ou −1 = aucun).
        public event Action<int> HoverChanged;
        /// Le suspense commence : (coffre choisi, durée en secondes).
        public event Action<int, float> SuspenseStarted;
        /// Le couvercle du coffre choisi s'ouvre (fin du suspense).
        public event Action<int> PickedLidOpening;
        /// Les deux autres couvercles s'ouvrent (une seule fois pour les deux).
        public event Action OthersLidsOpening;
        /// Un coffre montre son contenu : (index, piégé ?, choisi par le joueur ?).
        public event Action<int, bool, bool> ChestRevealed;
        /// Des couvercles ouverts se referment en douceur (ouverture du shop).
        public event Action LidsClosing;

        /// Quand c'est vrai, les coffres ignorent souris et clavier (shop ouvert, transition).
        /// C'est le GameManager qui décide de bloquer ; le piège déjà tiré ne change pas.
        public bool InputLocked { get; set; }

        /// Peut-on choisir un coffre en ce moment ? (pour l'affichage : invitation au clic)
        public bool AcceptsChoice => waitingForChoice && !InputLocked;

        public int ChestCount => chests.Length;
        public Chest GetChest(int index) => chests[index];
        /// Le coffre choisi en dernier (−1 avant le premier choix de l'étage).
        public int LastPicked { get; private set; } = -1;
        public int HoveredIndex { get; private set; } = -1;

        int trapIndex;
        int floor = 1;
        bool waitingForChoice;

        // Ces durées viennent de la spec (§2 et §3.4) ; elles sont les mêmes pour tous.
        const float PauseBeforeOthers = 0.3f;   // après le rebond du coffre choisi
        const float PauseBeforeFinished = 0.7f; // le temps de voir les deux autres coffres

        /// Durée du suspense (§3.3) : 0,6 s + 0,08 s par étage au-dessus du 1er, plafonnée à 1 s.
        /// Elle ne dépend QUE de l'étage, jamais du contenu du coffre.
        public static float SuspenseDurationFor(int floor) =>
            Mathf.Min(1f, 0.6f + 0.08f * (floor - 1));

        /// Prépare un nouvel étage : coffres fermés, nouveau piège caché.
        public void Begin(int floorNumber)
        {
            floor = floorNumber;
            for (int i = 0; i < chests.Length; i++)
            {
                chests[i].Index = i;
                chests[i].ResetChest();
            }
            // LE HASARD : Random.Range(0, 3) donne 0, 1 ou 2 (le 3 est exclu).
            // C'est le SEUL tirage qui compte ; les effets visuels utilisent un autre générateur.
            trapIndex = UnityEngine.Random.Range(0, chests.Length);
            LastPicked = -1;
            waitingForChoice = true;
            SetHovered(-1);
        }

        /// Plus aucun coffre ne peut être choisi à cet étage (le joueur a encaissé).
        public void CancelChoice()
        {
            waitingForChoice = false;
            SetHovered(-1);
        }

        /// Referme tous les coffres (visuel uniquement : le piège ne change pas).
        /// Utile en fin de partie pour dégager la vue sur le personnage.
        public void CloseAllLids(bool animated)
        {
            bool anyOpen = false;
            foreach (var chest in chests)
            {
                if (chest.IsLidOpen) anyOpen = true;
                if (animated) chest.CloseAnimated();
                else chest.ResetChest();
            }
            if (animated && anyOpen) LidsClosing?.Invoke();
        }

        public void Choose(int index)
        {
            if (InputLocked || !waitingForChoice || index < 0 || index >= chests.Length) return;
            waitingForChoice = false;
            LastPicked = index;
            SetHovered(-1);
            ChoiceMade?.Invoke();
            StartCoroutine(Reveal(index));
        }

        /// La chronologie d'un coffre ouvert (spec §2). Rien ici ne dépend de "won"
        /// avant la révélation : même suspense, même durée, mêmes sons.
        IEnumerator Reveal(int picked)
        {
            bool won = picked != trapIndex;
            float suspense = SuspenseDurationFor(floor);

            // 1. Suspense : le coffre choisi tremble, les deux autres s'effacent un peu.
            SuspenseStarted?.Invoke(picked, suspense);
            for (int i = 0; i < chests.Length; i++)
                if (i != picked) chests[i].StepBack();
            yield return chests[picked].Suspense(suspense);

            // 2. Ouverture avec rebond ; la couleur est révélée à la fin de l'ouverture.
            PickedLidOpening?.Invoke(picked);
            yield return chests[picked].OpenPicked(!won, () => ChestRevealed?.Invoke(picked, !won, true));

            // 3. Les deux autres coffres s'ouvrent ensemble, sans suspense.
            yield return new WaitForSeconds(PauseBeforeOthers);
            OthersLidsOpening?.Invoke();
            for (int i = 0; i < chests.Length; i++)
            {
                if (i == picked) continue;
                int index = i; // copie locale pour la fonction "() => …" ci-dessous
                bool isTrap = index == trapIndex;
                StartCoroutine(chests[index].OpenOther(isTrap, () => ChestRevealed?.Invoke(index, isTrap, false)));
            }
            yield return new WaitForSeconds(PauseBeforeFinished);

            Finished?.Invoke(won);
        }

        void Update()
        {
            // Un panneau UI Toolkit n'arrête pas le rayon physique ni le clavier :
            // il faut donc ce blocage explicite. Pendant un blocage, pas de survol non plus.
            if (!waitingForChoice || InputLocked)
            {
                SetHovered(-1);
                return;
            }

            // Raccourcis clavier 1, 2, 3 (pas de survol : ils choisissent directement).
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.digit1Key.wasPressedThisFrame) { Choose(0); return; }
                if (kb.digit2Key.wasPressedThisFrame) { Choose(1); return; }
                if (kb.digit3Key.wasPressedThisFrame) { Choose(2); return; }
            }

            var mouse = Mouse.current;
            var cam = Camera.main;
            if (mouse == null || cam == null) return;

            // On lance un "rayon" depuis la caméra vers le curseur et on regarde quel
            // coffre il touche. Le même rayon sert au survol et au clic.
            int under = -1;
            Ray ray = cam.ScreenPointToRay(mouse.position.ReadValue());
            if (Physics.Raycast(ray, out RaycastHit hit, 100f))
            {
                var chest = hit.collider.GetComponentInParent<Chest>();
                if (chest != null) under = chest.Index;
            }
            SetHovered(under);

            if (mouse.leftButton.wasPressedThisFrame && under >= 0) Choose(under);
        }

        void SetHovered(int index)
        {
            if (index == HoveredIndex) return;
            if (HoveredIndex >= 0 && HoveredIndex < chests.Length) chests[HoveredIndex].SetHovered(false);
            HoveredIndex = index;
            if (index >= 0) chests[index].SetHovered(true);
            HoverChanged?.Invoke(index);
        }
    }
}
