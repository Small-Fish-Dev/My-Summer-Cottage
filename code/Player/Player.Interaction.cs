namespace Sauna;

public partial class Player
{
	private const float INTERACTION_DISTANCE = 75f;
	private const float INTERACTION_SIZE = 10f;

	public Ray ViewRay => new( Camera.Transform.Position + Camera.Transform.Rotation.Forward * 25f, Camera.Transform.Rotation.Forward );
	public GameObject TargetedGameObject { get; private set; }

	private void UpdateInteractions()
	{
		var trace = Scene.Trace.Ray( ViewRay, INTERACTION_DISTANCE )
					.Size( INTERACTION_SIZE )
					.IgnoreGameObject( GameObject )
					.WithoutTags( "world" )
					.Run();

		TargetedGameObject = trace.GameObject;
	}
}
