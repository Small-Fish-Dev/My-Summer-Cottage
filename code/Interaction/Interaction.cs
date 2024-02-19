namespace Sauna;

public enum InputMode
{
	[Icon( "left_click" )]
	Pressed,

	[Icon( "highlight_mouse_cursor" )]
	Released,

	[Icon( "drag_click" )]
	Down
}

public struct Interaction
{
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

	private List<Interaction> _queue = new();
	public void AddInteraction( Interaction	interaction )
	{
		_queue.Add( interaction );
	}

	protected override void OnStart()
	{
		ObjectInteractions ??= new();
		ObjectInteractions.AddRange( _queue );
	}

	protected override void DrawGizmos()
	{
		
	}
}
