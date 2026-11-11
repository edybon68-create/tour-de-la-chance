using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace TourDeLaChance
{
    /// <summary>
    /// La pop-up « Choisis ton pseudo » (spec v4 §12.7). Simple objet piloté par
    /// PanelsController. Elle affiche et transmet : les RÈGLES (3 à 16 caractères, caractères
    /// permis, mots interdits) sont dans Leaderboard.CheckName.
    /// Le champ refuse simplement les caractères interdits à la frappe ; « Valider » n'est
    /// actif que si le pseudo respecte les règles ; « Au hasard » propose un pseudo amusant.
    /// </summary>
    public class NamePage : IDisposable
    {
        readonly TextField field;
        readonly Label message;
        readonly Button okButton, randomButton;
        bool busy;

        /// Le pseudo vient d'être validé et enregistré.
        public event Action Done;

        public NamePage(VisualElement page)
        {
            field = page.Q<TextField>("name-field");
            message = page.Q<Label>("name-message");
            okButton = page.Q<Button>("name-ok");
            randomButton = page.Q<Button>("name-random");
            okButton.AddToClassList(UiSounds.SilentClass); // son « amelioration_achat » à la validation
            field.RegisterValueChangedCallback(OnTyped);
            okButton.clicked += OnOk;
            randomButton.clicked += OnRandom;
        }

        public void Dispose()
        {
            field.UnregisterValueChangedCallback(OnTyped);
            okButton.clicked -= OnOk;
            randomButton.clicked -= OnRandom;
        }

        /// Ouvre la page avec le pseudo actuel (vide la 1re fois).
        public void Open()
        {
            field.SetValueWithoutNotify(Leaderboard.PlayerName);
            message.text = "";
            Validate(showMessage: false);
            field.schedule.Execute(() => field.Focus()).StartingIn(50);
        }

        void OnTyped(ChangeEvent<string> evt)
        {
            // Le champ « refuse » les caractères interdits : on les retire aussitôt.
            string clean = Leaderboard.CleanNameInput(evt.newValue);
            if (clean != evt.newValue) field.SetValueWithoutNotify(clean);
            Validate(showMessage: true);
        }

        void Validate(bool showMessage)
        {
            bool ok = Leaderboard.CheckName(field.value, out string why);
            okButton.SetEnabled(ok && !busy);
            // Trop court : pas de message rouge tant que le joueur écrit (le bouton grisé suffit).
            bool tooShort = string.IsNullOrEmpty(field.value) || field.value.Length < Leaderboard.NameMin;
            message.text = showMessage && !ok && !tooShort ? why : "";
        }

        void OnRandom()
        {
            field.SetValueWithoutNotify(Leaderboard.RandomName());
            Validate(showMessage: true);
        }

        async void OnOk()
        {
            if (busy) return;
            busy = true;
            okButton.SetEnabled(false);
            var result = await Leaderboard.SetNameAsync(field.value);
            busy = false;
            if (result.Status == NameStatus.Ok)
            {
                if (SoundManager.Instance != null) SoundManager.Instance.Play(Sfx.AmeliorationAchat, 0.5f);
                Done?.Invoke();
            }
            else
            {
                message.text = result.Message;
                Validate(showMessage: false);
                message.text = result.Message;
            }
        }

        // ---------- Outils de test ----------
        public void DebugType(string text)
        {
            field.value = text; // passe par OnTyped, comme une vraie frappe
        }
        public bool DebugCanValidate => okButton.enabledSelf;
        public string DebugField => field.value;
        public string DebugMessage => message.text;
        public void DebugClickOk() => OnOk();
    }
}
