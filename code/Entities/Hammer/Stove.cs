namespace Sauna;

[HammerEntity]
[EditorModel( "models/stove/stove.vmdl" )]
public partial class Stove : AnimatedEntity, IInteractable
{
	string IInteractable.DisplayTitle => "Kiuas";

	public Stove()
	{

	}

	public override void Spawn()
	{
		SetModel( "models/stove/stove.vmdl" );
		SetupPhysicsFromModel( PhysicsMotionType.Static );
	}
}
