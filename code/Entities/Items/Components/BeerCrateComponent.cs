namespace Sauna;

[Prefab, Title( "Beer Crate Component" )]
public partial class BeerCrateComponent : ItemComponent
{
	public const int Columns = 6;
	public const int Rows = 4;

	// public override string Model => "models/beer_crate/beer_crate.vmdl";
	// public override string Title => $"Kaljakori ({Count}/{Rows * Columns})";
	// public override Vector3 TitleOffset => Vector3.Up * 25f;

	/// <summary>
	/// The amount of beers still in the crate.
	/// </summary>
	[Net, Change( "beerCountChanged" )]
	public int Count { get; set; } = Rows * Columns;

	/// <summary>
	/// The offset between rows and columns.
	/// </summary>
	public (Vector3 Vertical, Vector3 Horizontal) Offset
	{
		get
		{
			if ( offset == null )
			{
				var slot = Item.GetAttachment( "slot", false )?.Position;
				var vertical = Item.GetAttachment( "slot_vertical", false )?.Position; // Forward (1, 0, 0)
				var horizontal = Item.GetAttachment( "slot_horizontal", false )?.Position; // Right (0, -1, 0)

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

	public override void Initialize()
	{
		Interactable.AddInteraction( InputButton.Use, new()
		{
			Predicate = ( Player player ) => Count > 0,
			Function = ( Player player ) =>
			{
				if ( Game.IsServer )
					Count -= 1;
			},
			Text = "Take a beer"
		} );
	}

	public override void OnNewModel( Model model )
	{
		// We want to set the beers at the beginning.
		if ( Game.IsClient )
			beerCountChanged( 0, Count );
	}

	// Handle the visual beers.
	private void beerCountChanged( int previous, int current )
	{
		// The SceneObject doesn't exist, we can't parent the beers.
		var sceneObject = Item?.SceneObject;
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

				var beerObject = new SceneObject( Game.SceneWorld, beerModel, transform );
				sceneObject.AddChild( $"beer_{i}", beerObject );
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
			}
		}
	}
}
