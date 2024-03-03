using Editor;
using Sandbox;
using Sauna.Event;
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

	/// <summary>
	/// Get the current story day
	/// </summary>
	public int CurrentStoryDay
	{
		get
		{
			var storyMaster = Scene.GetAllComponents<StoryMaster>().FirstOrDefault(); // Find the story master

			if ( storyMaster == null ) return 0;

			return storyMaster.StoryProgression.StoryDay;
		}
	}

	/// <summary>
	/// Get the current game day
	/// </summary>
	public int CurrentGameDay
	{
		get
		{
			return StoryProgression.GameDay;
		}
	}

	public SaunaDay CurrentSaunaDay => StoryDays.TryGetValue( StoryProgression.StoryDay, out var saunaDay ) ? saunaDay : LastValidSaunaDay;
	public SaunaDay LastValidSaunaDay => StoryDays.Any() ? StoryDays.Last().Value : null;
	public SaunaDay NextSaunaDay => StoryDays.TryGetValue( StoryProgression.StoryDay + 1, out var saunaDay ) ? saunaDay : null;

	TaskMaster _taskMaster => Components.Get<TaskMaster>();
	EventMaster _eventMaster => Components.Get<EventMaster>();

	/// <summary>
	/// Start the story day
	/// </summary>
	public void StartStoryDay()
	{
		if ( CurrentSaunaDay == null ) return;

		foreach ( var scriptedEvent in CurrentSaunaDay.ScriptedEvents )
		{
			if ( scriptedEvent.TriggerOnNewSession )
				scriptedEvent.TriggerTime = 0f;
			else
				scriptedEvent.TriggerTime = new Random().Float( scriptedEvent.TriggerTimeslot.x, scriptedEvent.TriggerTimeslot.y );
		}
	}

	/// <summary>
	/// Start the game day
	/// </summary>
	public void StartGameDay()
	{
		var nextSaunaDay = NextSaunaDay;

		if ( CurrentSaunaDay?.Completed ?? true ) // If we completed our story day
			if ( NextSaunaDay == null || !NextSaunaDay.NewSessionOnly ) // And we don't have a next day planned, or it's not new session only
				NextStoryDay(); // Move to the next story day without ending the session
	}

	/// <summary>
	/// Set the current story day and save it
	/// </summary>
	/// <param name="dayNumber"></param>
	public void SetStoryDay( int dayNumber )
	{
		StoryProgression.StoryDay = dayNumber;

		SaveStoryProgression();
	}

	/// <summary>
	/// Go to the next story day
	/// </summary>
	public void NextStoryDay()
	{
		var nestStoryDay = CurrentStoryDay + 1;
		SetStoryDay( nestStoryDay );
	}

	/// <summary>
	/// Set the current game day and save it
	/// </summary>
	/// <param name="dayNumber"></param>
	public void SetGameDay( int dayNumber )
	{
		StoryProgression.GameDay = dayNumber;
		SaveStoryProgression();
	}

	/// <summary>
	/// Go to the next game day
	/// </summary>
	public void NextGameDay()
	{
		var nextGameDay = CurrentGameDay + 1;
		SetGameDay( nextGameDay );
	}

	public void LoadStoryProgression()
	{
		if ( FileSystem.Data.FileExists( "story.json" ) )
			StoryProgression = FileSystem.Data.ReadJsonOrDefault<SaunaStoryProgression>( "story.json" );
		else
		{
			StoryProgression = new();
			SaveStoryProgression();
		}
	}

	/// <summary>
	/// Save the story progression (Story Day and Game Day)
	/// </summary>
	public void SaveStoryProgression()
	{
		FileSystem.Data.WriteJson( "story.json", StoryProgression );
	}

	/// <summary>
	/// Reset the story progression (Story Day and Game Day)
	/// </summary>
	public void ResetStoryProgression()
	{
		StoryProgression = new();
		SaveStoryProgression();
	}

	public void StartSession()
	{
		LoadStoryProgression();

		StartStoryDay();
		StartGameDay();


	}

	public void EndSession()
	{
		// Unload Events

		SaveGame();
	}

	public void SaveGame()
	{
		SaveStoryProgression();
		_taskMaster.SaveTasksProgression();
		_eventMaster.SaveEventsProgression();
	}

	protected override void OnStart()
	{
		if ( Scene.IsEditor ) return;

		StartSession();
	}

	protected override void OnFixedUpdate()
	{
		if ( Scene.IsEditor ) return;

		var timeManager = Scene.GetAllComponents<GameTimeManager>()?.FirstOrDefault();

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
			}
		}
	}
}
