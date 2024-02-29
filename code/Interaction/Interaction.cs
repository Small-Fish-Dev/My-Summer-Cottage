namespace Sauna;

public enum InputMode
{
	[Icon( "mouse" )]
	Pressed,

	[Icon( "front_hand" )]
	Released,

	[Icon( "ads_click" )]
	Down
}

public struct Interaction
{
	/// <summary>
	/// Unique identifier for this interaction
	/// </summary>
	[Property]
	public string Identifier { get; set; }

	/// <summary>
	/// Is this interaction only available when holding
	/// </summary>
	public bool HoldOnly { get; set; }

	/// <summary>
	/// The keybind used to trigger this interaction
	/// </summary>
	[Property]
	[InputAction]
	public string Keybind { get; set; }

	/// <summary>
	/// The UI description displayed when interacting
	/// </summary>
	[Property]
	public string Description { get; set; }

	/// <summary>
	/// The action that is performed when interacted with
	/// </summary>
	[Property]
	public InteractionEvent Action { get; set; }
	public delegate void InteractionEvent( Player interactor, GameObject obj );

	/// <summary>
	/// Whether or not the interaction is able to be performed
	/// </summary>
	[Property]
	public Func<bool> Disabled { get; set; }

	/// <summary>
	/// What the text should be displayed as
	/// </summary>
	[Property]
	public Func<string> DynamicText { get; set; }

	/// <summary>
	/// What context should the input be called in
	/// </summary>
	[Property]
	public InputMode InputMode { get; set; }

	/// <summary>
	/// Does this interaction use bounds?
	/// </summary>
	[Property]
	public bool HasBounds { get; set; }

	/// <summary>
	/// The position of this interaction, if it even has them
	/// </summary>
	[Property, ShowIf( "HasBounds", true )]
	public Vector3 Position { get; set; }

	/// <summary>
	/// The extents of this interaction, if it even has them
	/// </summary>
	[Property, ShowIf( "HasBounds", true )]
	public Vector3 Extents { get; set; }

	/// <summary>
	/// The text that should actually be displayed.
	/// </summary>
	[Hide, JsonIgnore]
	public string Text => DynamicText?.Invoke() ?? Description;

	/// <summary>
	/// Uses the interactions input mode and gets the key state by input action
	/// </summary>
	/// <param name="action"></param>
	/// <returns></returns>
	public bool InputFunction( string action )
	{
		switch ( InputMode )
		{
			case InputMode.Pressed:
				return Input.Pressed( action );
			case InputMode.Down:
				return Input.Down( action );
			case InputMode.Released:
				return Input.Pressed( action );
		};

		return false;
	}
}

public class Interactions : Component
{
	[Property]
	public List<Interaction> ObjectInteractions { get; set; }
	public IEnumerable<Interaction> AllInteractions => ObjectInteractions.Concat( programmedInteractions );

	private List<Interaction> programmedInteractions;

	public void AddInteraction( Interaction interaction )
	{
		programmedInteractions ??= new();
		programmedInteractions.Add( interaction );
	}

	protected override void OnStart()
	{
		ObjectInteractions ??= new();
	}

	protected override void DrawGizmos()
	{
		var interactions = ObjectInteractions;
		if ( interactions == null )
			return;

		for ( int i = 0; i < interactions.Count; i++ )
		{
			var interaction = interactions[i];
			if ( !interaction.HasBounds )
				continue;

			var bbox = new BBox( interaction.Position - interaction.Extents / 2, interaction.Position + interaction.Extents / 2 );
			Gizmo.Draw.Color = Color.Yellow;
			Gizmo.Draw.LineThickness = 0.5f;
			Gizmo.Draw.LineBBox( bbox );

			Gizmo.Draw.Color = Color.White;
			Gizmo.Draw.Text( $"{interaction.Description}", new Transform( interaction.Position ), "Consolas", 12 );

			if ( !Gizmo.HasSelected || GameObject != GameManager.ActiveScene )
				continue;

			using ( Gizmo.Scope( $"{interaction.Description}", new Transform( interaction.Position ) ) )
			{
				Gizmo.Hitbox.BBox( bbox );
				Gizmo.Hitbox.DepthBias = 0.01f;

				if ( Gizmo.IsShiftPressed )
				{
					if ( Gizmo.Control.Scale( "scale", Vector3.Zero, out var scale ) )
						ObjectInteractions[ObjectInteractions.IndexOf( interaction )] = interaction with
						{
							Extents = interaction.Extents + scale * 50
						};

					continue;
				}

				if ( Gizmo.Control.Position( "position", Vector3.Zero, out var pos ) )
					ObjectInteractions[ObjectInteractions.IndexOf( interaction )] = interaction with
					{
						Position = interaction.Position + pos
					};
			}
		}
	}
}
