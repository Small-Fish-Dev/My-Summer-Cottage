namespace Sauna;

public class DialogueStage
{
	[Property]
	public List<Interaction> AvailableResponses { get; set; }
}

public class Dialogue : Component
{
	/// <summary>
	/// Only the host can perform this dialogue.
	/// </summary>
	[Property]
	public bool HostOnly { get; set; }

	/// <summary>
	/// Each dialogue stage within the list are the possible interactions that can be performed.
	/// Ex. The first element in the list has the options "Accept" and "Decline"
	/// You can use SendToDialogueStage(...) and provide the index of the dialogue stage you'd like to move to.
	/// </summary>
	[Property]
	public List<DialogueStage> DialogueStages { get; set; }

	public int CurrentStage { get; set; } = 0;

	protected override void OnStart()
	{
		if ( !Network.Active )
			GameObject.NetworkSpawn();

		Network.SetOwnerTransfer( OwnerTransfer.Takeover );

		if ( HostOnly && !Player.Local.Connection.IsHost )
			EndDialogue();

		var interactions = Components.GetOrCreate<Interactions>();
		for ( int i = 0; i < DialogueStages.Count; ++i )
		{
			var stageIndex = i;
			for ( int j = 0; j < DialogueStages[i].AvailableResponses.Count; ++j )
			{
				DialogueStages[i].AvailableResponses[j].ShowWhenDisabled = false;
				DialogueStages[i].AvailableResponses[j].Disabled = () => stageIndex != CurrentStage;
			}
		}

		foreach ( var dialogueStage in DialogueStages )
			interactions.AddInteractions( dialogueStage.AvailableResponses );
	}

	public void SendToDialogueStage( int index )
	{
		CurrentStage = index;
	}

	public void EndDialogue()
	{
		CurrentStage = -1;
	}
}
