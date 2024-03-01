namespace Sauna;

public class ItemComponent : Component
{
	/// <summary>
	/// The name of the item.
	/// </summary>
	[Sync]
	[Property]
	public string Name { get; set; }

	/// <summary>
	/// The icon to display.
	/// </summary>
	[Property] public IconSettings Icon { get; set; }

	/// <summary>
	/// The description of the item.
	/// </summary>
	[Property] public string Description { get; set; }

	/// <summary>
	/// The weight (in grams) of the item.
	/// </summary>
	[Property] public int WeightInGrams { get; set; }

	/// <summary>
	/// The sell price of an item in mk (-1 indicating it cannot be sold).
	/// </summary>
	[Property] public int SellPrice { get; set; } = -1;

	public string Prefab { get; private set; }
	public Texture IconTexture => Texture.Load( FileSystem.Mounted, Icon.Path );
	public static implicit operator ItemComponent( GameObject obj )
		=> obj.Components.Get<ItemComponent>();

	/// <summary>
	/// If the item is in the player's inventory (this includes backpack and equipped items).
	/// </summary>
	public bool InInventory
	{
		get => InBackpack || (this is ItemEquipment equipment && equipment.Equipped);
	}

	/// <summary>
	/// If the item is in the player's backpack (note not equipped!).
	/// </summary>
	[Sync]
	public bool InBackpack
	{
		get => !GameObject.Enabled;
		set => GameObject.Enabled = !value;
	}

	protected override void OnAwake()
	{
		base.OnAwake();
		Prefab = GameObject.PrefabInstanceSource;
	}

	protected override void OnStart()
	{
		if ( !Network.Active )
			GameObject.NetworkSpawn();

		GameObject.Name = Name;
		Network.SetOwnerTransfer( OwnerTransfer.Takeover );
		Network.SetOrphanedMode( NetworkOrphaned.ClearOwner );

		// Pickup
		var interactions = Components.GetOrCreate<Interactions>();
		interactions.AddInteraction( new Interaction()
		{
			Identifier = "item.pickup",
			Action = ( Player interactor, GameObject obj ) => interactor.Inventory.GiveItem( this ),
			Keybind = "use",
			Description = "Pickup",
			Disabled = () => InInventory,
		} );
	}

	protected override void OnDestroy()
	{
		if ( IsProxy )
			return;

		var inventory = Player.Local.Inventory;
		inventory?.ClearItem( this );
	}
}
