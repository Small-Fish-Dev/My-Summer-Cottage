namespace Sauna;

partial class Player
{
	public Vector3 EyePosition { get; set; }
	public AnimatedEntity View { get; private set; }
	public Ray ViewRay => new Ray( EyePosition, ViewAngles.ToRotation().Forward );

	private float fov;

	public override void ClientSpawn()
	{
		if ( Game.LocalPawn != this ) 
			return;

		View = new AnimatedEntity();
		View.SetModel( "models/guy/guy.vmdl" );
		View.SetParent( this, true );
		View.EnableShadowInFirstPerson = false;

		Event.Run( "onSpawn", this );
	}

	public Vector3 GetEyePosition()
	{
		var bone = GetAttachment( "eyes" );
		return (bone?.Position ?? (Position + CollisionBox.Maxs.z)) + Rotation.Forward * 4f;
	}

	protected void CameraSimulate( IClient cl )
	{
		EyePosition = GetEyePosition();

		Camera.Position = EyePosition;
		Camera.Rotation = ViewAngles.ToRotation();

		fov = MathX.LerpTo( fov, Input.Down( InputButton.View ) ? 20f : 70f, 5f * Time.Delta );
		Camera.FieldOfView = Screen.CreateVerticalFieldOfView( fov );
		Camera.FirstPersonViewer = this;
		Camera.ZNear = 3f;
	}

	[Event.Client.Frame]
	private void onFrame()
	{
		if ( View == null || !View.IsValid )
			return;

		View.SetBoneTransform( View.GetBoneIndex( "head" ), new Transform( EyePosition + Rotation.Backward * 10, Transform.Zero.Rotation, 0 ), true );
		View.EnableDrawing = Camera.FirstPersonViewer == this;
	}
}
