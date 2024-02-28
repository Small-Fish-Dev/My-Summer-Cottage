namespace Sauna;

public class ItemComponent : Component
{
	[Property] public IconSettings Icon { get; set; }
	[Property] public string Name { get; set; }
	[Property] public string Description { get; set; }
	[Property] public int WeightInGrams { get; set; }

	public string Prefab { get; private set; }
	public Texture IconTexture => Texture.Load( FileSystem.Mounted, Icon.Path );

	public static implicit operator ItemComponent( GameObject obj )
		=> obj.Components.Get<ItemComponent>();

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
			Disabled = () => InBackpack,
		} );
	}

	protected override void OnDestroy()
	{
		if ( IsProxy )
			return;

		var inventory = Player.Local.Inventory;
		// TODO: Remove from inventory.
		// inventory?.RemoveItem( this );
	}
}
