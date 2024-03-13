﻿namespace Sauna;

public sealed class MarkerComponent : Component
{
	[Sync, Property] public string Title { get; set; }
	[Sync, Property] public MarkerIcon Icon { get; set; }
	[Sync, Property] public bool Networked { get; set; } = true;
	public Marker Marker { get; set; }

	protected override void OnUpdate()
	{
		if ( IsProxy && !Networked )
		{
			Marker?.Delete();
			return;
		}

		Marker ??= Marker.Create( Title, Transform.Position, Icon );
		Marker.Position = Transform.Position;
	}

	protected override void OnDestroy()
	{
		Marker?.Delete();
	}
}
