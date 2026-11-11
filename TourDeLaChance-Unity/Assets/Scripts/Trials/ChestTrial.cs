using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TourDeLaChance
{
    /// <summary>
    /// L'épreuve des coffres : 3 à 6 coffres (amélioration « Coffre en plus », v4 : jusqu'à 6),
    /// et TOUJOURS un seul piégé, tiré au hasard parmi les coffres présents à chaque étage.
    /// Le joueur clique sur un coffre (ou appuie sur 1 à 6, de gauche à droite).
    ///
    /// Elle déroule aussi la chronologie de l'ouverture (suspense, ouverture, révélation),
    /// toujours IDENTIQUE pour un trésor et pour un piège de la même rareté de coffre
    /// (« le spectacle ne triche jamais ») : la seule différence est la couleur révélée, et
    /// elle arrive à la même seconde. La rareté et le nombre de coffres sont connus AVANT le
    /// choix : les effets qui en dépendent ne révèlent rien.
    /// Elle prévient l'affichage par des événements (survol, suspense, révélation…).
    /// </summary>
    public class ChestTrial : MonoBehaviour
    {
        /// La scène contient 6 coffres (v4 §10.4) ; ceux en trop sont désactivés à chaque étage.
        public const int MaxChests = 6;

        [SerializeField] Chest[] chests;

        /// Prévient quand le joueur a fait son choix (pour bloquer l'interface).
        public event Action ChoiceMade;
        /// Prévient quand l'épreuve est terminée : true = gagné, false = piégé.
        public event Action<bool> Finished;

        // ---------- Événements d'affichage (ne changent rien aux règles) ----------
        /// Le coffre survolé a changé (index, ou −1 = aucun).
        public event Action<int> HoverChanged;
        /// Le suspense commence : (coffre choisi, durée en secondes).
        public event Action<int, float> SuspenseStarted;
        /// Le couvercle du coffre choisi s'ouvre (fin du suspense).
        public event Action<int> PickedLidOpening;
        /// Les autres couvercles s'ouvrent (une seule fois pour tous).
        public event Action OthersLidsOpening;
        /// Un coffre montre son contenu : (index, piégé ?, choisi par le joueur ?).
        public event Action<int, bool, bool> ChestRevealed;
        /// Des couvercles ouverts se referment en douceur (ouverture d'un panneau).
        public event Action LidsClosing;

        /// Quand c'est vrai, les coffres ignorent souris et clavier (panneau ouvert, transition).
        /// C'est le GameManager qui décide de bloquer ; le piège déjà tiré ne change pas.
        public bool InputLocked { get; set; }

        /// Peut-on choisir un coffre en ce moment ? (pour l'affichage : invitation au clic)
        public bool AcceptsChoice => waitingForChoice && !InputLocked;

        /// Le nombre de coffres PRÉSENTS à cet étage (3 à 6).
        public int ChestCount => activeCount;
        public Chest GetChest(int index) => chests[index];
        /// La rareté des coffres de l'étage (0 = Bois … 4 = Diamant).
        public int RarityRank { get; private set; }
        /// Le coffre choisi en dernier (−1 avant le premier choix de l'étage).
        public int LastPicked { get; private set; } = -1;
        public int HoveredIndex { get; private set; } = -1;
        /// OUTIL DE TEST : le coffre piégé (jamais lu par l'affichage).
        public int DebugTrapIndex => trapIndex;

        int trapIndex;
        int floor = 1;
        int activeCount = 3;
        bool waitingForChoice;

        // Ces durées viennent de la spec (§2 et §3.4) ; elles sont les mêmes pour tous.
        const float PauseBeforeOthers = 0.3f;   // après le rebond du coffre choisi
        const float PauseBeforeFinished = 0.7f; // le temps de voir les autres coffres

        // Avant le premier étage (écran titre) : 3 coffres en bois, à leurs places habituelles.
        void Awake()
        {
            float[] xs = PositionsFor(3);
            for (int i = 0; i < chests.Length; i++)
            {
                chests[i].gameObject.SetActive(i < 3);
                if (i < 3) chests[i].transform.localPosition = new Vector3(xs[i], 0f, 0f);
            }
        }

        /// Placement des coffres selon leur nombre (spec v3 §5.4, v4 §10.4) : positions x et
        /// échelle. 6 coffres : écartés de 1,5 m, échelle 0,75 (1,125 m de large, 0,375 m d'écart).
        static readonly float[][] Positions =
        {
            new[] { -2.6f, 0f, 2.6f },
            new[] { -3.3f, -1.1f, 1.1f, 3.3f },
            new[] { -3.6f, -1.8f, 0f, 1.8f, 3.6f },
            new[] { -3.75f, -2.25f, -0.75f, 0.75f, 2.25f, 3.75f }
        };
        public static float[] PositionsFor(int count) => Positions[Mathf.Clamp(count, 3, MaxChests) - 3];
        public static float ScaleFor(int count) => count >= 6 ? 0.75f : count >= 5 ? 0.85f : 1f;

        /// Durée du suspense (§3.3 et v3 §5.3.2) : 0,6 s + 0,08 s par étage au-dessus du 1er
        /// (au plus 1 s), plus 0,1 s par rang de rareté de coffre ; le tout plafonné à 1,4 s.
        /// Elle ne dépend QUE de l'étage et de la rareté (connue avant le choix), jamais du contenu.
        public static float SuspenseDurationFor(int floor, int rarityRank = 0) =>
            Mathf.Min(1.4f, Mathf.Min(1f, 0.6f + 0.08f * (floor - 1)) + 0.1f * rarityRank);

        /// Tire le coffre piégé parmi les "count" coffres présents (chacun a la même chance).
        /// C'est le SEUL tirage du piège ; la simulation des tests utilise la même fonction.
        public static int DrawTrap(int count, System.Random random) => random.Next(0, count);

        /// Prépare un nouvel étage : les bons coffres à leur place, fermés, de la couleur de
        /// leur rareté, et un nouveau piège caché parmi eux.
        public void Begin(int floorNumber, int count, int rarityRank, System.Random random)
        {
            floor = floorNumber;
            activeCount = Mathf.Clamp(count, 1, chests.Length);
            RarityRank = rarityRank;
            float[] xs = PositionsFor(activeCount);
            float scale = ScaleFor(activeCount);
            var rarity = Upgrades.Rarity(rarityRank);
            for (int i = 0; i < chests.Length; i++)
            {
                bool present = i < activeCount;
                chests[i].gameObject.SetActive(present);
                chests[i].Index = i;
                if (!present) continue;
                var t = chests[i].transform;
                t.localPosition = new Vector3(i < xs.Length ? xs[i] : 0f, t.localPosition.y, t.localPosition.z);
                t.localScale = Vector3.one * scale;
                chests[i].SetRarity(rarityRank, rarity != null ? rarity.bodyColor : (Color?)null);
                chests[i].ResetChest();
            }
            // LE HASARD : c'est le SEUL tirage qui compte pour le piège ; les effets visuels
            // utilisent d'autres générateurs.
            trapIndex = DrawTrap(activeCount, random);
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
        /// silent = ne prévient pas (pas de son coffre_ferme, ex. pendant le remélange).
        public void CloseAllLids(bool animated, bool silent = false)
        {
            bool anyOpen = false;
            for (int i = 0; i < activeCount; i++)
            {
                var chest = chests[i];
                if (chest.IsLidOpen) anyOpen = true;
                if (animated) chest.CloseAnimated();
                else chest.ResetChest();
            }
            if (animated && anyOpen && !silent) LidsClosing?.Invoke();
        }

        public void Choose(int index)
        {
            if (InputLocked || !waitingForChoice || index < 0 || index >= activeCount) return;
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
            float suspense = SuspenseDurationFor(floor, RarityRank);
            // Tremblement final : 5° + 1° par rang ; le Diamant saute juste avant de s'ouvrir.
            float shake = 5f + RarityRank;
            bool hopAtEnd = RarityRank >= 4;

            // 1. Suspense : le coffre choisi tremble, les autres s'effacent un peu.
            SuspenseStarted?.Invoke(picked, suspense);
            for (int i = 0; i < activeCount; i++)
                if (i != picked) chests[i].StepBack();
            yield return chests[picked].Suspense(suspense, shake, hopAtEnd);

            // 2. Ouverture avec rebond ; la couleur est révélée à la fin de l'ouverture.
            PickedLidOpening?.Invoke(picked);
            yield return chests[picked].OpenPicked(!won, () => ChestRevealed?.Invoke(picked, !won, true));

            // 3. Les autres coffres s'ouvrent ensemble, sans suspense.
            yield return new WaitForSeconds(PauseBeforeOthers);
            OthersLidsOpening?.Invoke();
            for (int i = 0; i < activeCount; i++)
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

            // Raccourcis clavier 1 à 6, seulement pour les coffres présents.
            var kb = Keyboard.current;
            if (kb != null)
            {
                Key[] keys = { Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5, Key.Digit6 };
                for (int i = 0; i < activeCount && i < keys.Length; i++)
                    if (kb[keys[i]].wasPressedThisFrame) { Choose(i); return; }
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
                if (chest != null && chest.Index < activeCount) under = chest.Index;
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
