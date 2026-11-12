using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace TourDeLaChance
{
    /// <summary>
    /// La pop-up « Renaissance » (spec v5 §13.3) : trois cadres, « Tu perds » (rouge), « Tu
    /// gardes » (bleu), « Tu gagnes » (or), avec TOUS les nombres lus dans la sauvegarde et les
    /// catalogues ; un conseil ; le bouton « Renaître — 10 000 » (grisé avec la raison si besoin)
    /// et sa confirmation. Après la renaissance : « Renaissance faite ✓ ».
    /// Ce n'est pas un MonoBehaviour : PanelsController le crée. Il AFFICHE et TRANSMET : la
    /// règle est dans Rebirth (sauvegarde) et GameManager.RequestRebirth (bon moment).
    /// </summary>
    public class RebirthPage : IDisposable
    {
        static readonly Color Red = new Color(0.839f, 0.271f, 0.271f);   // #D64545
        static readonly Color Blue = new Color(0.231f, 0.490f, 0.847f);  // #3B7DD8
        static readonly Color Gold = new Color(1f, 0.835f, 0.29f);       // #FFD54A

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
            var lose = Column(columns, "Tu perds", Red);
            Line(lose, $"Toutes tes pièces : {ShopUi.Thousands(Wallet.Coins)}");
            var levels = new[] { Upgrades_.Gains, Upgrades_.Chests, Upgrades_.Traps }
                .Select(id => (def: Upgrades.Catalog != null ? Upgrades.Catalog.Get(id) : null, level: Upgrades.Level(id)))
                .Select(x => $"{(x.def != null ? x.def.displayName : "?")} niv. {x.level}");
            Line(lose, "Tes améliorations : " + string.Join(", ", levels));

            // Tu gardes : objets, tenue, diamants, records.
            var keep = Column(columns, "Tu gardes", Blue);
            int objects = Shop.Catalog == null ? 0 : Shop.Catalog.Boxes.SelectMany(b => b.items).Count(i => i != null && Shop.CountOf(i) >= 1);
            Line(keep, $"Tes skins et accessoires : {objects} objet{(objects > 1 ? "s" : "")}");
            Line(keep, $"Ta tenue, tes diamants ({Diamonds.Count})");
            Line(keep, "Tes records et ton classement");

            // Tu gagnes : ×2 pièces, ×2 diamants, les Boîtes Animaux.
            var gain = Column(columns, "Tu gagnes", Gold);
            Line(gain, $"×{eco.RebirthCoinMultiplier} pièces sur chaque trésor, pour toujours");
            Line(gain, $"×{eco.RebirthDiamondMultiplier} diamants dans les trésors");
            var animals = Shop.Boxes.FirstOrDefault(b => b.requiresRebirth);
            if (animals != null) Line(gain, $"Les Boîtes Animaux ({ShopUi.Thousands(animals.price)} pièces)");
            page.Add(columns);

            page.Add(ShopUi.NewLabel("Tu rachèteras tes améliorations deux fois plus vite. La renaissance devient rentable après environ 3 heures de jeu.", "rebirth-tip"));

            // Le bouton (grisé avec la raison : pas assez de pièces, ou une cagnotte en jeu).
            var row = new VisualElement();
            row.AddToClassList("rebirth-row");
            rebirthButton = new Button(OnRebirthClicked) { text = $"Renaître — {ShopUi.Thousands(Rebirth.Cost)}" };
            rebirthButton.AddToClassList("btn");
            rebirthButton.AddToClassList("btn--or");
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
            yes.AddToClassList("btn"); yes.AddToClassList("btn--or"); yes.AddToClassList("v5-confirm-btn");
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

        static VisualElement Column(VisualElement parent, string title, Color color)
        {
            var col = new VisualElement { pickingMode = PickingMode.Ignore };
            col.AddToClassList("rebirth-col");
            col.style.borderTopColor = color; col.style.borderBottomColor = color;
            col.style.borderLeftColor = color; col.style.borderRightColor = color;
            var t = ShopUi.NewLabel(title, "rebirth-col__titre");
            t.style.color = color;
            col.Add(t);
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
