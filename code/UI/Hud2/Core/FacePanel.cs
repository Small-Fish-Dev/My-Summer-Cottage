namespace Sauna.UI.Hud2.Core;

/// <summary>
/// Panel that always faces the camera, but can be sized and positioned in 3D space
/// </summary>
public class FacePanel : PanelComponent
{
	[Property] public Vector2 Size { get; set; } = 24;

	private (Vector3 bottomLeft, Vector3 bottomRight, Vector3 topLeft, Vector3 topRight) _relativeCorners;

	private void UpdateElementRelativeCorners()
	{
		// Get size
		var sz = Size;

		// Get half of sizes
		var sw2 = sz.x / 2 * GameObject.Transform.Scale.x;
		var sh2 = sz.y / 2 * GameObject.Transform.Scale.y;

		// Get positions
		var cameraPosition = Scene.Camera.Transform.Position;
		var elementPosition = GameObject.Transform.Position;

		// Get normal
		var normal = (elementPosition - cameraPosition).Normal;

		// Prepare corners
		var vBottomLeft = normal + Vector3.Left * sw2 + Vector3.Up * sh2;
		var vBottomRight = normal + Vector3.Right * sw2 + Vector3.Up * sh2;
		var vTopLeft = normal + Vector3.Left * sw2 + Vector3.Down * sh2;
		var vTopRight = normal + Vector3.Right * sw2 + Vector3.Down * sh2;

		_relativeCorners = (vBottomLeft, vBottomRight, vTopLeft, vTopRight);
	}

	protected override void DrawGizmos()
	{
		base.DrawGizmos();

		if ( Scene?.Camera == null )
			return;

		if ( GameObject?.Transform == null )
			return;

		UpdateElementRelativeCorners();

		// Draw square
		Gizmo.Draw.Line( _relativeCorners.bottomLeft, _relativeCorners.bottomRight );
		Gizmo.Draw.Arrow( _relativeCorners.bottomLeft, _relativeCorners.topRight );
		Gizmo.Draw.Line( _relativeCorners.bottomLeft, _relativeCorners.topLeft );
		Gizmo.Draw.Line( _relativeCorners.topLeft, _relativeCorners.topRight );
		Gizmo.Draw.Line( _relativeCorners.topRight, _relativeCorners.bottomRight );
	}

	private (int w, int h) CalculateElementPanelSizeFromWorldSize()
	{
		// todo: optimize
		var topLeftScreenPos =
			Scene.Camera.PointToScreenPixels( _relativeCorners.topLeft + GameObject.Transform.Position, out _ );
		var bottomRightScreenPos =
			Scene.Camera.PointToScreenPixels( _relativeCorners.bottomRight + GameObject.Transform.Position, out _ );

		// Create initial sizes
		var sw = float.Abs( bottomRightScreenPos.x - topLeftScreenPos.x );
		var sh = float.Abs( bottomRightScreenPos.y - topLeftScreenPos.y );

		// Make them panel units
		sw *= Panel.ScaleFromScreen;
		sh *= Panel.ScaleFromScreen;

		// Floor them / make them integers
		var isw = (int)sw;
		var ish = (int)sh;

		return (isw, ish);
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();

		if ( Panel == null )
			return;

		UpdateElementRelativeCorners();

		// Make sure position: absolute
		Panel.Style.Position = PositionMode.Absolute;

		// Calculate panel size
		var proposedSize = CalculateElementPanelSizeFromWorldSize();

		// Set size
		Panel.Style.Width = Length.Pixels( proposedSize.w );
		Panel.Style.Height = Length.Pixels( proposedSize.h );

		// Start calculating panel position
		var gameObjectWorldPosition = GameObject.Transform.Position;
		var screenPosition = Scene.Camera.PointToScreenPixels( gameObjectWorldPosition, out _ );

		// Turn screen position into a panel position 
		var panelPosition = screenPosition * Panel.ScaleFromScreen;

		// Center the panel
		panelPosition.x -= proposedSize.w * 0.5f;
		panelPosition.y -= proposedSize.h * 0.5f;

		// Floor it / make it use integers
		var ipx = (int)panelPosition.x;
		var ipy = (int)panelPosition.y;

		// Set position
		Panel.Style.Left = Length.Pixels( ipx );
		Panel.Style.Top = Length.Pixels( ipy );

		// Set rotation (roll)
		var gameObjectRotationRoll = GameObject.Transform.Rotation.Angles().roll;
		if ( !gameObjectRotationRoll.AlmostEqual( 0.0f, 0.2f ) )
		{
			var tx = new PanelTransform();
			tx.AddRotation( 0, 0, gameObjectRotationRoll );
			Panel.Style.Transform = tx;
		}

		StateHasChanged();
	}
}
