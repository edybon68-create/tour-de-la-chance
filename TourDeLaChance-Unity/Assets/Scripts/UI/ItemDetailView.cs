using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace TourDeLaChance
{
    /// <summary>
    /// La FICHE AGRANDIE d'un objet (spec v5 §8.4) : au clic sur une carte de l'Index (ou de
    /// « Contenu et taux »), la carte s'agrandit depuis sa place (0,25 s, ease-out-back) en une
    /// fiche 960 × 640 ; la grille derrière s'assombrit. À gauche, l'objet en 3D sur le CHEVAL
    /// VIERGE qui tourne (un grand « ? » pour un Secret non obtenu) ; à droite, le nom, la
    /// rareté, la boîte et le numéro, l'emplacement, la chance (calculée avec les taux de la
    /// boîte), le bonus de tenue, combien on en a, la phrase de fiche, et deux boutons.
    /// Flèches ◀ ▶ (et touches ← →) : objet précédent / suivant dans l'ordre de l'onglet.
    /// Fermer : la croix, un clic sur le voile, ou Échap.
    ///
    /// v6 §6.1 et §6.2 : dans l'Index (hideUnowned), un objet non obtenu (non Secret) n'a PAS de
    /// 3D : le pictogramme « pas encore vu » en grand et « Voir dans la Boutique ». Le cheval se
    /// fait tourner en glissant (0,5° par pixel, élan, reprise de la rotation automatique) ;
    /// les flèches ◀ ▶ sont des bulles toujours visibles, la navigation fait la boucle.
    ///
    /// Ce n'est pas un MonoBehaviour : la page qui l'utilise l'avance à chaque image (Tick).
    /// Aucune règle de jeu : tous les nombres viennent de Shop (catalogue et collection).
    /// </summary>
    public class ItemDetailView
    {
        const float OpenTime = 0.25f;
        const string SecretId = "secret";

        readonly VisualElement overlay, sheet, visual, questionMark, placeholder, notSeen, hint;
        readonly Label nameLabel, rarityLabel, boxLabel, slotLabel, chanceLabel, bonusLabel, countLabel, flavorLabel, secretNote;
        readonly Button gotoInventory, gotoBox, gotoShop;
        readonly ShowcaseHorse horse;
        readonly bool hideUnowned;
        bool canRotate, dragging, rotatedOnce;
        float hintTime, hintFade = -1f;

        List<ShopItem> sequence = new List<ShopItem>();
        int index;
        float animTime = -1f, pulseTime;
        Vector2 fromOffset;
        float fromScale = 0.2f;

        /// « Voir dans l'inventaire » / « Voir la boîte ».
        public event Action<ShopItem> ShowInInventoryRequested;
        public event Action<LootBox> ShowBoxRequested;
        /// v6 §6.1 : « Voir dans la Boutique » (objet non obtenu de l'Index).
        public event Action<ShopItem> ShowInShopRequested;
        /// Pour les tests : la zone 3D se fait-elle tourner ? l'indice est-il visible ?
        public bool CanRotate => canRotate;
        public bool NotSeenShown => notSeen.style.display == DisplayStyle.Flex;

        public bool IsOpen => overlay.style.display == DisplayStyle.Flex;
        public ShopItem Current => index >= 0 && index < sequence.Count ? sequence[index] : null;
        /// Les textes de la fiche (pour les tests).
        public string Text => string.Join(" | ", new[] { nameLabel.text, rarityLabel.text, boxLabel.text, slotLabel.text, chanceLabel.text, bonusLabel.text, countLabel.text, flavorLabel.text });

        public ItemDetailView(VisualElement host, ShowcaseHorse horse, bool hideUnowned = false)
        {
            this.horse = horse;
            this.hideUnowned = hideUnowned;
            overlay = new VisualElement();
            overlay.AddToClassList("detail-overlay");
            overlay.RegisterCallback<PointerDownEvent>(e => { if (e.target == overlay) Close(); });

            sheet = new VisualElement();
            sheet.AddToClassList("detail-sheet");
            overlay.Add(sheet);

            // À gauche : le cheval vierge filmé (ou le « ? », ou l'icône de secours).
            visual = new VisualElement { pickingMode = PickingMode.Ignore };
            visual.AddToClassList("detail-visual");
            questionMark = ShopUi.NewLabel("?", "detail-question");
            visual.Add(questionMark);
            placeholder = new VisualElement { pickingMode = PickingMode.Ignore };
            placeholder.AddToClassList("detail-placeholder");
            visual.Add(placeholder);
            // v6 §6.1 : le pictogramme « pas encore vu » en grand et sa phrase.
            notSeen = new VisualElement { pickingMode = PickingMode.Ignore };
            notSeen.AddToClassList("detail-pas-vu");
            visual.Add(notSeen);
            // v6 §6.2 (art bible §15.6) : l'indice « Glisse pour tourner ».
            hint = new VisualElement { pickingMode = PickingMode.Ignore };
            hint.AddToClassList("tourne-indice");
            var hintIcon = new VisualElement { pickingMode = PickingMode.Ignore };
            hintIcon.AddToClassList("tourne-indice__icone");
            if (UiImages.Rotation != null) hintIcon.style.backgroundImage = new StyleBackground(UiImages.Rotation);
            else hintIcon.style.display = DisplayStyle.None;
            hint.Add(hintIcon);
            hint.Add(ShopUi.NewLabel("Glisse pour tourner", "tourne-indice__texte"));
            visual.Add(hint);
            // Glisser dans la zone 3D fait tourner le cheval (souris enfoncée).
            visual.RegisterCallback<PointerDownEvent>(OnPointerDown);
            visual.RegisterCallback<PointerMoveEvent>(OnPointerMove);
            visual.RegisterCallback<PointerUpEvent>(OnPointerUp);
            visual.RegisterCallback<PointerCaptureOutEvent>(_ => EndDrag());
            sheet.Add(visual);

            // À droite : les informations.
            var info = new VisualElement { pickingMode = PickingMode.Ignore };
            info.AddToClassList("detail-info");
            nameLabel = Add(info, "detail-name");
            rarityLabel = Add(info, "detail-rarity");
            boxLabel = Add(info, "detail-line");
            slotLabel = Add(info, "detail-line");
            chanceLabel = Add(info, "detail-line");
            bonusLabel = Add(info, "detail-line");
            countLabel = Add(info, "detail-line");
            flavorLabel = Add(info, "detail-flavor");
            secretNote = Add(info, "detail-line");
            var buttons = new VisualElement();
            buttons.AddToClassList("detail-buttons");
            gotoInventory = new Button(() => { if (Current != null) ShowInInventoryRequested?.Invoke(Current); }) { text = "Voir dans l'inventaire" };
            gotoInventory.AddToClassList("btn"); gotoInventory.AddToClassList("btn--bleu"); gotoInventory.AddToClassList("detail-btn");
            gotoBox = new Button(() => { var box = Shop.BoxOf(Current); if (box != null) ShowBoxRequested?.Invoke(box); }) { text = "Voir la boîte" };
            gotoBox.AddToClassList("btn"); gotoBox.AddToClassList("btn--sombre"); gotoBox.AddToClassList("detail-btn");
            gotoShop = new Button(() => { if (Current != null) ShowInShopRequested?.Invoke(Current); }) { text = "Voir dans la Boutique" };
            gotoShop.AddToClassList("btn"); gotoShop.AddToClassList("btn--bleu"); gotoShop.AddToClassList("detail-btn");
            buttons.Add(gotoInventory);
            buttons.Add(gotoShop);
            buttons.Add(gotoBox);
            info.Add(buttons);
            sheet.Add(info);

            // Flèches (v6 : bulles ardoise de 120 px, à cheval sur les deux bords) et croix.
            var prev = new Button(() => Step(-1));
            prev.AddToClassList("btn"); prev.AddToClassList("bulle"); prev.AddToClassList("bulle-petit"); prev.AddToClassList("bulle-petit--ardoise");
            prev.AddToClassList("detail-arrow"); prev.AddToClassList("detail-arrow--gauche");
            prev.Add(ShopUi.Chevron(false));
            var next = new Button(() => Step(+1));
            next.AddToClassList("btn"); next.AddToClassList("bulle"); next.AddToClassList("bulle-petit"); next.AddToClassList("bulle-petit--ardoise");
            next.AddToClassList("detail-arrow"); next.AddToClassList("detail-arrow--droite");
            next.Add(ShopUi.Chevron(true));
            sheet.Add(prev);
            sheet.Add(next);
            var close = new Button(Close);
            // v6 : la croix est une bulle rouge en image (art bible §15.1.6).
            close.AddToClassList("btn"); close.AddToClassList("bulle"); close.AddToClassList("bulle-petit"); close.AddToClassList("bulle-petit--rouge");
            close.AddToClassList("btn-rond"); close.AddToClassList("detail-close");
            var croix = new VisualElement { pickingMode = PickingMode.Ignore };
            croix.AddToClassList("croix");
            foreach (var (c, shadow) in new[] { ("croix-barre--a", true), ("croix-barre--b", true), ("croix-barre--a", false), ("croix-barre--b", false) })
            {
                var bar = new VisualElement { pickingMode = PickingMode.Ignore };
                bar.AddToClassList("croix-barre"); bar.AddToClassList(c);
                if (shadow) bar.AddToClassList("croix-barre--ombre"); // la copie « épaisseur » dessous
                croix.Add(bar);
            }
            close.Add(croix);
            sheet.Add(close);

            overlay.style.display = DisplayStyle.None;
            host.Add(overlay);
            ButtonStyler.Apply(overlay);
        }

        static Label Add(VisualElement parent, string className)
        {
            var l = ShopUi.NewLabel("", className);
            parent.Add(l);
            return l;
        }

        /// Ouvre la fiche sur un objet d'une liste (l'ordre de l'onglet), depuis une carte.
        public void Open(List<ShopItem> items, int start, VisualElement fromCard)
        {
            sequence = items ?? new List<ShopItem>();
            index = Mathf.Clamp(start, 0, Mathf.Max(0, sequence.Count - 1));
            overlay.style.display = DisplayStyle.Flex;
            // Point de départ de l'animation : la carte cliquée.
            fromOffset = Vector2.zero;
            fromScale = 0.2f;
            if (fromCard != null && sheet.parent != null)
            {
                Rect card = overlay.WorldToLocal(fromCard.worldBound);
                Rect ov = overlay.contentRect;
                fromOffset = card.center - ov.center;
                fromScale = Mathf.Clamp(card.width / 960f, 0.1f, 0.5f);
            }
            animTime = 0f;
            Fill();
            if (horse != null) horse.SetActive(true);
            if (SoundManager.Instance != null) SoundManager.Instance.Play(Sfx.UiClic);
            Render();
        }

        public void Close()
        {
            if (!IsOpen) return;
            EndDrag();
            overlay.style.display = DisplayStyle.None;
            if (horse != null) horse.SetActive(false);
        }

        /// Échap : ferme la fiche si elle est ouverte (et le dit).
        public bool HandleEscape()
        {
            if (!IsOpen) return false;
            Close();
            return true;
        }

        public void Step(int delta)
        {
            if (sequence.Count == 0) return;
            index = (index + delta + sequence.Count) % sequence.Count;
            Fill();
            if (SoundManager.Instance != null) SoundManager.Instance.Play(Sfx.UiClic);
        }

        void Fill()
        {
            var item = Current;
            if (item == null) return;
            var rarity = Shop.RarityOf(item);
            var box = Shop.BoxOf(item);
            int count = Shop.CountOf(item);
            bool owned = count >= 1;
            bool mystery = !owned && item.RarityId == SecretId;
            bool hidden = hideUnowned && !owned && !mystery; // v6 §6.1 : pas de 3D dans l'Index

            nameLabel.text = mystery ? "???" : item.DisplayName;
            rarityLabel.text = rarity != null ? rarity.displayName : "";
            if (rarity != null) rarityLabel.style.color = rarity.textColor;
            boxLabel.text = box != null ? $"{box.displayName} · n°{Shop.NumberOf(item):00}" : "";
            slotLabel.text = item.Category == ShopCategory.Accessoire ? SlotPhrase(item.Slot) : "Skin : la couleur du cheval";
            chanceLabel.text = $"Chance à chaque ouverture : {ShopUi.Percent(Shop.ItemChance(item))}";
            int bonus = Shop.BonusPercent(item);
            int diamondBonus = Shop.DiamondBonusPercentOf(item);
            bonusLabel.text = diamondBonus > 0
                ? $"Équipé, il donne +{bonus}\u00A0% de pièces et +{diamondBonus}\u00A0% de diamants"
                : $"Équipé, il donne +{bonus}\u00A0% de pièces sur les trésors";
            countLabel.text = owned ? $"Tu en as : {count}" : "Pas encore obtenu";
            flavorLabel.text = mystery ? "???" : (string.IsNullOrEmpty(item.FlavorText) ? "" : $"« {item.FlavorText} »");
            flavorLabel.style.display = flavorLabel.text.Length > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            secretNote.text = mystery ? "Objet secret : tu le découvriras en l'obtenant" : "";
            secretNote.style.display = mystery ? DisplayStyle.Flex : DisplayStyle.None;
            gotoInventory.style.display = owned ? DisplayStyle.Flex : DisplayStyle.None;
            gotoShop.style.display = hidden ? DisplayStyle.Flex : DisplayStyle.None;
            gotoBox.style.display = hidden ? DisplayStyle.None : DisplayStyle.Flex;
            ShopUi.ColorBorder(sheet, rarity);

            // La partie gauche : 3D sur le cheval vierge, « ? » pour un Secret non obtenu, ou
            // l'icône (de secours) pour un accessoire dont le modèle n'existe pas encore.
            bool needsModel = item.Category == ShopCategory.Accessoire && item.AccessoryPrefab == null;
            questionMark.style.display = mystery ? DisplayStyle.Flex : DisplayStyle.None;
            placeholder.Clear();
            placeholder.style.display = !mystery && !hidden && needsModel ? DisplayStyle.Flex : DisplayStyle.None;
            if (!mystery && !hidden && needsModel) placeholder.Add(ShopUi.Visual(item, ShopUi.Look.Normal, "detail-placeholder-visual"));
            notSeen.Clear();
            notSeen.style.display = hidden ? DisplayStyle.Flex : DisplayStyle.None;
            if (hidden)
            {
                notSeen.Add(ShopUi.NotSeenPicto(rarity, 260f));
                notSeen.Add(ShopUi.NewLabel("Pas encore obtenu. Tu peux le voir dans la Boutique.", "detail-pas-vu__texte"));
            }
            bool show3D = !mystery && !hidden && !needsModel && horse != null;
            // v6 §6.2 : on ne fait tourner que le modèle 3D (rien pour un objet sans modèle).
            canRotate = show3D;
            visual.pickingMode = show3D ? PickingMode.Position : PickingMode.Ignore;
            EndDrag();
            hint.style.display = show3D ? DisplayStyle.Flex : DisplayStyle.None;
            hint.style.opacity = 1f;
            hintFade = -1f;
            visual.style.backgroundImage = show3D ? new StyleBackground(Background.FromRenderTexture(horse.Texture)) : StyleKeyword.None;
            if (horse != null)
            {
                horse.SetActive(show3D && IsOpen);
                if (show3D) horse.Show(item);
            }
        }

        public static string SlotPhrase(AccessorySlot slot) => slot switch
        {
            AccessorySlot.Tete => "Se porte sur la tête",
            AccessorySlot.Visage => "Se porte sur le visage",
            AccessorySlot.Cou => "Se porte autour du cou",
            _ => "Se porte sur le dos"
        };

        /// Avance l'animation d'ouverture, le « ? » qui pulse et les touches ← →.
        public void Tick(float dt)
        {
            if (!IsOpen) return;
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.leftArrowKey.wasPressedThisFrame) Step(-1);
                if (kb.rightArrowKey.wasPressedThisFrame) Step(+1);
            }
            if (animTime >= 0f) animTime += dt;
            pulseTime += dt;
            // L'indice se balance (icône ±10°, pilule ±8 px en 1,2 s) puis s'efface en 0,3 s dès
            // que le joueur a fait tourner le modèle.
            hintTime += dt;
            float swing = Mathf.Sin(hintTime * Mathf.PI * 2f / 1.2f);
            hint.style.translate = new Translate(8f * swing, 0f);
            var icon = hint.Q<VisualElement>(className: "tourne-indice__icone");
            if (icon != null) icon.style.rotate = new Rotate(10f * swing);
            if (hintFade >= 0f)
            {
                hintFade += dt;
                hint.style.opacity = Mathf.Clamp01(1f - hintFade / 0.3f);
                if (hintFade >= 0.3f) { hint.style.display = DisplayStyle.None; hintFade = -1f; }
            }
            Render();
        }

        // ---------- v6 §6.2 : faire tourner le cheval à la main ----------

        void OnPointerDown(PointerDownEvent e)
        {
            if (!canRotate || horse == null || e.button != 0) return;
            dragging = true;
            visual.CapturePointer(e.pointerId);
            horse.BeginDrag();
        }

        void OnPointerMove(PointerMoveEvent e)
        {
            if (!dragging || horse == null) return;
            if (Mathf.Abs(e.deltaPosition.x) < 0.01f) return;
            horse.Drag(e.deltaPosition.x);
            if (!rotatedOnce || hint.style.display == DisplayStyle.Flex) { rotatedOnce = true; if (hintFade < 0f && hint.style.display == DisplayStyle.Flex) hintFade = 0f; }
        }

        void OnPointerUp(PointerUpEvent e)
        {
            if (!dragging) return;
            if (visual.HasPointerCapture(e.pointerId)) visual.ReleasePointer(e.pointerId);
            EndDrag();
        }

        void EndDrag()
        {
            if (!dragging) return;
            dragging = false;
            if (horse != null) horse.EndDrag();
        }

        /// OUTIL DE TEST : simule un glisser de dx pixels dans la zone 3D, puis le relâché.
        public void DebugDrag(float dx, int steps = 10)
        {
            if (!canRotate || horse == null) return;
            horse.BeginDrag();
            for (int i = 0; i < steps; i++) horse.Drag(dx / steps);
            horse.EndDrag();
            if (hint.style.display == DisplayStyle.Flex && hintFade < 0f) hintFade = 0f;
        }

        void Render()
        {
            float k = animTime < 0f ? 1f : Mathf.Clamp01(animTime / OpenTime);
            if (k >= 1f) animTime = -1f;
            float e = Ease.OutBack(k);
            float s = Mathf.LerpUnclamped(fromScale, 1f, e);
            sheet.style.scale = new Scale(new Vector3(s, s, 1f));
            sheet.style.translate = new Translate(fromOffset.x * (1f - e), fromOffset.y * (1f - e));
            overlay.style.backgroundColor = new Color(0.055f, 0.04f, 0.1f, 0.5f * Mathf.Clamp01(k * 1.5f));
            float pulse = 1f + 0.08f * Mathf.Sin(pulseTime * Mathf.PI * 2f / 1.4f);
            questionMark.style.scale = new Scale(new Vector3(pulse, pulse, 1f));
        }
    }
}
