using Sandbox;
using Sandbox.Utility;
using Sauna;
using static SaunaActionNodes;

public static partial class SaunaActionNodes
{
	/// <summary>
	/// Punches the player, detaches them from the ground and throws them away local to their transform
	/// </summary>
	[ActionGraphNode( "event.localpunchplayer" )]
	[Title( "Local Punch Player" ), Group( "Events" ), Icon( "sports_mma" )]
	public static void LocalPunch( Player player, Vector3 launchDirection, float strength )
	{
		if ( player == null ) return;

		var punchDirection = launchDirection.Normal;
		var punch = punchDirection * strength;

		if ( player.Components.TryGet<MoveHelper>( out MoveHelper moveHelper ) )
			moveHelper.Punch( punch );
	}

	/// <summary>
	/// Punches the player, detaches them from the ground and throws them away starting from the worldSource
	/// </summary>
	[ActionGraphNode( "event.worldpunchplayer" )]
	[Title( "World Punch Player" ), Group( "Events" ), Icon( "sports_mma" )]
	public static void WorldPunch( Player player, Vector3 worldSource, float strength )
	{
		if ( player == null ) return;

		var punchVector = player.Transform.Position + Vector3.Up * 36f - worldSource;
		var punchDirection = punchVector.Normal;
		var punch = punchDirection * strength;

		if ( player.Components.TryGet<MoveHelper>( out MoveHelper moveHelper ) )
			moveHelper.Punch( punch );
	}

	public enum EasingFunction
	{
		Linear,
		EaseIn,
		EaseOut,
		EaseInOut,
		BounceIn,
		BounceOut,
		BounceInOut
	}

	public static Easing.Function GetEasingFunction( EasingFunction easeFunction )
	{
		switch ( easeFunction )
		{
			case EasingFunction.Linear:
				return Easing.Linear;
			case EasingFunction.EaseIn:
				return Easing.EaseIn;
			case EasingFunction.EaseOut:
				return Easing.EaseOut;
			case EasingFunction.EaseInOut:
				return Easing.EaseInOut;
			case EasingFunction.BounceIn:
				return Easing.BounceIn;
			case EasingFunction.BounceOut:
				return Easing.BounceOut;
			case EasingFunction.BounceInOut:
				return Easing.BounceInOut;
			default:
				return Easing.Linear;
		}
	}

	/// <summary>
	/// Interpolates the position from A to B
	/// </summary>
	[ActionGraphNode( "event.interpolateposition" )]
	[Title( "Interpolate Position" ), Group( "Events" ), Icon( "double_arrow" )]
	public static async Task InterpolatePosition( GameObject target, Vector3 from, Vector3 to, float duration, EasingFunction easer )
	{
		TimeSince timeSince = 0;

		var easingFunction = GetEasingFunction( easer );

		while ( timeSince < duration )
		{
			if ( !target.IsValid() ) return;

			var newPos = Vector3.Lerp( from, to, easingFunction( timeSince / duration ) );
			target.Transform.Position = newPos;

			await Task.Delay( (int)(Time.Delta * 1000) );
		}
	}

	/// <summary>
	/// Interpolates the rotation from A to B
	/// </summary>
	[ActionGraphNode( "event.interpolaterotation" )]
	[Title( "Interpolate Rotation" ), Group( "Events" ), Icon( "sync" )]
	public static async Task InterpolateRotation( GameObject target, Rotation from, Rotation to, float duration, EasingFunction easer )
	{
		TimeSince timeSince = 0;

		var easingFunction = GetEasingFunction( easer );

		while ( timeSince < duration )
		{
			if ( !target.IsValid() ) return;

			var newRot = Rotation.Lerp( from, to, easingFunction( timeSince / duration ) );
			target.Transform.Rotation = newRot;

			await Task.Delay( (int)(Time.Delta * 1000) );
		}
	}

	/// <summary>
	/// Interpolates the scale from A to B
	/// </summary>
	[ActionGraphNode( "event.interpolatescale" )]
	[Title( "Interpolate Scale" ), Group( "Events" ), Icon( "aspect_ratio" )]
	public static async Task InterpolateScale( GameObject target, Vector3 from, Vector3 to, float duration, EasingFunction easer )
	{
		TimeSince timeSince = 0;

		var easingFunction = GetEasingFunction( easer );

		while ( timeSince < duration )
		{
			if ( !target.IsValid() ) return;

			var newScale = Vector3.Lerp( from, to, easingFunction( timeSince / duration ) );
			target.Transform.Scale = newScale;

			await Task.Delay( (int)(Time.Delta * 1000) );
		}
	}

	/// <summary>
	/// The event has finished playing.
	/// </summary>
	[ActionGraphNode( "event.finished" ), Pure]
	[Title( "Finish Event" ), Group( "Events" ), Icon( "flash_off" )]
	public static void EventFinished( EventComponent component )
	{
		component.Finish();
	}

}
