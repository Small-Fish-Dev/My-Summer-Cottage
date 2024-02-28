namespace Sauna;

public class ItemComponent : Component
{
	[Property] public IconSettings Icon { get; set; }
	[Property] public string Name { get; set; }
	[Property] public string Description { get; set; }
	[Property] public int WeightInGrams { get; set; }

	public string Prefab { get; private set; }
	public Texture IconTexture => Texture.Load( FileSystem.Mounted, Icon.Path );
	private ItemEquipment AsEquipment => this as ItemEquipment;

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
