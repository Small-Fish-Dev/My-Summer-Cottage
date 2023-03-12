namespace Sauna;

public partial class Mushroom : BaseItem, IInteractable
{
	string IInteractable.DisplayTitle => "Silokkijak";
	InteractionOffset IInteractable.Offset => Vector3.Up * 10f;

	public Mushroom()
	{
		var interactable = this as IInteractable;

		// TODO: Interactions.
	}

	public override void Spawn()
	{
		SetModel( "models/mushroom/mushroom.vmdl" );
		SetupPhysicsFromModel( PhysicsMotionType.Dynamic );

		PhysicsEnabled = false;
		Tags.Add( "nocollide" );
	}
}
