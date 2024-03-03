using System.Threading.Tasks;
using static Sauna.SaunaTask.Subtask;

namespace Sauna;

public enum TaskType
{
	[Hide]
	Any,
	[Icon( "woman" )]
	GivenByWife,
	[Icon( "feedback" )]
	GivenByNPC,
	[Icon( "event_note" )]
	GivenByEvent,
	[Icon( "menu_book" )]
	GivenByStory
}

public enum TaskRarity
{
	[Hide]
	Any,
	[Icon( "calendar_month" )]
	Common,
	[Icon( "date_range" )]
	Uncommon,
	[Icon( "today" )]
	Rare
}

[GameResource( "Task", "task", "A task for the player to complete", Icon = "event_busy", IconBgColor = "#4a4fa8", IconFgColor = "#ffffff" )]
public partial class SaunaTask : GameResource
{
	// Needs to be a class instead of struct so I can reference to it! :)
	public class Subtask
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
		[JsonIgnore]
		public int CurrentAmount { get; set; } = 0;

		[Hide]
		[JsonIgnore]
		public bool Completed { get; set; } = false;

		public Subtask() { }

		public void SetComplete( bool completed )
		{
			if ( completed != Completed )
			{
				Completed = completed;
				if ( completed )
					Log.Info( $"Completed the '{Description}' subtask ({CurrentAmount}/{AmountToComplete})" );
				else
					Log.Info( $"Uncompleted the '{Description}' subtask ({CurrentAmount}/{AmountToComplete})" );
			}
		}
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
	/// Who gave this task (Used to fetch tasks to give)
	/// </summary>
	[Property]
	public TaskType TaskType { get; set; } = TaskType.Any;

	/// <summary>
	/// How frequently this taks will be given out
	/// </summary>
	[Property]
	public TaskRarity TaskRarity { get; set; } = TaskRarity.Any;

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
	/// How many real-life seconds the player has to complete the task before it's automatically failed
	/// </summary>
	[Property]
	// [HideIf( "TimeLimited", false )] TODO: Add back when it's been fixed
	[Range( 10, 600, 10 )]
	public int TimeLimitInSeconds { get; set; } = 120;

	public delegate void TaskAction( Player player );

	/// <summary>
	/// A generic action that runs when the task has been given to the player (Like spawning a key item)
	/// </summary>
	[Property]
	public TaskAction OnStart { get; set; }

	/// <summary>
	/// A generic action that runs when the task has been succesfully succeeded by the player (Like rewarding the player with money)
	/// </summary>
	[Property]
	public TaskAction OnSuccess { get; set; }

	/// <summary>
	/// A generic action that runs when the task has been failed by the player (Like triggering a nagging wife SMS)
	/// </summary>
	[Property]
	public TaskAction OnFail { get; set; }

	public delegate bool TaskCondition( Player player );

	/// <summary>
	/// Check every tick for a fail condition, if it returns true then fail the task
	/// </summary>
	[Property]
	public TaskCondition FailConditionCheck { get; set; }

	[Property]
	public List<Subtask> Subtasks { get; set; } = new();

	[Hide]
	public int CurrentSubtaskOrder { get; set; } = 0;

	[Hide]
	public bool Successful { get; set; } = false;

	[Hide]
	public bool Started { get; set; } = false;

	[Hide]
	public bool Completed { get; set; } = false;

	[Hide]
	public TimeUntil TaskTimer { get; set; } = 0;

	/// <summary>
	/// The task has started
	/// </summary>
	public void Start()
	{
		Started = true;
		OnStart?.Invoke( Player.Local );

		if ( TimeLimited )
			TaskTimer = TimeLimitInSeconds;

		Log.Info( $"Task '{Name}' has started: {Description}" );

		foreach ( var subtask in Subtasks )
			Log.Info( $"New subtask: '{subtask.Description}' ({subtask.CurrentAmount}/{subtask.AmountToComplete})" );
	}

	/// <summary>
	/// Task completed succesfully
	/// </summary>
	public void Succeed()
	{
		Completed = true;
		Successful = true;

		Log.Info( $"Succesfully completed the '{Name}' task" );

		OnSuccess?.Invoke( Player.Local );
	}

	/// <summary>
	/// Task not completed
	/// </summary>
	public void Fail()
	{
		Completed = true;
		Successful = false;

		Log.Info( $"Failed the '{Name}' task" );

		OnFail?.Invoke( Player.Local );
	}

	/// <summary>
	/// Reset all progress on the task
	/// </summary>
	public void Reset()
	{
		Completed = false;
		Successful = false;
		Started = false;
		CurrentSubtaskOrder = 0;
		TaskTimer = TimeLimitInSeconds;

		foreach ( var subtask in Subtasks )
		{
			subtask.CurrentAmount = 0;
			subtask.Completed = false;
		}
	}

	/// <summary>
	/// Find a random task with the given parameters
	/// </summary>
	/// <param name="taskType"></param>
	/// <param name="taskRarity"></param>
	/// <returns></returns>
	public static SaunaTask GetRandomTask( TaskType taskType = TaskType.Any, TaskRarity taskRarity = TaskRarity.Any )
	{
		var allTasks = ResourceLibrary.GetAll<SaunaTask>();

		if ( taskType != TaskType.Any )
			allTasks = allTasks.Where( x => x.TaskType == taskType );

		if ( taskRarity != TaskRarity.Any )
			allTasks = allTasks.Where( x => x.TaskRarity == taskRarity );

		if ( allTasks.Any() )
		{
			var rnd = new Random();

			return rnd.FromList( allTasks.ToList() );
		}

		return null;
	}
}
