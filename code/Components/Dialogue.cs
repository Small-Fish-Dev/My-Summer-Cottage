using Sauna.Util.Extensions;

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
	[Property, HideIf( "Networked", true )]
	public bool HostOnly { get; set; }

	/// <summary>
	/// Networks the dialogue stage, however, actions will not be replicated on client.
	/// Instead, you'd ideally be broadcasting actions to everyone, for instance NetworkedDialogue(...)
	/// </summary>
	[Property, HideIf( "HostOnly", true )]
	public bool Networked { get; set; }

	/// <summary>
	/// Each dialogue stage within the list are the possible interactions that can be performed.
	/// Ex. The first element in the list has the options "Accept" and "Decline"
	/// You can use SendToDialogueStage(...) and provide the index of the dialogue stage you'd like to move to.
	/// </summary>
	[Property]
	public List<DialogueStage> DialogueStages { get; set; }

	[Sync]
	private int NetworkedStageIndex { get; set; } = 0;
	private int LocalStageIndex { get; set; } = 0;

	protected override void OnStart()
	{
		if ( !Network.Active )
			GameObject.NetworkSpawn();

		Network.SetOwnerTransfer( OwnerTransfer.Takeover );
		Network.SetOrphanedMode( NetworkOrphaned.ClearOwner );

		if ( HostOnly && !Player.Local.Connection.IsHost )
			EndDialogue();

		var interactions = Components.GetOrCreate<Interactions>();
		for ( int i = 0; i < DialogueStages.Count; ++i )
		{
			var stageIndex = i;
			for ( int j = 0; j < DialogueStages[i].AvailableResponses.Count; ++j )
			{
				DialogueStages[i].AvailableResponses[j].ShowWhenDisabled = false;
				DialogueStages[i].AvailableResponses[j].Disabled = () => stageIndex != GetStageIndex();
			}
		}

		foreach ( var dialogueStage in DialogueStages )
			interactions.AddInteractions( dialogueStage.AvailableResponses );
	}

	[Broadcast]
	public void NetworkedDialogue( int resourceId )
	{
		var resource = ResourceLibrary.Get<SoundWithSubtitlesResource>( resourceId );
		if ( resource is not null )
			Scene.SoundSystem().Play( resource, GameObject );
	}

	public void SendToDialogueStage( int index )
	{
		SetStageIndex( index );
	}

	public void EndDialogue()
	{
		SetStageIndex( -1 );
	}

	private void SetStageIndex( int i )
	{
		if ( Networked )
			NetworkedStageIndex = i;
		else
			LocalStageIndex = i;
	}

	private int GetStageIndex()
	{
		return Networked ? NetworkedStageIndex : LocalStageIndex;
	}
}
