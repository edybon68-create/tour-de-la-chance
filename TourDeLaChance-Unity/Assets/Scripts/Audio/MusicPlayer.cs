using UnityEngine;

namespace TourDeLaChance
{
    /// <summary>
    /// La musique (spec §5.2, §6.1, §6.2) : l'ambiance et la couche « tension » font 48 s,
    /// bouclent, et doivent rester CALÉES l'une sur l'autre. On les démarre donc au même
    /// instant précis avec PlayScheduled (l'horloge audio, plus précise que les images).
    ///
    /// - Ambiance : volume 0,35, fondu d'entrée de 1,5 s au lancement.
    /// - Tension : 0,35 × niveau de l'étage (0 à 0,8), qui change en fondu de 1 s.
    /// - Baisse (« ducking ») : × 0,5 en 0,05 s pendant un jingle, remonte en 0,5 s.
    /// - « Musique : non » rend les deux muettes SANS les arrêter : elles continuent de
    ///   tourner en silence et restent calées.
    /// </summary>
    public class MusicPlayer : MonoBehaviour
    {
        [SerializeField] SoundBank bank;
        const float BaseVolume = 0.35f;
        const float FadeInTime = 1.5f;
        const float TensionFadeTime = 1f;

        AudioSource ambient, tension;
        double startDsp;
        float tensionLevel, tensionFrom, tensionTo, tensionTime = 1f;
        float duck = 1f, duckUntil = -1f;
        float nextSyncCheck;
        const int MaxDriftSamples = 256; // ≈ 6 ms à 44,1 kHz

        /// Le niveau de tension visé (pour les tests).
        public float TensionTarget => tensionTo;
        public float TensionLevel => tensionLevel;
        public float DuckFactor => duck;
        public AudioSource Ambient => ambient;
        public AudioSource Tension => tension;

        /// Niveau de la couche tension selon l'étage (§6.1) : 1 → 0 ; 2 → 0,25 ; 3 → 0,45 ;
        /// 4 → 0,65 ; 5 et plus → 0,80.
        public static float TensionLevelFor(int floor)
        {
            if (floor <= 1) return 0f;
            if (floor == 2) return 0.25f;
            if (floor == 3) return 0.45f;
            if (floor == 4) return 0.65f;
            return 0.8f;
        }

        void Awake()
        {
            ambient = CreateSource(bank != null ? bank.ambientMusic : null);
            tension = CreateSource(bank != null ? bank.tensionMusic : null);
            ApplyMute(); // le réglage mémorisé s'applique AVANT le premier son
        }

        AudioSource CreateSource(AudioClip clip)
        {
            var s = gameObject.AddComponent<AudioSource>();
            s.clip = clip;
            s.loop = true;
            s.playOnAwake = false;
            s.spatialBlend = 0f;
            s.volume = 0f;
            return s;
        }

        void OnEnable() => SoundSettings.Changed += ApplyMute;
        void OnDisable() => SoundSettings.Changed -= ApplyMute;

        void Start()
        {
            // Les deux pistes démarrent au même instant, 0,2 s dans le futur
            // (le temps que tout soit prêt).
            startDsp = AudioSettings.dspTime + 0.2;
            if (ambient.clip != null) ambient.PlayScheduled(startDsp);
            if (tension.clip != null) tension.PlayScheduled(startDsp);
        }

        void Update()
        {
            // Fondu d'entrée de l'ambiance.
            float sinceStart = (float)(AudioSettings.dspTime - startDsp);
            float fadeIn = Mathf.Clamp01(sinceStart / FadeInTime);

            // Couche tension : fondu de 1 s vers le niveau visé.
            tensionTime += Time.unscaledDeltaTime / TensionFadeTime;
            tensionLevel = Mathf.Lerp(tensionFrom, tensionTo, Mathf.Clamp01(tensionTime));

            // Baisse pendant un jingle : descend vite (0,05 s), remonte doucement (0,5 s).
            bool ducked = Time.unscaledTime < duckUntil;
            duck = ducked
                ? Mathf.MoveTowards(duck, 0.5f, Time.unscaledDeltaTime * 0.5f / 0.05f)
                : Mathf.MoveTowards(duck, 1f, Time.unscaledDeltaTime * 0.5f / 0.5f);

            ambient.volume = BaseVolume * fadeIn * duck;
            tension.volume = BaseVolume * tensionLevel * duck;

            KeepInSync();
        }

        /// Sécurité : quand une piste est muette (volume 0 ou « Musique : non »), Unity peut
        /// la mettre en pause « virtuelle » pour économiser du calcul ; en revenant, elle peut
        /// reprendre légèrement décalée. Deux fois par seconde, on vérifie que les deux pistes
        /// sont au même endroit, et sinon on recale la tension sur l'ambiance.
        void KeepInSync()
        {
            if (Time.unscaledTime < nextSyncCheck) return;
            nextSyncCheck = Time.unscaledTime + 0.5f;
            if (!ambient.isPlaying || !tension.isPlaying || ambient.clip == null || tension.clip == null) return;

            int length = Mathf.Min(ambient.clip.samples, tension.clip.samples);
            int drift = Mathf.Abs(ambient.timeSamples - tension.timeSamples);
            drift = Mathf.Min(drift, length - drift); // autour du raccord de boucle, 1 et 47,99 s sont voisins
            if (drift > MaxDriftSamples) tension.timeSamples = ambient.timeSamples;
        }

        /// Écart entre les deux pistes, en échantillons (pour les tests).
        public int DriftSamples => ambient != null && tension != null ? ambient.timeSamples - tension.timeSamples : 0;

        /// Change le niveau de la couche tension (0 à 1), en fondu de 1 s.
        public void SetTension(float level)
        {
            if (Mathf.Approximately(level, tensionTo)) return;
            tensionFrom = tensionLevel;
            tensionTo = level;
            tensionTime = 0f;
        }

        /// Baisse la musique pendant "seconds" secondes (un jingle qui joue).
        public void Duck(float seconds)
        {
            duckUntil = Mathf.Max(duckUntil, Time.unscaledTime + seconds);
        }

        void ApplyMute()
        {
            bool mute = !SoundSettings.MusicOn;
            if (ambient != null) ambient.mute = mute;
            if (tension != null) tension.mute = mute;
        }
    }
}
