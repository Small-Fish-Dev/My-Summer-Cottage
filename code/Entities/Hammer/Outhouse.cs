namespace Sauna;

[HammerEntity]
[Solid]
public partial class Outhouse : ModelEntity, IInteractable
{
	/// <summary>
	/// The current user of this outhouse.
	/// </summary>
	public Player User { get; private set; }

	string IInteractable.DisplayTitle => "Huussi";
	InteractionOffset IInteractable.Offset => Vector3.Up * 30f;

	public Outhouse()
	{
		var interactable = this as IInteractable;

		interactable.AddInteraction( "use", new()
		{
			Predicate = ( Player pawn ) => true,
			Function = ( Player pawn ) =>
			{
				
			},
			TextFunction = () => "Take a piss"
		} );
	}

	public override void Spawn()
	{
		SetupPhysicsFromModel( PhysicsMotionType.Static );
		Tags.Add( "solid" );
	}
}
