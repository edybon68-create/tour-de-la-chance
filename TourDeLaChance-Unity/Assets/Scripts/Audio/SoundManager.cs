using System.Collections.Generic;
using UnityEngine;

namespace TourDeLaChance
{
    /// <summary>
    /// Le gestionnaire des effets sonores (spec §5 et §6). N'importe quel script d'affichage
    /// peut demander : SoundManager.Instance.Play(Sfx.UiClic).
    ///
    /// - Une petite réserve de 8 AudioSource : au plus 8 effets en même temps (anti-cacophonie).
    /// - Un même son ne peut pas être relancé moins de 0,03 s après lui-même.
    /// - Les sons « variés » ont une hauteur tirée au hasard entre 0,95 et 1,05.
    /// - « Effets : non » (SoundSettings) coupe tout.
    /// - Les jingles (résultats, révélations) font baisser la musique pendant qu'ils jouent.
    ///
    /// Journal de lecture : on ne peut pas écouter pendant les tests automatiques, alors
    /// on peut activer un journal (JournalEnabled) qui note chaque son demandé, avec l'heure.
    /// </summary>
    public class SoundManager : MonoBehaviour
    {
        /// Le gestionnaire de la scène, accessible de partout (il n'y en a qu'un).
        public static SoundManager Instance { get; private set; }

        [SerializeField] SoundBank bank;
        [SerializeField] MusicPlayer music;

        const int MaxVoices = 8;
        const float MinRepeatDelay = 0.03f;

        AudioSource[] voices;
        Sfx[] voiceIds;
        readonly Dictionary<Sfx, float> lastPlayTime = new Dictionary<Sfx, float>();
        readonly System.Random random = new System.Random(); // hasard des hauteurs, sans lien avec le jeu

        /// Active le journal de lecture (pour les tests).
        public static bool JournalEnabled;
        /// Les derniers sons demandés : "12.345 ui_clic vol=0.50 pitch=1.02".
        public static readonly List<string> Journal = new List<string>();

        void Awake()
        {
            Instance = this;
            voices = new AudioSource[MaxVoices];
            voiceIds = new Sfx[MaxVoices];
            for (int i = 0; i < MaxVoices; i++)
            {
                var source = gameObject.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = 0f; // son « 2D » : même volume partout
                voices[i] = source;
            }
        }

        void OnEnable() => SoundSettings.Changed += OnSettingsChanged;
        void OnDisable() => SoundSettings.Changed -= OnSettingsChanged;

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void OnSettingsChanged()
        {
            if (SoundSettings.EffectsOn) return;
            foreach (var v in voices) v.Stop(); // « Effets : non » coupe aussi ce qui joue
        }

        /// Joue un effet. volumeScale multiplie le volume de la fiche, pitch fixe la hauteur
        /// (0 = hauteur normale, ou variée si la fiche le demande). pitchJitter ajoute une
        /// variation au hasard (ex. 0,05 = ±5 %). Renvoie la source utilisée (ou null).
        public AudioSource Play(Sfx id, float volumeScale = 1f, float pitch = 0f, float pitchJitter = 0f)
        {
            var entry = bank != null ? bank.Find(id) : null;
            if (entry == null || entry.clip == null)
            {
                Note(id, "absent de la banque");
                return null;
            }
            if (!SoundSettings.EffectsOn) { Note(id, "coupé (Effets : non)"); return null; }

            // Anti-répétition : pas deux fois le même son en moins de 0,03 s.
            float now = Time.unscaledTime;
            if (lastPlayTime.TryGetValue(id, out float last) && now - last < MinRepeatDelay)
            {
                Note(id, "ignoré (répétition < 0,03 s)");
                return null;
            }

            var source = FreeVoice(out int index);
            if (source == null) { Note(id, "ignoré (8 effets en cours)"); return null; }
            lastPlayTime[id] = now;

            float finalPitch = pitch > 0f ? pitch : 1f;
            if (pitch <= 0f && entry.varyPitch) finalPitch = Range(0.95f, 1.05f);
            if (pitchJitter > 0f) finalPitch *= Range(1f - pitchJitter, 1f + pitchJitter);

            source.clip = entry.clip;
            source.volume = entry.volume * volumeScale;
            source.pitch = finalPitch;
            source.Play();
            voiceIds[index] = id;
            Note(id, $"vol={source.volume:0.00} pitch={finalPitch:0.00}");

            if (entry.ducksMusic && music != null) music.Duck(entry.clip.length / finalPitch);
            return source;
        }

        /// Arrête un effet en cours avec un petit fondu (ex. coffre_tremble à l'ouverture).
        public void Stop(Sfx id, float fade)
        {
            for (int i = 0; i < voices.Length; i++)
            {
                if (voices[i].isPlaying && voiceIds[i] == id)
                    StartCoroutine(FadeOut(voices[i], fade));
            }
            Note(id, $"arrêt (fondu {fade:0.00} s)");
        }

        System.Collections.IEnumerator FadeOut(AudioSource source, float fade)
        {
            float start = source.volume;
            for (float t = 0f; t < fade; t += Time.unscaledDeltaTime)
            {
                source.volume = Mathf.Lerp(start, 0f, t / fade);
                yield return null;
            }
            source.Stop();
        }

        AudioSource FreeVoice(out int index)
        {
            for (int i = 0; i < voices.Length; i++)
            {
                if (!voices[i].isPlaying) { index = i; return voices[i]; }
            }
            index = -1;
            return null;
        }

        static void Note(Sfx id, string detail)
        {
            if (!JournalEnabled) return;
            // Heure du JEU (Time.time) : c'est elle qui règle les animations.
            Journal.Add($"{Time.time:0.000} {ClipName(id)} {detail}");
            if (Journal.Count > 500) Journal.RemoveAt(0);
        }

        /// Le nom du fichier d'un son, ex. Sfx.UiClic → "ui_clic" (pour le journal).
        public static string ClipName(Sfx id)
        {
            var sb = new System.Text.StringBuilder();
            foreach (char c in id.ToString())
            {
                if (char.IsUpper(c) && sb.Length > 0) sb.Append('_');
                sb.Append(char.ToLowerInvariant(c));
            }
            return sb.ToString();
        }

        float Range(float min, float max) => min + (float)random.NextDouble() * (max - min);
    }
}
