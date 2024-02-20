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
	public static Dictionary<CreatorStage, CreatorComponent> All { get; private set; }
	public static CreatorStage Current { get; private set; } = CreatorStage.Identification;

	[Property] public CreatorStage Stage { get; set; }
	[Property] public CameraComponent Camera { get; set; }

	private Transform initialTransform;

	protected override void OnStart()
	{
		Current = CreatorStage.Identification; // TODO: Remove this, only needed cuz static shit is fucked.

		if ( All == null )
		{
			All = Scene.GetAllComponents<CreatorComponent>()
				.OrderBy( stage => (int)stage.Stage )
				.ToDictionary( stage => stage.Stage, stage => stage );

			All[Current].EnableStage();
		}

		initialTransform = Camera.Transform.World;
	}

	public void EnableStage()
	{
		Camera.IsMainCamera = true;
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

		Current = (CreatorStage)next;
		All[Current].EnableStage();
	}

	protected override void OnUpdate()
	{
		if ( Camera == null || !Camera.IsMainCamera )
			return;

		var transform = new Transform()
		{
			Position = Vector3.Up * 0.5f * MathF.Sin( Time.Now ),
			Rotation = Rotation.FromRoll( 3f * MathF.Sin( Time.Now / 2f ) )
		};

		Camera.Transform.Position = initialTransform.Position + transform.Position;
		Camera.Transform.Rotation = initialTransform.Rotation * transform.Rotation;
	}
}
