using Editor;
using Sandbox;
using Sauna.Event;
using static Sauna.TaskMaster;

namespace Sauna;

public class SaunaScriptedEvent
{
	/// <summary>
	/// Does this scripted event instantly start at the new session instead of a timeframe? 
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
	/// This scripted event needs to be completed for the story to progress
	/// </summary>
	[Property]
	public bool CompletionNecessary { get; set; } = true;

	public delegate Task<string> ScriptedAction( Player player );

	/// <summary>
	/// When the scripted event starts. Send sms here, calls, load events, trigger them, assign tasks, etc... 
	/// Output Result is a string defining which signal trigger is the one needed to complete this scripted action
	/// </summary>
	[Property]
	public ScriptedAction Setup { get; set; }

	[Hide]
	[JsonIgnore]
	public string SignalToComplete { get; set; }

	[Hide]
	[JsonIgnore]
	public bool Completed { get; set; } = false;

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
	GameTimeManager _timeManager => Components.Get<GameTimeManager>();

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
				scriptedEvent.TriggerTime = Game.Random.Float( scriptedEvent.TriggerTimeslot.x, scriptedEvent.TriggerTimeslot.y );
		}


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
		Log.Info( "Advancing story" );
	}

	/// <summary>
	/// Set the current game day and save it
	/// </summary>
	/// <param name="dayNumber"></param>
	public void SetGameDay( int dayNumber )
	{
		StoryProgression.GameDay = dayNumber;
		SaveStoryProgression();
		Log.Info( "SET TO" );
		Log.Info( dayNumber );
	}

	/// <summary>
	/// Go to the next game day
	/// </summary>
	public void NextGameDay()
	{
		var nextGameDay = CurrentGameDay + 1;
		SetGameDay( nextGameDay );
	}

	public void LoadNPCs()
	{
		foreach ( var spawner in Scene.GetAllComponents<NpcSpawnArea>() )
			spawner.SpawnNPCs();
	}

	public void UnloadNPCs()
	{
		foreach ( var spawner in Scene.GetAllComponents<NpcSpawnArea>() )
			spawner.RemoveNPCs();
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

		Log.Info( StoryProgression.GameDay );
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

	public void LoadEventPool()
	{
		if ( CurrentSaunaDay == null ) return;

		if ( CurrentSaunaDay.RandomEvents.ContainsKey( EventRarity.Common ) )
		{
			int eventsPicked = 0;

			for ( int common = 0; common < CurrentSaunaDay.RandomEvents[EventRarity.Common]; common++ )
			{
				var availableCommonEvents = _eventMaster.AllEvents.Where( x => x.Type != EventType.Direct && x.Rarity == EventRarity.Common )
					.Where( x => !_eventMaster.CurrentEvents.Contains( x ) )
					.ToList(); // We haven't chosen it yet

				if ( availableCommonEvents.Any() )
				{
					var chosenEvent = Game.Random.FromList( availableCommonEvents );
					chosenEvent.Enable();
					eventsPicked++;
				}
			}

			Log.Info( $"{eventsPicked} common events loaded." );
		}

		if ( CurrentSaunaDay.RandomEvents.ContainsKey( EventRarity.Uncommon ) )
		{
			int eventsPicked = 0;

			for ( int uncommon = 0; uncommon < CurrentSaunaDay.RandomEvents[EventRarity.Uncommon]; uncommon++ )
			{
				var availableUncommonEvents = _eventMaster.AllEvents.Where( x => x.Type != EventType.Direct && x.Rarity == EventRarity.Uncommon )
					.Where( x => !_eventMaster.CurrentEvents.Contains( x ) )
					.ToList(); // We haven't chosen it yet

				if ( availableUncommonEvents.Any() )
				{
					var chosenEvent = Game.Random.FromList( availableUncommonEvents );
					chosenEvent.Enable();
					eventsPicked++;
				}
			}

			Log.Info( $"{eventsPicked} uncommon events loaded." );
		}

		if ( CurrentSaunaDay.RandomEvents.ContainsKey( EventRarity.Rare ) )
		{
			int eventsPicked = 0;

			for ( int rare = 0; rare < CurrentSaunaDay.RandomEvents[EventRarity.Rare]; rare++ )
			{
				var availableRareEvents = _eventMaster.AllEvents.Where( x => x.Type != EventType.Direct && x.Rarity == EventRarity.Rare )
					.Where( x => !_eventMaster.CurrentEvents.Contains( x ) )
					.ToList(); // We haven't chosen it yet

				if ( availableRareEvents.Any() )
				{
					var chosenEvent = Game.Random.FromList( availableRareEvents );
					chosenEvent.Enable();
					eventsPicked++;
				}
			}

			Log.Info( $"{eventsPicked} rare events loaded." );
		}
	}

	[ConCmd]
	public static void StartSession()
	{
		var storyMaster = Game.ActiveScene.GetAllComponents<StoryMaster>().First();

		if ( storyMaster == null ) return;

		storyMaster.LoadStoryProgression();
		storyMaster.StartStoryDay();
		storyMaster._timeManager.StartDay();

		storyMaster._eventMaster.UnloadAllEvents();
		storyMaster.LoadEventPool();

		storyMaster.LoadNPCs();
		Log.Info( "Session started" );
	}

	[ConCmd]
	public static void EndSession()
	{
		var storyMaster = Game.ActiveScene.GetAllComponents<StoryMaster>().First();

		if ( storyMaster == null ) return;

		Log.Info( "Ending Session" );

		storyMaster._timeManager.EndDay();

		if ( storyMaster.CurrentSaunaDay.Completed )
		{
			storyMaster.NextStoryDay();
			storyMaster.NextGameDay();
		}
		else
		{
			storyMaster.NextGameDay();
		}

		storyMaster._eventMaster.UnloadAllEvents();

		storyMaster.UnloadNPCs();

		storyMaster.SaveGame();
	}

	public void SaveGame()
	{
		SaveStoryProgression();
		_taskMaster.SaveTasksProgression();
		_eventMaster.SaveEventsProgression();

		Log.Info( "Game Saved" );
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
						scriptedEvent.Triggered = true;
						BeginScriptedEvent( scriptedEvent );
					}
				}
			}

			if ( !CurrentSaunaDay.Completed )
				if ( CurrentSaunaDay.ScriptedEvents.All( x => x.Completed || x.Triggered && !x.CompletionNecessary ) )
				{
					CurrentSaunaDay.Completed = true;
					Log.Info( "All story scripted event requirements completed, story can now progress" );
				}
		}
	}

	internal async void BeginScriptedEvent( SaunaScriptedEvent scriptedEvent )
	{
		var signalToComplete = await scriptedEvent.Setup?.Invoke( Player.Local );
		scriptedEvent.SignalToComplete = signalToComplete;
	}
}
