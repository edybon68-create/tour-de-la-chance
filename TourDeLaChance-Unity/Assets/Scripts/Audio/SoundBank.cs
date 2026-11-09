using System;
using System.Collections.Generic;
using UnityEngine;

namespace TourDeLaChance
{
    /// <summary>
    /// La fiche des sons : pour chaque effet (Sfx), son fichier, son volume (spec §6.1),
    /// s'il est « varié » (hauteur au hasard entre 0,95 et 1,05) et s'il fait baisser la
    /// musique (les jingles de résultat et de révélation).
    /// C'est du CONTENU : un ScriptableObject réglable dans l'Inspector (Assets/Data/Sons.asset).
    /// </summary>
    [CreateAssetMenu(menuName = "Tour de la Chance/Banque de sons", fileName = "Sons")]
    public class SoundBank : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            public Sfx id;
            public AudioClip clip;
            [Range(0f, 1f)] public float volume = 0.5f;
            [Tooltip("Hauteur tirée au hasard entre 0,95 et 1,05 à chaque lecture.")]
            public bool varyPitch;
            [Tooltip("La musique baisse (× 0,5) pendant ce son.")]
            public bool ducksMusic;
        }

        public List<Entry> entries = new List<Entry>();

        [Header("Musiques (48 s, en boucle, jouées ensemble)")]
        public AudioClip ambientMusic;
        public AudioClip tensionMusic;

        /// Retrouve la fiche d'un son (null s'il manque).
        public Entry Find(Sfx id)
        {
            foreach (var e in entries)
                if (e != null && e.id == id) return e;
            return null;
        }
    }
}
