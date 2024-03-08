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

public enum HoldType : byte
{
	Idle,
	Rifle,
	FishingRod,
	Item,
	Flashlight,
	Melee
}

public class ItemEquipment : ItemComponent
{
	public static Model Parcel = Model.Load( "models/props/clothing_parcel/clothing_parcel.vmdl" );

	[Property, Category( "Equipment" )] public EquipSlot Slot { get; set; } = EquipSlot.Hand;
	[Property, Category( "Equipment" )] public HiddenBodyGroup HideBodygroups { get; set; }

	[Property, Category( "Holding" ), ShowIf( "Slot", EquipSlot.Hand )] public HoldType HoldType { get; set; } = HoldType.Item;
	[Property, Category( "Holding" )] public bool UpdatePosition { get; set; }
	[Property, Category( "Holding" ), ShowIf( "UpdatePosition", true )] public string Attachment { get; set; } = "hand_R";
	[Property, Category( "Holding" ), ShowIf( "UpdatePosition", true )] public Transform AttachmentTransform { get; set; } = global::Transform.Zero;

	public ModelRenderer Renderer { get; private set; }

	private ModelRenderer parcelRenderer;
	private BoxCollider parcelCollider;
	private Rigidbody parcelBody;
	private GameObject iconWorldObject;

	private bool _equipped = false;
	private bool _inParcel = false;

	[Sync]
	public bool InParcel
	{
		get => _inParcel;
		set
		{
			_inParcel = value && GoesInParcel; // Hacky, but for now don't put any hand items in parcel.
			if ( GoesInParcel ) UpdateParcel( _inParcel );
		}
	}

	private bool GoesInParcel => Slot != EquipSlot.Hand;

	[Sync]
	public bool Equipped
	{
		get => _equipped;
		set
		{
			_equipped = value;

			// Put in parcel.
			InParcel = !value;

			// Toggle renderer
			if ( !InParcel && !InInventory ) ToggleRenderer( true );
			else ToggleRenderer( value && !InParcel );

			// Bonemerge
			if ( Renderer is SkinnedModelRenderer skinned && !UpdatePosition )
			{
				skinned.BoneMergeTarget = value
					? GameObject.Parent?.Components.Get<SkinnedModelRenderer>( FindMode.EverythingInChildren )
					: null;
			}

			// Disable rigidbody and collider.
			if ( !GoesInParcel )
			{
				var body = GameObject?.Components.GetAll<Rigidbody>( FindMode.EverythingInSelfAndChildren ).FirstOrDefault( x => x != parcelBody );
				if ( body != null ) body.Enabled = !value;

				var collider = GameObject?.Components.GetAll<Collider>( FindMode.EverythingInSelfAndChildren ).FirstOrDefault( x => x != parcelCollider );
				if ( collider != null ) collider.Enabled = !value;
			}
		}
	}

	public void ToggleRenderer( bool value )
	{
		Renderer ??= Components.GetAll<ModelRenderer>( FindMode.InSelf ).FirstOrDefault( x => x != parcelRenderer );
		Renderer.Enabled = value;
	}

	private void UpdateParcel( bool value )
	{
		// Create
		if ( value )
		{
			parcelRenderer ??= Components.Create<ModelRenderer>();
			parcelRenderer.Enabled = true;
			parcelRenderer.Model = Parcel;

			parcelCollider ??= Components.Create<BoxCollider>();
			parcelCollider.Center = Vector3.Up * 4.8f;
			parcelCollider.Scale = new Vector3( 27f, 27f, 7.5f );
			parcelCollider.Enabled = true;

			parcelBody ??= Components.Create<Rigidbody>();
			parcelBody.Enabled = true;

			CreateIconWorldPanel();
			iconWorldObject.Enabled = true;

			return;
		}

		// Remove
		if ( parcelRenderer == null || iconWorldObject == null || parcelCollider == null || parcelBody == null )
			return;

		parcelRenderer.Enabled = false;
		iconWorldObject.Enabled = false;
		parcelCollider.Enabled = false;
		parcelBody.Enabled = false;
	}

	private void CreateIconWorldPanel()
	{
		if ( iconWorldObject is not null )
			return;

		iconWorldObject = new GameObject { Parent = GameObject };
		iconWorldObject.Transform.LocalPosition = new Vector3( 0, 0, 5 );
		iconWorldObject.Transform.LocalRotation = Rotation.FromPitch( 90 );
		iconWorldObject.NetworkSpawn();
		iconWorldObject.Components.GetOrCreate<Sandbox.WorldPanel>();
		iconWorldObject.Components.GetOrCreate<IconWorldPanel>().Icon = IconTexture;
	}

	protected override void OnAwake()
	{
		base.OnAwake();
		Renderer ??= Components.GetAll<ModelRenderer>( FindMode.InSelf ).FirstOrDefault( x => x != parcelRenderer );
	}

	protected override void OnStart()
	{
		base.OnStart();

		var interactions = Components.GetOrCreate<Interactions>();
		interactions.AddInteraction( new Interaction()
		{
			Identifier = "item.equip",
			Action = ( Player interactor, GameObject obj ) => interactor.Inventory.EquipItemFromWorld( this ),
			Keybind = "use2",
			Description = "Equip",
			Disabled = () => Player.Local.Inventory.IsSlotOccupied( Slot ),
			ShowWhenDisabled = () => true,
			Accessibility = AccessibleFrom.World,
		} );
	}

	protected override void OnPreRender()
	{
		if ( !Equipped || !Game.IsPlaying || GameObject == Scene )
			return;

		var player = GameObject.Parent.Components.Get<Player>( true );
		if ( player == null )
			return;

		GameObject.Transform.World = player.GetAttachment( Attachment, true ).ToWorld( AttachmentTransform );
		GameObject.Transform.ClearLerp();
	}

	#region GIZMO STUFF
	private SceneModel _model;
	private SceneObject GetModel()
	{
		var world = Game.ActiveScene?.SceneWorld;
		if ( world == null )
			return null;

		_model ??= new SceneModel( world, "models/guy/guy.vmdl", global::Transform.Zero );
		_model.RenderingEnabled = true;
		return _model;
	}

	protected override void DrawGizmos()
	{
		var ignore = false;
		if ( !UpdatePosition || Attachment == string.Empty )
			ignore = true;

		if ( ignore || GameObject != Game.ActiveScene )
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
		if ( renderer == null || renderer.Model == null )
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
