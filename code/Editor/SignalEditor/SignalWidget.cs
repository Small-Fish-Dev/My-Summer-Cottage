using Editor;
using Sandbox;

namespace Sauna;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
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

	private TypeOption GetTypeOption( Type type )
	{
		var typeDesc = TypeLibrary.GetType( type );

		var path = typeDesc is { }
			? GetTypePath( typeDesc )
			: $"System/{type.Name}";

		return new TypeOption( Menu.GetSplitPath( path ),
			type,
			type.Name,
			typeDesc?.Description,
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

	private record TypeOption( Menu.PathElement[] Path, Type Type, string Title, string Description, string Icon );

	private static bool SatisfiesConstraints( Type type, Type genericParam )
	{
		if ( genericParam is null )
		{
			return true;
		}

		if ( !genericParam.GenericParameterAttributes.AreSatisfiedBy( type ) )
		{
			return false;
		}

		// TODO: constraints might involve other generic parameters

		foreach ( var constraint in genericParam.GetGenericParameterConstraints() )
		{
			if ( !type.IsAssignableTo( constraint ) )
			{
				return false;
			}
		}

		foreach ( var hasImpl in genericParam.GetCustomAttributes<HasImplementationAttribute>() )
		{
			// Easy case

			if ( type.IsAssignableTo( hasImpl.BaseType ) )
			{
				continue;
			}

			var anyImplementing = TypeLibrary.GetTypes( hasImpl.BaseType )
				.Where( x => !x.IsAbstract && !x.IsInterface )
				.Any( x => x.TargetType.IsAssignableTo( type ) );

			if ( !anyImplementing )
			{
				return false;
			}
		}

		return true;
	}

	private IEnumerable<TypeOption> GetPossibleTypes()
	{
		var genericParam = typeof( Type );
		var listedTypes = new HashSet<Type>();

		foreach ( var type in SystemTypes )
		{
			if ( !listedTypes.Add( type ) ) continue;
			if ( !SatisfiesConstraints( type, genericParam ) ) continue;
			yield return GetTypeOption( type );
		}

		var componentTypes = TypeLibrary.GetTypes<Component>();
		var resourceTypes = TypeLibrary.GetTypes<GameResource>();
		var userTypes = TypeLibrary.GetTypes()
			.Where( x => x.TargetType.Assembly.GetName().Name?.StartsWith( "package." ) ?? false );

		foreach ( var typeDesc in componentTypes.Union( resourceTypes ).Union( userTypes ) )
		{
			if ( typeDesc.IsStatic ) continue;
			if ( typeDesc.IsGenericType ) continue;
			if ( typeDesc.HasAttribute<CompilerGeneratedAttribute>() ) continue;
			if ( typeDesc.Name.StartsWith( "<" ) || typeDesc.Name.StartsWith( "_" ) ) continue;
			if ( !listedTypes.Add( typeDesc.TargetType ) ) continue;
			if ( !SatisfiesConstraints( typeDesc.TargetType, genericParam ) ) continue;

			yield return GetTypeOption( typeDesc.TargetType );
		}
	}

	void OpenMenu()
	{
		var tests = PrefabLibrary.All
			.Select( x => x.Value.Prefab.ResourceName );

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

	private void PopulateSuffixMenu( Menu menu, IEnumerable<string> items, string filter = null )
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
			var filtered = items.Where( x => x.Contains( filter, StringComparison.OrdinalIgnoreCase ) ).ToArray();

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

		string getPath( string name )
		{
			return name;
		}

		menu.AddOptions( items, x => getPath( x ), x =>
		{
			SerializedProperty.SetValue( x );
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
