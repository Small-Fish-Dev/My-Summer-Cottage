using Editor;
using Sandbox;
using Sauna.Event;
using System.Diagnostics.Metrics;
using static Sauna.TaskMaster;

namespace Sauna;

public class SaunaScriptedEvent
{
	/// <summary>
	/// Does this scripted event get triggered at a certain time or with a signal
	/// </summary>
	[Property]
	[JsonInclude]
	public bool TriggerByTime { get; set; }

	/// <summary>
	/// Which signal is going to trigger this event
	/// </summary>
	[Property]
	[JsonInclude]
	[HideIf( "TriggerByTime", true )]
	public Signal SignalToTrigger { get; set; }

	/// <summary>
	/// How many seconds to wait after the signal to trigger this scripted event
	/// </summary>
	[Property]
	[JsonInclude]
	[HideIf( "TriggerByTime", true )]
	public float TriggerDelay { get; set; }

	/// <summary>
	/// When the scripted event plays in in-game hours (Ex. 7.5 = 07:30, 23.25 = 23:15, 0.5 = 00:30).
	/// </summary>
	[Property]
	[JsonInclude]
	[ShowIf( "TriggerByTime", true )]
	public RangedFloat TriggerTimeslot { get; set; } = new RangedFloat( 10.5f, 12.0f );

	[Hide]
	[JsonIgnore]
	public float TriggerTime { get; set; }

	/// <summary>
	/// This scripted event needs to be completed for the story to progress
	/// </summary>
	[Property]
	[JsonInclude]
	public bool CompletionNecessary { get; set; }

	public delegate Task<string> ScriptedAction( Player player );

	/// <summary>
	/// When the scripted event starts. Send sms here, calls, load events, trigger them, assign tasks, etc... 
	/// Output Result is a string defining which signal trigger is the one needed to complete this scripted action
	/// </summary>
	[Property]
	[JsonInclude]
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
	GameTimeManager _timeManager => Components.Get<GameTimeManager>();

	/// <summary>
	/// Start the story day
	/// </summary>
	public void StartStoryDay()
	{
		if ( CurrentSaunaDay == null ) return;

		foreach ( var scriptedEvent in CurrentSaunaDay.ScriptedEvents )
		{
			if ( scriptedEvent.TriggerTimeslot.Range == RangedFloat.RangeType.Between )
				scriptedEvent.TriggerTime = scriptedEvent.TriggerTimeslot.GetValue();
			else
				scriptedEvent.TriggerTime = scriptedEvent.TriggerTimeslot.FixedValue;
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

		foreach ( var npc in Scene.GetAllComponents<NPC>() )
		{
			if ( npc.IsValid() && npc.Health != null && !npc.Health.Alive )
				npc.Destroy();
		}
	}

	public void LoadItems()
	{
		foreach ( var spawner in Scene.GetAllComponents<ItemSpawnArea>() )
			spawner.SpawnItems();
	}

	public void UnloadItems()
	{
		foreach ( var spawner in Scene.GetAllComponents<ItemSpawnArea>() )
			spawner.RemoveItems();
	}

	public void LoadStoryProgression()
	{
		if ( FileSystem.OrganizationData.FileExists( "story.json" ) )
			StoryProgression = FileSystem.OrganizationData.ReadJsonOrDefault<SaunaStoryProgression>( "story.json" );
		else
		{
			StoryProgression = new();
			SaveStoryProgression();
		}
	}

	/// <summary>
	/// Save the story progression (Story Day and Game Day)
	/// </summary>
	public void SaveStoryProgression( bool print = true )
	{
		if ( !Connection.Local.IsHost )
			return;

		FileSystem.OrganizationData.WriteJson( "story.json", StoryProgression );

		if ( print )
			Log.Info( "Story saved..." );
	}

	/// <summary>
	/// Reset the story progression (Story Day and Game Day)
	/// </summary>
	public void ResetStoryProgression()
	{
		StoryProgression = new();

		if ( FileSystem.OrganizationData.FileExists( "story.json" ) )
			FileSystem.OrganizationData.DeleteFile( "story.json" );

		Log.Info( "Story reset!" );
	}

	public void ResetPlayer()
	{
		if ( FileSystem.OrganizationData.FileExists( PlayerSave.FILE_PATH ) )
			FileSystem.OrganizationData.DeleteFile( PlayerSave.FILE_PATH );
		Log.Info( "Character reset!" );
	}

	public struct TriggeringEvent
	{
		public EventDefinition Event;
		public float Time;

		public TriggeringEvent() { }
		public TriggeringEvent( EventDefinition @event, float time )
		{
			Event = @event;
			Time = time;
		}
	}
	public List<TriggeringEvent> EventsToTrigger { get; set; } = new();

	public void LoadEventPool()
	{
		if ( CurrentSaunaDay == null ) return;

		if ( CurrentSaunaDay.RandomEvents.ContainsKey( EventRarity.Common ) )
		{
			int eventsPicked = 0;

			for ( int common = 0; common < CurrentSaunaDay.RandomEvents[EventRarity.Common]; common++ )
			{
				var availableCommonEvents = EventMaster.Instance.AllEvents.Where( x => x.Type != EventType.Direct && x.Rarity == EventRarity.Common )
					.Where( x => !EventMaster.Instance.CurrentEvents.Contains( x ) )
					.ToList(); // We haven't chosen it yet

				if ( availableCommonEvents.Any() )
				{
					var chosenEvent = Game.Random.FromList( availableCommonEvents );

					if ( chosenEvent.Type == EventType.Random )
						EventsToTrigger.Add( new TriggeringEvent( chosenEvent, Game.Random.Float( 10f, 24f ) ) );
					else
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
				var availableUncommonEvents = EventMaster.Instance.AllEvents.Where( x => x.Type != EventType.Direct && x.Rarity == EventRarity.Uncommon )
					.Where( x => !EventMaster.Instance.CurrentEvents.Contains( x ) )
					.ToList(); // We haven't chosen it yet

				if ( availableUncommonEvents.Any() )
				{
					var chosenEvent = Game.Random.FromList( availableUncommonEvents );

					if ( chosenEvent.Type == EventType.Random )
						EventsToTrigger.Add( new TriggeringEvent( chosenEvent, Game.Random.Float( 10f, 24f ) ) );
					else
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
				var availableRareEvents = EventMaster.Instance.AllEvents.Where( x => x.Type != EventType.Direct && x.Rarity == EventRarity.Rare )
					.Where( x => !EventMaster.Instance.CurrentEvents.Contains( x ) )
					.ToList(); // We haven't chosen it yet


				if ( availableRareEvents.Any() )
				{
					var chosenEvent = Game.Random.FromList( availableRareEvents );

					if ( chosenEvent.Type == EventType.Random )
						EventsToTrigger.Add( new TriggeringEvent( chosenEvent, Game.Random.Float( 10f, 24f ) ) );
					else
						chosenEvent.Enable();

					eventsPicked++;
				}
			}

			Log.Info( $"{eventsPicked} rare events loaded." );
		}
	}

	[Broadcast( NetPermission.HostOnly )]
	public static void StartSession()
	{
		_ = PlayIntro();
	}

	private static async Task PlayIntro()
	{
		Player.Local.BlackScreen( 0f, 4f, 1f );
		UI.Hud.Instance.Panel.PlaySound( "car_intro" );
		await GameTask.DelayRealtimeSeconds( 2f );

		SetupSession();
	}

	private static void SetupSession()
	{
		var storyMaster = Game.ActiveScene.GetAllComponents<StoryMaster>().First();

		if ( storyMaster == null ) return;

		if ( storyMaster._taskMaster != null && _instance != null )
			storyMaster.ClearTasks();

		if ( Connection.Local.IsHost )
		{
			if ( storyMaster.CurrentSaunaDay.Completed )
				storyMaster.NextStoryDay();

			storyMaster.LoadStoryProgression();
			storyMaster.LoadNPCs();
			storyMaster.LoadItems();
		}

		storyMaster.StartStoryDay();
		storyMaster._timeManager.StartDay();

		EventMaster.Instance.UnloadAllEvents();
		storyMaster.LoadEventPool();
		storyMaster.SetRandomDialogues();
		storyMaster.RandomizeClothing();

		if ( Player.Local.IsValid() )
			Player.Local.Respawn();
	}

	[Broadcast( NetPermission.HostOnly )]
	public static void EndSession()
	{
		var storyMaster = Game.ActiveScene.GetAllComponents<StoryMaster>().First();

		if ( storyMaster == null ) return;

		Log.Info( "Ending Session" );

		storyMaster._timeManager.EndDay();

		storyMaster.NextGameDay();

		EventMaster.Instance.UnloadAllEvents();
		storyMaster.EventsToTrigger.Clear();
		storyMaster.ClearTriggeredEvents();

		if ( Connection.Local.IsHost )
		{
			storyMaster.UnloadNPCs();
			storyMaster.UnloadItems();
		}

		var stove = Game.ActiveScene.GetAllComponents<Stove>().First();
		stove?.Stop();

		storyMaster.SaveGame();
	}

	public void RandomizeClothing()
	{
		var modelToCheck = Model.Load( "models/props/clothing_parcel/clothing_parcel.vmdl" );
		var allClothingShops = Scene.GetAllComponents<ShopItem>()
			.Where( x => x.GameObject.Components.Get<ModelRenderer>()?.Model == modelToCheck );

		var allClothingPrefabs = PrefabLibrary.FindByComponent<ItemEquipment>()
			.Where( x => x.GetComponent<ItemEquipment>().Get<EquipSlot>( "Slot" ) != EquipSlot.Hand )
			.ToList();

		foreach ( var shop in allClothingShops )
		{
			var chosen = Game.Random.FromList( allClothingPrefabs ).Prefab;
			shop.Item = chosen;
			shop.CreateWorldIcon();
			shop.ResetName();
			shop.ResetPrice();
		}
	}

	public void SetRandomDialogues()
	{
		var allDialogues = Scene.GetAllComponents<DialogueTree>()
			.Where( x => x.HasRandomDialogues );

		foreach ( var dialogue in allDialogues )
			dialogue.SelectRandomDialogue();
	}

	public void ClearTriggeredEvents()
	{
		foreach ( var @event in CurrentSaunaDay.ScriptedEvents )
		{
			@event.Triggered = false;
		}
	}

	public void ClearTasks()
	{
		foreach ( var task in ActiveTasks.ToList() )
		{
			if ( task.Completed || !task.PersistThroughSessions )
			{
				task.Reset();
				_taskMaster.CurrentTasks.Remove( task );
			}
		}
	}

	public void SaveGame()
	{
		Player.Save();

		if ( Connection.Local.IsHost )
		{
			SaveStoryProgression();
			_taskMaster.SaveTasksProgression();
			EventMaster.Instance.SaveEventsProgression();
		}
	}

	public List<string> RandomTips = new()
	{
		"Take your pants off and press P to piss.",
		"Pissing on stuff might give different results.",
		"Airsoft can't hurt large animals, but an Axe could!",
		"Only a real rifle can hurt the king of the forest.",
		"If you follow the ? on the compass you might find something...",
		"The deeper in the forest you go, the rarer the animals.",
		"The deeper you cast your fishing, the better the fish.",
		"Foxes kill hares, but they might leave spoils behind!",
		"To continue with the story you must complete all primary tasks.",
		"Talk with folks around town, some days they might give you tasks.",
		"Remember to have pants on in the city, or the cops will attack.",
		"Remember to take pants off in the city, or the hobos will attack.",
		"Some of your tasks can be completed by other players.",
		"Press ESC for useful shortcuts such as the fish collection.",
		"You can drag items on your character to equip them.",
		"You can drag items off the inventory to drop them.",
		"You can zoom by holding the Middle Mouse.",
		"You can walk by holding the ALT key.",
		"You can crouch by holding the CTRL key.",
		"Don't bother shooting humans they don't drop anything.",
		"Run.",
		"The day will automatically end in the middle of the night.",
		"Try to go back home before 3am, weird things happen.",
		"Only some items can be sold at certain shops.",
		"If you ever end up losing an item you can buy a replacement at one of the town shops.",
		"You can view the sell price of an item by mousing over it in your inventory. Some items cannot be sold!"
	};

	TimeSince _lastTipAttempt = 0f;

	protected override void OnFixedUpdate()
	{
		if ( Scene.IsEditor ) return;

		var timeManager = Scene.GetAllComponents<GameTimeManager>()?.FirstOrDefault();

		if ( timeManager == null ) return;

		if ( _lastTipAttempt >= 60f )
		{
			if ( Game.Random.Float( 100 ) <= 30f )
				NotificationManager.Popup( "Tip:", Game.Random.FromList( RandomTips ), new Color( 207f / 255f, 124f / 255f, 23f / 255f ), "ui/hud/question_mark.png", time: 7 );
			_lastTipAttempt = 0f;
		}

		if ( CurrentSaunaDay != null && !timeManager.IsDayOver )
		{
			var currentHour = timeManager.InGameHours;

			foreach ( var scriptedEvent in CurrentSaunaDay.ScriptedEvents )
			{
				if ( !scriptedEvent.Triggered )
				{
					if ( scriptedEvent.SignalToTrigger == null || scriptedEvent.SignalToTrigger == "" || scriptedEvent.SignalToTrigger == String.Empty )
					{
						if ( scriptedEvent.TriggerTime != 0 )
						{
							if ( scriptedEvent.TriggerTime <= currentHour )
							{
								BeginScriptedEvent( scriptedEvent );
							}

						}
					}
				}
			}

			var eventsToRemove = new List<TriggeringEvent>();

			foreach ( var randomEvent in EventsToTrigger )
			{
				if ( !randomEvent.Event.HasBeenPlayed )
				{
					if ( randomEvent.Time <= currentHour )
					{
						randomEvent.Event.Enable();
						eventsToRemove.Add( randomEvent );
					}
				}
			}

			foreach ( var toRemove in eventsToRemove )
			{
				if ( EventsToTrigger.Contains( toRemove ) )
					EventsToTrigger.Remove( toRemove );
			}

			if ( !CurrentSaunaDay.Completed )
				if ( CurrentSaunaDay.ScriptedEvents.All( x => x.Completed || x.Triggered && !x.CompletionNecessary ) )
				{
					CurrentSaunaDay.Completed = true;
					Log.Info( "All story scripted event requirements completed, story can now progress" );
				}
		}
	}

	internal async void BeginScriptedEvent( SaunaScriptedEvent scriptedEvent, float delay = 0f )
	{
		scriptedEvent.Triggered = true;

		await Task.DelaySeconds( delay );
		var signalToComplete = await scriptedEvent.Setup?.Invoke( Player.Local );
		scriptedEvent.SignalToComplete = signalToComplete;
	}

	[ConCmd( "sauna_save" )]
	public static void SaveGameCmd()
	{
		var storyMaster = Game.ActiveScene.GetAllComponents<StoryMaster>().FirstOrDefault();

		storyMaster?.SaveGame();
	}

	[ConCmd( "sauna_reset" )]
	public static void DeleteSave()
	{
		var storyMaster = Game.ActiveScene.GetAllComponents<StoryMaster>().FirstOrDefault();

		if ( storyMaster != null )
		{
			storyMaster.ResetStoryProgression();
			storyMaster._taskMaster.ResetTasksProgression( false );
			EventMaster.Instance.ResetEventsProgression();
			storyMaster.ResetPlayer();
		}

		if ( FileSystem.OrganizationData.FileExists( "story.json" ) )
			FileSystem.OrganizationData.DeleteFile( "story.json" );
		if ( FileSystem.OrganizationData.FileExists( "tasks.json" ) )
			FileSystem.OrganizationData.DeleteFile( "tasks.json" );
		if ( FileSystem.OrganizationData.FileExists( "events.json" ) )
			FileSystem.OrganizationData.DeleteFile( "events.json" );
		if ( FileSystem.OrganizationData.FileExists( "story.json" ) )
			FileSystem.OrganizationData.DeleteFile( "story.json" );
		if ( FileSystem.OrganizationData.FileExists( PlayerSave.FILE_PATH ) )
			FileSystem.OrganizationData.DeleteFile( PlayerSave.FILE_PATH );

		Game.Close();
	}

	[ConCmd( "sauna_reset_story" )]
	public static void DeleteStory()
	{
		var storyMaster = Game.ActiveScene.GetAllComponents<StoryMaster>().FirstOrDefault();

		if ( storyMaster != null )
			storyMaster.ResetStoryProgression();
	}

	[ConCmd( "sauna_reset_tasks" )]
	public static void DeleteTasks()
	{
		var storyMaster = Game.ActiveScene.GetAllComponents<StoryMaster>().FirstOrDefault();

		if ( storyMaster != null )
			storyMaster._taskMaster.ResetTasksProgression( true );
	}

	[ConCmd( "sauna_reset_events" )]
	public static void DeleteEvents()
	{
		var storyMaster = Game.ActiveScene.GetAllComponents<StoryMaster>().FirstOrDefault();

		if ( storyMaster != null )
			EventMaster.Instance.ResetEventsProgression();
	}

	[ConCmd( "sauna_reset_player" )]
	public static void DeletePlayer()
	{
		var storyMaster = Game.ActiveScene.GetAllComponents<StoryMaster>().FirstOrDefault();

		if ( storyMaster != null )
			storyMaster.ResetPlayer();
	}
}
