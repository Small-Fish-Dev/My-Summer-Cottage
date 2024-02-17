namespace Sauna;

public partial class Player
{
	private const float INTERACTION_DISTANCE = 75f;
	private const float INTERACTION_SIZE = 10f;

	public Ray ViewRay => new( Camera.Transform.Position + Camera.Transform.Rotation.Forward * 25f, Camera.Transform.Rotation.Forward );
	public float InteractionDistance => 75f;

	private void UpdateInteractions()
	{
	}
}
