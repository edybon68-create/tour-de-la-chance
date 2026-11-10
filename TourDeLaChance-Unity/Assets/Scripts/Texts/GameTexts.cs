using System;
using System.Collections.Generic;
using UnityEngine;

namespace TourDeLaChance
{
    /// <summary>
    /// Les phrases du jeu (spec v3 §11) : c'est du CONTENU, rangé dans un ScriptableObject
    /// (Assets/Data/Textes.asset), que l'auteur peut modifier sans toucher au code.
    /// Mots spéciaux remplacés à l'affichage : {etage}, {cagnotte}, {gain}, {perte}.
    /// Le choix d'une phrase est de l'AFFICHAGE (voir TextPicker), jamais une règle de jeu.
    /// </summary>
    [CreateAssetMenu(fileName = "Textes", menuName = "Tour de la Chance/Textes du jeu")]
    public class GameTexts : ScriptableObject
    {
        /// Un texte réservé à un étage précis (« Jamais deux sans trois ! » à l'étage 3).
        [Serializable]
        public class FloorText
        {
            public int floor;
            [TextArea] public string text;
        }

        [Header("Bannière de montée (2e ligne)")]
        [Tooltip("Phrases de montée, tirées au hasard (jamais une des 3 dernières).")]
        [TextArea] public List<string> climbLines = new List<string>();
        [Tooltip("Textes spéciaux de certains étages (prioritaires sur les phrases au hasard).")]
        public List<FloorText> floorTexts = new List<FloorText>();
        [Tooltip("À partir de cet étage, tous les N étages (25, 30, 35…), ce texte s'affiche.")]
        public int repeatFromFloor = 25;
        public int repeatEvery = 5;
        [TextArea] public string repeatText = "Étage {etage} : tu as cassé la tour !";
        [Tooltip("3e ligne de la bannière, quand on bat son record.")]
        public string recordLine = "★ Nouveau record ! ★";

        [Header("Message de piège")]
        [TextArea] public List<string> trapLines = new List<string>();
        [Tooltip("Piège à l'étage 1 (prioritaire).")]
        [TextArea] public string trapFloor1 = "";
        [Tooltip("Piège à partir de cet étage (prioritaire après l'étage 1).")]
        public int trapHighFloor = 7;
        [TextArea] public string trapHigh = "";
        [Tooltip("Cagnotte perdue à partir de ce montant (prioritaire après les deux autres).")]
        public int bigLoss = 300;
        [TextArea] public string trapBigLoss = "";
        [Tooltip("2e ligne : cagnotte perdue (X = {perte}).")]
        public string lossLine = "Cagnotte perdue : {perte} pièces.";
        public string noLossLine = "Rien de perdu, rejoue !";

        [Header("Message d'encaissement")]
        [TextArea] public List<string> cashOutLines = new List<string>();
        public string cashOutLine = "Tu encaisses {gain} pièces.";
    }
}
