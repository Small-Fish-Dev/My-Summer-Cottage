using Editor;
using Sandbox;
using Sauna.Game;
using static Sauna.TaskMaster;

namespace Sauna;

public class SaunaScriptedEvent
{
	/// <summary>
	/// Does this scripted event instantly start at the new session instead of a timeframe? (Defaults to midnight if SaunaDay NewSessionOnly is false and the day rolls over)
	/// </summary>
	[Property]
	public bool TriggerOnNewSession { get; set; } = false;

	/// <summary>
	/// When the scripted event plays in in-game hours (Ex. 7.5 = 07:30, 23.25 = 23:15, 0.5 = 00:30).
	/// </summary>
	[Property]
	[HideIf( "TriggerOnNewSession", true )]
	public RangedFloat TriggerTimeslot { get; set; } = new RangedFloat( 10.5f, 12.0f );

	[Hide]
	[JsonIgnore]
	public float TriggerTime { get; set; } = 0f;

	/// <summary>
	/// This scripted event needs to be triggered for the story to progress
	/// </summary>
	[Property]
	public bool TriggerNecessary { get; set; } = true;

	public delegate void ScriptedAction( Player player );

	/// <summary>
	/// When the scripted event starts. Send sms here, calls, load events, trigger them, assign tasks, etc...
	/// </summary>
	[Property]
	public ScriptedAction Setup { get; set; }

	[Hide]
	[JsonIgnore]
	public bool Triggered { get; set; } = false;

	public SaunaScriptedEvent() { }
}

public class SaunaDay
{
	[Property]
	public List<SaunaScriptedEvent> ScriptedEvents { get; set; }

	/// <summary>
	/// This Story Day can only start on a new session (Exiting the car), you can't just pass the night for it to begin
	/// </summary>
	[Property]
	public bool NewSessionOnly { get; set; } = false;

	/// <summary>
	/// How many random events to load at the beginning of the session (ONLY session, not new day, events will unload at the end of session)
	/// </summary>
	[Property]
	[Category( "Event Pool" )]
	public Dictionary<EventRarity, int> RandomEvents { get; set; } = new Dictionary<EventRarity, int>
	{
		{ EventRarity.Common, 3 },
		{ EventRarity.Uncommon, 2 },
		{ EventRarity.Rare, 1 },
	};

	[Hide]
	[JsonIgnore]
	public bool Completed { get; set; } = false;
}

[Icon( "auto_stories" )]
public class StoryMaster : Component
{
	public class SaunaStoryProgression
	{
		[JsonInclude]
		public int StoryDay { get; set; } = 1;

		[JsonInclude]
		public int GameDay { get; set; } = 1;

		public SaunaStoryProgression() { }
	}

	/// <summary>
	/// Define each story days, if a story day hasn't been completed it will roll over to the next in-game day
	/// </summary>
	[Property]
	public Dictionary<int, SaunaDay> StoryDays { get; set; } = new();

	/// <summary>
	/// Current story day/game day
	/// </summary>
	[Property]
	[HostSync]
	public SaunaStoryProgression StoryProgression { get; set; }

	public SaunaDay CurrentSaunaDay => StoryDays.TryGetValue( StoryProgression.StoryDay, out var saunaDay ) ? saunaDay : LastValidSaunaDay;
	public SaunaDay LastValidSaunaDay => StoryDays.Any() ? StoryDays.Last().Value : null;

	public void InternalStartStoryDay()
	{
		if ( CurrentSaunaDay == null ) return;

		foreach ( var scriptedEvent in CurrentSaunaDay.ScriptedEvents )
		{
			if ( scriptedEvent.TriggerOnNewSession )
				scriptedEvent.TriggerTime = 0f;
			else
				scriptedEvent.TriggerTime = new Random().Float( scriptedEvent.TriggerTimeslot.x, scriptedEvent.TriggerTimeslot.y );
		}

		// Load events
	}

	public static void StartStoryDay()
	{
		var storyMaster = GameManager.ActiveScene.GetAllComponents<StoryMaster>().FirstOrDefault(); // Find the story master

		if ( storyMaster == null ) return;

		storyMaster.InternalStartStoryDay();
	}

	public void InternalStartGameDay()
	{
		// Whatever lol
	}

	public static void StartGameDay()
	{
		var storyMaster = GameManager.ActiveScene.GetAllComponents<StoryMaster>().FirstOrDefault(); // Find the story master

		if ( storyMaster == null ) return;

		storyMaster.InternalStartGameDay();
	}

	public void InternalSetStoryDay( int dayNumber )
	{
		StoryProgression.StoryDay = dayNumber;

		InternalSaveStoryProgression();
	}

	public static void SetStoryDay( int dayNumber )
	{
		var storyMaster = GameManager.ActiveScene.GetAllComponents<StoryMaster>().FirstOrDefault(); // Find the story master

		if ( storyMaster == null ) return;

		storyMaster.InternalSetStoryDay( dayNumber );
	}

	public void InternalSetGameDay( int dayNumber )
	{
		StoryProgression.GameDay = dayNumber;

		InternalSaveStoryProgression();
	}

	public static void SetGameDay( int dayNumber )
	{
		var storyMaster = GameManager.ActiveScene.GetAllComponents<StoryMaster>().FirstOrDefault(); // Find the story master

		if ( storyMaster == null ) return;

		storyMaster.InternalSetGameDay( dayNumber );
	}

	public void LoadStoryProgression()
	{
		if ( FileSystem.Data.FileExists( "story.json" ) )
			StoryProgression = FileSystem.Data.ReadJsonOrDefault<SaunaStoryProgression>( "story.json" );
		else
		{
			StoryProgression = new();
			InternalSaveStoryProgression();
		}
	}

	internal void InternalSaveStoryProgression()
	{
		FileSystem.Data.WriteJson( "story.json", StoryProgression );
	}

	/// <summary>
	/// Save the story progression (Story Day and Game Day)
	/// </summary>
	public static void SaveStoryProgression()
	{
		var storyMaster = GameManager.ActiveScene.GetAllComponents<StoryMaster>().FirstOrDefault(); // Find the story master

		if ( storyMaster == null ) return;

		storyMaster.InternalSaveStoryProgression();
	}

	internal void InternalResetStoryProgression()
	{
		StoryProgression = new();
		InternalSaveStoryProgression();
	}

	/// <summary>
	/// Reset the story progression (Story Day and Game Day)
	/// </summary>
	public static void ResetStoryProgression()
	{
		var storyMaster = GameManager.ActiveScene.GetAllComponents<StoryMaster>().FirstOrDefault(); // Find the story master

		if ( storyMaster == null ) return;

		storyMaster.InternalResetStoryProgression();
	}

	protected override void OnStart()
	{
		LoadStoryProgression();

		InternalStartStoryDay();
		InternalStartGameDay();
	}

	protected override void OnFixedUpdate()
	{
		var timeManager = Scene.GetAllComponents<GameTimeManager>().First();

		if ( timeManager == null ) return;

		if ( CurrentSaunaDay != null )
		{
			var currentHour = timeManager.InGameHours;

			foreach ( var scriptedEvent in CurrentSaunaDay.ScriptedEvents )
			{
				if ( !scriptedEvent.Triggered )
				{
					if ( scriptedEvent.TriggerTime <= currentHour )
					{
						scriptedEvent.Setup?.Invoke( Player.Local );
						scriptedEvent.Triggered = true;
					}
				}

				Log.Info( $"Triggered: {scriptedEvent.Triggered} - TriggerTime: {scriptedEvent.TriggerTime} - CurrentTime: {currentHour}" );
			}
		}
	}
}
