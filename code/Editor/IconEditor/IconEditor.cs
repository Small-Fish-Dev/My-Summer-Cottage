using Editor;
using Sandbox;

namespace Sauna;

public class IconEditor : GraphicsView
{
	private SceneObject _obj;
	private StringProperty _model;
	private AnglesProperty _angles;
	private Vector3Property _position;

	public IconEditor( Widget parent ) : base( parent )
	{
		var property = (parent as IconEditorPopup).Property;
		var icon = property.GetValue<IconSettings>();

		// Scene
		var world = new SceneWorld();

		var camera = new SceneCamera()
		{
			World = world,
			AmbientLightColor = Color.Black,
			OnRenderPostProcess = pp,
			AntiAliasing = false,
			BackgroundColor = Color.Transparent,
			Position = Vector3.Forward * 50f,
			FieldOfView = 60,
			Size = 128
		};

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
				_obj.Model = (mdl?.IsError ?? true)
					? Model.Load( "models/dev/box.vmdl" )
					: mdl;
			};
		}

		Layout.AddSpacingCell( 4 );
		{
			// Scene
			var renderer = Layout.Add( new NativeRenderingWidget( this )
			{
				Camera = camera,
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
						Rotation = _angles.Value
					} );

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

	private void pp()
	{
		// Handle shader post processing for the camera.
		var attributes = new RenderAttributes();
		Graphics.GrabFrameTexture( "FrameTexture", attributes );

		var shader = Material.FromShader( "shaders/item_icon.shader" );
		Graphics.Blit( shader, attributes );
	}

	[EditorEvent.Frame]
	private void Frame()
	{
		if ( _obj == null )
			return;

		_obj.Position = _position.Value;
		_obj.Rotation = _angles.Value;
	}
}
