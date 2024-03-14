using Sandbox;
using Editor;
using Sauna;

[Flags]
public enum HiddenBodyGroup
{
	None = 0,
	[Icon( "face" )]
	Head = 1,
	[Icon( "person_outline" )]
	Torso = 2,
	[Icon( "sign_language" )]
	Hands = 4,
	[Icon( "airline_seat_legroom_reduced" )]
	Legs = 8,
	[Icon( "do_not_step" )]
	Feet = 16
}

[Icon( "theater_comedy" )]
public class Character : Component, Component.ExecuteInEditor
{
	[Property]
	public Player Player { get; set; }

	float _fatness = 0f;

	[Property, Sync, MakeDirty]
	[Range( 0f, 1f, 0.05f )]
	public float Fatness // If we have a player assigned, use the player's fatness, else show the slider
	{
		get
		{
			if ( Player != null )
				return Player.Fatness;
			else
				return _fatness;
		}
		set => _fatness = value; // Who cares if it's set even with a player reference
	}

	float _height = 0f;

	[Property, Sync, MakeDirty]
	[Range( -100f, 100f, 1f )]
	public float Height
	{
		get
		{
			if ( Player != null )
				return Player.Height;
			else
				return _height;
		}
		set => _height = value;
	}

	[Property]
	public List<Model> Clothes { get; set; }

	[Property]
	public HiddenBodyGroup HideBodyGroup { get; set; }

	Dictionary<int, SceneModel> _clothing = new();

	protected override void DrawGizmos()
	{
		if ( !Game.IsPlaying )
		{
			var parent = Components.Get<SkinnedModelRenderer>().SceneModel;

			if ( !parent.IsValid() )
				return;

			CreatePreviewClothing( parent );

			parent.Morphs.Set( "fat", Fatness );
			parent.SetAnimParameter( "height", Height );

			parent.Transform = Transform.World;

			parent.SetBodyGroup( "head", HideBodyGroup.HasFlag( HiddenBodyGroup.Head ) ? 1 : 0 ); // Not implemented
			parent.SetBodyGroup( "torso", HideBodyGroup.HasFlag( HiddenBodyGroup.Torso ) ? 1 : 0 );
			parent.SetBodyGroup( "hands", HideBodyGroup.HasFlag( HiddenBodyGroup.Hands ) ? 1 : 0 ); // Not implemented
			parent.SetBodyGroup( "legs", HideBodyGroup.HasFlag( HiddenBodyGroup.Legs ) ? 1 : 0 );
			parent.SetBodyGroup( "feet", HideBodyGroup.HasFlag( HiddenBodyGroup.Feet ) ? 1 : 0 ); // Not implemented

			foreach ( var piece in _clothing.Values )
			{
				if ( !piece.IsValid() ) continue;
				piece.SetAnimParameter( "height", Height ); // Bone mergine doesn't work? Tried it even with a normal skinned model renderer
				piece.Update( Time.Delta );
			}

			parent.Update( Time.Delta );
		}
		else
		{
			DeletePreview();
		}
	}

	protected override void OnUpdate()
	{
		var parent = Components.Get<SkinnedModelRenderer>();

		if ( !parent.IsValid() )
			return;

		var value = MathF.Sin( Time.Now * 5f ) / 2f + 0.5f;
		var r = 45f + 219f * value;
		var g = 34f + 172f * value;
		var b = 30f + 150f * value;

		//Log.Info( $"Red: {r} Green: {g} Blue: {b}" );

		//parent.SceneModel.Attributes.Set( "g_flColorTint", new Vector3( r, g, b ) );
	}

	protected override void OnStart()
	{
		if ( Game.IsPlaying )
		{
			var model = Components.GetOrCreate<SkinnedModelRenderer>();
			model.Model = Model.Load( "models/guy/guy.vmdl" );
			model.SceneModel.Morphs.Set( "fat", Fatness );

			foreach ( var piece in Clothes )
			{
				var clothingGO = new GameObject( true, piece.Name );
				clothingGO.SetParent( GameObject );
				clothingGO.Transform.Local = new Transform( Vector3.Zero, Rotation.Identity );

				var clothing = clothingGO.Components.Create<SkinnedModelRenderer>();
				clothing.Model = piece;
				clothing.BoneMergeTarget = model;
			}

			model.SetBodyGroup( "head", HideBodyGroup.HasFlag( HiddenBodyGroup.Head ) ? 1 : 0 ); // Not implemented
			model.SetBodyGroup( "torso", HideBodyGroup.HasFlag( HiddenBodyGroup.Torso ) ? 1 : 0 );
			model.SetBodyGroup( "hands", HideBodyGroup.HasFlag( HiddenBodyGroup.Hands ) ? 1 : 0 ); // Not implemented
			model.SetBodyGroup( "legs", HideBodyGroup.HasFlag( HiddenBodyGroup.Legs ) ? 1 : 0 );
			model.SetBodyGroup( "feet", HideBodyGroup.HasFlag( HiddenBodyGroup.Feet ) ? 1 : 0 ); // Not implemented
		}
	}

	protected override void OnDisabled()
	{
		DeletePreview();
	}

	protected override void OnDestroy()
	{
		DeletePreview();

		foreach ( var model in Components.GetAll<SkinnedModelRenderer>() )
			model.Destroy();
	}

	void CreatePreviewClothing( SceneModel parent )
	{
		foreach ( var model in Clothes )
		{
			if ( model == null ) continue;

			var modelKey = Clothes.IndexOf( model );

			if ( _clothing.ContainsKey( modelKey ) )
			{
				if ( _clothing[modelKey].IsValid() && _clothing[modelKey].Model != model )
				{
					_clothing[modelKey].Delete();
					_clothing.Remove( modelKey );
				}
			}

			if ( !_clothing.ContainsKey( modelKey ) )
			{
				var piece = new SceneModel( Scene.SceneWorld, model.Name, Transform.World );
				piece.Batchable = false;
				parent.AddChild( "clothing", piece );
				_clothing.Add( modelKey, piece );
			}
		}

		List<int> toDelete = new();

		foreach ( var piece in _clothing )
		{
			if ( piece.Key < Clothes.Count() && Clothes[piece.Key] != null )
			{
				if ( !piece.Value.IsValid() ) continue;

				if ( Clothes[piece.Key] != piece.Value.Model )
				{
					toDelete.Add( piece.Key );
				}
			}
			else
			{
				toDelete.Add( piece.Key );
			}
		}

		foreach ( var deadManWalking in toDelete )
		{
			_clothing[deadManWalking].Delete();
			_clothing.Remove( deadManWalking );
		}
	}

	void DeletePreview()
	{
		foreach ( var piece in _clothing.Values )
			piece.Delete();
	}
}
