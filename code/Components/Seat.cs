using Sandbox.Diagnostics;

namespace Sauna.Components;

public sealed class Seat : Component
{
	[Property] public GameObject KneesPosition { get; set; }

	public bool IsOccupied => TheOneWhoFuckigSitsOnThisSeat.IsValid();

	[Sync] public Player TheOneWhoFuckigSitsOnThisSeat { get; private set; }

	private SceneModel _model;

	private SceneModel GetModel()
	{
		var world = Game.ActiveScene?.SceneWorld;
		if ( world == null )
			return null;

		_model ??= new SceneModel( world, "models/guy/guy.vmdl", global::Transform.Zero );
		// _model.RenderingEnabled = true;
		return _model;
	}

	protected override void OnAwake()
	{
		if ( !KneesPosition.IsValid() )
			throw new Exception( $"Seat {GameObject} doesn't have a valid KneesPosition" );
	}

	protected override void DrawGizmos()
	{
		// TODO: draw the player in a sitting position

		var model = GetModel();
		if ( model is null )
			return;

		if ( Game.IsPlaying || !Gizmo.HasSelected || !KneesPosition.IsValid() )
		{
			model.RenderingEnabled = false;
		}
		else
		{
			model.RenderingEnabled = true;
			model.Position = KneesPosition.Transform.Position;
			model.Rotation = KneesPosition.Transform.Rotation;
			model.SetAnimParameter( "sitting", true );
		}

		model.Update( Time.Delta );
	}

	[Broadcast]
	public void Sit( Player player )
	{
		if ( TheOneWhoFuckigSitsOnThisSeat.IsValid() )
			return;

		TheOneWhoFuckigSitsOnThisSeat = player;
		player.CurrentSeat = this;
	}

	[Broadcast]
	public void StandUp()
	{
		Assert.IsValid( TheOneWhoFuckigSitsOnThisSeat );
		
		TheOneWhoFuckigSitsOnThisSeat.CurrentSeat = null;
		TheOneWhoFuckigSitsOnThisSeat = null;
	}
}
