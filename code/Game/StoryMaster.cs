using Editor;
using Sandbox;
using Sauna.Game;

namespace Sauna;

public struct SaunaScriptedEvent
{
	/// <summary>
	/// When the scripted event plays in in-game hours (Ex. 7.5 = 07:30, 23.25 = 23:15, 0.5 = 00:30)
	/// </summary>
	[Property]
	public RangedFloat TriggerTime { get; set; }

	public delegate void ScriptedAction( Player player );

	/// <summary>
	/// When the scripted event starts. Send sms here, calls, load events, trigger them, assign tasks, etc...
	/// </summary>
	[Property]
	public ScriptedAction OnStart { get; set; }

	public bool Completed { get; set; } = false;

	public SaunaScriptedEvent() { }
}

public class SaunaDay
{
	[Property]
	public List<SaunaScriptedEvent> ScriptedEvents { get; set; }

	public bool Completed { get; set; } = false;
}

[Icon( "auto_stories" )]
public class StoryMaster : Component
{
	/// <summary>
	/// Define each story days, if a story day hasn't been completed it will roll over to the next in-game day
	/// </summary>
	[Property]
	public Dictionary<int, SaunaDay> StoryDays { get; set; }

	/// <summary>
	/// Current story day
	/// </summary>
	[Property]
	[HostSync]
	public int CurrentDay { get; set; } = 1;

	public SaunaDay CurrentSaunaDay => StoryDays.TryGetValue( CurrentDay, out var saunaDay ) ? saunaDay : LastValidSaunaDay;
	public SaunaDay LastValidSaunaDay => StoryDays.Any() ? StoryDays.Last().Value : null;

	protected override void OnFixedUpdate()
	{
		var timeManager = Scene.GetAllComponents<GameTimeManager>().First();

		if ( timeManager == null ) return;

		if ( CurrentSaunaDay != null )
		{
			var currentHour = timeManager.InGameHours;

			var saunaEvent = CurrentSaunaDay.ScriptedEvents.First();
			saunaEvent.Completed = true;

		}
	}
}
