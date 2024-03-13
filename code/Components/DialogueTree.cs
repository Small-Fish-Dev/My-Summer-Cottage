using System;

namespace Sauna;

public class DialogueResponse
{
	/// <summary>
	/// Unique identifier for this response
	/// </summary>
	[Property]
	public Signal Identifier { get; set; }

	/// <summary>
	/// The keybind used to trigger this response.
	/// </summary>
	[Property]
	[InputAction]
	public string Keybind { get; set; } = InputAction.Use;

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
	/// The max distance at which you can interact with the dialogue.
	/// </summary>
	[Property]
	public float InteractDistance { get; set; } = 125f;

	/// <summary>
	/// The color of the text of the response.
	/// </summary>
	[Property]
	public Func<Color> DynamicColor { get; set; } = () => Color.White;

	/// <summary>
	/// If the response is disabled.
	/// </summary>
	[Property]
	public Func<bool> Disabled { get; set; } = () => false;
}

public class DialogueStage
{
	/// <summary>
	/// Is this stage the first of a conversation. Used to pick random dialogue options.
	/// </summary>
	[Property]
	public bool IsInitial { get; set; } = false;

	/// <summary>
	/// Make this dialogue randomly pickable at the start of the day
	/// </summary>
	[Property]
	[HideIf( "IsInitial", false )]
	public bool AddToDialoguePool { get; set; } = false;

	/// <summary>
	/// How often will this dialogue be picked (10f = 10x times more often than one with 1f)
	/// </summary>
	[Property]
	[HideIf( "IsInitial", false )]
	public float DialoguePoolWeight { get; set; } = 1f;

	[Property]
	public List<DialogueResponse> AvailableResponses { get; set; }
}

public class DialogueTree : Component
{
	[Property]
	public string Name { get; set; } = "Speaker";

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
		GameObject.SetupNetworking();

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
						Disabled = () => IsDisabled( response, stageIndex ),
						ShowWhenDisabled = () => IsActiveStage( stageIndex ),
						Animation = InteractAnimations.None,
						DynamicColor = () => response.DynamicColor?.Invoke() ?? Color.White,
						InteractDistance = response.InteractDistance,
					}
				);
			}
		}

		SelectRandomDialogue();
	}

	public void SelectRandomDialogue()
	{
		var options = DialogueStages.Where( x => x.IsInitial && x.AddToDialoguePool );

		var totalWeight = options?
			.Select( x => x.DialoguePoolWeight )?
			.Sum() ?? 0f;

		var rng = Game.Random.Float( totalWeight );
		DialogueStage picked = null;

		foreach ( var dialogue in options )
		{
			if ( rng < dialogue.DialoguePoolWeight )
			{
				picked = dialogue;
				break;
			}
			else
				rng -= dialogue.DialoguePoolWeight;
		}

		if ( picked != null )
		{
			var stage = DialogueStages.IndexOf( picked );
			SendToDialogueStage( stage );
		}
	}

	public void SendToDialogueStage( int index )
	{
		SetStageIndex( index );
	}

	public void ResetDialogue()
	{
		SetStageIndex( 0 );
	}

	public void EndDialogue()
	{
		SetStageIndex( -1 );
	}

	private bool IsActiveStage( int stageIndex )
	{
		return GetStageIndex() == stageIndex;
	}

	private void SetStageIndex( int stageIndex )
	{
		if ( Networked )
			NetworkedStageIndex = stageIndex;
		else
			LocalStageIndex = stageIndex;
	}

	private int GetStageIndex()
	{
		return Networked ? NetworkedStageIndex : LocalStageIndex;
	}

	private bool IsDisabled( DialogueResponse response, int stageIndex )
	{
		if ( !IsActiveStage( stageIndex ) )
			return true;

		if ( response.Disabled is not null && response.Disabled() )
			return true;

		if ( HostOnly && !Player.Local.Connection.IsHost )
			return true;

		return false;
	}
}
