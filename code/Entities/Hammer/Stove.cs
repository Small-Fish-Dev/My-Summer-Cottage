namespace Sauna;

[HammerEntity]
[EditorModel( "models/stove/stove.vmdl" )]
public partial class Stove : AnimatedEntity, IInteractable
{
	[Net, Predicted] public bool Open { get; set; } = false;

	string IInteractable.DisplayTitle => "Kiuas";
	InteractionOffset IInteractable.Offset => Rotation.Backward * 15f + Vector3.Up * 30f;

	public Stove()
	{
		var interactable = this as IInteractable;

		interactable.AddInteraction( InputButton.Use, new()
		{
			Predicate = ( Player pawn ) => true,
			Function = ( Player pawn ) =>
			{
				Open = !Open;
				SetAnimParameter( "b_open", Open );
			},
			TextFunction = () => Open ? "Close" : "Open"
		} );
	}

	public override void Spawn()
	{
		SetModel( "models/stove/stove.vmdl" );
		SetupPhysicsFromModel( PhysicsMotionType.Static );
	}
}
