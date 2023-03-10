namespace Sauna;

public partial class Beer : BaseItem, IInteractable
{
	string IInteractable.DisplayTitle => "Viipuri-olut";

	public Beer()
	{
		var interactable = this as IInteractable;
	}

	public override void Spawn()
	{
		SetModel( "models/beer_bottle/beer.vmdl" );
		SetupPhysicsFromModel( PhysicsMotionType.Dynamic );
	}
}
