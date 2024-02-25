namespace Sauna.Game;

public class GameTimeManager : Component, Component.ExecuteInEditor
{
	[Property]
	[Category( "Components" )]
	public DirectionalLight Sun { get; set; }

	[Property]
	[Category( "Components" )]
	public SkyBox2D Skybox { get; set; }

	[Property]
	[Category( "Components" )]
	public GradientFog Fog { get; set; }

	[Property]
	[Category( "Visuals" )]
	public Gradient SkyDayColor { get; set; }

	[Property]
	[Category( "Visuals" )]
	public Color SkyNightColor { get; set; }

	/// <summary>
	/// Imagine it like the angle of a big pole sticking out of the Earth, and the Sun is spinning around it.
	/// The roll is ignored.
	/// </summary>
	[Property]
	[Category( "Visuals" )]
	public Angles SunOrbit { get; set; }

	/// <summary>
	/// Time of the sunrise in in-game seconds (7 AM by default)
	/// </summary>
	[Property]
	[Category( "Time" )]
	[Range( 0, 86400, 600 )]
	public int SunriseTime { get; set; } = 7 * 60 * 60;

	/// <summary>
	/// Time of the sunset in in-game seconds (10 PM by default)
	/// </summary>
	[Property]
	[Category( "Time" )]
	[Range( 0, 86400, 600 )]
	public int SunsetTime { get; set; } = 22 * 60 * 60;

	/// <summary>
	/// When do we start our day during normal gameplay (10 AM by default)
	/// </summary>
	[Property]
	[Category( "Time" )]
	[Range( 0, 86400, 600 )]
	public int StartTime { get; set; } = 10 * 60 * 60;

	/// <summary>
	/// Length of the day in real time second
	/// </summary>
	[Property]
	[Category( "Time" )]
	[Range( 0f, 1200f, 10f )]
	public float DayLength { get; set; } = 5 * 60;

	/// <summary>
	/// Progress of the day in percents [0; 1].
	/// </summary>
	public float DayPercent => _inGameTime / DayLength;

	public int InGameSeconds => (int)(DayPercent * 24 * 60 * 60);

	private TimeSince _inGameTime;

	protected override void OnAwake()
	{
		if ( !GameManager.IsPlaying ) return;

		NewDay();

		SetTimeFromSeconds( StartTime );
	}

	protected override void DrawGizmos()
	{
		if ( !Scene.IsEditor ) return;

		if ( Gizmo.IsSelected )
		{
			Gizmo.Draw.ScreenText( $"Day percent: {DayPercent * 100f:F1}", Vector2.One * 10, "Roboto", 12f,
				TextFlag.LeftTop );
		}
	}

	protected override void OnUpdate()
	{
		if ( Scene.IsEditor && !GameManager.IsPlaying )
		{
			SetTimeFromSeconds( StartTime );
		}

		if ( _inGameTime >= DayLength )
			NewDay();

		Rotation sunRotation;
		
		if (SunsetTime <= SunriseTime)
			Log.Error( "Sunset before the sunrise is not yet supported" );

		var igs = InGameSeconds;
		var isDayTime = igs > SunriseTime && igs < SunsetTime;
		if ( isDayTime )
		{
			// Value in [0; 1]
			var daytimePercent = ((float)igs).Remap( SunriseTime, SunsetTime );
			sunRotation = Rotation.From( SunOrbit.WithRoll( daytimePercent * 180 ) );

			if ( Sun != null )
				Sun.LightColor = SkyDayColor.Evaluate( daytimePercent );

			if ( Fog != null )
				Fog.Color = SkyDayColor.Evaluate( daytimePercent );

			if ( Skybox != null )
				Skybox.Tint = SkyDayColor.Evaluate( daytimePercent );
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

			if ( Sun != null )
				Sun.LightColor = SkyNightColor;

			if ( Fog != null )
				Fog.Color = SkyNightColor;

			if ( Skybox != null )
				Skybox.Tint = SkyNightColor;
		}


		if ( Sun != null )
		{
			Sun.Transform.Rotation = sunRotation.Right.EulerAngles;
			Sun.SkyColor = SkyNightColor;
		}

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
	[Broadcast( NetPermission.HostOnly )]
	public void SetTimeFromSeconds( int seconds )
	{
		_inGameTime = ((float)seconds).Remap( 0, 24 * 60 * 60, 0, DayLength );
	}

	/// <summary>
	/// Skip some time
	/// </summary>
	/// <param name="seconds">Time as in-game seconds</param>
	[Broadcast( NetPermission.HostOnly )]
	public void SkipTimeFromSeconds( int seconds )
	{
		_inGameTime += ((float)seconds).Remap( 0, 24 * 60 * 60, 0, DayLength );
	}

	[Broadcast( NetPermission.HostOnly )]
	private void NewDay()
	{
		_inGameTime = 0;
	}
}
