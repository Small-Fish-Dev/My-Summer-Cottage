namespace Sauna.Game;

public class GameTimeManager : Component
{
	[Property] public DirectionalLight Sun;

	[Property] public Gradient SkyDayColor;
	[Property] public Color SkyNightColor;

	/// <summary>
	/// Imagine it like the angle of a big pole sticking out of the Earth, and the Sun is spinning around it.
	/// The roll is ignored.
	/// </summary>
	[Property] public Angles SunOrbit;

	/// <summary>
	/// Time of the sunrise in in-game seconds (7 AM by default)
	/// </summary>
	[Property] public int SunriseTime = 7 * 60 * 60;

	/// <summary>
	/// Time of the sunset in in-game seconds (10 PM by default)
	/// </summary>
	[Property] public int SunsetTime = 22 * 60 * 60;

	/// <summary>
	/// Length of the day in real time second
	/// </summary>
	[Property] public float DayLength = 30 * 60;

	/// <summary>
	/// Progress of the day in percents [0; 1].
	/// </summary>
	public float DayPercent => _inGameTime / DayLength;

	public int InGameSeconds => (int)(DayPercent * 24 * 60 * 60);

	private TimeSince _inGameTime;

	protected override void OnAwake()
	{
		base.OnAwake();

		NewDay();
	}

	protected override void DrawGizmos()
	{
		base.DrawGizmos();

		if ( Gizmo.IsSelected && GameManager.IsPlaying )
		{
			Gizmo.Draw.ScreenText( $"Day percent: {DayPercent * 100f:F1}", Vector2.One * 10, "Roboto", 12f,
				TextFlag.LeftTop );
		}
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();

		if ( _inGameTime >= DayLength )
			NewDay();

		Rotation sunRotation;

		var igs = InGameSeconds;
		var isDayTime = igs > SunriseTime && igs < SunsetTime;
		if ( isDayTime )
		{
			// Value in [0; 1]
			var daytimePercent = ((float)igs).Remap( SunriseTime, SunsetTime );
			sunRotation = Rotation.From( SunOrbit.WithRoll( daytimePercent * 180 ) );
			Sun.LightColor = SkyDayColor.Evaluate( daytimePercent );
		}
		else
		{
			float nightPercent;
			if ( SunsetTime > SunriseTime )
			{
				/*
				 * TODO: pull many of these variables to a cached variable in case this code becomes a performance
				 * bottleneck
				 */
				var nightLength = 24f * 60f * 60f - Math.Abs( SunsetTime - SunriseTime );
				var nightLengthBeforeMidnight = 24f * 60f * 60f - SunsetTime;
				if ( igs > SunsetTime )
					nightPercent = ((float)igs).Remap( SunsetTime, 24 * 60 * 60,
						0, nightLengthBeforeMidnight / nightLength );
				else
					nightPercent = ((float)igs).Remap( 0, SunriseTime,
						nightLengthBeforeMidnight / nightLength, 1 );
			}
			else
			{
				nightPercent = ((float)igs).Remap( SunsetTime, SunriseTime );
			}

			sunRotation = Rotation.From( SunOrbit.WithRoll( nightPercent * 180 + 180 ) );
			Sun.LightColor = SkyNightColor;
		}

		Sun.Transform.Rotation = sunRotation.Right.EulerAngles;
		Sun.SkyColor = SkyNightColor;
		
		// using ( Gizmo.Scope() )
		// {
		// 	Gizmo.Draw.ScreenText( $"{DayPercent}", new Vector2( 200, 200 ) );
		//
		// 	// The sun
		// 	Gizmo.Draw.Line( Vector3.Zero, sunRotation.Left * 100 );
		//
		// 	// The "pole"
		// 	Gizmo.Draw.Color = Gizmo.Colors.Green;
		// 	Gizmo.Draw.Line( Vector3.Zero, SunOrbit.Forward * 100 );
		// }
	}

	/// <summary>
	/// Set the time
	/// </summary>
	/// <param name="seconds">Time as in-game seconds</param>
	public void SetTimeFromSeconds(int seconds)
	{
		_inGameTime = ((float) seconds).Remap( 0, 24 * 60 * 60, 0, DayLength );
	}

	private void NewDay()
	{
		_inGameTime = 0;
		Log.Info( "New day!" );
	}
}
