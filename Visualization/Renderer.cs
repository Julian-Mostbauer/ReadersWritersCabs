using Raylib_cs;
using System.Numerics;


namespace Visualization;

public class Renderer
{
    private readonly SimulationState _state;

    private Camera3D _mainCamera = new()
    {
        Position = new Vector3(0, 500, 500),
        Target = Vector3.Zero,
        Up = Vector3.UnitY,
        FovY = 45,
        Projection = CameraProjection.Perspective
    };

    public Renderer(SimulationState state) => _state = state;

    public void Initialize()
    {
        Raylib.InitWindow(1600, 1200, "Readers-Writers Visualization");
        Raylib.SetTargetFPS(60);
    }

    public void Update()
    {
        HandleInput();
        DrawFrame();
    }

    private void HandleInput()

    {
        // movement
        Vector3 cameraMovement = Vector3.Zero;
        if (Raylib.IsKeyDown(KeyboardKey.W)) cameraMovement.Z -= 10.0f;
        if (Raylib.IsKeyDown(KeyboardKey.S)) cameraMovement.Z += 10.0f;
        if (Raylib.IsKeyDown(KeyboardKey.A)) cameraMovement.X -= 10.0f;
        if (Raylib.IsKeyDown(KeyboardKey.D)) cameraMovement.X += 10.0f;
        if (Raylib.IsKeyDown(KeyboardKey.Up)) cameraMovement.Y += 10.0f;
        if (Raylib.IsKeyDown(KeyboardKey.Down)) cameraMovement.Y -= 10.0f;

        _mainCamera.Position += cameraMovement;
        _mainCamera.Target += cameraMovement;

        // rotation
        float rotationSpeed = 0.05f;
        if (Raylib.IsKeyDown(KeyboardKey.Right))
        {
            var angle = rotationSpeed;
            var direction = _mainCamera.Target - _mainCamera.Position;
            var cosAngle = MathF.Cos(angle);
            var sinAngle = MathF.Sin(angle);
            var newX = direction.X * cosAngle - direction.Z * sinAngle;
            var newZ = direction.X * sinAngle + direction.Z * cosAngle;
            _mainCamera.Target = new Vector3(_mainCamera.Position.X + newX, _mainCamera.Target.Y,
                _mainCamera.Position.Z + newZ);
        }

        if (Raylib.IsKeyDown(KeyboardKey.Left))
        {
            var angle = -rotationSpeed;
            var direction = _mainCamera.Target - _mainCamera.Position;
            var cosAngle = MathF.Cos(angle);
            var sinAngle = MathF.Sin(angle);
            var newX = direction.X * cosAngle - direction.Z * sinAngle;
            var newZ = direction.X * sinAngle + direction.Z * cosAngle;
            _mainCamera.Target = new Vector3(_mainCamera.Position.X + newX, _mainCamera.Target.Y,
                _mainCamera.Position.Z + newZ);
        }
    }

    private void DrawFrame()
    {
        Raylib.BeginDrawing();
        Raylib.ClearBackground(Color.SkyBlue);

        Raylib.BeginMode3D(_mainCamera);

        Raylib.DrawPlane(Vector3.Zero, new Vector2(1000, 1000), Color.Green);
        Raylib.DrawSphere(Vector3.Zero, 30, Color.DarkGray);

        foreach (var entity in _state.Entities)
        {
            Raylib.DrawCube(entity.Position, 10, 10, 10, entity.CurrentColor);
            DrawEntityLabel(entity);
        }

        Raylib.EndMode3D();
        DrawHud();
        Raylib.EndDrawing();
    }

    private void DrawEntityLabel(Entity entity)
    {
        var screenPos = Raylib.GetWorldToScreen(entity.Position, _mainCamera);
        Raylib.DrawText(entity.Status, (int)screenPos.X, (int)screenPos.Y, 20, Color.Black);
    }

    private void DrawHud()
    {
        Raylib.DrawText($"Readers: {_state.Entities.Count(e => e.Type == EntityType.Reader)}", 10, 10, 20, Color.Black);
        Raylib.DrawText($"Writers: {_state.Entities.Count(e => e.Type == EntityType.Writer)}", 10, 40, 20, Color.Black);
    }
}