namespace Sauna;

[HammerEntity]
[Solid]
public partial class Door : ModelEntity, IInteractable
{
	public override void Spawn()
	{
		SetupPhysicsFromModel( PhysicsMotionType.Keyframed );
	}
}
