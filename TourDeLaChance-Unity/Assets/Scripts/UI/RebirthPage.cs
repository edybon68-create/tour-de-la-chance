using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace TourDeLaChance
{
    /// <summary>
    /// La pop-up « Renaissance », REFAITE en v6 (art bible §15.4) : une grande scène au centre
    /// (rayons dorés qui tournent, illustration du cheval doré qui flotte, macaron « ×2 » qui
    /// pulse), puis trois cartes à ICÔNES dans l'ordre Tu perds · TU GAGNES (au centre, plus
    /// grande) · Tu gardes, la phrase importante, et un énorme bouton vert « Renaître ! 10 000 »
    /// (gris avec une jauge « 6 420 / 10 000 » s'il manque des pièces). Le CONTENU ne change pas
    /// (v5 §13.3) : vrais nombres lus dans la sauvegarde, deux raisons de grisé, confirmation,
    /// état « Renaissance faite ». Phrase de rentabilité v6 : « environ 2 h 45 ».
    /// Ce n'est pas un MonoBehaviour : PanelsController le crée et l'avance (Tick). Il AFFICHE et
    /// TRANSMET : la règle est dans Rebirth (sauvegarde) et GameManager.RequestRebirth (bon moment).
    /// </summary>
    public class RebirthPage : IDisposable
    {
        readonly VisualElement page;
        readonly GameManager game;
        readonly Sprite iconUpgrades, iconRecords;
        VisualElement confirm, hero, macaron;
        RaysElement rays;
        Button rebirthButton;
        Label reasonLabel;
        float time;

        public RebirthPage(VisualElement page, GameManager game, Sprite iconUpgrades = null, Sprite iconRecords = null)
        {
            this.page = page;
            this.game = game;
            this.iconUpgrades = iconUpgrades;
            this.iconRecords = iconRecords;
        }

        public void Dispose() { }

        public void Open() => Refresh();

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

        /// Les petites animations : rayons (un tour en 24 s), illustration qui flotte (±6 px en
        /// 2,4 s), macaron qui pulse (1 → 1,08 → 1 en 1,4 s).
        public void Tick(float dt)
        {
            time += dt;
            if (rays != null) rays.style.rotate = new Rotate(time * 360f / 24f);
            if (hero != null) hero.style.translate = new Translate(0f, 6f * Mathf.Sin(time * Mathf.PI * 2f / 2.4f));
            if (macaron != null)
            {
                float s = 1f + 0.04f * (1f - Mathf.Cos(time * Mathf.PI * 2f / 1.4f));
                macaron.style.scale = new Scale(new Vector3(s, s, 1f));
            }
        }

        public void Refresh()
        {
            page.Clear();
            var eco = EconomyCatalog.Instance;
            bool done = Rebirth.IsDone && !Rebirth.Available;

            page.Add(Scene(done));

            if (done)
            {
                // E. Après la renaissance : une seule carte or, pas de bouton.
                var card = new VisualElement { pickingMode = PickingMode.Ignore };
                card.AddToClassList("rn-fait");
                card.AddToClassList("carte");
                card.AddToClassList("carte--or");
                card.Add(ShopUi.NewLabel($"Renaissance faite ! ×{eco.RebirthCoinMultiplier} pièces et ×{eco.RebirthDiamondMultiplier} diamants, pour toujours", "rn-fait__texte"));
                page.Add(card);
                return;
            }

            // B. Les trois cartes : perds · gagnes · gardes.
            var cards = new VisualElement { pickingMode = PickingMode.Ignore };
            cards.AddToClassList("rn-cartes");
            var lose = Card(cards, "Tu perds", "carte--rouge", false);
            lose.Add(Icon("coin", null, ShopUi.Thousands(Wallet.Coins), Badge.Minus));
            lose.Add(Icon(null, iconUpgrades, "Améliorations", Badge.Minus));

            var gain = Card(cards, "Tu gagnes", "carte--or", true);
            gain.Add(Icon("coin", null, "Pièces", Badge.X2));
            gain.Add(Icon("diamant", null, "Diamants", Badge.X2));
            var animals = Shop.Boxes.FirstOrDefault(b => b.requiresRebirth);
            if (animals != null) gain.Add(Icon(null, animals.icon, "Nouvelles boîtes", Badge.None));

            var keep = Card(cards, "Tu gardes", "carte--bleue", false);
            int objects = Shop.Catalog == null ? 0 : Shop.Catalog.Boxes.SelectMany(b => b.items).Count(i => i != null && Shop.CountOf(i) >= 1);
            keep.Add(Icon(null, Shop.GetEquippedSkin()?.Icon, $"{objects} objet{(objects > 1 ? "s" : "")}", Badge.Check));
            keep.Add(Icon("diamant", null, Diamonds.Count.ToString(), Badge.Check));
            keep.Add(Icon(null, iconRecords, "Records", Badge.Check));
            page.Add(cards);

            // C. La phrase importante (la fin en vert).
            var phrase = new VisualElement { pickingMode = PickingMode.Ignore };
            phrase.AddToClassList("rn-phrase");
            phrase.Add(ShopUi.NewLabel("Pièces et améliorations reviennent à zéro. ", "message-important__texte"));
            phrase.Add(ShopUi.NewLabel("Tu gardes tout le reste !", "message-important__texte message-important__texte--positif"));
            page.Add(phrase);

            // D. Le gros bouton (gris + jauge s'il manque des pièces).
            string reason = Reason();
            if (Rebirth.MissingCoins > 0)
            {
                var gauge = new VisualElement { pickingMode = PickingMode.Ignore };
                gauge.AddToClassList("rn-jauge");
                var fill = new VisualElement { pickingMode = PickingMode.Ignore };
                fill.AddToClassList("rn-jauge__plein");
                fill.style.width = Length.Percent(100f * Mathf.Clamp01(Wallet.Coins / (float)Rebirth.Cost));
                gauge.Add(fill);
                gauge.Add(ShopUi.NewLabel($"{ShopUi.Thousands(Wallet.Coins)} / {ShopUi.Thousands(Rebirth.Cost)}", "rn-jauge__texte"));
                page.Add(gauge);
            }
            rebirthButton = new Button(OnRebirthClicked);
            rebirthButton.AddToClassList("btn"); rebirthButton.AddToClassList("bulle"); rebirthButton.AddToClassList("bulle-rect");
            rebirthButton.AddToClassList("bulle-rect--vert"); rebirthButton.AddToClassList("rn-bouton");
            rebirthButton.Add(ShopUi.NewLabel("Renaître !", "bulle-rect__texte"));
            var coin = new VisualElement { pickingMode = PickingMode.Ignore };
            coin.AddToClassList("coin");
            rebirthButton.Add(coin);
            rebirthButton.Add(ShopUi.NewLabel(ShopUi.Thousands(Rebirth.Cost), "bulle-rect__texte"));
            rebirthButton.SetEnabled(reason.Length == 0);
            page.Add(rebirthButton);
            reasonLabel = ShopUi.NewLabel(reason, "rn-raison");
            reasonLabel.style.display = reason.Length > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            page.Add(reasonLabel);
            page.Add(ShopUi.NewLabel("Tu rachèteras tes améliorations deux fois plus vite. La renaissance devient rentable après environ 2 h 45 de jeu.", "rn-note"));

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

        /// A. La scène : lueur, rayons, illustration, macaron « ×2 » (image ou substitut dessiné).
        VisualElement Scene(bool done)
        {
            var scene = new VisualElement { pickingMode = PickingMode.Ignore };
            scene.AddToClassList("rn-scene");
            var glow = new VisualElement { pickingMode = PickingMode.Ignore };
            glow.AddToClassList("rn-lueur");
            glow.generateVisualContent += DrawGlow;
            scene.Add(glow);
            rays = new RaysElement { pickingMode = PickingMode.Ignore };
            rays.AddToClassList("rn-rayons");
            rays.Configure(12, new Color(1f, 0.835f, 0.29f, 0.55f));
            scene.Add(rays);
            hero = new VisualElement { pickingMode = PickingMode.Ignore };
            hero.AddToClassList("rn-heros");
            if (UiImages.Heros != null) hero.style.backgroundImage = new StyleBackground(UiImages.Heros);
            else if (Shop.GetEquippedSkin()?.Icon != null) hero.style.backgroundImage = new StyleBackground(Shop.GetEquippedSkin().Icon);
            scene.Add(hero);
            macaron = new VisualElement { pickingMode = PickingMode.Ignore };
            macaron.AddToClassList("rn-macaron");
            if (UiImages.Macaron != null) macaron.style.backgroundImage = new StyleBackground(UiImages.Macaron);
            else macaron.generateVisualContent += DrawMedal;
            macaron.style.translate = new Translate(200f, -40f);
            macaron.Add(ShopUi.NewLabel("×2", "rn-macaron__texte"));
            if (done) macaron.Add(ShopUi.NewLabel("Fait !", "rn-macaron__fait"));
            scene.Add(macaron);
            return scene;
        }

        static void DrawGlow(MeshGenerationContext ctx)
        {
            var r = ctx.visualElement.contentRect;
            var p = ctx.painter2D;
            for (int i = 6; i >= 1; i--)
            {
                p.fillColor = new Color(1f, 0.914f, 0.627f, 0.08f);
                p.BeginPath();
                p.Arc(r.center, r.width * 0.5f * i / 6f, Angle.Degrees(0f), Angle.Degrees(360f));
                p.Fill();
            }
        }

        /// Substitut du macaron : un disque or dentelé contouré d'encre.
        static void DrawMedal(MeshGenerationContext ctx)
        {
            var r = ctx.visualElement.contentRect;
            var p = ctx.painter2D;
            Vector2 c = r.center;
            float rad = r.width * 0.46f;
            p.BeginPath();
            for (int i = 0; i < 32; i++)
            {
                float a = i * Mathf.PI * 2f / 32f;
                float rr = i % 2 == 0 ? rad : rad * 0.9f;
                var pt = c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * rr;
                if (i == 0) p.MoveTo(pt); else p.LineTo(pt);
            }
            p.ClosePath();
            p.fillColor = new Color(1f, 0.784f, 0.227f);
            p.Fill();
            p.strokeColor = new Color(0.067f, 0.051f, 0.122f);
            p.lineWidth = 5f;
            p.Stroke();
        }

        static VisualElement Card(VisualElement parent, string title, string colorClass, bool center)
        {
            var card = new VisualElement { pickingMode = PickingMode.Ignore };
            card.AddToClassList("rn-carte");
            card.AddToClassList("carte");
            card.AddToClassList(colorClass);
            if (center) card.AddToClassList("rn-carte--centre");
            card.Add(ShopUi.NewLabel(title, "rn-carte__titre"));
            var icons = new VisualElement { pickingMode = PickingMode.Ignore };
            icons.AddToClassList("rn-icones");
            card.Add(icons);
            parent.Add(card);
            return icons;
        }

        enum Badge { None, Minus, Check, X2 }

        /// Une icône de carte (classe USS « coin »/« diamant », ou une image), sa légende et sa pastille.
        static VisualElement Icon(string ussClass, Sprite sprite, string caption, Badge badge)
        {
            var box = new VisualElement { pickingMode = PickingMode.Ignore };
            box.AddToClassList("rn-ic");
            var img = new VisualElement { pickingMode = PickingMode.Ignore };
            img.AddToClassList("rn-ic__img");
            if (ussClass != null) img.AddToClassList(ussClass);
            else if (sprite != null) img.style.backgroundImage = new StyleBackground(sprite);
            img.style.marginLeft = 0;
            box.Add(img);
            switch (badge)
            {
                case Badge.Minus: img.Add(Pastille("rn-pastille--moins", "−")); break;
                case Badge.Check: img.Add(Pastille("rn-pastille--coche", "✓")); break;
                case Badge.X2: img.Add(ShopUi.NewLabel("×2", "rn-x2")); break;
            }
            box.Add(ShopUi.NewLabel(caption, "rn-ic__legende"));
            return box;
        }

        static VisualElement Pastille(string cls, string text)
        {
            var p = new VisualElement { pickingMode = PickingMode.Ignore };
            p.AddToClassList("rn-pastille");
            p.AddToClassList(cls);
            p.Add(ShopUi.NewLabel(text, "rn-pastille__texte"));
            return p;
        }

        /// Pourquoi on ne peut pas renaître maintenant ("" si on peut).
        string Reason()
        {
            if (!Rebirth.Available) return "Renaissance déjà faite.";
            if (Rebirth.MissingCoins > 0) return $"Il te manque {ShopUi.Thousands(Rebirth.MissingCoins)} pièces";
            if (!game.CanRebirthNow) return "Termine ta partie (ou encaisse) d'abord";
            return "";
        }

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
