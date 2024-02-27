namespace Sauna;

[GameResource( "Task", "task", "A task for the player to complete", Icon = "event_busy", IconBgColor = "#4a4fa8", IconFgColor = "#ffffff" )]
public partial class SaunaTask : GameResource
{
	public struct Subtask
	{
		/// <summary>
		/// What to display for this subtask
		/// </summary>
		[Property]
		public string Description { get; set; } = "Do that thing";

		/// <summary>
		/// Which order will this subtask be bunched in, previous subtask orders need to be completed
		/// </summary>
		[Property]
		public int SubtaskOrder { get; set; } = 0;

		/// <summary>
		/// Which trigger signal is going to trigger the progress/completion of this subtask
		/// </summary>
		[Property]
		public string TriggerSignal { get; set; }

		/// <summary>
		/// How many times the trigger needs to run before the subtask is considered completed
		/// </summary>
		[Property]
		//[ShowIf( "EvaluateOnTick", null )] // TODO: Add this back when it's fixed
		[Range( 1, 100, 1 )]
		public int AmountToComplete { get; set; } = 1;

		public delegate int TaskRequirement( Player player );

		/// <summary>
		/// When the condition needs to be constantly checked instead of requiring triggers (Return the int amount used with AmountToComplete)
		/// </summary>
		[Property]
		public TaskRequirement EvaluateOnTick { get; set; }

		[Hide]
		public int CurrentAmount { get; set; } = 0;

		[Hide]
		public bool Completed { get; set; } = false;

		public Subtask() { }
	}

	/// <summary>
	/// The name of this task, keep it short 2-4 words
	/// </summary>
	[Property]
	public string Name { get; set; } = "Default Name";

	/// <summary>
	/// A rought description of what happened and what you need to do
	/// </summary>
	[Property]
	public string Description { get; set; } = "Kindly do the needful";

	/// <summary>
	/// Is this a primary task that will show up on the hud?
	/// </summary>
	[Property]
	public bool IsPrimary { get; set; } = true;

	/// <summary>
	/// Will the progress of other players in the server count towards your own progress on this task?
	/// </summary>
	[Property]
	public bool Global { get; set; } = false;

	/// <summary>
	/// Does the player need to complete this task within a time limit before it automatically fails?
	/// </summary>
	[Property]
	public bool TimeLimited { get; set; } = false;

	/// <summary>
	/// How many real-life the player has to complete the task before it's automatically failed
	/// </summary>
	[Property]
	// [HideIf( "TimeLimited", false )] TODO: Add back when it's been fixed
	[Range( 10, 300, 10 )]
	public int TimeLimitInSeconds { get; set; } = 120;

	public delegate void TaskAction( Player player );

	/// <summary>
	/// A generic action that runs when the task has been given to the player (Like spawning a key item)
	/// </summary>
	[Property]
	public TaskAction OnStart { get; set; }

	/// <summary>
	/// A generic action that runs when the task has been succesfully completed by the player (Like rewarding the player with money)
	/// </summary>
	[Property]
	public TaskAction OnComplete { get; set; }

	/// <summary>
	/// A generic action that runs when the task has been failed by the player (Like triggering a nagging wife SMS)
	/// </summary>
	[Property]
	public TaskAction OnFail { get; set; }

	[Property]
	public List<Subtask> Subtasks { get; set; } = new();
}
