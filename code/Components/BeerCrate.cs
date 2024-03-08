namespace Sauna;

public sealed class BeerCrate : Component
{
	public const int Columns = 6;
	public const int Rows = 4;

	/// <summary>
	/// The beer we are giving the player when they interact.
	/// </summary>
	[Property]
	public GameObject Beer { get; set; }

	/// <summary>
	/// The amount of beers still in the crate.
	/// </summary>
	[Sync, Property, Range( 0, 24, 1 ), TargetSave]
	public int Count
	{
		get => _count;
		set
		{
			value = value.Clamp( 0, Columns * Rows );

			BeerCountChanged( _count, value );
			_count = value;
			UpdateName();
		}
	}

	private int _count = Rows * Columns;

	/// <summary>
	/// The offset between rows and columns.
	/// </summary>
	public (Vector3 Vertical, Vector3 Horizontal) Offset
	{
		get
		{
			if ( offset == null )
			{
				var slot = renderer.GetAttachment( "slot", false )?.Position;
				var vertical = renderer.GetAttachment( "slot_vertical", false )?.Position; // Forward (1, 0, 0)
				var horizontal = renderer.GetAttachment( "slot_horizontal", false )?.Position; // Right (0, -1, 0)
				if ( slot == null || vertical == null || horizontal == null )
					return (Vector3.Zero, Vector3.Zero); // We failed to get the attachments somehow...

				offset = (vertical.Value - slot.Value, horizontal.Value - slot.Value);
			}

			return offset.Value;
		}
	}

	private (Vector3, Vector3)? offset;
	private Dictionary<int, SceneObject> beers = new();
	private Model beerModel = Model.Load( "models/beer_bottle/beer.vmdl" );
	private SkinnedModelRenderer renderer;
	private ItemComponent itemComponent;

	protected override void OnAwake()
	{
		renderer = Components.Get<SkinnedModelRenderer>( FindMode.EverythingInSelfAndDescendants );
		itemComponent = Components.Get<ItemComponent>();
		UpdateName();
	}

	protected override void OnStart()
	{
		// Take a beer.
		var interactions = Components.GetOrCreate<Interactions>();

		interactions.AddInteraction( new Interaction()
		{
			Identifier = "beer_crate.take",
			Action = ( Player interactor, GameObject obj ) =>
			{
				var inventory = interactor.Inventory;
				if ( inventory == null )
					return;

				GameObject beer;
				beer = Beer.Clone();
				beer.NetworkSpawn();

				if ( inventory.EquippedItems[(int)EquipSlot.Hand] != null )
				{
					if ( !inventory.HasSpaceInBackpack() )
						return;


					beer.Enabled = true;
					beer.BreakFromPrefab();
					inventory.GiveItem( beer );
					Count--;

					return;
				}

				beer.BreakFromPrefab();
				inventory.EquipItemFromWorld( beer );
				Count--;
			},
			Keybind = "use2",
			DynamicText = () => $"Take a beer",
			Disabled = () => Count <= 0,
		} );

		BeerCountChanged( 0, Count );
	}

	protected override void OnEnabled()
	{
		BeerCountChanged( 0, Count );
		UpdateName();
	}

	protected override void OnDisabled()
	{
		foreach ( var (index, beer) in beers )
			if ( beer.IsValid() )
			{
				renderer?.SceneObject?.RemoveChild( beer );
				beer.Delete();
			}

		beers.Clear();
	}

	private void UpdateName()
	{
		if ( itemComponent is not null )
			itemComponent.Name = $"Beer Crate ({Count}/{Columns * Rows})";
	}

	// Handle the visual beers.
	private void BeerCountChanged( int previous, int current )
	{
		// The SceneObject doesn't exist, we can't parent the beers.
		var sceneObject = renderer?.SceneObject;
		if ( sceneObject == null || !sceneObject.IsValid() )
			return;

		// Add new beers.
		if ( current > previous )
		{
			for ( int i = previous; i < current; i++ )
			{
				var row = (int)MathF.Floor( i / Columns ) + 1;
				var column = i % Columns;

				var rot = Rotation.FromYaw( Game.Random.Int( 0, 360 ) );
				var pos = (row - Rows / 2f - 0.5f) * Offset.Vertical
					+ (column - Columns / 2f + 0.5f) * Offset.Horizontal
					+ sceneObject.Position
					+ Vector3.Up * 0.5f;
				var transform = new Transform( pos, rot )
					.RotateAround( sceneObject.Position, sceneObject.Rotation ); // Make sure the beers are properly rotated.

				var beerObject = new SceneObject( sceneObject.World, beerModel, transform );
				sceneObject.AddChild( $"beer_{i}", beerObject );
				if ( !beers.ContainsKey( i ) )
					beers.Add( i, beerObject );
			}

			return;
		}

		// Delete the beers that were taken.
		for ( int i = previous; i >= current; i-- )
		{
			if ( beers.TryGetValue( i, out var child ) )
			{
				sceneObject.RemoveChild( child );
				child.Delete();
				beers.Remove( i );
			}
		}
	}
}
