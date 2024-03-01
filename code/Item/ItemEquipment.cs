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
		}

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

		Transform.LocalRotation = transform.Rotation *
			(Attachment == string.Empty
				? Rotation.Identity
				: AttachmentTransform.Rotation);
	}

	#region GIZMO STUFF
	private SceneModel _model;
	private SceneObject _child;
	private SceneObject GetModel()
	{
		var world = GameManager.ActiveScene?.SceneWorld;
		if ( world == null )
			return null;

		_model ??= new SceneModel( world, "models/guy/guy.vmdl", global::Transform.Zero );
			
		if ( _child == null )
		{
			var renderer = Components.Get<ModelRenderer>( FindMode.EverythingInSelfAndDescendants );
			_child = new SceneObject( world, renderer.Model );
			_model.AddChild( "held", _child );
		}

		_model.RenderingEnabled = true;
		_child.RenderingEnabled = true;
		return _child;
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

			if ( _child != null )
				_child.RenderingEnabled = false;

			return;
		}

		var model = GetModel();
		if ( model == null )
			return;

		var attachment = _model.GetAttachment( Attachment ) ?? global::Transform.Zero;
		model.Position = attachment.Position + AttachmentTransform.Position;
		model.Rotation = attachment.Rotation * AttachmentTransform.Rotation;

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
			{
				Log.Error( pos * model.Rotation );
				AttachmentTransform = AttachmentTransform.WithPosition( AttachmentTransform.Position + pos * model.Rotation );
			}
		}
	}
	#endregion
}
