using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace TourDeLaChance
{
    /// <summary>
    /// L'onglet « MES SKINS » de l'Inventaire (spec v6 §7.2) : les 5 tenues sauvegardées, en
    /// colonne (v6.5 : lignes du « Casier », art bible §16.1.6 ; textes et règles inchangés). Une case vide : « Emplacement libre » + « Sauver ma tenue actuelle ». Une case
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

        /// Une ligne de tenue (v6.5, art bible §16.1.6) : le rond du numéro, puis le nom, le
        /// bonus et 3 mini-cases (skin + 2 accessoires), puis « Mettre » et les petits boutons.
        /// La tenue portée a l'anneau or autour de la ligne et le badge vert sur son numéro.
        VisualElement Slot(int slot, SavedOutfits.Entry e)
        {
            var box = new VisualElement();
            box.AddToClassList("tenue");
            bool worn = !e.IsEmpty && SavedOutfits.IsWorn(e);
            if (worn)
            {
                box.AddToClassList("tenue--portee");
                box.Add(Ignored("tenue__anneau-encre"));
                box.Add(Ignored("tenue__anneau"));
            }

            var number = Ignored("tenue__numero");
            number.Add(ShopUi.NewLabel((slot + 1).ToString(), "tenue__numero-texte"));
            if (worn)
            {
                var badge = Ignored("casier-badge-equipe tenue__numero-badge");
                badge.Add(Ignored("casier-coche__court"));
                badge.Add(Ignored("casier-coche__long"));
                number.Add(badge);
            }
            box.Add(number);

            if (e.IsEmpty)
            {
                box.AddToClassList("tenue--vide");
                box.Add(ShopUi.NewLabel("Emplacement libre", "tenue__libre"));
                box.Add(BubbleButton("Sauver ma tenue actuelle", "bulle-rect--violet", "tenue__sauver",
                    () => AskName(SavedOutfits.DefaultName(slot), n => SavedOutfits.SaveCurrent(slot, n))));
                return box;
            }

            var middle = Ignored("tenue__milieu");
            var top = Ignored("tenue__haut");
            top.Add(ShopUi.NewLabel(e.nom, "tenue__nom"));
            int coins = SavedOutfits.CoinBonus(e), diamonds = SavedOutfits.DiamondBonus(e);
            top.Add(InventoryPage.BonusBadge(coins, "casier-badge-bonus tenue__bonus-badge"));
            if (diamonds > 0) top.Add(ShopUi.NewLabel($"{ShopUi.Diamant} +{diamonds} %", "tenue__diamants"));
            middle.Add(top);
            // Les 3 mini-cases : skin + 2 accessoires (place vide = « + », « ? » si l'objet
            // n'est plus possédé).
            var icons = Ignored("tenue__icones");
            foreach (var id in new[] { e.skin, e.acc1, e.acc2 }) icons.Add(MiniIcon(id));
            middle.Add(icons);
            int missing = SavedOutfits.MissingCount(e);
            if (missing > 0) middle.Add(ShopUi.NewLabel(missing == 1 ? "1 objet manquant" : $"{missing} objets manquants", "tenue__manque"));
            box.Add(middle);

            var wear = BubbleButton("Mettre", "bulle-rect--vert", "tenue__mettre", () => Wear(slot));
            wear.SetEnabled(!worn);
            box.Add(wear);
            var small = new VisualElement();
            small.AddToClassList("tenue__petits");
            small.Add(NewButton("Renommer", "btn--sombre", () => AskName(e.nom, n => SavedOutfits.Rename(slot, n))));
            small.Add(NewButton("Remplacer par ma tenue", "btn--sombre", () => Confirm($"Remplacer « {e.nom} » par ta tenue actuelle ?", "Oui", () => SavedOutfits.ReplaceWithCurrent(slot))));
            small.Add(NewButton("Supprimer", "btn--sombre", () => Confirm($"Supprimer « {e.nom} » ?", "Supprimer", () => SavedOutfits.Delete(slot))));
            box.Add(small);
            return box;
        }

        static VisualElement Ignored(string classes)
        {
            var v = new VisualElement { pickingMode = PickingMode.Ignore };
            foreach (var c in classes.Split(' ')) v.AddToClassList(c);
            return v;
        }

        /// Une mini-case de 76 px, même anatomie que les cases de la grille (sans badge bonus).
        static VisualElement MiniIcon(string id)
        {
            var item = string.IsNullOrEmpty(id) ? null : Shop.Find(id);
            if (string.IsNullOrEmpty(id))
            {
                var empty = Ignored("tenue__mini tenue__mini--vide");
                empty.Add(Ignored("tenue__plus tenue__plus--h"));
                empty.Add(Ignored("tenue__plus tenue__plus--v"));
                return empty;
            }
            if (item == null || !Shop.IsOwned(item))
            {
                var lost = Ignored("tenue__mini tenue__mini--vide");
                lost.Add(ShopUi.NewLabel("?", "tenue__icone-manque"));
                return lost;
            }
            var cell = Ignored("tenue__mini casier-case case--" + InventoryPage.RarityClass(item));
            cell.Add(ShopUi.Visual(item, ShopUi.Look.Normal, "tenue__mini-visuel"));
            return cell;
        }

        /// Un bouton « bulle » rectangulaire (image rendue dans Blender, 9-slice).
        static Button BubbleButton(string text, string color, string extra, Action click)
        {
            var b = new Button(click);
            foreach (var c in new[] { "btn", "bulle", "bulle-rect", color, extra }) b.AddToClassList(c);
            b.Add(ShopUi.NewLabel(text, "bulle-rect__texte"));
            return b;
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
