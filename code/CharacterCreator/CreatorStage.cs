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

	protected override void OnStart()
	{
		if ( All == null )
		{
			All = Scene.GetAllComponents<CreatorComponent>()
				.OrderBy( stage => (int)stage.Stage )
				.ToDictionary( stage => stage.Stage, stage => stage );

			All[Current].EnableStage();
		}
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

		var delta = 3f * MathF.Sin( Time.Now * 2f );
		Camera.Transform.LocalRotation *= Rotation.FromPitch( delta );
	}
}
