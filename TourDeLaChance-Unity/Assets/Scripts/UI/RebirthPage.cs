using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace TourDeLaChance
{
    /// <summary>
    /// La pop-up « Renaissance » (spec v5 §13.3) : trois cartes, « Tu perds » (rouge), « Tu
    /// gardes » (bleu), « Tu gagnes » (or), avec TOUS les nombres lus dans la sauvegarde et les
    /// catalogues ; un conseil ; le message important (v5.1) ; le gros bouton vert « Renaître !
    /// [pièce] 10 000 » (grisé avec la raison si besoin) et sa confirmation. Après la renaissance : « Renaissance faite ✓ ».
    /// Ce n'est pas un MonoBehaviour : PanelsController le crée. Il AFFICHE et TRANSMET : la
    /// règle est dans Rebirth (sauvegarde) et GameManager.RequestRebirth (bon moment).
    /// </summary>
    public class RebirthPage : IDisposable
    {

        readonly VisualElement page;
        readonly GameManager game;
        VisualElement confirm;
        Button rebirthButton;
        Label reasonLabel;

        public RebirthPage(VisualElement page, GameManager game)
        {
            this.page = page;
            this.game = game;
        }

        public void Dispose() { }

        public void Open()
        {
            Refresh();
        }

        /// Échap ferme d'abord la confirmation.
        public bool HandleEscape()
        {
            if (confirm == null || confirm.style.display == DisplayStyle.None) return false;
            confirm.style.display = DisplayStyle.None;
            return true;
        }

        /// Les textes de la page (pour les tests).
        public string Text => string.Join(" | ", page.Query<Label>().ToList().Select(l => l.text));
        public bool CanClick => rebirthButton != null && rebirthButton.enabledSelf;

        public void Refresh()
        {
            page.Clear();
            if (Rebirth.IsDone && !Rebirth.Available)
            {
                var done = new VisualElement { pickingMode = PickingMode.Ignore };
                done.AddToClassList("rebirth-done");
                done.Add(ShopUi.NewLabel("Renaissance faite ✓", "rebirth-done__titre"));
                done.Add(ShopUi.NewLabel($"×{EconomyCatalog.Instance.RebirthCoinMultiplier} pièces et ×{EconomyCatalog.Instance.RebirthDiamondMultiplier} diamants actifs, pour toujours.", "rebirth-done__texte"));
                done.Add(ShopUi.NewLabel("D'autres renaissances arriveront dans une prochaine version.", "rebirth-done__petit"));
                page.Add(done);
                return;
            }

            var eco = EconomyCatalog.Instance;
            var columns = new VisualElement { pickingMode = PickingMode.Ignore };
            columns.AddToClassList("rebirth-columns");

            // Tu perds : TOUTES les pièces et les 3 améliorations.
            var lose = Column(columns, "Tu perds", "carte--rouge");
            Line(lose, $"Toutes tes pièces : {ShopUi.Thousands(Wallet.Coins)}");
            var levels = new[] { Upgrades_.Gains, Upgrades_.Chests, Upgrades_.Traps }
                .Select(id => (def: Upgrades.Catalog != null ? Upgrades.Catalog.Get(id) : null, level: Upgrades.Level(id)))
                .Select(x => $"{(x.def != null ? x.def.displayName : "?")} niv. {x.level}");
            Line(lose, "Tes améliorations : " + string.Join(", ", levels));

            // Tu gardes : objets, tenue, diamants, records.
            var keep = Column(columns, "Tu gardes", "carte--bleue");
            int objects = Shop.Catalog == null ? 0 : Shop.Catalog.Boxes.SelectMany(b => b.items).Count(i => i != null && Shop.CountOf(i) >= 1);
            Line(keep, $"Tes skins et accessoires : {objects} objet{(objects > 1 ? "s" : "")}");
            Line(keep, $"Ta tenue, tes diamants ({Diamonds.Count})");
            Line(keep, "Tes records et ton classement");

            // Tu gagnes : ×2 pièces, ×2 diamants, les Boîtes Animaux.
            var gain = Column(columns, "Tu gagnes", "carte--or");
            Line(gain, $"×{eco.RebirthCoinMultiplier} pièces sur chaque trésor, pour toujours");
            Line(gain, $"×{eco.RebirthDiamondMultiplier} diamants dans les trésors");
            var animals = Shop.Boxes.FirstOrDefault(b => b.requiresRebirth);
            if (animals != null) Line(gain, $"Les Boîtes Animaux ({ShopUi.Thousands(animals.price)} pièces)");
            page.Add(columns);

            page.Add(ShopUi.NewLabel("Tu rachèteras tes améliorations deux fois plus vite. La renaissance devient rentable après environ 3 heures de jeu.", "rebirth-tip"));

            // v5.1 : le message important, en blanc contouré, la partie positive en vert.
            var message = new VisualElement { pickingMode = PickingMode.Ignore };
            message.AddToClassList("message-important");
            message.Add(ShopUi.NewLabel("Les pièces et les améliorations sont remises à zéro.", "message-important__texte"));
            message.Add(ShopUi.NewLabel("Tu gardes tout le reste !", "message-important__texte message-important__texte--positif"));
            page.Add(message);

            // Le bouton (grisé avec la raison : pas assez de pièces, ou une cagnotte en jeu).
            // v5.1 : gros bouton vert d'action, avec son prix.
            var row = new VisualElement();
            row.AddToClassList("rebirth-row");
            rebirthButton = PopupDecor.PriceButton(OnRebirthClicked, "Renaître !", Rebirth.Cost, false);
            rebirthButton.AddToClassList("rebirth-btn");
            row.Add(rebirthButton);
            reasonLabel = ShopUi.NewLabel(Reason(), "missing");
            row.Add(reasonLabel);
            rebirthButton.SetEnabled(reasonLabel.text.Length == 0);
            page.Add(row);

            // La confirmation (taille S), cachée.
            confirm = new VisualElement();
            confirm.AddToClassList("v5-confirm");
            var box = new VisualElement();
            box.AddToClassList("v5-confirm-box");
            box.Add(ShopUi.NewLabel("Sûr ? Tes pièces et tes améliorations reviendront à 0. Tes objets et tes diamants restent.", "v5-confirm-text"));
            var buttons = new VisualElement();
            buttons.AddToClassList("v5-confirm-buttons");
            var yes = new Button(OnConfirm) { text = "Oui, renaître !" };
            yes.AddToClassList("btn"); yes.AddToClassList("btn--vert"); yes.AddToClassList("v5-confirm-btn");
            var no = new Button(() => confirm.style.display = DisplayStyle.None) { text = "Annuler" };
            no.AddToClassList("btn"); no.AddToClassList("btn--sombre"); no.AddToClassList("v5-confirm-btn");
            buttons.Add(yes);
            buttons.Add(no);
            box.Add(buttons);
            confirm.Add(box);
            confirm.style.display = DisplayStyle.None;
            page.Add(confirm);
            ButtonStyler.Apply(page);
        }

        /// Pourquoi on ne peut pas renaître maintenant ("" si on peut).
        string Reason()
        {
            if (!Rebirth.Available) return "Renaissance déjà faite.";
            if (Rebirth.MissingCoins > 0) return $"Il te manque {ShopUi.Thousands(Rebirth.MissingCoins)} pièces";
            if (!game.CanRebirthNow) return "Termine ta partie (ou encaisse) d'abord";
            return "";
        }

        /// Un cadre « carte » v5.1 : fond en dégradé vif (classe de couleur), titre blanc contouré.
        static VisualElement Column(VisualElement parent, string title, string colorClass)
        {
            var col = new VisualElement { pickingMode = PickingMode.Ignore };
            col.AddToClassList("rebirth-col");
            col.AddToClassList("carte");
            col.AddToClassList(colorClass);
            col.Add(ShopUi.NewLabel(title, "rebirth-col__titre"));
            parent.Add(col);
            return col;
        }

        static void Line(VisualElement col, string text) => col.Add(ShopUi.NewLabel(text, "rebirth-col__ligne"));

        void OnRebirthClicked()
        {
            if (Reason().Length > 0) { UiSounds.PlayRefus(); return; }
            confirm.style.display = DisplayStyle.Flex;
        }

        void OnConfirm()
        {
            confirm.style.display = DisplayStyle.None;
            if (!game.RequestRebirth()) { UiSounds.PlayRefus(); Refresh(); }
        }

        /// OUTIL DE TEST : clic sur « Renaître » puis « Oui, renaître ! ».
        public bool DebugConfirm()
        {
            if (Reason().Length > 0) return false;
            return game.RequestRebirth();
        }
    }
}
