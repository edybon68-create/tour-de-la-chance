using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;

namespace TourDeLaChance
{
    /// <summary>
    /// L'onglet « Inventaire » du shop (spec boites §7.4) : ce qu'on possède, pour ÉQUIPER
    /// et VENDRE les doublons. Ce n'est pas un MonoBehaviour : c'est un simple objet créé et
    /// piloté par ShopController. Il affiche et transmet les clics ; les règles (équiper,
    /// vendre, total des doublons) sont dans la classe Shop.
    /// </summary>
    public class InventoryPage
    {
        readonly ScrollView list;
        readonly Button skinsTab, accessoriesTab, equipButton, sellButton, sellAllButton;
        readonly Label selectedLabel;

        /// Prévient le ShopController qu'il faut montrer un objet sur le personnage (aperçu).
        public event Action<ShopItem> PreviewRequested;
        /// Une vente a eu lieu (son « vente » + défilement du porte-monnaie).
        public event Action Sold;
        /// Un objet vient d'être équipé (petit saut d'accueil du personnage).
        public event Action<ShopItem> Equipped;

        ShopCategory category = ShopCategory.Skin;
        ShopItem selected;

        public InventoryPage(VisualElement page)
        {
            list = page.Q<ScrollView>("inv-list");
            skinsTab = page.Q<Button>("inv-skins");
            accessoriesTab = page.Q<Button>("inv-accessoires");
            equipButton = page.Q<Button>("inv-equip");
            sellButton = page.Q<Button>("inv-sell");
            sellAllButton = page.Q<Button>("inv-sellall");
            selectedLabel = page.Q<Label>("inv-selected");

            skinsTab.clicked += () => ShowCategory(ShopCategory.Skin, null);
            accessoriesTab.clicked += () => ShowCategory(ShopCategory.Accessoire, null);
            equipButton.clicked += OnEquip;
            sellButton.clicked += OnSell;
            sellAllButton.clicked += OnSellAll;
        }

        /// À l'ouverture de l'onglet : sous-onglet « Skins » par défaut (ou celui de l'objet
        /// demandé, depuis l'Index), carte sélectionnée = l'objet équipé (ou l'objet demandé).
        public void Open(ShopItem focus = null)
        {
            if (focus != null) ShowCategory(focus.Category, focus);
            else ShowCategory(ShopCategory.Skin, null);
        }

        void ShowCategory(ShopCategory newCategory, ShopItem focus)
        {
            category = newCategory;
            selected = focus ?? (category == ShopCategory.Skin
                ? Shop.GetEquippedSkin()
                : Shop.GetEquippedAccessory(AccessorySlot.Tete));
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
                foreach (var item in OwnedItems(i => i.Category == ShopCategory.Skin)) grid.Add(Card(item));
                list.Add(grid);
            }
            else
            {
                // Quatre rangées titrées : Tête, Visage, Cou, Dos.
                foreach (AccessorySlot slot in Enum.GetValues(typeof(AccessorySlot)))
                {
                    list.Add(ShopUi.NewLabel(SlotName(slot), "row-title"));
                    var grid = NewGrid();
                    foreach (var item in OwnedItems(i => i.Category == ShopCategory.Accessoire && i.Slot == slot))
                        grid.Add(Card(item));
                    list.Add(grid);
                }
            }
            RefreshActions();
        }

        /// Les objets possédés d'une catégorie : d'abord le gratuit, puis les autres triés par
        /// rareté (du Secret au Commun), puis par numéro.
        static IEnumerable<ShopItem> OwnedItems(Func<ShopItem, bool> filter)
        {
            var catalog = Shop.Catalog;
            if (catalog == null) yield break;
            foreach (var free in catalog.FreeItems.Where(i => i != null && filter(i))) yield return free;
            var owned = catalog.Boxes.SelectMany(b => b.items)
                .Where(i => i != null && filter(i) && Shop.CountOf(i) >= 1)
                .OrderByDescending(Shop.RarityRank)
                .ThenBy(Shop.NumberOf);
            foreach (var item in owned) yield return item;
        }

        VisualElement Card(ShopItem item)
        {
            var card = ShopUi.ItemCard(item, ShopUi.Look.Normal, item == selected, () => Select(item), out var inner);
            int count = Shop.CountOf(item);
            if (count >= 2) inner.Add(ShopUi.NewLabel("×" + count, "item-badge"));
            if (Shop.IsEquipped(item))
            {
                var equipped = ShopUi.NewLabel("Équipé ✓", "item-extra");
                equipped.AddToClassList("item-extra--equipped");
                inner.Add(equipped);
            }
            return card;
        }

        /// La zone d'action suit le tableau de la spec (§7.4).
        void RefreshActions()
        {
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
                equipButton.text = equipped ? "Équipé ✓" : "Équiper";
                equipButton.SetEnabled(!equipped && Shop.IsOwned(selected));

                // « Vendre » seulement s'il y a au moins 2 exemplaires (jamais le dernier).
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
            if (Shop.TryEquip(selected)) Equipped?.Invoke(selected); // Shop.Changed → Refresh
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

        public static string SlotName(AccessorySlot slot) => slot switch
        {
            AccessorySlot.Tete => "Tête",
            AccessorySlot.Visage => "Visage",
            AccessorySlot.Cou => "Cou",
            _ => "Dos"
        };
    }
}
