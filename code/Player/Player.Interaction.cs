namespace Sauna;

public partial class Player
{
	private const float INTERACTION_DISTANCE = 75f;
	private const float INTERACTION_SIZE = 10f;

	public Ray ViewRay => new( Camera.Transform.Position + Camera.Transform.Rotation.Forward * 25f, Camera.Transform.Rotation.Forward );
	public GameObject TargetedGameObject { get; private set; }
	public SceneTraceResult InteractionTrace { get; private set; }

	private void UpdateInteractions()
	{
		InteractionTrace = Scene.Trace.Ray( ViewRay, INTERACTION_DISTANCE )
					.Size( INTERACTION_SIZE )
					.IgnoreGameObject( GameObject )
					.WithoutTags( "world" )
					.Run();

		var obj = InteractionTrace.GameObject;
		obj = obj?.GetInteractions() == null ? null : obj;

		TargetedGameObject = obj;
	}
}
