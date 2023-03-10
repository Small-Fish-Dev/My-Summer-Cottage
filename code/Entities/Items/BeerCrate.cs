namespace Sauna;

public partial class BeerCrate : BaseItem, IInteractable
{
	public const int Columns = 6;
	public const int Rows = 4;

	string IInteractable.DisplayTitle => $"Kaljakori ({Count}/{Rows * Columns})";
	InteractionOffset IInteractable.Offset => Vector3.Up * 25f;

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
				var slot = GetAttachment( "slot", false )?.Position;
				var vertical = GetAttachment( "slot_vertical", false )?.Position; // Forward (1, 0, 0)
				var horizontal = GetAttachment( "slot_horizontal", false )?.Position; // Right (0, -1, 0)

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

	public BeerCrate()
	{
		var interactable = this as IInteractable;

		interactable.AddInteraction( InputButton.Use, new()
		{
			Predicate = ( Player player ) => Count > 0,
			Function = ( Player player ) => 
			{
				if ( Game.IsServer )
					Count--;
			},
			Text = "Take a beer"
		} );
	}

	public override void Spawn()
	{
		SetModel( "models/beer_crate/beer_crate.vmdl" );
		SetupPhysicsFromModel( PhysicsMotionType.Dynamic );
	}

	public override void OnNewModel( Model model )
	{
		base.OnNewModel( model );
		
		// We want to set the beers at the beginning.
		if ( Game.IsClient )
			beerCountChanged( 0, Count );
	}

	// Handle the visual beers.
	private void beerCountChanged( int previous, int current )
	{
		// The SceneObject doesn't exist, we can't parent the beers.
		if ( SceneObject == null || !SceneObject.IsValid() )
			return;

		// Add new beers.
		if ( current > previous )
		{
			for ( int i = previous; i < current; i++ )
			{
				var row = (int)MathF.Floor( i / Columns ) + 1;
				var column = i % Columns;

				var rot = Rotation.FromYaw( Game.Random.Int( 0, 360 ) ) * SceneObject.Transform.Rotation;
				var transform = new Transform( (row - Rows / 2f - 0.5f) * Offset.Vertical + (column - Columns / 2f + 0.5f) * Offset.Horizontal + SceneObject.Transform.Position, rot );

				var sceneObject = new SceneObject( Game.SceneWorld, beerModel, transform );
				SceneObject.AddChild( $"beer_{i}", sceneObject );
				beers.Add( i, sceneObject );
			}

			return;
		}

		// Delete the beers that were taken.
		for ( int i = current; i > current - Math.Abs( previous - current ); i-- )
		{
			if ( beers.TryGetValue( i, out var beer ) )
				beer.Delete();
		}
	}
}
