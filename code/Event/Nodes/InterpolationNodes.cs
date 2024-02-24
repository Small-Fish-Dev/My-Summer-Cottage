﻿using Sandbox;
using Sandbox.Utility;
using Sauna;

public static partial class InterpolationNodes
{
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
	public static async Task InterpolatePosition( GameObject target, Vector3 from, Vector3 to, EasingFunction easer, float duration = 1f )
	{
		TimeSince _timeSince = 0;

		var easingFunction = GetEasingFunction( easer );

		while ( _timeSince < duration )
		{
			if ( !target.IsValid() ) return;

			var newPos = Vector3.Lerp( from, to, easingFunction( _timeSince / duration ) );
			target.Transform.Position = newPos;

			await Task.Delay( (int)(Time.Delta * 1000) );
		}
	}

	/// <summary>
	/// Interpolates the rotation from A to B
	/// </summary>
	[ActionGraphNode( "event.interpolaterotation" )]
	[Title( "Interpolate Rotation" ), Group( "Events" ), Icon( "sync" )]
	public static async Task InterpolateRotation( GameObject target, Rotation from, Rotation to, EasingFunction easer, float duration = 1f )
	{
		TimeSince _timeSince = 0;

		var easingFunction = GetEasingFunction( easer );

		while ( _timeSince < duration )
		{
			if ( !target.IsValid() ) return;

			var newRot = Rotation.Lerp( from, to, easingFunction( _timeSince / duration ) );
			target.Transform.Rotation = newRot;

			await Task.Delay( (int)(Time.Delta * 1000) );
		}
	}

	/// <summary>
	/// Interpolates the scale from A to B
	/// </summary>
	[ActionGraphNode( "event.interpolatescale" )]
	[Title( "Interpolate Scale" ), Group( "Events" ), Icon( "aspect_ratio" )]
	public static async Task InterpolateScale( GameObject target, Vector3 from, Vector3 to, EasingFunction easer, float duration = 1f )
	{
		TimeSince _timeSince = 0;

		var easingFunction = GetEasingFunction( easer );

		while ( _timeSince < duration )
		{
			if ( !target.IsValid() ) return;

			var newScale = Vector3.Lerp( from, to, easingFunction( _timeSince / duration ) );
			target.Transform.Scale = newScale;

			await Task.Delay( (int)(Time.Delta * 1000) );
		}
	}
}
