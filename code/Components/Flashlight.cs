namespace Sauna;

public sealed class Flashlight : Component
{
	[Property, Sync] public bool On { get; set; } = false;

	[Property, Sync] public float Radius { get; set; } = 30f;
	[Property, Sync] public float Distance { get; set; } = 1000f;
	[Property, Sync] public float Brightness { get; set; } = 1.0f;
	[Property, Sync] public float Strength { get; set; } = 15f;
	[Property, Sync] public Color Color { get; set; } = Color.White;

	SpotLight _spotLight;
	ItemEquipment _itemEquipment;

	protected override void OnStart()
	{
		_itemEquipment = Components.Get<ItemEquipment>( FindMode.EverythingInSelfAndChildren );
		_spotLight = Components.GetOrCreate<SpotLight>( FindMode.EverythingInChildren );

		var interactions = Components.GetOrCreate<Interactions>();
		interactions.AddInteraction( new Interaction
		{
			Accessibility = AccessibleFrom.Hands,
			Action = ( Player player, GameObject obj ) =>
			{
				On = !On;
				// todo @ceitine: add click on/off sound
			},
			DynamicText = () => On ? "Turn off" : "Turn on",
			Keybind = "mouse1",
			Animation = InteractAnimations.Action
		} );
	}

	protected override void OnUpdate()
	{
		if ( _spotLight == null )
			return;

		_spotLight.Enabled = On;
		_spotLight.ConeOuter = Radius;
		_spotLight.Radius = Distance;
		_spotLight.Attenuation = Brightness;
		_spotLight.LightColor = Color * Strength;

		if ( _itemEquipment != null && _itemEquipment.Equipped )
		{
			var equipper = GameObject.Parent.Components.Get<Player>();

			if ( equipper != null )
			{
				var model = equipper.Renderer;

				if ( model != null )
				{
					var attachment = model.GetAttachment( "hand_R" );

					if ( attachment != null )
					{
						_spotLight.Transform.Position = attachment.Value.PointToWorld( new Vector3( 0f, -10f, 0f ) );
						_spotLight.Transform.Rotation = attachment.Value.Rotation.RotateAroundAxis( Vector3.Down, 90f );
					}
				}
			}
		}
		else
		{
			_spotLight.Transform.LocalPosition = new Vector3( 0f, 7.7f, 0f );
			_spotLight.Transform.LocalRotation = Rotation.FromYaw( 90f );
		}
	}
}
