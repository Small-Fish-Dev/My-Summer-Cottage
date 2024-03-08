using Sandbox;
using Sauna;

public partial class NPC
{
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

			MoveHelper.WishVelocity = wishDirection * WalkSpeed;
		}

		MoveHelper.Move();
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
