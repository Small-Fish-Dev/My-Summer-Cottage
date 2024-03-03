using Sauna.Util.Extensions;

namespace Sauna;

public class DialogueResponse
{
	/// <summary>
	/// Unique identifier for this response
	/// </summary>
	[Property]
	public string Identifier { get; set; }

	/// <summary>
	/// The keybind used to trigger this response.
	/// </summary>
	[Property]
	[InputAction]
	public string Keybind { get; set; }

	/// <summary>
	/// The UI description displayed for this response.
	/// </summary>
	[Property]
	public string Description { get; set; }

	/// <summary>
	/// The action that is performed when this response is selected.
	/// </summary>
	[Property]
	public Interaction.InteractionEvent Action { get; set; }

	/// <summary>
	/// The color of the text of the response.
	/// </summary>
	[Property]
	public Func<Color> DynamicColor { get; set; }

	/// <summary>
	/// If the response is disabled.
	/// </summary>
	[Property]
	public Func<bool> Disabled { get; set; }
}

public class DialogueStage
{
	[Property]
	public List<DialogueResponse> AvailableResponses { get; set; }
}

public class DialogueComponent : Component
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
				var response = DialogueStages[i].AvailableResponses[j];
				interactions.AddInteraction
				(
					new Interaction()
					{
						Identifier = response.Identifier,
						Keybind = response.Keybind,
						Description = response.Description,
						Action = response.Action,
						Disabled = () => !IsActiveStage( stageIndex ) || (response.Disabled is not null && response.Disabled()),
						ShowWhenDisabled = () => IsActiveStage( stageIndex ),
						DisableUseAnimation = true,
						DynamicColor = () => response.DynamicColor?.Invoke() ?? Color.White
					}
				);
			}
		}
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

	private bool IsActiveStage( int i )
	{
		return GetStageIndex() == i;
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
