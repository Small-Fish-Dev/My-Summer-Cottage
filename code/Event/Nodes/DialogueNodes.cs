namespace Sauna;

public static partial class DialogueNodes
{
	[ActionGraphNode( "event.dialoguewithreponses", DefaultOutputSignal = false )]
	[Title( "Dialogue With Responses" ), Group( "Events" ), Icon( "question_answer" )]
	public static async Task<Task> DialogueWithResponses( List<Interaction> responses, Action<Interaction> response )
	{
		return Task.CompletedTask;
	}
}
