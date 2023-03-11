namespace Sauna;

public partial class Ruler : BaseItem, IInteractable
{
	string IInteractable.DisplayTitle => "Viivain";

	public Ruler()
	{
		var interactable = this as IInteractable;

		interactable.AddInteraction( InputButton.Use, new()
		{
			Predicate = ( Player pawn ) => true,
			Function = ( Player pawn ) =>
			{
				var size = pawn.Size ?? 0f;

				// TODO: Add custom messages, even effects based on your size.
				var value = MathF.Min( size / 11f, 1f );
				var color = new Color( 1f - value, value, 0f );

				Eventlogger.Send( To.Single( pawn ), new Eventlogger.Component[] 
				{
					new( "You measure your penoid to be " ), 
					new( $"~{size:F1}", color: color ),
					new( " cm." )
				} );
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
