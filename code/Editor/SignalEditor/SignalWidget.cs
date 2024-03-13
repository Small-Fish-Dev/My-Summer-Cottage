using Editor;

namespace Sauna;

using System;
using System.Collections.Generic;
using System.Linq;
using Editor.NodeEditor;
using Sandbox;
using Sauna.Event;

[CustomEditor( typeof( Signal ) )]
internal class SignalWidget : ControlWidget
{
	public override bool IsControlButton => true;
	public override bool IsControlHovered => base.IsControlHovered || _menu.IsValid();

	Menu _menu;

	public SignalWidget( SerializedProperty property ) : base( property )
	{
		Cursor = CursorShape.Finger;

		Layout = Layout.Row();
		Layout.Spacing = 2;
	}

	protected override void PaintControl()
	{
		var value = SerializedProperty.GetValue<Signal>();

		var color = IsControlHovered ? Theme.Blue : Theme.ControlText;
		var rect = LocalRect;

		rect = rect.Shrink( 8, 0 );

		Paint.SetPen( color );
		Paint.DrawText( rect, value.Identifier ?? "None", TextFlag.LeftCenter );

		Paint.SetPen( color );
		Paint.DrawIcon( rect, "Arrow_Drop_Down", 17, TextFlag.RightCenter );
	}

	protected override void OnMousePress( MouseEvent e )
	{
		base.OnMousePress( e );

		if ( e.LeftMouseButton && !_menu.IsValid() )
		{
			OpenMenu();
		}
	}

	private SignalOption GetSignalOption( string identifier, string type = "", string parent = "", string group = "" )
	{
		return new SignalOption( identifier, type, parent, group );
	}

	private record SignalOption( string Identifier, string Type = "", string Parent = "", string Group = "" );

	private Menu.PathElement[] GetPath( SignalOption signal )
	{
		var elements = new List<Menu.PathElement>();

		if ( signal.Group != "" )
			elements.Add( new Menu.PathElement( signal.Group, Order: 1, IsHeading: false ) );
		if ( signal.Parent != "" )
			elements.Add( new Menu.PathElement( signal.Parent, Order: 2, IsHeading: false ) );
		if ( signal.Type != "" )
			elements.Add( new Menu.PathElement( signal.Type, Order: 3, IsHeading: true ) );

		elements.Add( new Menu.PathElement( signal.Identifier, Order: 4, IsHeading: false ) );

		return elements.ToArray();
	}

	void OpenMenu()
	{
		const string mainScene = "finland";

		var sceneTriggers = ResourceLibrary.GetAll<SceneFile>()
			.Where( x => x.Title == mainScene )
			.SelectMany( scene =>
			{
				var allSignals = new List<SignalOption>();
				var children = scene.GameObjects;

				allSignals.AddRange( children.SelectMany( child =>
				{
					var childSignals = new List<SignalOption>();
					var components = child["Components"]?.AsArray() ?? null;

					if ( components != null )
					{
						childSignals.AddRange( components
							.Where( component =>
							{
								var type = component["__type"].ToString(); // Don't bother me about this!!

								return (type == "EventAreaTrigger" || type == "EventInteractionTrigger" || type == "EventPissTrigger" || type == "EventSellAreaTrigger");
							} )
							.Where( x => x["TriggerSignalIdentifier"] != null && x["TriggerSignalIdentifier"].ToString() != "" )
							.Select( y => GetSignalOption( y["TriggerSignalIdentifier"].ToString(), y["__type"].ToString(), child["Name"].ToString(), "Scene" ) ) );
					}

					return childSignals;
				} ) );

				return allSignals;
			} );

		var allEvents = PrefabLibrary.All
			.SelectMany( prefab =>
			{
				var allSignals = new List<SignalOption>();
				var components = prefab.Value.GetComponents<EventDefinition>();

				allSignals.AddRange( components
				.SelectMany( component =>
				{
					var finalSignals = new List<Signal>();
					var signals = component.Get<List<Signal>>( "EventSignals" );

					if ( signals != null )
						finalSignals.AddRange( signals );

					var triggers = prefab.Value.GetComponents<EventTrigger>();

					allSignals.AddRange( triggers
					.Select( trigger => GetSignalOption( trigger.Get<string>( "TriggerSignalIdentifier" ), trigger.Type.ToString(), component.Get<string>( "EventName" ), "Events" ) ) );

					var children = prefab.Value.Prefab.RootObject["Children"]?.AsArray() ?? null;

					if ( children != null )
					{
						foreach ( var child in children )
						{
							var components = child["Components"]?.AsArray() ?? null;

							if ( components != null )
							{
								allSignals.AddRange( components
									.Where( component =>
									{
										var type = component["__type"].ToString(); // Don't bother me about this!!

										return (type == "EventAreaTrigger" || type == "EventInteractionTrigger" || type == "EventPissTrigger" || type == "EventSellAreaTrigger");
									} )
									.Where( x => x["TriggerSignalIdentifier"] != null && x["TriggerSignalIdentifier"].ToString() != "" )
									.Select( y => GetSignalOption( y["TriggerSignalIdentifier"].ToString(), y["__type"].ToString(), component.Get<string>( "EventName" ), "Events" ) ) );
							}
						}
					}

					return finalSignals
					.Select( signal => GetSignalOption( $"{signal.Identifier}", component.Type.ToString(), component.Get<string>( "EventName" ), "Events" ) );

				} ) );


				return allSignals;
			} );

		var allItems = PrefabLibrary.All
			.SelectMany( prefab =>
			{
				var components = prefab.Value.GetComponents<ItemComponent>();

				return components
					.SelectMany( component =>
					{
						List<SignalOption> options = new();

						options.Add( GetSignalOption( $"item.picked.{component.Get<string>( "Name" )}", component.Type.ToString(), component.Get<string>( "Name" ), "Items" ) );
						options.Add( GetSignalOption( $"item.received.{component.Get<string>( "Name" )}", component.Type.ToString(), component.Get<string>( "Name" ), "Items" ) );
						options.Add( GetSignalOption( $"item.dropped.{component.Get<string>( "Name" )}", component.Type.ToString(), component.Get<string>( "Name" ), "Items" ) );
						options.Add( GetSignalOption( $"item.removed.{component.Get<string>( "Name" )}", component.Type.ToString(), component.Get<string>( "Name" ), "Items" ) );

						if ( component.Type.TargetType == typeof( ItemEquipment ) )
						{
							options.Add( GetSignalOption( $"item.equipped.{component.Get<string>( "Name" )}", component.Type.ToString(), component.Get<string>( "Name" ), "Items" ) );
							options.Add( GetSignalOption( $"item.unequipped.{component.Get<string>( "Name" )}", component.Type.ToString(), component.Get<string>( "Name" ), "Items" ) );
							options.Add( GetSignalOption( $"item.used_1.{component.Get<string>( "Name" )}", component.Type.ToString(), component.Get<string>( "Name" ), "Items" ) );
							options.Add( GetSignalOption( $"item.used_2.{component.Get<string>( "Name" )}", component.Type.ToString(), component.Get<string>( "Name" ), "Items" ) );
						}

						return options;
					} );
			} );


		var allPrefabDialogues = PrefabLibrary.All
			.SelectMany( prefab =>
			{
				var components = prefab.Value.GetComponents<DialogueTree>();
				Log.Info( components.Count() );
				return components
					.SelectMany( component =>
					{
						List<SignalOption> options = new();

						Log.Info( "fuck" );
						var stages = component.Get<List<DialogueStage>>( "DialogueStages" ) ?? null;

						if ( stages != null )
						{
							var stageid = 0;
							foreach ( var stage in stages )
							{
								foreach ( var response in stage.AvailableResponses )
								{
									if ( response.Identifier != null || response.Identifier != "" || response.Identifier != String.Empty )
									{
										options.Add( GetSignalOption( response.Identifier, $"Stage: {stageid}", component.Get<string>( "Name" ), "Dialogues" ) );
									}
								}
								stageid++;
							}
						}

						return options;
					} );
			} );

		var allTasks = ResourceLibrary.GetAll<SaunaTask>()
			.SelectMany( x =>
			{
				return new List<SignalOption>
				{
					GetSignalOption( $"task.start.{x.Name}", x.TaskType.ToString(), x.Name, "Tasks" ),
					GetSignalOption( $"task.success.{x.Name}", x.TaskType.ToString(), x.Name, "Tasks" ),
					GetSignalOption( $"task.fail.{x.Name}", x.TaskType.ToString(), x.Name.ToString(), "Tasks" )
				};
			} );


		var allInteractions = PrefabLibrary.All
			.SelectMany( prefab =>
			{
				var components = prefab.Value.GetComponents<Interactions>();

				return components
					.SelectMany( component =>
					{
						List<SignalOption> options = new();

						var thing = component.Object;

						var interactions = thing["ObjectInteractions"].AsArray();

						if ( interactions != null )
						{
							foreach ( var interaction in interactions )
							{
								options.Add( GetSignalOption( interaction["Identifier"].ToString(), component.Type.ToString(), prefab.Value.Name, "Interactions" ) );
							}
						}

						return options;
					} );
			} );


		sceneTriggers = sceneTriggers.Concat( allTasks );
		sceneTriggers = sceneTriggers.Concat( allItems );
		sceneTriggers = sceneTriggers.Concat( allEvents );
		sceneTriggers = sceneTriggers.Concat( allInteractions );
		sceneTriggers = sceneTriggers.Concat( allPrefabDialogues );

		_menu = new Menu();
		_menu.DeleteOnClose = true;

		_menu.AddLineEdit( "Filter",
			placeholder: "Add or Filter Signals...",
			autoFocus: true,
			onChange: s => PopulateMenu( _menu, sceneTriggers, s ) );

		_menu.AboutToShow += () => PopulateMenu( _menu, sceneTriggers );

		_menu.OpenAtCursor( true );
		_menu.MinimumWidth = ScreenRect.Width;
	}

	private void PopulateMenu( Menu menu, IEnumerable<SignalOption> items, string filter = null )
	{
		menu.RemoveMenus();
		menu.RemoveOptions();

		foreach ( var widget in menu.Widgets.Skip( 1 ) )
		{
			menu.RemoveWidget( widget );
		}

		const int maxFiltered = 10;

		var useFilter = !string.IsNullOrEmpty( filter );
		var truncated = 0;

		if ( useFilter )
		{
			var filtered = items.Where( x => x.Identifier.Contains( filter, StringComparison.OrdinalIgnoreCase ) ).ToArray();

			if ( filtered.Length > maxFiltered + 1 )
			{
				truncated = filtered.Length - maxFiltered;
				items = filtered.Take( maxFiltered );
			}
			else
			{
				items = filtered;
			}
		}

		menu.AddOptions( items, GetPath, x =>
		{
			SerializedProperty.SetValue( new Signal( x.Identifier ) );
			SignalValuesChanged();
		}, flat: useFilter );

		if ( truncated > 0 )
		{
			menu.AddOption( $"...and {truncated} more" );
		}

		if ( useFilter )
		{
			void AddCurrent()
			{
				SerializedProperty.SetValue( new Signal( filter ) );
				SignalValuesChanged();
			}
			menu.AddOption( $"New '{filter}'", "add_circle_outline", AddCurrent );
		}

		if ( SerializedProperty.GetValue<Signal>().Identifier != "" )
		{
			void Clear()
			{
				SerializedProperty.SetValue( new Signal( "" ) );
				SignalValuesChanged();
			}
			menu.AddOption( $"Clear", "clear", Clear );
		}

		menu.AdjustSize();
		menu.Update();
	}
}
