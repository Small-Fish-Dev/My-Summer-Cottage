namespace Sauna;

public class TaskMaster : Component
{
	[Property]
	public List<SaunaTask> CurrentTasks { get; set; }
}
