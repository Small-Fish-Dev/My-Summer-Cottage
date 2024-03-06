using Editor;
using Sandbox;

namespace Sauna;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;
using Editor.NodeEditor;
using Facepunch.ActionGraphs;
using Sandbox;
using Sauna.Event;
using DisplayInfo = Sandbox.DisplayInfo;

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

	IEnumerable<SignalOption> GetTriggers( JsonObject obj )
	{
		var components = obj["Components"].AsArray();

		return components
			.Where( component => component["__type"].ToString() == "EventAreaTrigger" )
			.Where( component => component["TriggerSignalIdentifier"].ToString() != String.Empty )
			.Select( component => GetSignalOption( component["__type"].ToString(), component["TriggerSignalIdentifier"].ToString() ) );
	}

	void OpenMenu()
	{
		const string mainScene = "finland";

		var test = ResourceLibrary.GetAll<SceneFile>()
			.Where( x => x.Title == mainScene )
			.SelectMany( x =>
			{
				return x.GameObjects.Where( x => x.ContainsKey( "Components" ) )
				.SelectMany( x =>
				{
					var components = x["Components"].AsArray();
					return components
						.Where( component => component["__type"].ToString() == "EventAreaTrigger" )
						.Select( y => GetSignalOption( y["TriggerSignalIdentifier"].ToString(), y["__type"].ToString(), x["Name"].ToString(), "Scene" ) );
				} );
			} );

		var eventDefinitions = PrefabLibrary.All
			.Select( x => x.Value.Prefab.RootObject )
			.SelectMany( x =>
			{
				var components = x["Components"].AsArray();
				return components
					.Where( component => component["__type"].ToString() == "EventDefinition" )
					.Select( component => GetSignalOption( component["__type"].ToString(), component["EventName"].ToString() ) );
			} );

		var allTriggers = PrefabLibrary.All
			.Select( x => x.Value.Prefab.RootObject )
			.SelectMany( x =>
			{
				var components = x["Components"].AsArray();
				return components
					.Where( component => component["__type"].ToString() == "EventAreaTrigger" )
					.Select( component => GetSignalOption( component["__type"].ToString(), "hello" ) );
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

		foreach ( var prefab in PrefabLibrary.All )
		{
			foreach ( var component in prefab.Value.GetComponents<ItemComponent>() )
			{
				Log.Info( prefab.Value.Name );
			}
		}
		var allTasks = ResourceLibrary.GetAll<SaunaTask>()
			.SelectMany( x =>
			{
				return new List<SignalOption>
				{
					GetSignalOption( $"task.start.{x.Name}", x.Name, x.TaskType.ToString(), "Tasks" ),
					GetSignalOption( $"task.success.{x.Name}", x.Name, x.TaskType.ToString(), "Tasks" ),
					GetSignalOption( $"task.fail.{x.Name}", x.Name, x.TaskType.ToString(), "Tasks" )
				};
			} );


		test = test.Concat( allTasks );
		test = test.Concat( allTriggers );
		test = test.Concat( allItems );

		_menu = new Menu();
		_menu.DeleteOnClose = true;

		_menu.AddLineEdit( "Filter",
			placeholder: "Add or Filter Signals...",
			autoFocus: true,
			onChange: s => PopulateMenu( _menu, test, s ) );

		_menu.AboutToShow += () => PopulateMenu( _menu, test );

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
