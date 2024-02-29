namespace Sauna;

public enum CreatorStage : byte
{
	Identification,
	Appearance,
	Dressing_up,
	Confirmation
}

public class CreatorComponent : Component
{
	public static Dictionary<CreatorStage, CreatorComponent> All 
		=> GameManager.ActiveScene?.GetAllObjects( false )
			.Select( x => x?.Components.Get<CreatorComponent>( FindMode.InSelf ) )
			.Where( x => x != null )
			.OrderBy( stage => (int)stage.Stage )
			.ToDictionary( stage => stage.Stage, stage => stage );

	public static CreatorStage Current { get; private set; } = CreatorStage.Identification;
	public static CameraComponent TargetCamera { get; private set; }

	[Property] public CreatorStage Stage { get; set; }
	[Property] public CameraComponent Camera { get; set; }

	private Transform initialTransform;

	protected override void OnStart()
	{
		Current = CreatorStage.Identification; // TODO: Remove this, only needed cuz static shit is fucked.
		Camera.Enabled = false;

		if ( Current == Stage )
			EnableStage();
		
		initialTransform = Camera.Transform.World;
	}

	public void EnableStage()
	{
		TargetCamera = Camera;
	}

	public static void PreviousStage()
	{
		var previous = (int)Current - 1;
		if ( previous < 0 )
			return;

		All[Current = (CreatorStage)previous].EnableStage();
	}

	public static void NextStage()
	{
		var values = Enum.GetValues<CreatorStage>();
		var next = (int)Current + 1;
		if ( next >= values.Length )
		{
			// WE DID IT!!
			return;
		}

		All[Current = (CreatorStage)next].EnableStage();
	}

	private int GetBone( EquipSlot slot )
		=> slot switch
		{
			EquipSlot.Head => 7,
			EquipSlot.Face => 6,
			EquipSlot.Body => 4,
			EquipSlot.Legs => 20,
			EquipSlot.Feet => 22,
			_ => 0
		};

	protected override void OnUpdate()
	{
		if ( TargetCamera != Camera )
			return;

		var transform = new Transform()
		{
			Position = Vector3.Up * 0.3f * MathF.Sin( Time.Now ),
			Rotation = Rotation.FromRoll( 2f * MathF.Sin( Time.Now / 2f ) )
		};

		var delta = 2.3f * RealTime.Delta;
		Scene.Camera.Transform.Position = Vector3.Lerp( Scene.Camera.Transform.Position, initialTransform.Position + transform.Position, delta );
		Scene.Camera.Transform.Rotation = Rotation.Slerp( Scene.Camera.Transform.Rotation, initialTransform.Rotation * transform.Rotation, delta );
		Scene.Camera.FieldOfView = MathX.Lerp( Scene.Camera.FieldOfView, Camera.FieldOfView, 2f * delta );
	}
}
