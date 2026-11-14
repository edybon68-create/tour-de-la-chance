using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace TourDeLaChance
{
    /// <summary>
    /// Le panneau « Améliorations » (spec v3 §5.7, v4 §10.6) : une carte par amélioration, avec
    /// son niveau (10 pastilles), l'effet actuel → l'effet suivant, et le bouton « Améliorer ».
    /// Ce n'est pas un MonoBehaviour : un simple objet créé et piloté par PanelsController.
    ///
    /// Il AFFICHE et TRANSMET : l'achat est décidé par Upgrades.TryBuy, et TOUS les chiffres
    /// (prix, multiplicateurs, chances, risque) sont lus dans UpgradeCatalog via Upgrades :
    /// changer un poids dans le catalogue change le pourcentage affiché, sans toucher à l'UI.
    ///
    /// v7 (art bible §17.4) : ÉPURÉ, comme les trois cartes de la Renaissance. Les 3 cartes
    /// sont côte à côte, SANS défilement : l'icône (qui flotte, sur ses rayons), le nom, le
    /// niveau en jauge de 10 segments, l'effet AVANT → APRÈS en grand (règle d'or : l'effet reste
    /// visible sans rien ouvrir), un gros bouton (prix seul). La description et le tableau
    /// détaillé sont derrière le bouton « i ». Une seule phrase pour toute la fenêtre, en pied.
    /// </summary>
    public class UpgradesPage : IDisposable
    {
        readonly ScrollView list;
        readonly MonoBehaviour host; // pour lancer les petites animations (coroutines)

        /// Un niveau vient d'être acheté (id, niveau atteint) : saut du cheval, son…
        public event Action<string, int> Bought;

        // Animation d'achat en cours : pendant 0,3 s, le bouton de la carte est inactif.
        float buyLockedUntil = -1f;
        string flashId;
        int popLevel = -1;

        // v5.1 : la carte a un fond en dégradé (une image) : le flash or TEINTE cette image
        // (blanc = couleurs normales, or pâle = la carte s'illumine).
        static readonly Color CardTint = Color.white;
        static readonly Color FlashTint = new Color(1f, 0.93f, 0.55f);

        public UpgradesPage(ScrollView page, MonoBehaviour coroutineHost)
        {
            list = page;
            host = coroutineHost;
        }

        public void Dispose() { }

        public void Open()
        {
            CloseInfo();
            Refresh();
            list.scrollOffset = Vector2.zero;
        }

        // Les icônes qui flottent et leurs rayons qui tournent (animés par Tick).
        readonly List<(VisualElement icon, RaysElement rays, float phase)> floaters = new List<(VisualElement, RaysElement, float)>();
        readonly List<VisualElement> nextSegments = new List<VisualElement>(); // (V7.1 : plus de segments, liste gardée vide)
        VisualElement infoVeil;

        public void Refresh()
        {
            list.Clear();
            floaters.Clear();
            nextSegments.Clear();
            var catalog = Upgrades.Catalog;
            if (catalog == null) return;
            // Pas de défilement : tout tient dans la fenêtre (1280 × 800).
            list.verticalScrollerVisibility = ScrollerVisibility.Hidden;
            list.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            list.AddToClassList("up7-page");

            // En haut à droite : le solde de pièces.
            var head = new VisualElement { pickingMode = PickingMode.Ignore };
            head.AddToClassList("up7-entete");
            var balance = new VisualElement { pickingMode = PickingMode.Ignore };
            balance.AddToClassList("up7-solde");
            balance.Add(Icon("coin up7-solde__piece"));
            balance.Add(ShopUi.NewLabel(ShopUi.Thousands(Wallet.Coins), "up7-solde__texte"));
            head.Add(balance);
            list.Add(head);

            var row = new VisualElement { pickingMode = PickingMode.Ignore };
            row.AddToClassList("up7-cartes");
            foreach (var def in catalog.Upgrades) row.Add(Card(def));
            list.Add(row);
            // UNE phrase pour toutes les cartes.
            list.Add(ShopUi.NewLabel("Chaque amélioration compte à partir du prochain étage.", "up7-pied"));
            ButtonStyler.Apply(list);
        }

        /// À chaque image (appelé par PanelsController) : les icônes flottent (±5 px en 2,4 s),
        /// leurs rayons tournent (un tour en 40 s), le prochain segment clignote doucement.
        public void Tick()
        {
            // v7 (point 14) : AUCUN défilement : tout tient dans la fenêtre, la liste reste en haut.
            if (list.scrollOffset != Vector2.zero) list.scrollOffset = Vector2.zero;
            float t = Time.realtimeSinceStartup;
            foreach (var (icon, rays, phase) in floaters)
            {
                icon.style.translate = new Translate(0f, 5f * Mathf.Sin(t * Mathf.PI * 2f / 2.4f + phase));
                rays.style.rotate = new Rotate(t * 9f);
            }
            float blink = 0.22f + 0.115f * (1f - Mathf.Cos(t * Mathf.PI * 2f / 1.2f));
            foreach (var seg in nextSegments) seg.style.backgroundColor = new Color(0.067f, 0.051f, 0.122f, blink);
        }

        static VisualElement Icon(string classes)
        {
            var e = new VisualElement { pickingMode = PickingMode.Ignore };
            foreach (var c in classes.Split(' ')) e.AddToClassList(c);
            return e;
        }

        VisualElement Card(UpgradeDef def)
        {
            int level = Upgrades.Level(def.id);
            bool max = level >= Upgrades_.MaxLevel;

            var card = new VisualElement();
            card.AddToClassList("up-card");
            card.AddToClassList("up7-carte");
            // v5.1 (art bible §12.18) : une couleur vive par amélioration.
            card.AddToClassList("carte");
            card.AddToClassList(def.id switch
            {
                Upgrades_.Gains => "carte--or",
                Upgrades_.Chests => "carte--violette",
                _ => "carte--bleue"
            });

            // Le bouton « i » : la description, le tableau détaillé et le prix du niveau suivant.
            var info = new Button(() => OpenInfo(def));
            foreach (var c in new[] { "btn", "bulle", "bulle-petit", "bulle-petit--ardoise", "up7-info" }) info.AddToClassList(c);
            info.Add(ShopUi.NewLabel("i", "up7-info__texte"));
            card.Add(info);

            // 1. L'icône (150 px) sur sa lueur et ses rayons ; elle flotte.
            var hero = new VisualElement { pickingMode = PickingMode.Ignore };
            hero.AddToClassList("up7-heros");
            var glow = new VisualElement { pickingMode = PickingMode.Ignore };
            glow.AddToClassList("up7-heros__lueur");
            glow.style.backgroundImage = new StyleBackground(UiFxTextures.Halo);
            hero.Add(glow);
            var rays = new RaysElement();
            rays.AddToClassList("up7-heros__rayons");
            rays.Configure(10, new Color(1f, 1f, 1f, 0.30f));
            hero.Add(rays);
            var icon = Icon("up7-heros__icone");
            if (def.icon != null) icon.style.backgroundImage = new StyleBackground(def.icon);
            hero.Add(icon);
            floaters.Add((icon, rays, floaters.Count * 0.8f));
            card.Add(hero);

            // 2. Le nom, puis (v7, retour de l'auteur, point 14) UNE phrase courte qui dit ce que
            //    fait l'amélioration, visible sans ouvrir le « i » (texte des données du catalogue).
            card.Add(ShopUi.NewLabel(def.displayName, "up7-nom"));
            if (!string.IsNullOrEmpty(def.description))
            {
                // V7.1 n°3 : la phrase dans la couleur foncée de la famille de la carte, sans contour.
                var phrase = ShopUi.NewLabel(def.description, "up7-phrase");
                phrase.AddToClassList("up7-phrase--" + def.id);
                card.Add(phrase);
            }

            // 3. Le niveau : 10 segments, et « 2/10 ».
            var levelRow = new VisualElement { pickingMode = PickingMode.Ignore };
            levelRow.AddToClassList("up7-niveau");
            // V7.1 n°3 : une JAUGE CONTINUE (plus de cases à cocher qui se lisaient à l'envers) :
            // remplissage blanc jusqu'au niveau acquis, 9 crans qui découpent les 10 niveaux, or au MAX.
            var dots = new List<VisualElement>();
            var gauge = Icon("up7-jauge");
            var fill = Icon("up7-jauge__plein" + (max ? " up7-jauge__plein--max" : ""));
            fill.style.width = Length.Percent(100f * level / Upgrades_.MaxLevel);
            if (!max) fill.style.backgroundImage = new StyleBackground(GaugeGradient);
            fill.Add(Icon("up7-jauge__reflet"));
            gauge.Add(fill);
            for (int i = 1; i < Upgrades_.MaxLevel; i++)
            {
                var notch = Icon("up7-jauge__cran");
                notch.style.left = Length.Percent(100f * i / Upgrades_.MaxLevel);
                gauge.Add(notch);
            }
            dots.Add(fill); // le « pop » après un achat s'applique au remplissage
            levelRow.Add(gauge);
            levelRow.Add(ShopUi.NewLabel($"{level}/{Upgrades_.MaxLevel}", "up7-niveau__texte"));
            card.Add(levelRow);

            // 4. L'effet, le héros de la carte : AVANT › APRÈS (ou la valeur actuelle au max).
            var well = Icon("up7-effet");
            switch (def.id)
            {
                case Upgrades_.Gains: GainsEffect(well, level, max); break;
                case Upgrades_.Chests: ChestsEffect(well, level, max); break;
                case Upgrades_.Traps: TrapsEffect(well, level, max); break;
            }
            card.Add(well);

            // 5. Le bouton : prix seul (le vert dit « améliorer »), gris s'il manque des pièces ; MAX en or.
            if (max)
            {
                var maxPill = Icon("up7-max");
                maxPill.Add(Icon("up7-max__medaille"));
                maxPill.Add(ShopUi.NewLabel("MAX", "up7-max__texte"));
                card.Add(maxPill);
            }
            else
            {
                string id = def.id;
                bool canBuy = Upgrades.CanBuy(id) && Time.time >= buyLockedUntil;
                // v7 (retour de l'auteur, point 15, appliqué aussi ici pour rester cohérent) :
                // le PRIX est toujours sur le bouton ; gris s'il n'y a pas assez de pièces.
                var buy = new Button(() => OnBuy(id));
                buy.Add(Icon("coin up7-acheter__piece"));
                buy.Add(ShopUi.NewLabel(ShopUi.Thousands(Upgrades.NextPrice(id)), "bulle-rect__texte up7-acheter__prix"));
                foreach (var c in new[] { "btn", "bulle", "bulle-rect", "bulle-rect--vert", "up-buy", "up7-acheter", UiSounds.SilentClass }) buy.AddToClassList(c);
                buy.name = "buy-" + id;
                buy.SetEnabled(canBuy);
                card.Add(buy);
            }

            // Animation de l'achat qui vient d'avoir lieu sur CETTE carte.
            if (flashId == def.id && host != null)
            {
                flashId = null;
                host.StartCoroutine(FlashCard(card));
                if (popLevel >= 1 && dots.Count > 0) host.StartCoroutine(PopDot(dots[0]));
                host.StartCoroutine(JumpIcon(icon));
            }
            return card;
        }

        // ---------- Le puits « Effet » (tous les nombres viennent des fonctions du jeu) ----------

        /// « ×1,4 › ×1,6 » : l'avant en petit (75 %), l'après en grand ; dessous « par trésor ».
        static VisualElement BeforeAfter(string before, string after, bool max)
        {
            var line = Icon("up7-avant-apres");
            if (max) { line.Add(ShopUi.NewLabel(after, "up7-apres")); return line; }
            line.Add(ShopUi.NewLabel(before, "up7-avant"));
            line.Add(Chevron());
            line.Add(ShopUi.NewLabel(after, "up7-apres"));
            return line;
        }

        /// Le chevron « › » : deux barres blanches contour d'encre.
        static VisualElement Chevron()
        {
            var c = Icon("up7-chevron");
            c.Add(Icon("up7-chevron__barre up7-chevron__barre--a"));
            c.Add(Icon("up7-chevron__barre up7-chevron__barre--b"));
            return c;
        }

        static void GainsEffect(VisualElement well, int level, bool max)
        {
            string now = $"×{ShopUi.Number(Upgrades.GainMultiplier(level))}";
            string next = $"×{ShopUi.Number(Upgrades.GainMultiplier(max ? level : level + 1))}";
            well.Add(BeforeAfter(now, next, max));
            well.Add(ShopUi.NewLabel("par trésor", "up7-etiquette"));
        }

        /// Deux barres empilées « Avant » / « Après », découpées selon les chances de chaque
        /// rareté de coffre (une part sous 4 % garde au moins 6 px pour rester visible).
        static void ChestsEffect(VisualElement well, int level, bool max)
        {
            var now = Upgrades.ChestWeights(level);
            var next = Upgrades.ChestWeights(max ? level : level + 1);
            well.Add(ChanceBar(max ? "Niveau" : "Avant", now));
            if (!max) well.Add(ChanceBar("Après", next));
            // QA v7 p2 n°12 : la légende des couleurs (une pastille par rareté de coffre possible).
            var legend = Icon("up7-legende");
            for (int i = 0; i < Upgrades.RarityCount; i++)
            {
                bool used = (i < now.Length && now[i] > 0) || (i < next.Length && next[i] > 0);
                if (!used) continue;
                var item = Icon("up7-legende__item");
                var dot = Icon("up7-legende__pastille");
                dot.style.backgroundColor = SignalColor(i);
                item.Add(dot);
                item.Add(ShopUi.NewLabel(Upgrades.Rarity(i).shortName, "up7-legende__texte"));
                legend.Add(item);
            }
            well.Add(legend);
        }

        // V7.1 n°3 : couleurs de SIGNAL des raretés de coffre (bois, bronze, argent, or, diamant) :
        // les couleurs des modèles 3D se confondaient dans la barre.
        static readonly Color[] Signal = { new Color32(0xC0, 0x8A, 0x5A, 255), new Color32(0xE8, 0x85, 0x3A, 255), new Color32(0xD6, 0xDE, 0xEA, 255), new Color32(0xFF, 0xD5, 0x4A, 255), new Color32(0x7F, 0xE3, 0xFF, 255) };
        static Color SignalColor(int rank) => rank >= 0 && rank < Signal.Length ? Signal[rank] : Upgrades.Rarity(rank).bodyColor;

        static Texture2D gaugeGradient;
        /// Le dégradé du remplissage de la jauge : #FFFFFF en haut → #FFF3B0 en bas.
        static Texture2D GaugeGradient => gaugeGradient != null ? gaugeGradient
            : gaugeGradient = UiFxTextures.VerticalGradient(Color.white, new Color32(0xFF, 0xF3, 0xB0, 255));

        static VisualElement ChanceBar(string label, int[] weights)
        {
            var row = Icon("up7-barre-ligne");
            row.Add(ShopUi.NewLabel(label, "up7-barre-ligne__texte"));
            var bar = Icon("up7-barre");
            int total = 0;
            for (int i = 0; i < weights.Length; i++) total += Mathf.Max(0, weights[i]);
            for (int i = 0; i < weights.Length && i < Upgrades.RarityCount; i++)
            {
                if (weights[i] <= 0 || total <= 0) continue;
                var part = Icon("up7-barre__part");
                part.style.flexGrow = weights[i];
                part.style.minWidth = 6f;
                part.style.backgroundColor = SignalColor(i);
                bar.Add(part);
            }
            row.Add(bar);
            return row;
        }

        /// « 25 % › 50 % » des étages au plus grand nombre de coffres possible, puis le risque.
        static void TrapsEffect(VisualElement well, int level, bool max)
        {
            int[] now = Upgrades.CountWeights(level);
            int[] next = Upgrades.CountWeights(max ? level : level + 1);
            int baseCount = Upgrades.Catalog != null ? Upgrades.Catalog.BaseChestCount : 3;
            // Le plus grand nombre de coffres qui a une chance (au niveau suivant).
            int top = 0;
            for (int i = 0; i < next.Length; i++) if (next[i] > 0) top = i;
            int a = top < now.Length ? now[top] : 0, b = top < next.Length ? next[top] : 0;
            well.Add(BeforeAfter($"{a}\u00A0%", $"{b}\u00A0%", max));
            well.Add(ShopUi.NewLabel($"étages à {baseCount + top} coffres", "up7-etiquette"));
            int riskNow = Mathf.RoundToInt(Upgrades.AverageRiskPercent(level));
            int riskNext = Mathf.RoundToInt(Upgrades.AverageRiskPercent(max ? level : level + 1));
            well.Add(ShopUi.NewLabel(max ? $"Risque : {riskNow}\u00A0%" : $"Risque : {riskNow}\u00A0% › {riskNext}\u00A0%", "up7-risque"));
        }

        // ---------- La petite fenêtre « i » ----------

        void OpenInfo(UpgradeDef def)
        {
            CloseInfo();
            int level = Upgrades.Level(def.id);
            bool max = level >= Upgrades_.MaxLevel;
            infoVeil = Icon("up7-info-voile");
            infoVeil.pickingMode = PickingMode.Position;
            infoVeil.RegisterCallback<PointerDownEvent>(e => { if (e.target == infoVeil) CloseInfo(); });
            var box = Icon("up7-info-boite");
            box.pickingMode = PickingMode.Position;
            box.Add(ShopUi.NewLabel(def.displayName, "up7-info-titre"));
            // Le détail d'avant (inchangé) : il garde tous les chiffres.
            var details = Icon("up7-info-details");
            switch (def.id)
            {
                case Upgrades_.Gains: AddGainsEffect(details, level, max); break;
                case Upgrades_.Chests: AddChestsEffect(details, level, max); break;
                case Upgrades_.Traps: AddTrapsEffect(details, level, max); break;
            }
            box.Add(details);
            box.Add(ShopUi.NewLabel(max ? "Niveau maximum atteint." : $"Niveau {level + 1} : {ShopUi.Thousands(Upgrades.NextPrice(def.id))} pièces", "up7-info-texte up7-info-prix"));
            var ok = new Button(CloseInfo) { text = "OK" };
            ok.AddToClassList("btn"); ok.AddToClassList("btn--vert"); ok.AddToClassList("asc-info-ok");
            box.Add(ok);
            infoVeil.Add(box);
            // Posée DANS la page (hiérarchie de la ScrollView, par-dessus la liste) : elle disparaît
            // avec la page quand on change de fenêtre.
            list.hierarchy.Add(infoVeil);
            ButtonStyler.Apply(infoVeil);
        }

        void CloseInfo()
        {
            if (infoVeil != null) infoVeil.RemoveFromHierarchy();
            infoVeil = null;
        }

        /// Échap ferme d'abord la fenêtre « i ».
        public bool HandleEscape()
        {
            if (infoVeil == null) return false;
            CloseInfo();
            return true;
        }

        /// Le texte de la fenêtre « i » ouverte ("" si fermée), pour les tests.
        public string InfoText => infoVeil != null ? string.Join(" | ", infoVeil.Query<Label>().ToList().ConvertAll(l => l.text)) : "";
        /// OUTIL DE TEST : ouvre la fenêtre « i » d'une amélioration.
        public void DebugInfo(string id) { var def = Upgrades.Catalog != null ? Upgrades.Catalog.Get(id) : null; if (def != null) OpenInfo(def); }

        // ---------- Le détail complet (fenêtre « i ») : les trois façons d'avant ----------

        static void AddGainsEffect(VisualElement card, int level, bool max)
        {
            string now = $"Maintenant : trésors ×{ShopUi.Number(Upgrades.GainMultiplier(level))}";
            string text = max ? now : $"{now} → Prochain : ×{ShopUi.Number(Upgrades.GainMultiplier(level + 1))}";
            var label = ShopUi.NewLabel(text, "up-effect");
            label.AddToClassList("up-effect--strong");
            card.Add(label);
            // v6 (QA n°36) : cette ligne décrit le niveau SUIVANT : on le dit (« Au prochain
            // niveau : … »). Au niveau max, elle décrit le niveau actuel.
            string effect = Upgrades.GainText(max ? level : level + 1);
            if (!string.IsNullOrEmpty(effect))
                card.Add(ShopUi.NewLabel(max ? effect : $"Au prochain niveau : {LowerFirst(effect)}", "up-effect"));
        }

        /// « Trésors +60 % » → « trésors +60 % » (pour l'écrire au milieu d'une phrase).
        static string LowerFirst(string text) =>
            string.IsNullOrEmpty(text) ? text : char.ToLowerInvariant(text[0]) + text.Substring(1);

        static void AddChestsEffect(VisualElement card, int level, bool max)
        {
            int[] now = Upgrades.ChestWeights(level);
            int[] next = Upgrades.ChestWeights(max ? level : level + 1);
            var table = new VisualElement { pickingMode = PickingMode.Ignore };
            table.AddToClassList("up-table");
            table.Add(Row(null, "Coffres", "Maintenant", max ? "" : "Prochain", header: true));
            for (int i = 0; i < Upgrades.RarityCount; i++)
            {
                int a = i < now.Length ? now[i] : 0, b = i < next.Length ? next[i] : 0;
                if (a == 0 && b == 0) continue; // rareté pas encore possible : on ne la montre pas
                var rarity = Upgrades.Rarity(i);
                table.Add(Row(rarity.icon, $"{rarity.shortName} ×{ShopUi.Number(rarity.multiplier)}",
                              $"{a}\u00A0%", max ? "" : $"{b}\u00A0%", header: false));
            }
            card.Add(table);
            string avg = $"Gain moyen ×{ShopUi.Number(Round2(Upgrades.AverageChestMultiplier(level)))}";
            if (!max) avg += $" → ×{ShopUi.Number(Round2(Upgrades.AverageChestMultiplier(level + 1)))}";
            var label = ShopUi.NewLabel(avg, "up-effect");
            label.AddToClassList("up-effect--strong");
            card.Add(label);
        }

        static void AddTrapsEffect(VisualElement card, int level, bool max)
        {
            int[] now = Upgrades.CountWeights(level);
            int[] next = Upgrades.CountWeights(max ? level : level + 1);
            int baseCount = Upgrades.Catalog != null ? Upgrades.Catalog.BaseChestCount : 3;
            for (int i = 0; i < now.Length || i < next.Length; i++)
            {
                int a = i < now.Length ? now[i] : 0, b = i < next.Length ? next[i] : 0;
                if (a == 0 && b == 0) continue;
                string text = max ? $"{baseCount + i} coffres : {a}\u00A0% des étages"
                                  : $"{baseCount + i} coffres : {a}\u00A0% → {b}\u00A0% des étages";
                card.Add(ShopUi.NewLabel(text, "up-effect"));
            }
            string risk = $"Risque moyen : {Mathf.RoundToInt(Upgrades.AverageRiskPercent(level))}\u00A0%";
            if (!max) risk += $" → {Mathf.RoundToInt(Upgrades.AverageRiskPercent(level + 1))}\u00A0%";
            var label = ShopUi.NewLabel(risk, "up-effect");
            label.AddToClassList("up-effect--strong");
            card.Add(label);
            card.Add(ShopUi.NewLabel("Toujours un seul coffre piégé.", "up-effect"));
        }

        /// Arrondi à 2 décimales « comme à l'école » (1,125 → 1,13), pas « au pair le plus proche ».
        static float Round2(float v) => (float)(Math.Round(v * 100.0, MidpointRounding.AwayFromZero) / 100.0);

        static VisualElement Row(Sprite icon, string name, string now, string next, bool header)
        {
            var row = new VisualElement { pickingMode = PickingMode.Ignore };
            row.AddToClassList("up-row");
            if (header) row.AddToClassList("up-row--header");
            var iconBox = new VisualElement { pickingMode = PickingMode.Ignore };
            iconBox.AddToClassList("up-cell-icon");
            if (icon != null) iconBox.style.backgroundImage = new StyleBackground(icon);
            row.Add(iconBox);
            var nameLabel = ShopUi.NewLabel(name, "up-cell");
            nameLabel.AddToClassList("up-cell--name");
            row.Add(nameLabel);
            row.Add(ShopUi.NewLabel(now, "up-cell"));
            row.Add(ShopUi.NewLabel(next, "up-cell"));
            return row;
        }

        // ---------- Achat ----------

        /// Clic sur « Améliorer » : la règle est dans Upgrades.TryBuy (qui relit le niveau et le
        /// solde à chaque clic). Le bouton est inactif pendant les 0,3 s de l'animation.
        void OnBuy(string id)
        {
            if (Time.time < buyLockedUntil) return;
            if (!Upgrades.TryBuy(id)) return; // Upgrades.Changed → PanelsController → Refresh
            buyLockedUntil = Time.time + 0.3f;
            flashId = id;
            popLevel = Upgrades.Level(id);
            Bought?.Invoke(id, popLevel);
            Refresh();
            if (host != null) host.StartCoroutine(UnlockLater());
        }

        IEnumerator UnlockLater()
        {
            yield return new WaitForSeconds(0.3f);
            Refresh(); // le bouton redevient actif (s'il reste assez de pièces)
        }

        /// La carte fait un flash or (son dégradé est teinté d'or pâle puis revient) en 0,4 s.
        static IEnumerator FlashCard(VisualElement card)
        {
            for (float t = 0f; t < 0.4f; t += Time.deltaTime)
            {
                card.style.unityBackgroundImageTintColor = Color.Lerp(CardTint, FlashTint, Ease.Sine(t / 0.4f));
                yield return null;
            }
            card.style.unityBackgroundImageTintColor = StyleKeyword.Null;
        }

        /// L'icône saute de 16 px après un achat (0,3 s).
        static IEnumerator JumpIcon(VisualElement icon)
        {
            for (float t = 0f; t < 0.3f; t += Time.deltaTime)
            {
                icon.style.marginTop = -16f * Mathf.Sin(t / 0.3f * Mathf.PI);
                yield return null;
            }
            icon.style.marginTop = 0f;
        }

        /// La pastille du nouveau niveau se remplit avec un « pop » (0 → 1,3 → 1 en 0,25 s).
        static IEnumerator PopDot(VisualElement dot)
        {
            for (float t = 0f; t < 0.25f; t += Time.deltaTime)
            {
                float s = Ease.Pop(t / 0.25f, 0f, 1.3f);
                dot.style.scale = new Scale(new Vector3(s, s, 1f));
                yield return null;
            }
            dot.style.scale = StyleKeyword.Null;
        }

        /// OUTIL DE TEST : simule un clic sur « Améliorer ».
        public void DebugClickBuy(string id) => OnBuy(id);
    }
}
