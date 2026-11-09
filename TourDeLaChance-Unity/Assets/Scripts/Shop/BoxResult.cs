namespace TourDeLaChance
{
    /// <summary>
    /// Le résultat d'une ouverture de boîte, renvoyé par Shop.TryOpenBox à l'interface.
    /// Quand l'animation commence, tout est DÉJÀ décidé et sauvegardé : l'animation ne fait
    /// que raconter ce résultat.
    /// </summary>
    public struct BoxResult
    {
        public LootBox Box;
        public ShopItem Item;
        public RarityDef Rarity;
        public bool IsNew;   // premier exemplaire ?
        public int Count;    // nombre d'exemplaires après l'ouverture
    }
}
