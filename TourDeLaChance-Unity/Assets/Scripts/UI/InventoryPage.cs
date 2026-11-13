using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;

namespace TourDeLaChance
{
    /// <summary>
    /// Le panneau « Inventaire » (spec boites §7.4, v5 §4.4) : ce qu'on possède, pour ESSAYER,
    /// ÉQUIPER et VENDRE les doublons. Ce n'est pas un MonoBehaviour : un simple objet créé et
    /// piloté par PanelsController. Il affiche et transmet les clics ; les règles (équiper,
    /// 2 accessoires au plus, vendre, total des doublons) sont dans la classe Shop.
    ///
    /// v5 : onglet Accessoires avec, en haut, les deux cases « Accessoire 1 » et « Accessoire 2 »
    /// (vide = « Libre » en pointillés, sinon l'objet et « Retirer ») ; dessous, les rangées Tête /
    /// Visage / Cou / Dos des objets possédés (plus d'objet « Rien » : on « Retire »). Un objet
    /// d'un emplacement déjà occupé porte « Remplace : Couronne ». Si 2 accessoires sont portés
    /// et que l'emplacement est libre, « Équiper » ouvre la petite fenêtre « Lequel enlever ? ».
    /// </summary>
    public class InventoryPage : IDisposable
    {
        readonly VisualElement page;
        readonly ScrollView list;
        readonly Button skinsTab, accessoriesTab, equipButton, sellButton, sellAllButton;
        readonly Label selectedLabel, bonusLabel;
        VisualElement replaceDialog;

        /// Prévient le PanelsController qu'il faut montrer un objet sur le personnage (aperçu).
        public event Action<ShopItem> PreviewRequested;
        /// Une vente a eu lieu (son « vente » + défilement du porte-monnaie).
        public event Action Sold;
        /// Un objet vient d'être équipé (petit saut d'accueil du personnage).
        public event Action<ShopItem> Equipped;

        ShopCategory category = ShopCategory.Skin;
        ShopItem selected;

        public InventoryPage(VisualElement page, PanelsController owner)
        {
            this.page = page;
            list = page.Q<ScrollView>("inv-list");
            skinsTab = page.Q<Button>("inv-skins");
            accessoriesTab = page.Q<Button>("inv-accessoires");
            equipButton = page.Q<Button>("inv-equip");
            sellButton = page.Q<Button>("inv-sell");
            sellAllButton = page.Q<Button>("inv-sellall");
            selectedLabel = page.Q<Label>("inv-selected");
            bonusLabel = page.Q<Label>("inv-bonus");
            skinsTab.AddToClassList(UiSounds.TabClass);
            accessoriesTab.AddToClassList(UiSounds.TabClass);

            skinsTab.clicked += OnSkinsTab;
            accessoriesTab.clicked += OnAccessoriesTab;
            equipButton.clicked += OnEquip;
            sellButton.clicked += OnSell;
            sellAllButton.clicked += OnSellAll;
        }

        /// Correctif B9 : on se désabonne de TOUT ce à quoi on s'est abonné.
        public void Dispose()
        {
            skinsTab.clicked -= OnSkinsTab;
            accessoriesTab.clicked -= OnAccessoriesTab;
            equipButton.clicked -= OnEquip;
            sellButton.clicked -= OnSell;
            sellAllButton.clicked -= OnSellAll;
        }

        void OnSkinsTab() => ShowCategory(ShopCategory.Skin, null);
        void OnAccessoriesTab() => ShowCategory(ShopCategory.Accessoire, null);

        /// À l'ouverture : « Skins » par défaut (ou l'onglet de l'objet demandé depuis l'Index).
        public void Open(ShopItem focus = null)
        {
            CloseReplaceDialog();
            if (focus != null) ShowCategory(focus.Category, focus);
            else ShowCategory(ShopCategory.Skin, null);
        }

        /// Échap ferme d'abord la fenêtre « Lequel enlever ? ».
        public bool HandleEscape()
        {
            if (replaceDialog == null || replaceDialog.parent == null) return false;
            CloseReplaceDialog();
            return true;
        }

        void ShowCategory(ShopCategory newCategory, ShopItem focus)
        {
            category = newCategory;
            selected = focus ?? (category == ShopCategory.Skin
                ? Shop.GetEquippedSkin()
                : Shop.EquippedAccessories().FirstOrDefault());
            if (focus != null) PreviewRequested?.Invoke(focus);
            Refresh();
            list.scrollOffset = UnityEngine.Vector2.zero;
        }

        void Select(ShopItem item)
        {
            selected = item;
            PreviewRequested?.Invoke(item); // aperçu sur le personnage, rien n'est sauvegardé
            Refresh();
        }

        /// Redessine la page à partir de la collection.
        public void Refresh()
        {
            ShopUi.SetClass(skinsTab, "subtab--active", category == ShopCategory.Skin);
            ShopUi.SetClass(accessoriesTab, "subtab--active", category == ShopCategory.Accessoire);

            list.Clear();
            if (category == ShopCategory.Skin)
            {
                var grid = NewGrid();
                foreach (var item in OwnedItems(i => i.Category == ShopCategory.Skin, includeFree: true)) grid.Add(Card(item));
                list.Add(grid);
            }
            else
            {
                list.Add(AccessorySlots());
                // Quatre rangées titrées : Tête, Visage, Cou, Dos (sans les objets « Rien »).
                foreach (AccessorySlot slot in Enum.GetValues(typeof(AccessorySlot)))
                {
                    var items = OwnedItems(i => i.Category == ShopCategory.Accessoire && i.Slot == slot, includeFree: false).ToList();
                    list.Add(ShopUi.NewLabel(SlotName(slot), "row-title"));
                    var grid = NewGrid();
                    foreach (var item in items) grid.Add(Card(item));
                    if (items.Count == 0) grid.Add(ShopUi.NewLabel("Aucun pour l'instant.", "inv-empty"));
                    list.Add(grid);
                }
            }
            RefreshActions();
            ButtonStyler.Apply(list);
        }

        /// Les deux grandes cases « Accessoire 1 » et « Accessoire 2 ».
        VisualElement AccessorySlots()
        {
            var row = new VisualElement { pickingMode = PickingMode.Ignore };
            row.AddToClassList("inv-slots");
            var worn = Shop.EquippedAccessories();
            for (int i = 0; i < Shop.MaxAccessories; i++)
            {
                var slot = new VisualElement();
                slot.AddToClassList("inv-slot");
                slot.Add(ShopUi.NewLabel($"Accessoire {i + 1}", "inv-slot__titre"));
                if (i < worn.Count)
                {
                    var item = worn[i];
                    slot.AddToClassList("inv-slot--plein");
                    ShopUi.ColorBorder(slot, Shop.RarityOf(item));
                    var inner = new VisualElement { pickingMode = PickingMode.Ignore };
                    inner.AddToClassList("inv-slot__contenu");
                    inner.Add(ShopUi.Visual(item, ShopUi.Look.Normal, "inv-slot__visuel"));
                    var texts = new VisualElement { pickingMode = PickingMode.Ignore };
                    texts.Add(ShopUi.NewLabel(item.DisplayName, "inv-slot__nom"));
                    texts.Add(ShopUi.NewLabel($"{SlotName(item.Slot)} · +{Shop.BonusPercent(item)}\u00A0%", "inv-slot__detail"));
                    inner.Add(texts);
                    slot.Add(inner);
                    var remove = new Button(() => { if (Shop.Unequip(item.Slot)) Equipped?.Invoke(item); }) { text = "Retirer" };
                    remove.AddToClassList("btn"); remove.AddToClassList("btn--sombre"); remove.AddToClassList("inv-slot__retirer");
                    slot.Add(remove);
                }
                else
                {
                    slot.AddToClassList("inv-slot--libre");
                    slot.Add(ShopUi.NewLabel("Libre", "inv-slot__libre"));
                }
                row.Add(slot);
            }
            return row;
        }

        /// Les objets possédés d'une catégorie : (le gratuit,) puis les autres triés par
        /// rareté (du Secret au Commun), puis par boîte et numéro.
        static IEnumerable<ShopItem> OwnedItems(Func<ShopItem, bool> filter, bool includeFree)
        {
            var catalog = Shop.Catalog;
            if (catalog == null) yield break;
            if (includeFree) foreach (var free in catalog.FreeItems.Where(i => i != null && filter(i))) yield return free;
            var boxes = catalog.Boxes.ToList();
            var owned = catalog.Boxes.SelectMany(b => b.items)
                .Where(i => i != null && filter(i) && Shop.CountOf(i) >= 1)
                .OrderByDescending(Shop.RarityRank)
                .ThenBy(i => boxes.IndexOf(Shop.BoxOf(i)))
                .ThenBy(Shop.NumberOf);
            foreach (var item in owned) yield return item;
        }

        VisualElement Card(ShopItem item)
        {
            var card = ShopUi.ItemCard(item, ShopUi.Look.Normal, item == selected, () => Select(item), out var inner);
            int count = Shop.CountOf(item);
            if (count >= 2) inner.Add(ShopUi.NewLabel("×" + count, "item-badge"));
            int bonus = Shop.BonusPercent(item);
            if (bonus > 0) inner.Add(ShopUi.NewLabel($"+{bonus}\u00A0%", "bonus-badge"));
            if (Shop.IsEquipped(item) && !item.IsFree)
            {
                var equipped = ShopUi.NewLabel("Équipé ✓", "item-extra");
                equipped.AddToClassList("item-extra--equipped");
                inner.Add(equipped);
            }
            else
            {
                // v5 §4.4 : « Remplace : Couronne » (même emplacement déjà occupé).
                var replaced = Shop.ReplacedBySameSlot(item);
                if (replaced != null) inner.Add(ShopUi.NewLabel($"Remplace : {replaced.DisplayName}", "item-extra item-extra--remplace"));
            }
            return card;
        }

        /// La zone d'action suit le tableau de la spec (boites §7.4, v5 §4.4).
        void RefreshActions()
        {
            // v5 §4.5 : le bonus de la tenue SAUVEGARDÉE (sans plafond) ; en aperçu, ce qu'il
            // deviendrait (calcul seulement, rien n'est sauvegardé).
            int now = Shop.OutfitBonusPercent;
            string line = $"Bonus de ta tenue : +{now}\u00A0% sur les trésors (skin + 2 accessoires)";
            if (selected != null && !Shop.IsEquipped(selected))
                line += $"\nAvec cet objet : +{Shop.OutfitBonusPercentWith(selected)}\u00A0%";
            bonusLabel.text = line;

            if (selected == null)
            {
                selectedLabel.text = "";
                equipButton.style.display = DisplayStyle.None;
                sellButton.style.display = DisplayStyle.None;
            }
            else
            {
                int count = Shop.CountOf(selected);
                selectedLabel.text = selected.IsFree ? selected.DisplayName : $"{selected.DisplayName} (×{count})";
                equipButton.style.display = DisplayStyle.Flex;
                bool equipped = Shop.IsEquipped(selected);
                bool accessory = selected.Category == ShopCategory.Accessoire && !selected.IsFree;
                // Un accessoire porté : « Retirer » ; un skin porté : « Équipé ✓ ».
                equipButton.text = equipped ? (accessory ? "Retirer" : "Équipé ✓") : "Équiper";
                equipButton.SetEnabled((equipped && accessory) || (!equipped && Shop.IsOwned(selected)));

                bool canSell = Shop.CanSellDuplicate(selected);
                sellButton.style.display = canSell ? DisplayStyle.Flex : DisplayStyle.None;
                if (canSell) sellButton.text = $"Vendre 1 doublon (+{Shop.SellValue(selected)})";
            }

            int total = Shop.DuplicatesTotal();
            sellAllButton.text = total > 0 ? $"Vendre tous les doublons (+{total})" : "Aucun doublon";
            sellAllButton.SetEnabled(total > 0);
        }

        void OnEquip()
        {
            if (selected == null) return;
            bool accessory = selected.Category == ShopCategory.Accessoire && !selected.IsFree;
            if (accessory && Shop.IsEquipped(selected))
            {
                if (Shop.Unequip(selected.Slot)) Equipped?.Invoke(selected);
                return;
            }
            if (Shop.NeedsReplaceChoice(selected)) { OpenReplaceDialog(selected); return; }
            if (Shop.TryEquip(selected)) Equipped?.Invoke(selected); // Shop.Changed → Refresh
        }

        // =====================================================================
        // « Tu portes déjà 2 accessoires. Lequel enlever ? » (§4.4)
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
            if (Shop.TryEquip(item, toRemove)) Equipped?.Invoke(item);
        }

        void CloseReplaceDialog()
        {
            if (replaceDialog != null) replaceDialog.RemoveFromHierarchy();
            replaceDialog = null;
        }

        void OnSell()
        {
            if (Shop.TrySellDuplicate(selected)) Sold?.Invoke();
        }

        void OnSellAll()
        {
            if (Shop.SellAllDuplicates() > 0) Sold?.Invoke();
        }

        static VisualElement NewGrid()
        {
            var grid = new VisualElement { pickingMode = PickingMode.Ignore };
            grid.AddToClassList("grid");
            return grid;
        }

        /// Pour les tests : la ligne du bonus de tenue.
        public string BonusText => bonusLabel.text;
        public bool ReplaceDialogOpen => replaceDialog != null;
        /// OUTIL DE TEST : sélectionne une carte (comme un clic).
        public void DebugSelect(ShopItem item) { if (item.Category != category) ShowCategory(item.Category, item); else Select(item); }
        /// OUTIL DE TEST : clic sur « Équiper » / « Retirer ».
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
