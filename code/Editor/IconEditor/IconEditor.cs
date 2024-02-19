using Editor;
using Sandbox;
using System;

namespace Sauna;

public class IconEditor : GraphicsView
{
	public const int RENDER_RESOLUTION = 128;

	private SceneObject _obj;
	private StringProperty _model;
	private AnglesProperty _angles;
	private Vector3Property _position;
	private SceneCamera _camera;
	private SceneLight _light;

	private void FitCamera()
	{
		var bounds = _obj.Model.Bounds;
		var max = bounds.Size;
		var radius = MathF.Max( max.x, MathF.Max( max.y, max.z ) );
		var dist = radius / MathF.Sin( _camera.FieldOfView.DegreeToRadian() );

		var viewDirection = Vector3.Forward;
		var pos = viewDirection * dist + bounds.Center;

		_camera.Position = pos;
		_camera.Rotation = global::Rotation.LookAt( bounds.Center - _camera.Position ).RotateAroundAxis( -viewDirection, 90 );
		_light.Position = pos + _camera.Rotation.Backward * 20f;
	}

	public IconEditor( Widget parent ) : base( parent )
	{
		// Scene
		var world = new SceneWorld();
		_camera = new SceneCamera()
		{
			World = world,
			AmbientLightColor = Color.White,
			AntiAliasing = false,
			BackgroundColor = Color.Transparent,
			FieldOfView = 40,
			ZFar = 5000,
			ZNear = 2
		};

		_light = new SceneLight( world, Vector3.Forward * 15f, 1000f, Color.White * 0.7f );
		_ = new SceneDirectionalLight( world, global::Rotation.From( 45, -45, 45 ), Color.White * 10f );

		var property = (parent as IconEditorPopup).Property;
		var icon = property.GetValue<IconSettings>();
		if ( icon.Guid == Guid.Empty ) // Generate if empty.
		{
			property.SetValue( icon = new IconSettings
			{
				Model = icon.Model,
				Rotation = global::Rotation.Identity,
				Position = Vector3.Zero,
				Guid = Guid.NewGuid()
			} );
		}

		// Layout
		Layout = Layout.Column();
		Layout.Margin = 10;
		{
			// Properties
			_angles = Layout.Add( new AnglesProperty( this )
			{
				Value = icon.Rotation.Angles()
			}, 0 );

			Layout.AddSpacingCell( 4 );

			_position = Layout.Add( new Vector3Property( this )
			{
				Value = icon.Position
			}, 0 );

			Layout.AddSpacingCell( 4 );

			_model = Layout.Add( new StringProperty( this )
			{
				Value = icon.Model
			}, 0 );
			_model.TextEdited += ( text ) =>
			{
				var mdl = Model.Load( text );
				_obj.Model = mdl?.ResourcePath == "models/dev/error.vmdl"
					? Model.Load( "models/dev/box.vmdl" )
					: mdl;
			};
		}

		Layout.AddSpacingCell( 4 );
		{
			// Scene
			var renderer = Layout.Add( new NativeRenderingWidget( this )
			{
				Camera = _camera,
				TranslucentBackground = true,
				RenderEveryFrame = true
			}, 1 );
		}
		
		Layout.AddSpacingCell( 4 );
		{
			// Save Button
			var button = Layout.Add( new global::Editor.Button( this )
			{
				Text = "Save Icon Settings",
				Clicked = () =>
				{
					property.SetValue( new IconSettings()
					{
						Model = _model.Value,
						Position = _position.Value,
						Rotation = _angles.Value,
						Guid = icon.Guid,
					} );

					var pixmap = new Pixmap( RENDER_RESOLUTION, RENDER_RESOLUTION );
					var path = $"{Project.Current.GetRootPath().Replace( '\\', '/' )}/ui/icons/{icon.Guid}.png";
					_camera.RenderToPixmap( pixmap );
					pixmap.SavePng( path );

					parent.Close();
				}
			}, 1 );
		}

		// Object
		var mdl = Model.Load( _model.Value );
		_obj = new SceneObject(
			world,
			(mdl?.IsError ?? true)
				? Model.Load( "models/dev/box.vmdl" )
				: mdl
		);
	}

	[EditorEvent.Frame]
	private void Frame()
	{
		if ( _obj == null )
			return;

		FitCamera();

		_obj.Position = _position.Value;
		_obj.Rotation = _angles.Value;
	}
}
