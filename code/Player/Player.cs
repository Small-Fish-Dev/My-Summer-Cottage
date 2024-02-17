namespace Sauna;

public sealed class PlayerController : Component
{
    [Property] public CharacterController CharacterController { get; set; }
    [Property] public float CrouchMoveSpeed { get; set; } = 90.0f;
    [Property] public float WalkMoveSpeed { get; set; } = 190.0f;
    [Property] public float RunMoveSpeed { get; set; } = 190.0f;
    [Property] public float SprintMoveSpeed { get; set; } = 320.0f;

    [Property] public Sandbox.Citizen.CitizenAnimationHelper AnimationHelper { get; set; }

    [Sync] public bool Crouching { get; set; }
    [Sync] public Angles EyeAngles { get; set; }
    [Sync] public Vector3 WishVelocity { get; set; }

    private const float EYE_HEIGHT = 64;
    private const float DUCK_HEIGHT = 28;

    public bool _wishCrouch;
    private float _activeEyeHeight = EYE_HEIGHT;

    private RealTimeSince _timeSinceGrounded;
    private RealTimeSince _timeSinceUngrounded;
    private RealTimeSince _timeSinceLastJump;

    private float CurrentMoveSpeed
    {
        get
        {
            if (Crouching) return CrouchMoveSpeed;
            if (Input.Down("run")) return SprintMoveSpeed;
            if (Input.Down("walk")) return WalkMoveSpeed;
            return RunMoveSpeed;
        }
    }

    protected override void OnUpdate()
    {
        if (!IsProxy)
        {
            MouseInput();
            Transform.Rotation = new Angles(0, EyeAngles.yaw, 0);
        }

        UpdateAnimation();
    }

    protected override void OnFixedUpdate()
    {
        if (IsProxy)
            return;

        CrouchingInput();
        MovementInput();
    }

    private void MouseInput()
    {
        var e = EyeAngles;
        e += Input.AnalogLook;
        e.pitch = e.pitch.Clamp(-90, 90);
        e.roll = 0.0f;
        EyeAngles = e;
    }

    private float GetFriction()
    {
        if (CharacterController.IsOnGround)
            return 6.0f;

        // air friction
        return 0.2f;
    }

    private void MovementInput()
    {
        if (CharacterController is null)
            return;

        var cc = CharacterController;

        Vector3 halfGravity = Scene.PhysicsWorld.Gravity * Time.Delta * 0.5f;

        WishVelocity = Input.AnalogMove;

        if (_timeSinceGrounded < 0.2f && _timeSinceLastJump > 0.3f && Input.Pressed("jump"))
        {
            _timeSinceLastJump = 0;
            cc.Punch(Vector3.Up * 300);
        }

        if (!WishVelocity.IsNearlyZero())
        {
            WishVelocity = new Angles(0, EyeAngles.yaw, 0).ToRotation() * WishVelocity;
            WishVelocity = WishVelocity.WithZ(0);
            WishVelocity = WishVelocity.ClampLength(1);
            WishVelocity *= CurrentMoveSpeed;

            if (!cc.IsOnGround)
                WishVelocity = WishVelocity.ClampLength(50);
        }


        cc.ApplyFriction(GetFriction());

        if (cc.IsOnGround)
        {
            cc.Accelerate(WishVelocity);
            cc.Velocity = CharacterController.Velocity.WithZ(0);
        }
        else
        {
            cc.Velocity += halfGravity;
            cc.Accelerate(WishVelocity);

        }

        cc.Move();

        if (!cc.IsOnGround)
            cc.Velocity += halfGravity;
        else
            cc.Velocity = cc.Velocity.WithZ(0);

        if (cc.IsOnGround)
            _timeSinceGrounded = 0;
        else
            _timeSinceUngrounded = 0;
    }

    private bool CanUncrouch()
    {
        if (!Crouching)
            return true;

        if (_timeSinceUngrounded < 0.2f)
            return false;

        var tr = CharacterController.TraceDirection(Vector3.Up * DUCK_HEIGHT);
        return !tr.Hit; // hit nothing - we can!
    }

    public void CrouchingInput()
    {
        _wishCrouch = Input.Down("duck");

        if (_wishCrouch == Crouching)
            return;

        // crouch
        if (_wishCrouch)
        {
            CharacterController.Height = 36;
            Crouching = _wishCrouch;

            // if we're not on the ground, slide up our bbox so when we crouch
            // the bottom shrinks, instead of the top, which will mean we can reach
            // places by crouch jumping that we couldn't.
            if (!CharacterController.IsOnGround)
            {
                CharacterController.MoveTo(Transform.Position += Vector3.Up * DUCK_HEIGHT, false);
                Transform.ClearLerp();
                _activeEyeHeight -= DUCK_HEIGHT;
            }

            return;
        }

        // uncrouch
        if (!_wishCrouch)
        {
            if (!CanUncrouch())
                return;

            CharacterController.Height = 64;
            Crouching = _wishCrouch;
        }
    }

    private void UpdateCamera()
    {
        var camera = Scene.GetAllComponents<CameraComponent>().Where(x => x.IsMainCamera).FirstOrDefault();
        if (camera is null)
            return;

        var targetEyeHeight = Crouching ? 28 : 64;
        _activeEyeHeight = _activeEyeHeight.LerpTo(targetEyeHeight, RealTime.Delta * 10.0f);

        var targetCameraPos = Transform.Position + new Vector3(0, 0, _activeEyeHeight);

        // smooth view z, so when going up and down stairs or ducking, it's smooth af
        if (_timeSinceUngrounded > 0.2f)
            targetCameraPos.z = camera.Transform.Position.z.LerpTo(targetCameraPos.z, RealTime.Delta * 25.0f);

        camera.Transform.Position = targetCameraPos;
        camera.Transform.Rotation = EyeAngles;
        camera.FieldOfView = Preferences.FieldOfView;
    }

    protected override void OnPreRender()
    {
        if (IsProxy)
            return;

        UpdateCamera();
    }

    private void UpdateAnimation()
    {
        if (AnimationHelper is null)
            return;

        var wv = WishVelocity.Length;

        AnimationHelper.WithWishVelocity(WishVelocity);
        AnimationHelper.WithVelocity(CharacterController.Velocity);
        AnimationHelper.IsGrounded = CharacterController.IsOnGround;
        AnimationHelper.DuckLevel = Crouching ? 1.0f : 0.0f;

        AnimationHelper.MoveStyle = wv < 160f ? Sandbox.Citizen.CitizenAnimationHelper.MoveStyles.Walk : Sandbox.Citizen.CitizenAnimationHelper.MoveStyles.Run;

        var lookDir = EyeAngles.ToRotation().Forward * 1024;
        AnimationHelper.WithLook(lookDir, 1, 0.5f, 0.25f);
    }
}
