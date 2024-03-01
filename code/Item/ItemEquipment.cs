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

			// Disable rigidbody and collider.
			var body = GameObject?.Components.Get<Rigidbody>( FindMode.EverythingInSelfAndChildren );
			if ( body != null && body != parcelBody ) body.Enabled = !value;

			var collider = GameObject?.Components.Get<Collider>( FindMode.EverythingInSelfAndChildren );
			if ( collider != null && collider != parcelCollider ) collider.Enabled = !value;
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

		var player = GameObject.Parent.Components.Get<Player>( true );
		if ( player == null )
			return;

		Renderer ??= Components.Get<SkinnedModelRenderer>( FindMode.InSelf ) ?? Components.Get<ModelRenderer>( FindMode.InSelf );
		var obj = Renderer.SceneObject;
		if ( obj.IsValid() )
			obj.Transform = player.GetAttachment( Attachment, true ).ToWorld( AttachmentTransform );
	}

	#region GIZMO STUFF
	private SceneModel _model;
	private SceneObject GetModel()
	{
		var world = GameManager.ActiveScene?.SceneWorld;
		if ( world == null )
			return null;

		_model ??= new SceneModel( world, "models/guy/guy.vmdl", global::Transform.Zero );
		_model.RenderingEnabled = true;
		return _model;
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
		var ignore = false;
		if ( !UpdatePosition || Attachment == string.Empty )
			ignore = true;

		if ( ignore || GameObject != GameManager.ActiveScene )
			ignore = true;

		if ( ignore || !Gizmo.HasSelected )
		{
			if ( _model != null )
				_model.RenderingEnabled = false;

			return;
		}

		var model = GetModel();
		if ( model == null )
			return;

		var renderer = Components.Get<ModelRenderer>( FindMode.EverythingInSelfAndDescendants );
		if ( renderer == null )
			return;

		var attachment = _model.GetAttachment( Attachment ) ?? global::Transform.Zero;
		Gizmo.Draw.Model( renderer.Model, model.Transform );

		Gizmo.Draw.IgnoreDepth = true;
		Gizmo.Draw.SolidSphere( attachment.Position, 0.1f );
		Gizmo.Draw.IgnoreDepth = false;

		model.Transform = attachment.ToWorld( AttachmentTransform );

		using ( Gizmo.Scope( $"{Name}", new Transform( model.Position, model.Rotation ) ) )
		{
			Gizmo.Hitbox.DepthBias = 0.01f;

			if ( Gizmo.IsShiftPressed )
			{
				if ( Gizmo.Control.Rotate( "rotate", out var rotate ) )
					AttachmentTransform = AttachmentTransform.WithRotation( AttachmentTransform.Rotation * rotate.ToRotation() );

				return;
			}

			if ( Gizmo.Control.Position( "position", Vector3.Zero, out var pos ) )
				AttachmentTransform = AttachmentTransform.WithPosition( AttachmentTransform.Position + pos * AttachmentTransform.Rotation );
		}
	}
	#endregion
}
