using Sandbox;

[Icon( "person_add_alt_1" )]
public sealed class PlayerSpawner : Component
{
	protected override void DrawGizmos()
	{
		var draw = Gizmo.Draw;
		draw.Model( "models/guy/guy.vmdl" );
	}
	protected override void OnUpdate()
	{

	}
}
