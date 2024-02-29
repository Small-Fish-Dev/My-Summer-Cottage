using Editor;
using Sandbox;

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
}

public struct SaunaDay
{
	[Property]
	public List<SaunaScriptedEvent> ScriptedEvents { get; set; }
}

[Icon( "auto_stories" )]
public class StoryDirector : Component
{
	/// <summary>
	/// Define each story days, if a story day hasn't been completed it will roll over to the next in-game day
	/// </summary>
	[Property]
	public Dictionary<int, SaunaDay> StoryDays { get; set; }
}
