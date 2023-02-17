namespace Sauna;

partial class Player
{
	public Vector3 EyePosition { get; set; }
	public AnimatedEntity View { get; private set; }

	public override void ClientSpawn()
	{
		if ( Game.LocalPawn != this ) return;

		View = new AnimatedEntity();
		View.SetModel( "models/guy/guy.vmdl" );
		View.SetParent( this, true );
		View.EnableShadowInFirstPerson = false;
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

		var ang = Drunkness >= 25
			? Drunkness / 10f * MathF.Sin( Time.Now )
			: 0f;
		Camera.Rotation = ViewAngles.WithRoll( ang ).ToRotation();

		var fov = Drunkness >= 40
			? Drunkness / 10f * MathF.Sin( Time.Now )
			: 0f;
		Camera.FieldOfView = Screen.CreateVerticalFieldOfView( 70f + fov );
		Camera.FirstPersonViewer = this;
		Camera.ZNear = 3f;

		Sauna.Effects.MotionBlur.Scale = 0.2f * (Drunkness / 100f);
		Sauna.Effects.MotionBlur.Samples = 4;
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
