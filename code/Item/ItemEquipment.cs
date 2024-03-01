namespace Sauna;

public enum EquipSlot : byte
{
	Head,
	Face,
	Body,
	Legs,
	Feet,
	Hand
}

public class ItemEquipment : ItemComponent
{
	[Property, Category( "Equipment" )] public EquipSlot Slot { get; set; } = EquipSlot.Hand;
	[Property, Category( "Equipment" )] public HiddenBodyGroup HideBodygroups { get; set; }

	[Property, Category( "Attachment" )] public bool UpdatePosition { get; set; }
	[Property, Category( "Attachment" ), ShowIf( "UpdatePosition", true )] public string Attachment { get; set; } = "hand_R";
	[Property, Category( "Attachment" ), ShowIf( "UpdatePosition", true )] public Transform AttachmentTransform { get; set; }

	public ModelRenderer Renderer { get; private set; }

	private ModelRenderer parcelRenderer;
	private BoxCollider parcelCollider;
	private Rigidbody parcelBody;
	private GameObject iconWorldObject;

	private bool _equipped = false;

	[Sync]
	public bool Equipped
	{
		get => _equipped;
		set
		{
			_equipped = value;

			// Toggle renderer.
			ToggleRenderer( value );

			// Parcel
			UpdateParcel( value );

			// Bonemerge
			if ( Renderer is SkinnedModelRenderer skinned )
			{
				skinned.BoneMergeTarget = value
					? GameObject.Parent?.Components.Get<SkinnedModelRenderer>( FindMode.EverythingInChildren )
					: null;
			}
		}
	}

	public void ToggleRenderer( bool value )
	{
		Renderer ??= Components.Get<SkinnedModelRenderer>( FindMode.InSelf ) ?? Components.Get<ModelRenderer>( FindMode.InSelf );
		Renderer.Enabled = value;
	}

	public bool UpdateParcel( bool value )
	{
		if ( parcelBody == null && Components.Get<Rigidbody>( FindMode.EverythingInSelfAndDescendants ) != null )
			return false;

		if ( parcelRenderer == null )
		{
			parcelRenderer = Components.Create<ModelRenderer>();
			parcelRenderer.Model = Model.Load( "models/props/clothing_parcel/clothing_parcel.vmdl" );

			parcelCollider = Components.GetOrCreate<BoxCollider>();
			parcelCollider.Center = Vector3.Up * 4.8f;
			parcelCollider.Scale = new Vector3( 27f, 27f, 7.5f );

			parcelBody = Components.GetOrCreate<Rigidbody>();

			CreateIconWorldPanel();
		}

		iconWorldObject.Enabled = !value;
		parcelRenderer.Enabled = !value;
		parcelCollider.Enabled = !value;
		parcelBody.Enabled = !value;

		return true;
	}

	protected override void OnPreRender()
	{
		if ( !Equipped || !UpdatePosition )
			return;

		var player = GameObject.Parent.Components.Get<Player>();
		if ( player == null )
			return;

		var transform = player.GetAttachment( Attachment );
		Transform.LocalPosition = AttachmentTransform.Position +
			(Attachment == string.Empty
				? 0
				: transform.Position);

		Transform.LocalRotation = AttachmentTransform.Rotation *
			(Attachment == string.Empty
				? Rotation.Identity
				: transform.Rotation);
	}

	private void CreateIconWorldPanel()
	{
		if ( iconWorldObject is not null )
			return;

		iconWorldObject = new GameObject { Parent = GameObject };
		iconWorldObject.Transform.LocalPosition = new Vector3( 0, 0, 10 );
		iconWorldObject.Transform.LocalRotation = Rotation.FromPitch( 90 );
		iconWorldObject.NetworkSpawn();
		iconWorldObject.Components.GetOrCreate<Sandbox.WorldPanel>();
		iconWorldObject.Components.GetOrCreate<IconWorldPanel>().Icon = IconTexture;
	}

	protected override void DrawGizmos()
	{

	}
}
