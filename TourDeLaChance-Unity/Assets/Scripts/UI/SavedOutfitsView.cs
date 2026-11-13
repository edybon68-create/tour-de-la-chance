using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace TourDeLaChance
{
    /// <summary>
    /// L'onglet « MES SKINS » de l'Inventaire (spec v6 §7.2) : les 5 tenues sauvegardées, en
    /// colonne. Une case vide : « Emplacement libre » + « Sauver ma tenue actuelle ». Une case
    /// remplie : le nom, 3 mini-icônes, les bonus, « Portée ✓ », et les boutons Mettre / Renommer /
    /// Remplacer par ma tenue / Supprimer. Les petites fenêtres (nom, confirmations) sont posées
    /// sur la page.
    /// Pur affichage : les règles (nom, objets manquants, équiper en une fois) sont dans
    /// SavedOutfits et Shop. Ce n'est pas un MonoBehaviour : InventoryPage le crée.
    /// </summary>
    public class SavedOutfitsView
    {
        readonly VisualElement page;
        VisualElement dialog;
        TextField nameField;
        Action onOk;

        /// Une tenue vient d'être mise (texte du petit message, objet pour le saut du cheval).
        public event Action<string> Worn;
        public bool DialogOpen => dialog != null;

        public SavedOutfitsView(VisualElement page) { this.page = page; }

        /// Construit la liste des 5 cases.
        public VisualElement Build()
        {
            var list = new VisualElement { pickingMode = PickingMode.Ignore };
            list.AddToClassList("tenues");
            var all = SavedOutfits.All();
            for (int i = 0; i < SavedOutfits.SlotCount; i++) list.Add(Slot(i, all[i]));
            return list;
        }

        VisualElement Slot(int slot, SavedOutfits.Entry e)
        {
            var box = new VisualElement();
            box.AddToClassList("tenue");
            if (e.IsEmpty)
            {
                box.AddToClassList("tenue--vide");
                box.Add(ShopUi.NewLabel("Emplacement libre", "tenue__libre"));
                box.Add(NewButton("Sauver ma tenue actuelle", "btn--vert", () => AskName(SavedOutfits.DefaultName(slot), n => SavedOutfits.SaveCurrent(slot, n))));
                return box;
            }

            // Les 3 mini-icônes : skin + 2 accessoires (case vide si pas d'accessoire, « ? » gris
            // si l'objet n'est plus possédé).
            var icons = new VisualElement { pickingMode = PickingMode.Ignore };
            icons.AddToClassList("tenue__icones");
            foreach (var id in new[] { e.skin, e.acc1, e.acc2 }) icons.Add(MiniIcon(id));
            box.Add(icons);

            var infos = new VisualElement { pickingMode = PickingMode.Ignore };
            infos.AddToClassList("tenue__infos");
            infos.Add(ShopUi.NewLabel(e.nom, "tenue__nom"));
            int coins = SavedOutfits.CoinBonus(e), diamonds = SavedOutfits.DiamondBonus(e);
            infos.Add(ShopUi.NewLabel($"+{coins} % pièces" + (diamonds > 0 ? $" · +{diamonds} % {ShopUi.Diamant}" : ""), "tenue__bonus"));
            int missing = SavedOutfits.MissingCount(e);
            if (missing > 0) infos.Add(ShopUi.NewLabel(missing == 1 ? "1 objet manquant" : $"{missing} objets manquants", "tenue__manque"));
            bool worn = SavedOutfits.IsWorn(e);
            if (worn) infos.Add(ShopUi.NewLabel("Portée ✓", "tenue__portee"));
            box.Add(infos);

            var buttons = new VisualElement();
            buttons.AddToClassList("tenue__boutons");
            var wear = NewButton("Mettre", "btn--vert", () => Wear(slot));
            wear.SetEnabled(!worn);
            buttons.Add(wear);
            buttons.Add(NewButton("Renommer", "btn--sombre", () => AskName(e.nom, n => SavedOutfits.Rename(slot, n))));
            buttons.Add(NewButton("Remplacer par ma tenue", "btn--sombre", () => Confirm($"Remplacer « {e.nom} » par ta tenue actuelle ?", "Oui", () => SavedOutfits.ReplaceWithCurrent(slot))));
            buttons.Add(NewButton("Supprimer", "btn--sombre", () => Confirm($"Supprimer « {e.nom} » ?", "Supprimer", () => SavedOutfits.Delete(slot))));
            box.Add(buttons);
            return box;
        }

        static VisualElement MiniIcon(string id)
        {
            var cell = new VisualElement { pickingMode = PickingMode.Ignore };
            cell.AddToClassList("tenue__icone");
            if (string.IsNullOrEmpty(id)) return cell;
            var item = Shop.Find(id);
            if (item == null || !Shop.IsOwned(item)) { cell.Add(ShopUi.NewLabel("?", "tenue__icone-manque")); return cell; }
            cell.Add(ShopUi.Visual(item, ShopUi.Look.Normal));
            return cell;
        }

        static Button NewButton(string text, string color, Action click)
        {
            var b = new Button(click) { text = text };
            b.AddToClassList("btn"); b.AddToClassList(color); b.AddToClassList("tenue__btn");
            return b;
        }

        /// « Mettre » : tout en une fois (Shop.TryEquipOutfit). Objet manquant : on met le reste.
        void Wear(int slot)
        {
            var e = SavedOutfits.Get(slot);
            // Compté AVANT : un objet retiré du catalogue (identifiant inconnu) compte aussi.
            int before = SavedOutfits.MissingCount(e);
            int missing = SavedOutfits.Wear(slot);
            if (missing < 0) return;
            missing = Mathf.Max(missing, before);
            Worn?.Invoke(missing > 0
                ? "Un objet de cette tenue n'est plus dans ta collection : il a été laissé de côté."
                : $"Tenue « {e.nom} » mise : +{Shop.OutfitBonusPercent} %");
        }

        // ---------- Les petites fenêtres ----------

        /// La fenêtre du nom (taille S) : 1 à 16 caractères permis, Entrée = OK, Échap = Annuler.
        void AskName(string start, Action<string> ok)
        {
            Close();
            dialog = Dialog(out var box);
            box.Add(ShopUi.NewLabel("Nom de la tenue", "v5-confirm-text"));
            nameField = new TextField { maxLength = SavedOutfits.MaxNameLength, value = SavedOutfits.Filter(start) };
            nameField.AddToClassList("nom-champ");
            nameField.AddToClassList("name-field");
            // À chaque frappe : les caractères refusés disparaissent, le 17e est refusé.
            nameField.RegisterValueChangedCallback(evt =>
            {
                string clean = SavedOutfits.Filter(evt.newValue);
                if (clean != evt.newValue) nameField.SetValueWithoutNotify(clean);
            });
            nameField.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter) { Ok(); evt.StopPropagation(); }
            }, TrickleDown.TrickleDown);
            box.Add(nameField);
            var field = nameField; // gardé ici : Close() remet nameField à null avant l'action
            onOk = () => ok(field.value);
            box.Add(Buttons(("OK", "btn--vert", Ok), ("Annuler", "btn--sombre", Close)));
            nameField.schedule.Execute(() => nameField.Focus()).StartingIn(30);
        }

        void Confirm(string text, string yes, Action action)
        {
            Close();
            dialog = Dialog(out var box);
            box.Add(ShopUi.NewLabel(text, "v5-confirm-text"));
            onOk = action;
            box.Add(Buttons((yes, "btn--vert", Ok), ("Annuler", "btn--sombre", Close)));
        }

        VisualElement Dialog(out VisualElement box)
        {
            var d = new VisualElement();
            d.AddToClassList("v5-confirm");
            box = new VisualElement();
            box.AddToClassList("v5-confirm-box");
            d.Add(box);
            page.Add(d);
            return d;
        }

        VisualElement Buttons(params (string text, string color, Action click)[] list)
        {
            var row = new VisualElement();
            row.AddToClassList("v5-confirm-buttons");
            foreach (var (text, color, click) in list)
            {
                var b = new Button(click) { text = text };
                b.AddToClassList("btn"); b.AddToClassList(color); b.AddToClassList("v5-confirm-btn");
                row.Add(b);
            }
            ButtonStyler.Apply(row);
            return row;
        }

        void Ok()
        {
            var action = onOk;
            Close();
            action?.Invoke();
        }

        public void Close()
        {
            if (dialog != null) dialog.RemoveFromHierarchy();
            dialog = null;
            nameField = null;
            onOk = null;
        }

        /// Échap : ferme la petite fenêtre (= Annuler).
        public bool HandleEscape()
        {
            if (dialog == null) return false;
            Close();
            return true;
        }

        // ---------- Outils de test ----------
        public void DebugSave(int slot, string name) => AskNameAndOk(SavedOutfits.DefaultName(slot), name, n => SavedOutfits.SaveCurrent(slot, n));
        void AskNameAndOk(string start, string typed, Action<string> ok)
        {
            AskName(start, ok);
            nameField.value = typed; // passe par le filtre, comme une frappe
            Ok();
        }
        public string DebugType(string text)
        {
            AskName("", _ => { });
            nameField.value = text;
            string result = nameField.value;
            Close();
            return result;
        }
        public void DebugWear(int slot) => Wear(slot);
    }
}
