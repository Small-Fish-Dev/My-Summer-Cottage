using Sandbox;
using Sauna;

public sealed class NPC : Component
{
	[Property]
	public NavMeshAgent Agent { get; set; }

	[Property]
	public SkinnedModelRenderer Model { get; set; }

	public Vector3 TargetPosition { get; set; }
	Vector3 _currentTargetPosition;

	protected override void OnUpdate()
	{
		if ( Agent == null ) return;

		if ( TargetPosition.Distance( _currentTargetPosition ) >= 30f )
		{
			_currentTargetPosition = TargetPosition;
			var closestPoint = Scene.NavMesh.GetClosestPoint( _currentTargetPosition );
			Log.Info( "MOVING" );
			if ( closestPoint != null )
				Agent.MoveTo( closestPoint.Value );
		}

		Model.Transform.Rotation = Rotation.LookAt( Agent.Velocity.WithZ( 0f ), Vector3.Up );

		if ( Model == null ) return;

		var oldX = Model.GetFloat( "move_x" );
		var oldY = Model.GetFloat( "move_y" );
		var newX = Vector3.Dot( Agent.Velocity, Model.Transform.Rotation.Forward ) / 100f;
		var newY = Vector3.Dot( Agent.Velocity, Model.Transform.Rotation.Right ) / 100f;
		var x = MathX.Lerp( oldX, newX, Time.Delta * 5f );
		var y = MathX.Lerp( oldY, newY, Time.Delta * 5f );

		Model.Set( "move_x", x );
		Model.Set( "move_y", y );
	}

	protected override void OnFixedUpdate()
	{
		if (Scene.GetAllComponents<Player>().FirstOrDefault() is Player player)
			TargetPosition = player.Transform.Position;
	}
}
