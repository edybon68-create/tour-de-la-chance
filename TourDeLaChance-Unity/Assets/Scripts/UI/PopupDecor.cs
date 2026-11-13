using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace TourDeLaChance
{
    /// <summary>
    /// v5.1 (art bible §12.18) : les petites pièces d'interface « façon jeu Roblox » partagées
    /// par les pop-ups. Pur AFFICHAGE : aucune règle de jeu ici, tous les chiffres viennent
    /// de ceux qui appellent (prix, taux lus dans les données des boîtes).
    ///
    /// - SectionTitle : un titre de séparation encadré de deux décorations (« couronne Boîtes
    ///   de pirates couronne »). La police n'a pas d'émoji : les décorations sont des IMAGES
    ///   (pièce, diamant) ou des FORMES dessinées par le code (couronne, patte, étoile).
    /// - PriceButton : la grosse pilule verte en relief « Ouvrir [pièce] 250 ».
    /// - CardRays : le rayonnement blanc derrière l'illustration d'une carte.
    /// - RateBadges : les objets d'une boîte, un par rareté, avec le VRAI taux de la rareté
    ///   dans un petit badge dessous (comme les vitrines d'œufs des jeux de ce genre).
    /// </summary>
    public static class PopupDecor
    {
        /// Les décorations possibles autour d'un titre de section.
        public enum Deco { Piece, Diamant, Couronne, Patte, Etoile }

        static readonly Color Ink = new Color(0.067f, 0.051f, 0.122f);   // #110D1F
        static readonly Color Gold = new Color(1f, 0.835f, 0.29f);       // #FFD54A
        static readonly Color GoldDark = new Color(0.85f, 0.6f, 0.1f);   // #D99A1A
        static readonly Color Pink = new Color(1f, 0.62f, 0.77f);        // #FF9FC4

        // =====================================================================
        // Titre de section
        // =====================================================================

        /// Un titre de séparation : [déco] Texte [déco], centré.
        public static VisualElement SectionTitle(string text, Deco deco)
        {
            var row = new VisualElement { pickingMode = PickingMode.Ignore };
            row.AddToClassList("section-titre");
            row.Add(DecoElement(deco));
            row.Add(ShopUi.NewLabel(text, "section-titre__texte"));
            row.Add(DecoElement(deco));
            return row;
        }

        static VisualElement DecoElement(Deco deco)
        {
            var e = new VisualElement { pickingMode = PickingMode.Ignore };
            e.AddToClassList("section-deco");
            switch (deco)
            {
                case Deco.Piece: e.AddToClassList("section-deco--piece"); break;
                case Deco.Diamant: e.AddToClassList("section-deco--diamant"); break;
                case Deco.Couronne: e.generateVisualContent += DrawCrown; break;
                case Deco.Patte: e.generateVisualContent += DrawPaw; break;
                default: e.generateVisualContent += DrawStar; break;
            }
            return e;
        }

        /// Une couronne dorée à 3 pointes, contour d'encre, une perle au bout de chaque pointe.
        static void DrawCrown(MeshGenerationContext ctx)
        {
            var r = ctx.visualElement.contentRect;
            var p = ctx.painter2D;
            float w = r.width, h = r.height;
            Vector2 P(float x, float y) => new Vector2(r.x + x * w, r.y + y * h);
            p.lineWidth = 3f;
            p.lineJoin = LineJoin.Round;
            p.strokeColor = Ink;
            p.fillColor = Gold;
            p.BeginPath();
            p.MoveTo(P(0.12f, 0.85f));
            p.LineTo(P(0.06f, 0.30f));
            p.LineTo(P(0.32f, 0.55f));
            p.LineTo(P(0.50f, 0.18f));
            p.LineTo(P(0.68f, 0.55f));
            p.LineTo(P(0.94f, 0.30f));
            p.LineTo(P(0.88f, 0.85f));
            p.ClosePath();
            p.Fill();
            p.Stroke();
            // Le bandeau du bas, plus foncé.
            p.fillColor = GoldDark;
            p.BeginPath();
            p.MoveTo(P(0.12f, 0.70f));
            p.LineTo(P(0.88f, 0.70f));
            p.LineTo(P(0.88f, 0.85f));
            p.LineTo(P(0.12f, 0.85f));
            p.ClosePath();
            p.Fill();
            p.Stroke();
            // Les perles.
            foreach (var c in new[] { P(0.06f, 0.26f), P(0.50f, 0.13f), P(0.94f, 0.26f) })
            {
                p.fillColor = Color.white;
                p.BeginPath();
                p.Arc(c, Mathf.Min(w, h) * 0.08f, Angle.Degrees(0f), Angle.Degrees(360f));
                p.Fill();
                p.Stroke();
            }
        }

        /// Une patte d'animal rose : un gros coussinet et quatre doigts.
        static void DrawPaw(MeshGenerationContext ctx)
        {
            var r = ctx.visualElement.contentRect;
            var p = ctx.painter2D;
            float s = Mathf.Min(r.width, r.height);
            Vector2 c = r.center;
            p.lineWidth = 3f;
            p.strokeColor = Ink;
            p.fillColor = Pink;
            void Disc(Vector2 at, float rad)
            {
                p.BeginPath();
                p.Arc(at, rad, Angle.Degrees(0f), Angle.Degrees(360f));
                p.Fill();
                p.Stroke();
            }
            Disc(c + new Vector2(0f, s * 0.16f), s * 0.24f);
            Disc(c + new Vector2(-s * 0.32f, -s * 0.06f), s * 0.11f);
            Disc(c + new Vector2(-s * 0.12f, -s * 0.28f), s * 0.11f);
            Disc(c + new Vector2(s * 0.12f, -s * 0.28f), s * 0.11f);
            Disc(c + new Vector2(s * 0.32f, -s * 0.06f), s * 0.11f);
        }

        /// Une étoile dorée à 5 branches, contour d'encre.
        static void DrawStar(MeshGenerationContext ctx)
        {
            var r = ctx.visualElement.contentRect;
            var p = ctx.painter2D;
            Vector2 c = r.center;
            float R = Mathf.Min(r.width, r.height) * 0.46f;
            p.lineWidth = 3f;
            p.lineJoin = LineJoin.Round;
            p.strokeColor = Ink;
            p.fillColor = Gold;
            p.BeginPath();
            for (int i = 0; i < 10; i++)
            {
                float a = -Mathf.PI / 2f + i * Mathf.PI / 5f;
                var pt = c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * (i % 2 == 0 ? R : R * 0.45f);
                if (i == 0) p.MoveTo(pt); else p.LineTo(pt);
            }
            p.ClosePath();
            p.Fill();
            p.Stroke();
        }

        // =====================================================================
        // La pilule de prix (bouton vert en relief)
        // =====================================================================

        /// La grosse pilule verte : [verbe] [icône de la monnaie] [montant], en blanc contouré.
        /// Le verbe peut être vide. Le style (relief, bord bas plus foncé) vient de .btn--vert ;
        /// la taille de .btn-prix (Assets/UI/V51.uss).
        public static Button PriceButton(Action onClick, string verb, int amount, bool diamonds)
        {
            var b = new Button(onClick);
            b.AddToClassList("btn");
            b.AddToClassList("btn--vert");
            b.AddToClassList("btn-prix");
            if (!string.IsNullOrEmpty(verb)) b.Add(ShopUi.NewLabel(verb, "btn-prix__verbe"));
            var icon = new VisualElement { pickingMode = PickingMode.Ignore };
            icon.AddToClassList(diamonds ? "diamant" : "coin");
            icon.AddToClassList("btn-prix__icone");
            b.Add(icon);
            b.Add(ShopUi.NewLabel(ShopUi.Thousands(amount), "btn-prix__montant"));
            return b;
        }

        /// Le texte lisible d'une pilule de prix (« Ouvrir 250 »), pour les tests.
        public static string TextOf(Button b) =>
            b.text.Length > 0 ? b.text : string.Join(" ", b.Query<Label>().ToList().Select(l => l.text));

        // =====================================================================
        // Le rayonnement et les badges de taux
        // =====================================================================

        /// Des rayons blancs doux derrière l'illustration d'une carte (classe .carte-rayons).
        public static VisualElement CardRays()
        {
            var rays = new RaysElement();
            rays.AddToClassList("carte-rayons");
            rays.Configure(12, new Color(1f, 1f, 1f, 0.85f));
            return rays;
        }

        /// Une rangée de vignettes : pour chaque rareté de la boîte (poids > 0), un objet de
        /// cette rareté (le premier de la liste) cerclé de la couleur de rareté, et dessous le
        /// badge du taux de la RARETÉ, lu dans les données de la boîte (toujours vrai).
        /// Un Secret jamais obtenu reste un « ? », comme dans l'Index.
        public static VisualElement RateBadges(LootBox box)
        {
            var row = new VisualElement { pickingMode = PickingMode.Ignore };
            row.AddToClassList("taux-rangee");
            foreach (var rarity in Shop.RaritiesIn(box))
            {
                var items = Shop.ItemsOfRarity(box, rarity.id);
                var slot = new VisualElement { pickingMode = PickingMode.Ignore };
                slot.AddToClassList("taux-objet");
                var frame = new VisualElement { pickingMode = PickingMode.Ignore };
                frame.AddToClassList("taux-objet__cadre");
                ShopUi.ColorBorder(frame, rarity);
                if (items.Count > 0)
                {
                    var item = items[0];
                    bool mystery = Shop.CountOf(item) < 1 && item.RarityId == "secret";
                    frame.Add(ShopUi.Visual(item, mystery ? ShopUi.Look.Mystery : ShopUi.Look.Normal, "taux-objet__visuel"));
                }
                slot.Add(frame);
                var badge = ShopUi.NewLabel(ShopUi.Percent(Shop.RarityChance(box, rarity)), "taux-badge");
                badge.style.backgroundColor = rarity.color;
                slot.Add(badge);
                row.Add(slot);
            }
            return row;
        }

        /// v6.5 §2 : la vitrine des Boîtes Pirates, avec 6 aperçus au lieu de 3 : 2 objets par
        /// rareté (Super rare, Légendaire, Secret). Les deux objets d'une même rareté sont collés
        /// l'un à l'autre et partagent UN badge de taux (le taux de la rareté, pas de l'objet) :
        /// on voit tout de suite qu'il y a beaucoup de bons objets dedans, sans grands vides.
        /// Les Secrets sont TOUJOURS des « ? » ici (spec : « 2 points d'interrogation »), même
        /// déjà obtenus : la vitrine garde la surprise.
        /// Pur affichage : le choix des objets montrés ne change aucune chance.
        public static VisualElement RateBadgesPairs(LootBox box, int perRarity = 2)
        {
            var row = new VisualElement { pickingMode = PickingMode.Ignore };
            row.AddToClassList("taux-rangee");
            row.AddToClassList("taux-rangee--paires");
            foreach (var rarity in Shop.RaritiesIn(box))
            {
                bool secret = rarity.id == "secret";
                var shownItems = ShowcaseItems(box, rarity.id, perRarity);
                var group = new VisualElement { pickingMode = PickingMode.Ignore };
                group.AddToClassList("taux-groupe");
                var frames = new VisualElement { pickingMode = PickingMode.Ignore };
                frames.AddToClassList("taux-groupe__cadres");
                foreach (var item in shownItems)
                {
                    var frame = new VisualElement { pickingMode = PickingMode.Ignore };
                    frame.AddToClassList("taux-objet__cadre");
                    frame.AddToClassList("taux-objet__cadre--petit");
                    ShopUi.ColorBorder(frame, rarity);
                    frame.Add(ShopUi.Visual(item, secret ? ShopUi.Look.Mystery : ShopUi.Look.Normal, "taux-objet__visuel"));
                    frames.Add(frame);
                }
                group.Add(frames);
                var badge = ShopUi.NewLabel(ShopUi.Percent(Shop.RarityChance(box, rarity)), "taux-badge");
                badge.style.backgroundColor = rarity.color;
                group.Add(badge);
                row.Add(group);
            }
            // v6.5 (retouche) : les aperçus prennent TOUTE la largeur de la carte. Une feuille USS
            // ne sait pas faire « un carré dont le côté dépend de la largeur » : on attend donc que
            // la rangée connaisse sa largeur (GeometryChangedEvent), puis on calcule le côté.
            row.RegisterCallback<GeometryChangedEvent>(_ => FitPairs(row));
            return row;
        }

        // Écarts fixes de la vitrine Pirates (pixels de référence) : entre les deux cadres d'une
        // paire (serrés), et entre deux paires.
        const float PairGap = 4f, GroupGap = 14f;

        /// Donne à chaque cadre le même côté, pour que les 3 paires remplissent la rangée.
        static void FitPairs(VisualElement row)
        {
            float width = row.contentRect.width;
            if (float.IsNaN(width) || width <= 0f) return;
            var groups = row.Query<VisualElement>(className: "taux-groupe").ToList();
            int frames = 0;
            foreach (var g in groups) frames += g.Query<VisualElement>(className: "taux-objet__cadre").ToList().Count;
            if (frames == 0) return;
            // largeur = côtés des cadres + écarts dans les paires + écarts entre les paires
            float gaps = PairGap * Mathf.Max(0, frames - groups.Count) + GroupGap * Mathf.Max(0, groups.Count - 1);
            float side = Mathf.Floor(Mathf.Min(110f, (width - gaps) / frames));
            for (int gi = 0; gi < groups.Count; gi++)
            {
                groups[gi].style.marginLeft = gi == 0 ? 0f : GroupGap;
                groups[gi].style.marginRight = 0f;
                var list = groups[gi].Query<VisualElement>(className: "taux-objet__cadre").ToList();
                for (int i = 0; i < list.Count; i++)
                {
                    var f = list[i];
                    if (Mathf.Approximately(f.resolvedStyle.width, side)) continue; // déjà à la bonne taille
                    f.style.width = side;
                    f.style.height = side;
                    f.style.marginLeft = i == 0 ? 0f : PairGap;
                    f.style.marginRight = 0f;
                    var visual = f.Q<VisualElement>(className: "taux-objet__visuel");
                    if (visual != null) { visual.style.width = side * 0.82f; visual.style.height = side * 0.82f; }
                    var question = f.Q<Label>(className: "item-question");
                    if (question != null) question.style.fontSize = side * 0.6f;
                }
            }
        }

        /// Les objets à montrer pour une rareté : les « meilleurs », c'est-à-dire les plus
        /// spectaculaires (ceux qui ont une image, puis ceux qui ont un effet animé), et à
        /// égalité l'ordre de la boîte. Jamais plus que ce que la boîte contient.
        static List<ShopItem> ShowcaseItems(LootBox box, string rarityId, int count)
        {
            var items = Shop.ItemsOfRarity(box, rarityId);
            return items
                .Select((item, index) => (item, index))
                .OrderByDescending(x => x.item.Icon != null)
                .ThenByDescending(x => x.item.SkinFx != SkinFx.Aucun || x.item.Animation != AccessoryAnimation.Aucune)
                .ThenBy(x => x.index)
                .Take(count)
                .Select(x => x.item)
                .ToList();
        }
    }
}
