namespace Sauna;

public partial class Ruler : ModelEntity, IInteractable
{
	string IInteractable.DisplayTitle => "Viivotin";

	public Ruler()
	{
		var interactable = this as IInteractable;

		// Turn radio on.
		interactable.AddInteraction( InputButton.Use, new()
		{
			Predicate = ( Player pawn ) => true,
			Function = ( Player pawn ) =>
			{
				var size = pawn.Size;

				// TODO: Add custom messages, even effects based on your size.
				Subtitles.Send( To.Single( pawn ), $"You measure your penoid to be ~{size:F1} cm.", wrapper: '*' );
			},
			Text = "Measure"
		} );
	}

	public override void Spawn()
	{
		SetModel( "models/ruler/ruler.vmdl" );
		SetupPhysicsFromModel( PhysicsMotionType.Dynamic );
	}
}
