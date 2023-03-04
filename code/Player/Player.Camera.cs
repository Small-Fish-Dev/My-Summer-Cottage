namespace Sauna;

public struct CameraData
{
	public Vector3 Position;
	public Rotation Rotation;
	public float FOV;
	public IEntity Viewer;
}

partial class Player
{
	public Vector3 EyePosition { get; set; }
	public CameraData CameraData { get; private set; }
	public AnimatedEntity View { get; private set; }
	public Ray ViewRay => new Ray( EyePosition, ViewAngles.ToRotation().Forward );

	private float fov;
	private CameraData? overrideCamera = null;

	public override void ClientSpawn()
	{
		if ( Game.LocalPawn != this ) 
			return;

		View = new AnimatedEntity();
		View.SetModel( "models/guy/guy.vmdl" );
		View.SetParent( this, true );
		View.EnableShadowCasting = false;

		Event.Run( "OnSpawn", this );
	}

	/// <summary>
	/// Gets the player's true eye position.
	/// </summary>
	/// <returns></returns>
	public Vector3 GetEyePosition()
	{
		var bone = GetAttachment( "eyes" );
		return (bone?.Position ?? (Position + CollisionBox.Maxs.z)) + Rotation.Forward * 4f;
	}

	/// <summary>
	/// Overrides the player's camera.
	/// </summary>
	/// <param name="data"></param>
	public void ApplyCameraOverride( CameraData? data )
		=> overrideCamera = data;

	protected void CameraSimulate( IClient cl )
	{
		// Assign CameraData
		EyePosition = GetEyePosition();
		CameraData = overrideCamera == null
			? new CameraData
			{
				Position = EyePosition,
				Rotation = ViewAngles.ToRotation(),
				FOV = Screen.CreateVerticalFieldOfView( fov = MathX.LerpTo( fov, Input.Down( InputButton.View ) ? 20f : 70f, 5f * Time.Delta ) ),
				Viewer = this
			}
			: overrideCamera.Value;

		// Set CameraData values.
		Camera.Position = CameraData.Position;
		Camera.Rotation = CameraData.Rotation;
		Camera.FieldOfView = CameraData.FOV;
		Camera.FirstPersonViewer = CameraData.Viewer;
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
