using Sandbox;

public sealed class MooseTest : Component
{
	[Property]
	[Range( 0f, 100f, 1f )]
	public float AnimationSpeed { get; set; } = 50f;
	protected override void OnUpdate()
	{
		var renderer = Components.Get<SkinnedModelRenderer>();
		renderer.Set( "move_x", AnimationSpeed );
	}
}
