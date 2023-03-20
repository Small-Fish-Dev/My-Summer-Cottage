namespace Sauna;

[Prefab]
public class PhysicsComponent : ItemComponent
{
	/// <summary>
	/// The motion type of the physics.
	/// </summary>
	[Prefab]
	public PhysicsMotionType MotionType { get; set; } = PhysicsMotionType.Dynamic;

	/// <summary>
	/// Are physics enabled or not.
	/// </summary>
	[Prefab]
	public bool PhysicsEnabled { get; set; } = true;

	public override void Initialize()
	{
		if ( Game.IsClient )
			return;

		Item.SetupPhysicsFromModel( MotionType );
		Item.PhysicsEnabled = PhysicsEnabled;
	}
}
