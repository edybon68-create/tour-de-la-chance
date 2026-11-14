using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace TourDeLaChance
{
    /// <summary>
    /// Le panneau « Inventaire », refait en v6.5 façon « Casier » (spec v6.5 §4, art bible §16.1) :
    /// trois zones qui ont chacune UN rôle, pour qu'on voie tout de suite où l'on est.
    ///   - à GAUCHE : le menu (Skins · Accessoires · Mes skins), la carte « Bonus de tenue » et le
    ///     bouton « Équiper le meilleur » tout en bas ;
    ///   - au MILIEU : le grand titre de la section, son compteur, puis la grille de cases carrées
    ///     colorées selon la rareté (ou les 5 tenues de « Mes skins ») ;
    ///   - à DROITE : la vitrine bleue avec le grand cheval 3D (on le fait tourner en le glissant,
    ///     sans aucune indication à l'écran) et, dessous, la fiche de l'objet sélectionné.
    ///
    /// Choix de la v6.5 (le plus simple) : cliquer une case la SÉLECTIONNE et l'ESSAIE sur le
    /// cheval (rien n'est sauvegardé) ; elle n'est ÉQUIPÉE qu'avec le bouton de la fiche.
    ///
    /// Ce n'est pas un MonoBehaviour : un simple objet créé et piloté par PanelsController. Il
    /// affiche et transmet les clics ; les règles (équiper, 2 accessoires au plus, « Lequel
    /// enlever ? », vendre, meilleure tenue, bonus) restent dans la classe Shop. Toute la page est
    /// construite ici (Panels.uxml ne contient plus que l'élément vide « page-inventaire ») ; les
    /// couleurs et tailles sont dans V65.uss.
    /// </summary>
    public class InventoryPage : IDisposable
    {
        enum Section { Skins = 0, Accessoires = 1, Tenues = 2 }

        // Durées des petites animations (art bible §16.1).
        const float RingPeriod = 1.4f;     // l'anneau or « respire »
        const float SecretPeriod = 3f;     // le reflet qui traverse les cases Secret
        const float SecretSweep = 0.6f;    // … en 0,6 s
        const float ShinePeriod = 4f;      // l'éclat sur « Équiper le meilleur »
        const float RaysTurn = 30f;        // les rayons de la vitrine font un tour en 30 s
        const float BounceTime = 0.25f;    // rebond des cases qui changent (1 → 1,12 → 1)
        const float EntryHeight = 96f, EntryGap = 10f; // les entrées du menu

        readonly VisualElement page;
        readonly PanelsController owner;
        readonly SavedOutfitsView outfits;

        // --- Gauche
        readonly VisualElement cursor;                      // la face bleue + la pointe qui glissent
        readonly Button[] entries = new Button[3];
        readonly Label[] entryCounts = new Label[3];
        readonly Label bonusValue, bonusTry, bonusDiamonds;
        readonly VisualElement gaugeFill, gaugeCheck;
        readonly Button bestButton;
        readonly Label bestText;
        readonly VisualElement bestShine;

        // --- Milieu
        readonly Label sectionTitle, sectionCount;
        readonly VisualElement dots;
        readonly ScrollView list;
        readonly Button sellButton, sellAllButton;

        // --- Droite
        readonly VisualElement scene, horse;
        readonly RaysElement rays;
        readonly Label ficheName, rarityPill, bonusPillText, diamondPill, ficheNote, ficheEmpty, actionText;
        readonly VisualElement pillsRow, bonusPill;
        readonly Button actionButton;

        VisualElement replaceDialog;
        Section section = Section.Skins;
        ShopItem selected;

        // Animations conduites par Tick
        float time;
        readonly List<VisualElement> rings = new List<VisualElement>();
        readonly List<VisualElement> secretShines = new List<VisualElement>();
        readonly List<(VisualElement e, float start)> bounces = new List<(VisualElement, float)>();
        float secretPhase = -1f;

        /// Prévient le PanelsController qu'il faut montrer un objet sur le personnage (essayage).
        public event Action<ShopItem> PreviewRequested;
        /// Une vente a eu lieu (son « vente » + défilement du porte-monnaie).
        public event Action Sold;
        /// Un objet vient d'être équipé ou enlevé (petit saut du cheval).
        public event Action<ShopItem> Equipped;

        /// L'élément où s'affiche l'image du cheval (la caméra de la vitrine y est posée).
        public VisualElement Showcase => horse;
        /// Toute la scène de la vitrine : c'est là qu'on glisse pour faire tourner le cheval.
        public VisualElement ShowcaseArea => scene;

        public InventoryPage(VisualElement page, PanelsController owner)
        {
            this.page = page;
            this.owner = owner;
            page.Clear();
            page.AddToClassList("casier");

            // =================================================================
            // GAUCHE : menu, bonus, « Équiper le meilleur »
            // =================================================================
            var left = Box("casier-gauche");
            page.Add(left);

            var menu = Box("casier-menu");
            left.Add(menu);
            // La face bleue (et sa pointe) est UN seul élément qu'on fait glisser d'une entrée à
            // l'autre (transition USS de 0,18 s). Elle est placée AVANT les entrées : dessous.
            cursor = Box("casier-menu__curseur", PickingMode.Ignore);
            cursor.Add(Box("casier-menu__pointe", PickingMode.Ignore));
            cursor.Add(Box("casier-menu__face", PickingMode.Ignore));
            menu.Add(cursor);
            string[] names = { "Skins", "Accessoires", "Mes skins" };
            string[] icons = { "casier-icone--skins", "casier-icone--accessoires", "casier-icone--tenues" };
            for (int i = 0; i < 3; i++)
            {
                int index = i;
                var entry = new Button(() => OnEntry((Section)index));
                entry.AddToClassList("casier-entree");
                entry.AddToClassList(UiSounds.TabClass);
                var icon = Box("casier-entree__icone", PickingMode.Ignore);
                icon.AddToClassList(icons[i]);
                entry.Add(icon);
                entry.Add(ShopUi.NewLabel(names[i], "casier-entree__texte"));
                entryCounts[i] = ShopUi.NewLabel("", "casier-entree__compteur");
                entry.Add(entryCounts[i]);
                entries[i] = entry;
                menu.Add(entry);
            }

            // La carte « Bonus de tenue » : obligatoire (règle d'or : le bonus est AFFICHÉ).
            var bonus = Box("casier-bonus", PickingMode.Ignore);
            bonus.Add(ShopUi.NewLabel("Bonus de tenue", "casier-bonus__titre"));
            var bonusRow = Box("casier-bonus__rangee", PickingMode.Ignore);
            bonusRow.Add(Box("casier-piece casier-piece--40", PickingMode.Ignore));
            bonusValue = ShopUi.NewLabel("", "casier-bonus__valeur");
            bonusRow.Add(bonusValue);
            bonus.Add(bonusRow);
            var gaugeRow = Box("casier-jauge-rangee", PickingMode.Ignore);
            var gauge = Box("casier-jauge", PickingMode.Ignore);
            gaugeFill = Box("casier-jauge__plein", PickingMode.Ignore);
            gauge.Add(gaugeFill);
            gaugeRow.Add(gauge);
            gaugeCheck = Box("casier-jauge__coche", PickingMode.Ignore);
            AddCheck(gaugeCheck);
            gaugeRow.Add(gaugeCheck);
            gaugeRow.Add(ShopUi.NewLabel($"max {Shop.OutfitBonusGauge} %", "casier-jauge__max"));
            bonus.Add(gaugeRow);
            bonusTry = ShopUi.NewLabel("", "casier-bonus__essai");
            bonus.Add(bonusTry);
            bonusDiamonds = ShopUi.NewLabel("", "casier-bonus__diamants");
            bonus.Add(bonusDiamonds);
            left.Add(bonus);

            // « Équiper le meilleur » : collé en bas de la colonne (margin-top: auto en USS).
            bestButton = new Button(OnBest);
            foreach (var c in new[] { "btn", "bulle", "bulle-rect", "bulle-rect--vert", "casier-meilleur" }) bestButton.AddToClassList(c);
            bestButton.Add(Box("casier-meilleur__medaille", PickingMode.Ignore));
            bestText = ShopUi.NewLabel("", "bulle-rect__texte");
            bestText.AddToClassList("casier-meilleur__texte");
            bestButton.Add(bestText);
            bestShine = Box("btn-eclat", PickingMode.Ignore);
            bestButton.Add(bestShine);
            left.Add(bestButton);

            // =================================================================
            // MILIEU : titre, compteur, grille
            // =================================================================
            var middle = Box("casier-milieu");
            page.Add(middle);
            var header = Box("casier-entete", PickingMode.Ignore);
            sectionTitle = ShopUi.NewLabel("", "casier-titre");
            header.Add(sectionTitle);
            sectionCount = ShopUi.NewLabel("", "casier-compteur");
            header.Add(sectionCount);
            header.Add(Box("casier-entete__vide", PickingMode.Ignore));
            // Accessoires : « 2 au maximum » en deux petits ronds, sans texte.
            dots = Box("casier-ronds", PickingMode.Ignore);
            dots.Add(Box("casier-rond", PickingMode.Ignore));
            dots.Add(Box("casier-rond", PickingMode.Ignore));
            header.Add(dots);
            middle.Add(header);
            middle.Add(Box("casier-filet", PickingMode.Ignore));
            list = new ScrollView();
            list.AddToClassList("shop-scroll");
            list.AddToClassList("casier-liste");
            middle.Add(list);
            // La vente des doublons (fonction gardée de la v5, sous la grille).
            var sellRow = Box("casier-vente");
            sellButton = new Button(OnSell);
            sellButton.AddToClassList("btn"); sellButton.AddToClassList("btn--sombre"); sellButton.AddToClassList("casier-vente__btn");
            sellAllButton = new Button(OnSellAll);
            sellAllButton.AddToClassList("btn"); sellAllButton.AddToClassList("btn--sombre"); sellAllButton.AddToClassList("casier-vente__btn");
            sellRow.Add(sellButton);
            sellRow.Add(sellAllButton);
            middle.Add(sellRow);

            // =================================================================
            // DROITE : la vitrine (scène + fiche)
            // =================================================================
            var vitrine = Box("casier-vitrine");
            page.Add(vitrine);
            scene = Box("casier-scene");
            var glow = Box("casier-scene__lueur", PickingMode.Ignore);
            glow.style.backgroundImage = new StyleBackground(UiFxTextures.Halo);
            scene.Add(glow);
            rays = new RaysElement();
            rays.AddToClassList("casier-scene__rayons");
            scene.Add(rays);
            var socle = Box("casier-scene__socle", PickingMode.Ignore);
            socle.generateVisualContent += DrawSocle;
            scene.Add(socle);
            horse = Box("casier-scene__cheval", PickingMode.Ignore);
            horse.name = "inv-showcase";
            scene.Add(horse);
            vitrine.Add(scene);

            var fiche = Box("casier-fiche", PickingMode.Ignore);
            ficheName = ShopUi.NewLabel("", "casier-fiche__nom");
            fiche.Add(ficheName);
            pillsRow = Box("casier-fiche__pilules", PickingMode.Ignore);
            rarityPill = ShopUi.NewLabel("", "casier-pilule");
            rarityPill.AddToClassList("casier-pilule--rarete");
            pillsRow.Add(rarityPill);
            bonusPill = Box("casier-pilule casier-pilule--bonus", PickingMode.Ignore);
            bonusPill.Add(Box("casier-piece casier-piece--26", PickingMode.Ignore));
            bonusPillText = ShopUi.NewLabel("", "casier-pilule__texte");
            bonusPill.Add(bonusPillText);
            pillsRow.Add(bonusPill);
            diamondPill = ShopUi.NewLabel("", "casier-pilule casier-pilule--diamant");
            pillsRow.Add(diamondPill);
            fiche.Add(pillsRow);
            ficheNote = ShopUi.NewLabel("", "casier-fiche__note");
            fiche.Add(ficheNote);
            ficheEmpty = ShopUi.NewLabel("Choisis un objet", "casier-fiche__vide");
            fiche.Add(ficheEmpty);
            fiche.Add(Box("casier-fiche__ressort", PickingMode.Ignore));
            actionButton = new Button(OnEquip);
            foreach (var c in new[] { "btn", "bulle", "bulle-rect", "casier-action" }) actionButton.AddToClassList(c);
            actionText = ShopUi.NewLabel("", "bulle-rect__texte");
            actionButton.Add(actionText);
            fiche.Add(actionButton);
            vitrine.Add(fiche);

            outfits = new SavedOutfitsView(page);
            outfits.Worn += OnOutfitWorn;
            SavedOutfits.Changed += OnOutfitsChanged;
            ButtonStyler.Apply(page);
        }

        /// Correctif B9 : on se désabonne de TOUT ce à quoi on s'est abonné.
        public void Dispose()
        {
            outfits.Worn -= OnOutfitWorn;
            SavedOutfits.Changed -= OnOutfitsChanged;
        }

        static VisualElement Box(string classes, PickingMode picking = PickingMode.Position)
        {
            var e = new VisualElement { pickingMode = picking };
            foreach (var c in classes.Split(' ')) e.AddToClassList(c);
            return e;
        }

        /// La coche blanche « équipé », faite de deux barres USS tournées (aucune image).
        static void AddCheck(VisualElement badge)
        {
            badge.Add(Box("casier-coche__court", PickingMode.Ignore));
            badge.Add(Box("casier-coche__long", PickingMode.Ignore));
        }

        // =====================================================================
        // Navigation
        // =====================================================================

        /// À l'ouverture : « Skins » par défaut (ou la section de l'objet demandé depuis l'Index).
        public void Open(ShopItem focus = null)
        {
            CloseReplaceDialog();
            if (focus != null) ShowSection(focus.Category == ShopCategory.Skin ? Section.Skins : Section.Accessoires, focus);
            else ShowSection(Section.Skins, null);
        }

        void OnEntry(Section s)
        {
            if (s == section && !outfits.DialogOpen) return;
            ShowSection(s, null);
        }

        void ShowSection(Section s, ShopItem focus)
        {
            section = s;
            outfits.Close();
            CloseReplaceDialog();
            if (s == Section.Tenues) selected = null;
            else
            {
                selected = focus ?? (s == Section.Skins ? Shop.GetEquippedSkin() : Shop.EquippedAccessories().FirstOrDefault());
                if (selected != null && !selected.IsFree && Shop.CountOf(selected) < 1) selected = null;
            }
            // L'essayage ne dure que le temps d'une section : on revient à la tenue en cours,
            // puis on essaie l'objet demandé.
            PreviewRequested?.Invoke(selected);
            Refresh();
            list.scrollOffset = Vector2.zero;
            // La grille s'efface puis réapparaît en remontant de 12 px (transition USS de 0,15 s).
            list.AddToClassList("casier-liste--entree");
            list.schedule.Execute(() => list.RemoveFromClassList("casier-liste--entree")).StartingIn(80);
        }

        void Select(ShopItem item)
        {
            selected = item;
            PreviewRequested?.Invoke(item); // essayage sur le cheval de la vitrine, rien n'est sauvegardé
            Refresh();
        }

        /// Échap ferme d'abord les petites fenêtres.
        public bool HandleEscape()
        {
            if (outfits.HandleEscape()) return true;
            if (replaceDialog == null || replaceDialog.parent == null) return false;
            CloseReplaceDialog();
            return true;
        }

        void OnOutfitsChanged() { if (section == Section.Tenues) Refresh(); }

        void OnOutfitWorn(string message)
        {
            owner.ShowToast(message);
            Equipped?.Invoke(Shop.GetEquippedSkin());
        }

        // =====================================================================
        // Dessin de la page
        // =====================================================================

        /// Redessine la page à partir de la collection.
        public void Refresh()
        {
            rings.Clear();
            secretShines.Clear();
            RefreshMenu();
            RefreshBonus();
            RefreshBestButton();
            RefreshHeader();

            list.Clear();
            if (section == Section.Tenues)
            {
                list.Add(outfits.Build());
                rings.AddRange(list.Query<VisualElement>(className: "tenue__anneau").ToList());
            }
            else
            {
                var grid = Box("casier-grille", PickingMode.Ignore);
                var items = section == Section.Skins
                    ? OwnedItems(i => i.Category == ShopCategory.Skin, includeFree: true)
                    : OwnedItems(i => i.Category == ShopCategory.Accessoire, includeFree: false);
                foreach (var item in items) grid.Add(Case(item));
                if (grid.childCount == 0) grid.Add(ShopUi.NewLabel("Aucun pour l'instant : ouvre des boîtes dans la Boutique !", "casier-grille__vide"));
                list.Add(grid);
            }
            RefreshFiche();
            RefreshSell();
            ButtonStyler.Apply(page);
        }

        void RefreshMenu()
        {
            for (int i = 0; i < 3; i++) ShopUi.SetClass(entries[i], "casier-entree--active", (int)section == i);
            cursor.style.translate = new Translate(0f, (int)section * (EntryHeight + EntryGap));
            entryCounts[0].text = OwnedItems(i => i.Category == ShopCategory.Skin, includeFree: true).Count().ToString();
            entryCounts[1].text = OwnedItems(i => i.Category == ShopCategory.Accessoire, includeFree: false).Count().ToString();
            entryCounts[2].text = $"{SavedOutfits.All().Count(e => !e.IsEmpty)}/{SavedOutfits.SlotCount}";
        }

        /// La carte « Bonus de tenue » : le bonus de la tenue SAUVEGARDÉE, sa jauge (0 à 50 %),
        /// ce que donnerait l'objet essayé, et le bonus de diamants s'il y en a un (v6 §11.3).
        void RefreshBonus()
        {
            int now = Shop.OutfitBonusPercent;
            bonusValue.text = $"+{now} %";
            float k = Mathf.Clamp01(now / (float)Shop.OutfitBonusGauge);
            gaugeFill.style.width = Length.Percent(100f * k);
            gaugeCheck.style.display = k >= 1f ? DisplayStyle.Flex : DisplayStyle.None;

            bool trying = section != Section.Tenues && selected != null && !Shop.IsEquipped(selected);
            int withIt = trying ? Shop.OutfitBonusPercentWith(selected) : 0;
            bonusTry.text = trying ? $"Avec cet objet : +{withIt} %" : "";
            // QA v7 p2 n°8 : en vert seulement si c'est mieux ; une baisse s'affiche en rouge.
            ShopUi.SetClass(bonusTry, "casier-bonus__essai--baisse", trying && withIt < Shop.OutfitBonusPercent);
            bonusTry.style.display = trying ? DisplayStyle.Flex : DisplayStyle.None;

            int raw = Shop.DiamondBonusPercentRaw;
            bool diamonds = raw > 0 || Diamonds.Shards > 0;
            bonusDiamonds.text = diamonds
                ? $"{ShopUi.Diamant} +{Shop.DiamondBonusPercent} % (max {Shop.DiamondBonusCap} %) · éclats : {ShopUi.Number(Diamonds.Shards / 1000f)}"
                : "";
            bonusDiamonds.style.display = diamonds ? DisplayStyle.Flex : DisplayStyle.None;
        }

        /// « Équiper le meilleur », ou en gris « Déjà le meilleur ! » quand il ne changerait rien.
        void RefreshBestButton()
        {
            var best = Shop.BestOutfit();
            bool already = Shop.IsWearing(best.Skin, best.Accessories);
            bestText.text = already ? "Déjà le\nmeilleur !" : "Équiper\nle meilleur";
            bestButton.SetEnabled(!already);
            bestShine.style.display = already ? DisplayStyle.None : DisplayStyle.Flex;
        }

        void RefreshHeader()
        {
            sectionTitle.text = section switch { Section.Skins => "Skins", Section.Accessoires => "Accessoires", _ => "Mes skins" };
            if (section == Section.Tenues)
            {
                sectionCount.text = $"{SavedOutfits.All().Count(e => !e.IsEmpty)} / {SavedOutfits.SlotCount}";
            }
            else
            {
                bool skins = section == Section.Skins;
                Func<ShopItem, bool> filter = skins ? (i => i.Category == ShopCategory.Skin) : (i => i.Category == ShopCategory.Accessoire);
                int owned = OwnedItems(filter, includeFree: skins).Count();
                int total = (Shop.Catalog?.Boxes.SelectMany(b => b.items).Count(i => i != null && filter(i)) ?? 0)
                          + (skins ? Shop.Catalog?.FreeItems.Count(i => i != null && filter(i)) ?? 0 : 0);
                sectionCount.text = $"{owned} / {total}";
            }
            dots.style.display = section == Section.Accessoires ? DisplayStyle.Flex : DisplayStyle.None;
            int worn = Shop.EquippedAccessories().Count;
            for (int i = 0; i < dots.childCount; i++) ShopUi.SetClass(dots[i], "casier-rond--plein", i < worn);
        }

        /// Les objets possédés d'une catégorie : (le gratuit,) puis les autres triés par
        /// rareté (v6 §5.3 : du Commun au Secret), puis par boîte (ordre de la Boutique) et numéro.
        static IEnumerable<ShopItem> OwnedItems(Func<ShopItem, bool> filter, bool includeFree)
        {
            var catalog = Shop.Catalog;
            if (catalog == null) yield break;
            if (includeFree) foreach (var free in catalog.FreeItems.Where(i => i != null && filter(i))) yield return free;
            var boxes = Shop.BoxesInShopOrder.ToList();
            var owned = catalog.Boxes.SelectMany(b => b.items)
                .Where(i => i != null && filter(i) && Shop.CountOf(i) >= 1)
                .OrderBy(Shop.RarityRank)
                .ThenBy(i => boxes.IndexOf(Shop.BoxOf(i)))
                .ThenBy(Shop.NumberOf);
            foreach (var item in owned) yield return item;
        }

        /// Une case de la grille (art bible §16.1.6). La « place » (le bouton) ne découpe rien :
        /// l'anneau or et le badge vert peuvent dépasser ; la case elle-même (fond, icône, bonus)
        /// a ses coins arrondis et coupe ce qui dépasse.
        VisualElement Case(ShopItem item)
        {
            var place = new Button(() => Select(item)) { userData = item };
            place.AddToClassList("casier-place");
            bool isSelected = item == selected;
            ShopUi.SetClass(place, "casier-place--choisie", isSelected);
            if (isSelected)
            {
                // L'anneau or (doublé d'encre pour se voir sur le blanc), DERRIÈRE la case.
                var outer = Box("casier-anneau-encre", PickingMode.Ignore);
                var ring = Box("casier-anneau", PickingMode.Ignore);
                place.Add(outer);
                place.Add(ring);
                rings.Add(ring);
            }

            var box = Box("casier-case case--" + RarityClass(item), PickingMode.Ignore);
            box.Add(ShopUi.Visual(item, ShopUi.Look.Normal, "casier-case__visuel"));
            int count = Shop.CountOf(item);
            if (count >= 2) box.Add(ShopUi.NewLabel("×" + count, "casier-case__nombre"));
            int bonus = Shop.BonusPercent(item);
            if (bonus > 0) box.Add(BonusBadge(bonus, "casier-badge-bonus"));
            int diamondBonus = Shop.DiamondBonusPercentOf(item);
            if (diamondBonus > 0) box.Add(ShopUi.NewLabel($"{ShopUi.Diamant}+{diamondBonus} %", "casier-case__diamant"));
            if (item.RarityId == "secret")
            {
                // Le reflet arc-en-ciel qui traverse la case Secret toutes les 3 s (§13.6).
                var shine = Box("casier-case__reflet", PickingMode.Ignore);
                shine.generateVisualContent += DrawSecretShine;
                box.Add(shine);
                secretShines.Add(shine);
            }
            place.Add(box);

            if (Shop.IsEquipped(item))
            {
                var badge = Box("casier-badge-equipe", PickingMode.Ignore);
                AddCheck(badge);
                place.Add(badge);
            }
            return place;
        }

        /// La pilule « pièce + +5 % » (coin bas gauche des cases, ligne des tenues).
        public static VisualElement BonusBadge(int percent, string className)
        {
            var pill = Box(className, PickingMode.Ignore);
            pill.Add(Box("casier-piece casier-badge-bonus__piece", PickingMode.Ignore));
            pill.Add(ShopUi.NewLabel($"+{percent} %", "casier-badge-bonus__texte"));
            return pill;
        }

        /// Le nom de classe USS d'une rareté (« super_rare » → « super_rare »), « commun » à défaut.
        public static string RarityClass(ShopItem item)
        {
            string id = item != null ? item.RarityId : "";
            return string.IsNullOrEmpty(id) ? "commun" : id;
        }

        // ---------- La fiche ----------

        void RefreshFiche()
        {
            bool outfitsView = section == Section.Tenues;
            var item = outfitsView ? null : selected;
            bool hasItem = item != null;
            ficheEmpty.style.display = !hasItem && !outfitsView ? DisplayStyle.Flex : DisplayStyle.None;
            ficheName.style.display = hasItem || outfitsView ? DisplayStyle.Flex : DisplayStyle.None;
            pillsRow.style.display = hasItem || outfitsView ? DisplayStyle.Flex : DisplayStyle.None;
            ficheNote.text = "";
            UpdateRays(item);

            if (outfitsView)
            {
                // « Mes skins » : la fiche résume la tenue que porte le cheval.
                SetName("Ta tenue");
                rarityPill.style.display = DisplayStyle.None;
                bonusPill.style.display = DisplayStyle.Flex;
                bonusPillText.text = $"+{Shop.OutfitBonusPercent} % de pièces";
                int d = Shop.DiamondBonusPercent;
                diamondPill.style.display = d > 0 ? DisplayStyle.Flex : DisplayStyle.None;
                diamondPill.text = $"{ShopUi.Diamant} +{d} %";
                actionButton.style.display = DisplayStyle.None;
                return;
            }
            if (!hasItem)
            {
                actionButton.style.display = DisplayStyle.None;
                return;
            }

            SetName(item.IsFree || Shop.CountOf(item) < 2 ? item.DisplayName : $"{item.DisplayName} ×{Shop.CountOf(item)}");
            var rarity = Shop.RarityOf(item);
            rarityPill.style.display = rarity != null && !item.IsFree ? DisplayStyle.Flex : DisplayStyle.None;
            rarityPill.text = rarity != null ? rarity.displayName : "";
            foreach (var g in UiFxTextures.CaseGradients) rarityPill.RemoveFromClassList("casier-pilule--" + g.file.Substring(5));
            rarityPill.AddToClassList("casier-pilule--" + RarityClass(item));
            int bonus = Shop.BonusPercent(item);
            bonusPill.style.display = DisplayStyle.Flex;
            bonusPillText.text = $"+{bonus} % de pièces";
            int diamondBonus = Shop.DiamondBonusPercentOf(item);
            diamondPill.style.display = diamondBonus > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            diamondPill.text = $"{ShopUi.Diamant} +{diamondBonus} %";

            bool equipped = Shop.IsEquipped(item);
            bool accessory = item.Category == ShopCategory.Accessoire && !item.IsFree;
            if (!equipped)
            {
                // v5 §4.4 : « Remplace : Couronne » (même emplacement déjà occupé).
                var replaced = Shop.ReplacedBySameSlot(item);
                if (replaced != null) ficheNote.text = $"Remplace : {replaced.DisplayName}";
            }

            // Le bouton : Équiper (vert), Enlever (violet), ou « Équipé » en gris pour le skin
            // porté (on ne peut pas enlever un skin, on en met un autre).
            actionButton.style.display = DisplayStyle.Flex;
            actionButton.RemoveFromClassList("bulle-rect--vert");
            actionButton.RemoveFromClassList("bulle-rect--violet");
            if (equipped && accessory) { actionText.text = "Enlever"; actionButton.AddToClassList("bulle-rect--violet"); actionButton.SetEnabled(true); }
            else if (equipped) { actionText.text = "Équipé"; actionButton.AddToClassList("bulle-rect--vert"); actionButton.SetEnabled(false); }
            else { actionText.text = "Équiper"; actionButton.AddToClassList("bulle-rect--vert"); actionButton.SetEnabled(Shop.IsOwned(item)); }
        }

        /// Le nom, réduit de 40 à 30 px s'il est long (une seule ligne).
        void SetName(string text)
        {
            ficheName.text = text;
            ficheName.style.fontSize = text.Length > 16 ? Mathf.Max(30f, 40f - (text.Length - 16) * 1.5f) : 40f;
        }

        /// Rayons de la couleur de la rareté derrière le cheval : seulement de Super rare à Secret
        /// (§13.6 : plus c'est rare, plus il y en a).
        void UpdateRays(ShopItem item)
        {
            var rarity = item != null && !item.IsFree ? Shop.RarityOf(item) : null;
            bool show = rarity != null && Shop.RarityRank(item) >= 3;
            rays.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
            if (show)
            {
                var c = rarity.color; c.a = 0.4f;
                rays.Configure(12, c);
            }
        }

        void RefreshSell()
        {
            bool canSell = section != Section.Tenues && selected != null && Shop.CanSellDuplicate(selected);
            sellButton.style.display = canSell ? DisplayStyle.Flex : DisplayStyle.None;
            if (canSell) sellButton.text = $"Vendre 1 doublon (+{Shop.SellValue(selected)})";
            int total = Shop.DuplicatesTotal();
            sellAllButton.text = total > 0 ? $"Vendre tous les doublons (+{total})" : "Aucun doublon";
            sellAllButton.SetEnabled(total > 0);
        }

        // =====================================================================
        // Les actions (les règles sont dans Shop)
        // =====================================================================

        /// Le bouton de la fiche : Équiper, ou Enlever un accessoire porté.
        void OnEquip()
        {
            if (selected == null) return;
            bool accessory = selected.Category == ShopCategory.Accessoire && !selected.IsFree;
            if (accessory && Shop.IsEquipped(selected))
            {
                if (Shop.Unequip(selected.Slot)) { Equipped?.Invoke(selected); TryOnSelected(); }
                return;
            }
            if (Shop.NeedsReplaceChoice(selected)) { OpenReplaceDialog(selected); return; }
            if (Shop.TryEquip(selected)) { Equipped?.Invoke(selected); BounceItems(new[] { selected }); }
        }

        /// « Équiper le meilleur » : la tenue de Shop.BestOutfit, équipée en une fois. Pas de
        /// confirmation (rien n'est perdu). Petit message « Tenue : +22 % → +40 % ».
        void OnBest()
        {
            var best = Shop.BestOutfit();
            if (Shop.IsWearing(best.Skin, best.Accessories)) return;
            var before = new HashSet<ShopItem>(Shop.EquippedAccessories()) { Shop.GetEquippedSkin() };
            int beforePercent = Shop.OutfitBonusPercent;
            Shop.TryEquipOutfit(best.Skin, best.Accessories.Count > 0 ? best.Accessories[0] : null, best.Accessories.Count > 1 ? best.Accessories[1] : null);
            owner.ShowToast($"Tenue : +{beforePercent} % → +{Shop.OutfitBonusPercent} %");
            Equipped?.Invoke(best.Skin);
            var changed = new List<ShopItem> { best.Skin };
            changed.AddRange(best.Accessories);
            BounceItems(changed.Where(i => i != null && !before.Contains(i)));
            TryOnSelected();
        }

        /// Après un changement de tenue, le cheval remet sa tenue (PanelsController) : si l'objet
        /// sélectionné n'est pas porté, on l'essaie de nouveau, pour que la vitrine montre
        /// toujours « la tenue + l'objet sélectionné ».
        void TryOnSelected()
        {
            if (section != Section.Tenues && selected != null && !Shop.IsEquipped(selected)) PreviewRequested?.Invoke(selected);
        }

        /// Les cases des objets qui viennent d'être équipés font un petit rebond, l'une après
        /// l'autre (0,06 s d'écart). La page est redessinée juste après (Shop.Changed) : on
        /// cherche donc les cases à l'image suivante.
        void BounceItems(IEnumerable<ShopItem> items)
        {
            var wanted = items.ToList();
            list.schedule.Execute(() =>
            {
                int n = 0;
                foreach (var place in list.Query<Button>(className: "casier-place").ToList())
                    if (place.userData is ShopItem it && wanted.Contains(it)) bounces.Add((place, time + 0.06f * n++));
            }).StartingIn(20);
        }

        void OnSell()
        {
            if (Shop.TrySellDuplicate(selected)) Sold?.Invoke();
        }

        void OnSellAll()
        {
            if (Shop.SellAllDuplicates() > 0) Sold?.Invoke();
        }

        // =====================================================================
        // « Tu portes déjà 2 accessoires. Lequel enlever ? » (v5 §4.4)
        // =====================================================================

        void OpenReplaceDialog(ShopItem item)
        {
            CloseReplaceDialog();
            replaceDialog = new VisualElement();
            replaceDialog.AddToClassList("v5-confirm");
            var box = new VisualElement();
            box.AddToClassList("v5-confirm-box");
            box.Add(ShopUi.NewLabel("Tu portes déjà 2 accessoires. Lequel enlever ?", "v5-confirm-text"));
            var cards = new VisualElement();
            cards.AddToClassList("v5-replace-cards");
            foreach (var worn in Shop.EquippedAccessories())
            {
                var w = worn;
                var card = ShopUi.ItemCard(w, ShopUi.Look.Normal, false, () => Replace(item, w), out var inner);
                inner.Add(ShopUi.NewLabel(SlotName(w.Slot), "item-extra"));
                cards.Add(card);
            }
            box.Add(cards);
            var cancel = new Button(CloseReplaceDialog) { text = "Annuler" };
            cancel.AddToClassList("btn"); cancel.AddToClassList("btn--sombre"); cancel.AddToClassList("v5-confirm-btn");
            box.Add(cancel);
            replaceDialog.Add(box);
            page.Add(replaceDialog);
            ButtonStyler.Apply(replaceDialog);
        }

        void Replace(ShopItem item, ShopItem toRemove)
        {
            CloseReplaceDialog();
            if (Shop.TryEquip(item, toRemove)) { Equipped?.Invoke(item); BounceItems(new[] { item }); }
        }

        void CloseReplaceDialog()
        {
            if (replaceDialog != null) replaceDialog.RemoveFromHierarchy();
            replaceDialog = null;
        }

        // =====================================================================
        // Les petites animations (appelé à chaque image quand l'Inventaire est ouvert)
        // =====================================================================

        public void Tick(float dt)
        {
            time += dt;
            // L'anneau or respire : opacité 1 → 0,7 → 1 en 1,4 s.
            float breath = 0.85f + 0.15f * Mathf.Cos(2f * Mathf.PI * time / RingPeriod);
            foreach (var r in rings) r.style.opacity = breath;
            // Les rayons de la vitrine tournent (un tour en 30 s).
            if (rays.resolvedStyle.display == DisplayStyle.Flex) rays.style.rotate = new Rotate(time * 360f / RaysTurn);
            // Le reflet des cases Secret.
            float phase = Mathf.Repeat(time, SecretPeriod);
            float newPhase = phase < SecretSweep ? phase / SecretSweep : -1f;
            if (secretShines.Count > 0 && (newPhase >= 0f || secretPhase >= 0f))
                foreach (var s in secretShines) s.MarkDirtyRepaint();
            secretPhase = newPhase;
            // L'éclat sur « Équiper le meilleur » : traverse en 0,5 s toutes les 4 s.
            TickShine();
            // Les rebonds des cases (1 → 1,12 → 1 en 0,25 s).
            for (int i = bounces.Count - 1; i >= 0; i--)
            {
                var (e, start) = bounces[i];
                float u = (time - start) / BounceTime;
                if (u < 0f) continue;
                if (u >= 1f || e.panel == null) { e.style.scale = StyleKeyword.Null; bounces.RemoveAt(i); continue; }
                float s = 1f + 0.12f * Mathf.Sin(u * Mathf.PI);
                e.style.scale = new Scale(new Vector3(s, s, 1f));
            }
        }

        void TickShine()
        {
            if (bestShine.resolvedStyle.display != DisplayStyle.Flex) return;
            float width = bestButton.resolvedStyle.width;
            if (float.IsNaN(width) || width <= 0f) return;
            float phase = Mathf.Repeat(time, ShinePeriod);
            if (phase > 0.5f) { bestShine.style.opacity = 0f; return; }
            bestShine.style.opacity = 1f;
            bestShine.style.left = Mathf.Lerp(-60f, width + 20f, phase / 0.5f);
        }

        /// La bande blanche (35 %, 30 px) qui traverse une case Secret en diagonale.
        void DrawSecretShine(MeshGenerationContext ctx)
        {
            if (secretPhase < 0f) return;
            var r = ctx.visualElement.contentRect;
            if (r.width <= 0f) return;
            float x = Mathf.Lerp(-r.width * 0.6f, r.width * 1.6f, secretPhase);
            var p = ctx.painter2D;
            p.fillColor = new Color(1f, 1f, 1f, 0.35f);
            p.BeginPath();
            // Un parallélogramme penché : la bande monte vers la droite.
            p.MoveTo(new Vector2(x, r.yMax));
            p.LineTo(new Vector2(x + 30f, r.yMax));
            p.LineTo(new Vector2(x + 30f + r.height * 0.6f, r.yMin));
            p.LineTo(new Vector2(x + r.height * 0.6f, r.yMin));
            p.ClosePath();
            p.Fill();
        }

        /// Le socle : une ellipse bleu foncé à 45 % sous le cheval (dessinée par le code).
        static void DrawSocle(MeshGenerationContext ctx)
        {
            var r = ctx.visualElement.contentRect;
            if (r.width <= 0f) return;
            var p = ctx.painter2D;
            p.fillColor = new Color(0.122f, 0.29f, 0.549f, 0.45f); // #1F4A8C
            p.BeginPath();
            const int steps = 48;
            for (int i = 0; i <= steps; i++)
            {
                float a = i * Mathf.PI * 2f / steps;
                var pt = r.center + new Vector2(Mathf.Cos(a) * r.width * 0.5f, Mathf.Sin(a) * r.height * 0.5f);
                if (i == 0) p.MoveTo(pt); else p.LineTo(pt);
            }
            p.ClosePath();
            p.Fill();
        }

        // =====================================================================
        // Pour les tests
        // =====================================================================

        /// La carte du bonus, en une ligne (tenue, essai, diamants).
        public string BonusText => $"Bonus de tenue : {bonusValue.text}" + (bonusTry.text.Length > 0 ? " · " + bonusTry.text : "") + (bonusDiamonds.text.Length > 0 ? " · " + bonusDiamonds.text : "");
        public bool ReplaceDialogOpen => replaceDialog != null;
        /// Ce que dit la fiche (nom | bouton).
        public string FicheText => $"{ficheName.text} | {(actionButton.resolvedStyle.display == DisplayStyle.Flex ? actionText.text : "-")}";
        public string SectionText => $"{sectionTitle.text} {sectionCount.text}";
        /// OUTIL DE TEST : sélectionne une case (comme un clic).
        public void DebugSelect(ShopItem item)
        {
            var wanted = item.Category == ShopCategory.Skin ? Section.Skins : Section.Accessoires;
            if (wanted != section) ShowSection(wanted, item); else Select(item);
        }
        /// OUTILS DE TEST : les entrées du menu (0 Skins, 1 Accessoires, 2 Mes skins).
        public void DebugEntry(int index) => OnEntry((Section)index);
        public void DebugOutfitsTab() => OnEntry(Section.Tenues);
        public void DebugClickBest() => OnBest();
        public string BestButtonText => bestText.text.Replace("\n", " ");
        public SavedOutfitsView Outfits => outfits;
        /// OUTIL DE TEST : clic sur le bouton de la fiche (Équiper / Enlever).
        public void DebugClickEquip() => OnEquip();
        /// OUTIL DE TEST : dans la fenêtre « Lequel enlever ? », clic sur l'objet à enlever (null = Annuler).
        public void DebugChooseReplace(ShopItem toRemove)
        {
            if (replaceDialog == null) return;
            if (toRemove == null) CloseReplaceDialog();
            else Replace(selected, toRemove);
        }

        public static string SlotName(AccessorySlot slot) => slot switch
        {
            AccessorySlot.Tete => "Tête",
            AccessorySlot.Visage => "Visage",
            AccessorySlot.Cou => "Cou",
            _ => "Dos"
        };
    }
}
