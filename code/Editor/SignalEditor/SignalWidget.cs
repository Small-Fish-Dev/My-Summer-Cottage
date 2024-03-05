using Editor;
using Sandbox;

namespace Sauna;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;
using Editor.NodeEditor;
using Facepunch.ActionGraphs;
using Sandbox;
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
		var value = SerializedProperty.GetValue<Type>();

		var color = IsControlHovered ? Theme.Blue : Theme.ControlText;
		var rect = LocalRect;

		rect = rect.Shrink( 8, 0 );

		var desc = value is not null ? TypeLibrary.GetType( value ) : null;

		Paint.SetPen( color );
		Paint.DrawText( rect, desc?.Title ?? "None", TextFlag.LeftCenter );

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

	private static HashSet<Type> SystemTypes { get; } = new()
	{
		typeof(int),
		typeof(float),
		typeof(string),
		typeof(bool),
		typeof(GameObject),
		typeof(GameTransform),
		typeof(Color),
		typeof(Vector2),
		typeof(Vector3),
		typeof(Vector4),
		typeof(Angles),
		typeof(Rotation),
		typeof(object)
	};

	private SignalOption GetSignalOption( Type type, string title )
	{
		var typeDesc = TypeLibrary.GetType( type );

		var path = typeDesc is { }
			? GetTypePath( typeDesc )
			: $"System/{type.Name}";

		return new SignalOption( Menu.GetSplitPath( path ),
			type,
			title,
			typeDesc?.Icon );
	}

	private static string FormatAssemblyName( Assembly asm )
	{
		var name = asm.GetName().Name!;

		if ( name.StartsWith( "package.", StringComparison.OrdinalIgnoreCase ) )
		{
			name = name.Substring( "package.".Length );
		}

		if ( name.StartsWith( "local.", StringComparison.OrdinalIgnoreCase ) )
		{
			name = name.Substring( "local.".Length );
		}

		return name.ToTitleCase();
	}

	private static string GetAssemblyQualifiedPath( TypeDescription typeDesc )
	{
		var path = !string.IsNullOrEmpty( typeDesc.Namespace )
			? typeDesc.Namespace.Replace( '.', '/' )
			: FormatAssemblyName( typeDesc.TargetType.Assembly );

		return path;
	}

	private string GetTypePath( TypeDescription typeDesc )
	{
		if ( typeDesc.TargetType.DeclaringType != null )
		{
			return $"{GetTypePath( TypeLibrary.GetType( typeDesc.TargetType.DeclaringType ) )}/{typeDesc.Title}";
		}

		var prefix = typeDesc.Group ?? GetAssemblyQualifiedPath( typeDesc );
		var icon = typeDesc.Icon;

		if ( typeDesc.TargetType.IsAssignableTo( typeof( Resource ) ) )
		{
			prefix = $"Resource/{prefix}";
			icon ??= "description";
		}
		else if ( typeDesc.TargetType.IsAssignableTo( typeof( Component ) ) )
		{
			prefix = $"Component/{prefix}";
			icon ??= "category";
		}
		else if ( SystemTypes.Contains( typeDesc.TargetType ) )
		{
			prefix = typeDesc.Group ?? typeDesc.Namespace?.Replace( '.', '/' ) ?? "Sandbox";
		}

		icon ??= "check_box_outline_blank";

		return $"{prefix}/{typeDesc.Title}:{icon}@2000";
	}

	private record SignalOption( Menu.PathElement[] Path, Type Type, string Title, string Icon );

	void OpenMenu()
	{
		var tests = PrefabLibrary.All
			.Select( x => x.Value.Prefab.RootObject )
			.SelectMany( x =>
			{
				var components = x["Components"].AsArray();
				return components
					.Where( component => component["__type"].ToString() == "EventDefinition" )
					.Select( component => GetSignalOption( TypeLibrary.GetType( component["__type"].ToString() ).TargetType, component["EventName"].ToString() ) );
			} );

		_menu = new Menu();
		_menu.DeleteOnClose = true;

		_menu.AddLineEdit( "Filter",
			placeholder: "Filter Signalers...",
			autoFocus: true,
			onChange: s => PopulateSuffixMenu( _menu, tests, s ) );

		_menu.AboutToShow += () => PopulateSuffixMenu( _menu, tests );

		_menu.OpenAtCursor( true );
		_menu.MinimumWidth = ScreenRect.Width;
	}

	private void PopulateSuffixMenu( Menu menu, IEnumerable<SignalOption> items, string filter = null )
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
			var filtered = items.Where( x => x.Title.Contains( filter, StringComparison.OrdinalIgnoreCase ) ).ToArray();

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
		else
		{
			if ( items.Count() > maxFiltered + 1 )
			{
				truncated = items.Count() - maxFiltered;
				items = items.Take( maxFiltered );
			}
		}

		menu.AddOptions( items, x => $"Events/{x.Title}", x =>
		{
			SerializedProperty.SetValue( x.Title );
			SignalValuesChanged();
		}, flat: useFilter );

		if ( truncated > 0 )
		{
			menu.AddOption( $"...and {truncated} more" );
		}

		menu.AdjustSize();
		menu.Update();
	}
}
