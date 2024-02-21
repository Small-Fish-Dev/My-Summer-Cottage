﻿namespace Sauna;

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

	[Property] public CreatorStage Stage { get; set; }
	[Property] public CameraComponent Camera { get; set; }

	private Transform initialTransform;

	protected override void OnStart()
	{
		Current = CreatorStage.Identification; // TODO: Remove this, only needed cuz static shit is fucked.

		if ( Current != Stage )
			DisableStage();

		initialTransform = Camera.Transform.World;
	}

	public void DisableStage()
	{
		GameObject.Enabled = false;
	}

	public void EnableStage()
	{
		GameObject.Enabled = true;
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

		All[Current].DisableStage();
		All[Current = (CreatorStage)next].EnableStage();
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
