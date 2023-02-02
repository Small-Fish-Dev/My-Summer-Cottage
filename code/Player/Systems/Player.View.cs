namespace Sauna;

partial class Player
{
	public AnimatedEntity View { get; private set; }

	public override void ClientSpawn()
	{
		if ( Game.LocalPawn != this ) return;

		View = new AnimatedEntity();
		View.SetModel( "models/guy/guy.vmdl" );
		View.SetParent( this, true );
	}

	[Event.Client.Frame]
	private void viewFrame()
	{
		if ( View == null || !View.IsValid )
			return;

		View.SetBoneTransform( View.GetBoneIndex( "head" ), new Transform( EyePosition + Rotation.Backward * 10, Transform.Zero.Rotation, 0 ), true );
		View.EnableDrawing = Camera.FirstPersonViewer == this;
	}
}
