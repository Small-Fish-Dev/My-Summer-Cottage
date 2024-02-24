using Sandbox;

namespace Sauna;

public partial class Player
{
	private const float INTERACTION_DISTANCE = 75f;
	private const float INTERACTION_SIZE = 10f;

	public Ray ViewRay => new( Camera.Transform.Position + Camera.Transform.Rotation.Forward * 25f, Camera.Transform.Rotation.Forward );
	public GameObject TargetedGameObject { get; private set; }
	public SceneTraceResult InteractionTrace { get; private set; }
	public BBox? InteractionBounds { get; private set; }

	private void UpdateInteractions()
	{
		var thinTrace = Scene.Trace.Ray( ViewRay, INTERACTION_DISTANCE )
					.IgnoreGameObject( GameObject )
					.WithoutTags( "world" )
					.Run();

		if ( thinTrace.GameObject != null && thinTrace.GameObject.GetInteractions() != null )
		{
			InteractionTrace = thinTrace;
			InteractionBounds = thinTrace.GameObject != TargetedGameObject ? null : InteractionBounds;
			TargetedGameObject = thinTrace.GameObject;
		}
		else
		{
			InteractionTrace = Scene.Trace.Ray( ViewRay, INTERACTION_DISTANCE )
						.Size( INTERACTION_SIZE )
						.IgnoreGameObject( GameObject )
						.WithoutTags( "world" )
						.Run();
			var obj = InteractionTrace.GameObject;
			obj = obj?.GetInteractions() == null ? null : obj;

			InteractionBounds = InteractionTrace.GameObject != TargetedGameObject ? null : InteractionBounds;
			TargetedGameObject = obj;
		}

		// Get bounds again.
		if ( InteractionBounds == null && TargetedGameObject != null )
			InteractionBounds = TargetedGameObject.GetBounds().Translate( -TargetedGameObject.Transform.Position );
	}

	[Broadcast]
	public void BroadcastInteraction( Vector3 position, Rotation rotation )
	{
		Renderer.Set( "right_ik_pos", position );
		Renderer.Set( "right_ik_rot", rotation );
		Renderer.Set( "use", true );
	}
}
