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
	[Property, Category( "Equipment" )] public bool UseSkinTint { get; set; }

	[Property, Category( "Holding" ), ShowIf( "Slot", EquipSlot.Hand )] public HoldType HoldType { get; set; } = HoldType.Item;
	[Property, Category( "Holding" )] public bool UpdatePosition { get; set; }
	[Property, Category( "Holding" ), ShowIf( "UpdatePosition", true )] public string Attachment { get; set; } = "hand_R";
	[Property, Category( "Holding" ), ShowIf( "UpdatePosition", true )] public Transform AttachmentTransform { get; set; } = global::Transform.Zero;

	public ModelRenderer Renderer { get; private set; }

	private ModelRenderer parcelRenderer;
	private BoxCollider parcelCollider;
	private Rigidbody parcelBody;
	private GameObject iconWorldObject;

	private readonly SoundEvent _equipSound = ResourceLibrary.Get<SoundEvent>( "sounds/misc/pickup.sound" );

	public bool IsClothing => Slot != EquipSlot.Hand;
	public bool Equipped => State == ItemState.Equipped;

	public void UpdateEquipped()
	{
		if ( Equipped )
			ToggleRenderer( Equipped );

		// Use skin color as tint.
		var player = GameObject.Parent?.Components?.Get<Player>( true );
		if ( UseSkinTint && player != null )
			Renderer.Tint = player.SkinColor;

		// Bonemerge
		if ( Renderer is SkinnedModelRenderer skinned && !UpdatePosition )
		{
			skinned.BoneMergeTarget = Equipped
				? GameObject.Parent?.Components.Get<SkinnedModelRenderer>( FindMode.EverythingInChildren )
				: null;
		}

		// Toggle colliders and rigidbodies, update parcel
		if ( !IsClothing )
		{
			var body = GameObject?.Components.GetAll<Rigidbody>( FindMode.EverythingInSelfAndChildren ).FirstOrDefault( x => x != parcelBody );
			if ( body != null ) body.Enabled = !Equipped;

			var collider = GameObject?.Components.GetAll<Collider>( FindMode.EverythingInSelfAndChildren ).FirstOrDefault( x => x != parcelCollider );
			if ( collider != null ) collider.Enabled = !Equipped;
		}
		else if ( State != ItemState.Backpack )
			UpdateParcel( State == ItemState.None );
	}

	private void ToggleRenderer( bool value )
	{
		Renderer ??= Components.GetAll<ModelRenderer>( FindMode.InSelf ).FirstOrDefault( x => x != parcelRenderer );
		Renderer.Enabled = value;
	}

	private void UpdateParcel( bool value )
	{
		ToggleRenderer( !value );

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
		iconWorldObject.Components.GetOrCreate<Sandbox.WorldPanel>();
		iconWorldObject.Components.GetOrCreate<IconWorldPanel>().Icon = IconTexture;
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
			Sound = () => _equipSound,
		} );
	}

	protected override void OnPreRender()
	{
		if ( !Equipped || !UpdatePosition || !Game.IsPlaying || GameObject == Scene )
			return;

		var player = GameObject.Parent.Components.Get<Player>( true );
		if ( player == null )
			return;

		var obj = Renderer?.SceneObject;
		if ( !obj.IsValid() )
			return;

		var transform = player.GetAttachment( Attachment, true ).ToWorld( AttachmentTransform );
		obj.Transform = transform;
		(obj as SceneModel)?.Update( RealTime.Delta );
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
