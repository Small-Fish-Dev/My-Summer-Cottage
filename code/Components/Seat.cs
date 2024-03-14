namespace Sauna.Components;

public sealed class Seat : Component
{
	public static Seat Target { get; private set; }

	[Sync] public bool IsOccupied { get; set; }
	[Property] public Transform SeatTransform { get; set; } = global::Transform.Zero;

	public Transform GlobalTransform => new Transform( Transform.Position + SeatTransform.Position * Transform.Rotation, Transform.Rotation * SeatTransform.Rotation );

	private SceneModel _model;

	protected override void OnStart()
	{
		GameObject.SetupNetworking();

		var interactions = Components.GetOrCreate<Interactions>();
		interactions.HideOnEmpty = true;
		interactions.AddInteraction( new Interaction
		{
			Identifier = "chair.sit",
			Accessibility = AccessibleFrom.World,
			Description = "Sit",
			Disabled = () => IsOccupied,
			Action = ( Player player, GameObject obj ) =>
			{
				player.Shitting = GlobalTransform;
				Target = this;
				IsOccupied = true;
			},
			Keybind = "use",
			Animation = InteractAnimations.None
		} );
	}

	private SceneModel GetModel()
	{
		var world = Game.ActiveScene?.SceneWorld;
		if ( world == null )
			return null;

		_model ??= new SceneModel( world, "models/guy/guy.vmdl", global::Transform.Zero );
		// _model.RenderingEnabled = true;
		return _model;
	}

	protected override void OnDestroy()
	{
		_model?.Delete();
	}

	protected override void DrawGizmos()
	{
		// TODO: draw the player in a sitting position

		var model = GetModel();
		if ( model is null )
			return;

		if ( Game.IsPlaying || !Gizmo.HasSelected )
		{
			model.RenderingEnabled = false;
		}
		else
		{
			model.RenderingEnabled = true;
			model.Position = GlobalTransform.Position;
			model.Rotation = GlobalTransform.Rotation;
			model.SetAnimParameter( "sitting", true );
		}

		if ( (GameObject == Game.ActiveScene || !GameObject.IsPrefabInstance) && Gizmo.HasSelected )
			using ( Gizmo.Scope( $"Seat", new Transform( GlobalTransform.Position, model.Rotation ) ) )
			{
				Gizmo.Hitbox.DepthBias = 0.01f;

				if ( Gizmo.IsShiftPressed )
				{
					if ( Gizmo.Control.Rotate( "rotate", out var rotate ) )
						SeatTransform = SeatTransform.WithRotation( SeatTransform.Rotation * rotate.ToRotation() );

					return;
				}

				if ( Gizmo.Control.Position( "position", Vector3.Zero, out var pos ) )
					SeatTransform = SeatTransform.WithPosition( SeatTransform.Position + pos * SeatTransform.Rotation );
			}

		model.Update( Time.Delta );
	}
}
