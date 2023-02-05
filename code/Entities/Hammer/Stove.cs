namespace Sauna;

[HammerEntity]
[Solid]
[Model( Model = "models/sauna stove/sauna_stove.vmdl" )]
public partial class Stove : AnimatedEntity, IInteractable
{
	string IInteractable.DisplayTitle => "Kiuas";

	public Stove()
	{

	}

	public override void Spawn()
	{
		SetupPhysicsFromModel( PhysicsMotionType.Static );
	}
}
