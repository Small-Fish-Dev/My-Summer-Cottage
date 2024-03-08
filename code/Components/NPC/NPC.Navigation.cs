using Sandbox;
using Sauna;

public partial class NPC
{
	static Vector3[] _possibleDirections =
	{
		Vector3.Forward,								// North
		( Vector3.Forward + Vector3.Left ) / 2f,		// North-East
		Vector3.Left,									// East
		( Vector3.Left + Vector3.Backward ) / 2f,		// South-East
		Vector3.Backward,								// South
		( Vector3.Backward + Vector3.Right ) / 2f,		// South-West
		Vector3.Right,									// West
		( Vector3.Right + Vector3.Forward ) / 2f,		// North-West
	};

	public virtual void ComputeNavigation()
	{
		if ( MoveHelper == null ) return;
		CheckNewTargetPos();

		var distanceToTarget = Transform.Position.Distance( TargetPosition );

		if ( distanceToTarget >= MoveHelper.TraceRadius )
		{
			var movement3D = false; // TODO: Replace with property if we want flying NPCs
			var positionDifference = TargetPosition - Transform.Position;
			var wishDirection = (movement3D ? positionDifference.WithZ( 0f ) : positionDifference).Normal;

			var interestVector = getInterest( wishDirection );
			var dangerVector = getDanger();

			MoveHelper.WishVelocity = wishDirection * WalkSpeed;
		}

		MoveHelper.Move();
	}

	float[] getInterest( Vector3 direction )
	{
		return _possibleDirections
			.Select( x => direction.Dot( x ) )
			.ToArray();
	}

	float[] getDanger()
	{
		var totalDirections = _possibleDirections.Length;
		var directionDanger = new float[totalDirections];
		var currentDirection = 0;

		foreach ( var direction in _possibleDirections )
		{
			var startPosition = Transform.Position + Vector3.Up * (MoveHelper.StepHeight + MoveHelper.TraceRadius / 2f);
			var endPosition = startPosition + direction * 100f;
			var dangerTrace = Scene.Trace.Sphere( MoveHelper.TraceRadius, startPosition, endPosition )
				.IgnoreGameObjectHierarchy( GameObject )
				.Run();

			directionDanger[currentDirection] = dangerTrace.Hit ? 5f : 0f;

			if ( dangerTrace.Hit )
			{
				var directionToLeft = (currentDirection - 1 + totalDirections) % totalDirections;
				var directionToRight = (currentDirection + 1) % totalDirections;

				directionDanger[directionToLeft] += 2f;
				directionDanger[directionToRight] += 2f;
			}

			currentDirection++;
		}

		return directionDanger;
	}

	void CheckNewTargetPos()
	{
		if ( TargetObject.IsValid() )
		{
			if ( !IsWithinRange( TargetObject, DesiredDistance ) ) // Has our target moved?
				MoveTo( GetPreferredTargetPosition( TargetObject ) );
		}
	}

	/// <summary>
	/// Make the NPC move to the target position
	/// </summary>
	/// <param name="targetPosition"></param>
	public void MoveTo( Vector3 targetPosition )
	{
		TargetPosition = targetPosition;
	}
}
