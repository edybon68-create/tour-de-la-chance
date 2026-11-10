using UnityEngine;

namespace TourDeLaChance
{
    /// <summary>
    /// Le jaillissement de pièces d'un trésor (spec §3.5). Pur affichage.
    ///
    /// Les 30 pièces sont créées UNE fois au démarrage (une "réserve", ou pool) puis cachées
    /// et réutilisées : créer et détruire des objets en boucle fait ramer le jeu.
    /// Elles n'ont pas de collider : elles ne gênent jamais le clic sur les coffres.
    /// </summary>
    public class CoinBurst : MonoBehaviour
    {
        [SerializeField] Material coinMaterial; // M_Or
        const int PoolSize = 40; // v3 : jusqu'à 40 pièces pour un coffre de diamant
        /// Le plus grand nombre de pièces d'un jaillissement.
        public const int MaxCoins = PoolSize;
        const float Gravity = 9.81f;
        const float MaxLife = 1.2f;
        const float ShrinkTime = 0.15f;
        static readonly Vector3 CoinScale = new Vector3(0.22f, 0.025f, 0.22f);

        /// Une pièce de la réserve et son mouvement.
        class Coin
        {
            public Transform transform;
            public Vector3 velocity;
            public Vector3 spinAxis;
            public float spinSpeed;
            public float age;
            public bool active;
        }

        readonly Coin[] coins = new Coin[PoolSize];

        // Générateur de hasard SÉPARÉ de celui du piège : aucun effet visuel
        // ne peut influencer le tirage du coffre piégé.
        readonly System.Random random = new System.Random();

        /// Combien de pièces sont encore visibles (pour les tests).
        public int ActiveCount
        {
            get
            {
                int n = 0;
                foreach (var c in coins) if (c != null && c.active) n++;
                return n;
            }
        }

        void Awake()
        {
            for (int i = 0; i < PoolSize; i++)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                go.name = "Piece_" + i;
                Destroy(go.GetComponent<Collider>()); // pas de collider
                go.transform.SetParent(transform, false);
                go.transform.localScale = CoinScale;
                if (coinMaterial != null) go.GetComponent<Renderer>().sharedMaterial = coinMaterial;
                go.SetActive(false);
                coins[i] = new Coin { transform = go.transform };
            }
        }

        /// Nombre de pièces pour un étage : 8 + 2 × étage, maximum 30 (v2, inchangé).
        public static int CountForFloor(int floor) => Mathf.Min(30, 8 + 2 * floor);

        /// Fait jaillir "count" pièces depuis le centre d'un coffre.
        public void Burst(Vector3 chestPosition, int count)
        {
            Vector3 start = new Vector3(chestPosition.x, 0.9f, chestPosition.z);
            int launched = 0;
            foreach (var coin in coins)
            {
                if (launched >= count) break;
                if (coin.active) continue;
                launched++;

                // Vitesse : vers le haut entre 3,5 et 5 m/s, sur les côtés dans un disque de 1,2 m/s.
                float up = Range(3.5f, 5f);
                float angle = Range(0f, Mathf.PI * 2f);
                float radius = 1.2f * Mathf.Sqrt(Range(0f, 1f)); // racine : répartition uniforme dans le disque
                coin.velocity = new Vector3(Mathf.Cos(angle) * radius, up, Mathf.Sin(angle) * radius);
                coin.spinAxis = new Vector3(Range(-1f, 1f), Range(-1f, 1f), Range(-1f, 1f)).normalized;
                if (coin.spinAxis == Vector3.zero) coin.spinAxis = Vector3.up;
                coin.spinSpeed = Range(540f, 900f);
                coin.age = 0f;
                coin.active = true;
                coin.transform.position = start;
                coin.transform.rotation = Quaternion.identity;
                coin.transform.localScale = CoinScale;
                coin.transform.gameObject.SetActive(true);
            }
        }

        void Update()
        {
            float dt = Time.deltaTime;
            foreach (var coin in coins)
            {
                if (coin == null || !coin.active) continue;
                coin.age += dt;
                coin.velocity.y -= Gravity * dt;                 // la gravité tire vers le bas
                coin.transform.position += coin.velocity * dt;   // on avance
                coin.transform.Rotate(coin.spinAxis, coin.spinSpeed * dt, Space.World);

                // Fin : la pièce redescend sous y = 0,6, ou elle a vécu 1,2 s.
                bool falling = coin.velocity.y < 0f;
                float endAge = MaxLife;
                if (falling && coin.transform.position.y < 0.6f) endAge = coin.age; // fin maintenant
                // Elle rétrécit pendant ses 0,15 dernières secondes (on prévoit sa fin
                // d'après la durée maximale ; si elle tombe plus tôt, elle disparaît).
                float shrink = Mathf.Clamp01((MaxLife - coin.age) / ShrinkTime);
                if (falling)
                {
                    // Temps restant avant de passer sous y = 0,6 (approximation simple).
                    float height = coin.transform.position.y - 0.6f;
                    float speed = -coin.velocity.y;
                    float timeLeft = speed > 0.01f ? height / speed : 1f;
                    shrink = Mathf.Min(shrink, Mathf.Clamp01(timeLeft / ShrinkTime));
                }
                coin.transform.localScale = CoinScale * shrink;

                if (coin.age >= endAge || coin.age >= MaxLife)
                {
                    coin.active = false;
                    coin.transform.gameObject.SetActive(false);
                }
            }
        }

        float Range(float min, float max) => min + (float)random.NextDouble() * (max - min);
    }
}
