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
	[Icon( "accessible_forward" )]
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
	[ShowIf( "Player", null )]
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

	[Property]
	public List<Model> Clothes { get; set; }

	[Property]
	public HiddenBodyGroup HideBodyGroup { get; set; }


	SceneModel _model;
	Dictionary<int, SceneModel> _clothing = new();

	protected override void DrawGizmos()
	{
		if ( Game.IsEditor && !Components.TryGet<SkinnedModelRenderer>( out var _ ) )
		{
			if ( _model == null || !_model.IsValid() )
			{
				CreatePreviewModel();
			}
			else
			{
				CreatePreviewClothing();

				_model.Morphs.Set( "fat", Fatness );
				_model.Transform = Transform.World;

				_model.SetBodyGroup( "head", HideBodyGroup.HasFlag( HiddenBodyGroup.Head ) ? 1 : 0 ); // Not implemented
				_model.SetBodyGroup( "torso", HideBodyGroup.HasFlag( HiddenBodyGroup.Torso ) ? 1 : 0 );
				_model.SetBodyGroup( "hands", HideBodyGroup.HasFlag( HiddenBodyGroup.Hands ) ? 1 : 0 ); // Not implemented
				_model.SetBodyGroup( "legs", HideBodyGroup.HasFlag( HiddenBodyGroup.Legs ) ? 1 : 0 );
				_model.SetBodyGroup( "feet", HideBodyGroup.HasFlag( HiddenBodyGroup.Feet ) ? 1 : 0 ); // Not implemented

				_model.Update( Time.Delta );

				foreach ( var piece in _clothing.Values )
				{
					piece.Morphs.Set( "fat", Fatness );
					piece.Update( Time.Delta );
					piece.MergeBones( _model ); // Interestingly, you need to bonemerge after updating
				}
			}
		}
		else
		{
			DeletePreview();
		}
	}

	protected override void OnStart()
	{
		var model = Components.GetOrCreate<SkinnedModelRenderer>();
		model.Model = Model.Load( "models/guy/guy.vmdl" );
		model.SceneModel.Morphs.Set( "fat", Fatness );

		foreach ( var piece in Clothes )
		{
			var clothing = Components.Create<SkinnedModelRenderer>();
			clothing.Model = piece;
			clothing.BoneMergeTarget = model;
		}

		model.SetBodyGroup( "head", HideBodyGroup.HasFlag( HiddenBodyGroup.Head ) ? 1 : 0 ); // Not implemented
		model.SetBodyGroup( "torso", HideBodyGroup.HasFlag( HiddenBodyGroup.Torso ) ? 1 : 0 );
		model.SetBodyGroup( "hands", HideBodyGroup.HasFlag( HiddenBodyGroup.Hands ) ? 1 : 0 ); // Not implemented
		model.SetBodyGroup( "legs", HideBodyGroup.HasFlag( HiddenBodyGroup.Legs ) ? 1 : 0 );
		model.SetBodyGroup( "feet", HideBodyGroup.HasFlag( HiddenBodyGroup.Feet ) ? 1 : 0 ); // Not implemented
	}

	protected override void OnDisabled()
	{
		DeletePreview();
	}

	protected override void OnDestroy()
	{
		DeletePreview();

		if ( Components.TryGet<SkinnedModelRenderer>( out var model ) )
			model.Destroy();
	}

	void CreatePreviewModel()
	{
		_model?.Delete();
		_model = new SceneModel( Gizmo.World, "models/guy/guy.vmdl", Transform.World );
		_model.Batchable = false;
	}

	void CreatePreviewClothing()
	{
		foreach ( var model in Clothes )
		{
			if ( model == null ) continue;

			var modelKey = Clothes.IndexOf( model );

			if ( _clothing.ContainsKey( modelKey ) )
			{
				if ( _clothing[modelKey] != null && _clothing[modelKey].Model != model )
				{
					_clothing[modelKey].Delete();
					_clothing.Remove( modelKey );
				}
			}

			if ( !_clothing.ContainsKey( modelKey ) )
			{
				var piece = new SceneModel( Gizmo.World, model.Name, Transform.World );
				piece.Batchable = false;
				piece.MergeBones( _model );
				_clothing.Add( modelKey, piece );
			}
		}

		List<int> toDelete = new();

		foreach ( var piece in _clothing )
		{
			if ( piece.Key < Clothes.Count() && Clothes[piece.Key] != null )
			{
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
		_model?.Delete();

		foreach ( var piece in _clothing )
			piece.Value.Delete();
	}
}
