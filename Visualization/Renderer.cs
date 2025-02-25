using Raylib_cs;
using System.Numerics;
using static Raylib_cs.Color;

namespace Visualization;

public class Renderer(SimulationState state)
{
    private const int CubeSideLen = 10;
    private const int DbRad = 50;

    private Camera3D _camera = new()
    {
        Position = new Vector3(0, 500, 500),
        Target = Vector3.Zero,
        Up = Vector3.UnitY,
        FovY = 45,
        Projection = CameraProjection.Perspective
    };

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
        Vector3 forward = Vector3.Normalize(_camera.Target - _camera.Position);
        Vector3 right = Vector3.Normalize(Vector3.Cross(forward, _camera.Up));

        if (Raylib.IsKeyDown(KeyboardKey.W)) cameraMovement += forward * 2.0f;
        if (Raylib.IsKeyDown(KeyboardKey.S)) cameraMovement -= forward * 2.0f;
        if (Raylib.IsKeyDown(KeyboardKey.A)) cameraMovement -= right * 2.0f;
        if (Raylib.IsKeyDown(KeyboardKey.D)) cameraMovement += right * 2.0f;

        cameraMovement *= Raylib.IsKeyDown(KeyboardKey.LeftShift) ? 5 : 1;

        _camera.Position += cameraMovement;
        _camera.Target += cameraMovement;

        // rotation
        float rotationSpeed = 0.025f;
        Vector3 direction = _camera.Target - _camera.Position;
        Vector3 directionRight = Vector3.Normalize(Vector3.Cross(direction, _camera.Up));

        if (Raylib.IsKeyDown(KeyboardKey.Right))
        {
            direction = Vector3.Transform(direction, Matrix4x4.CreateFromAxisAngle(Vector3.UnitY, -rotationSpeed));
        }

        if (Raylib.IsKeyDown(KeyboardKey.Left))
        {
            direction = Vector3.Transform(direction, Matrix4x4.CreateFromAxisAngle(Vector3.UnitY, rotationSpeed));
        }

        if (Raylib.IsKeyDown(KeyboardKey.Up))
        {
            direction = Vector3.Transform(direction, Matrix4x4.CreateFromAxisAngle(directionRight, rotationSpeed));
        }

        if (Raylib.IsKeyDown(KeyboardKey.Down))
        {
            direction = Vector3.Transform(direction, Matrix4x4.CreateFromAxisAngle(directionRight, -rotationSpeed));
        }

        _camera.Target = _camera.Position + direction;
    }

    private void DrawFrame()
    {
        Raylib.BeginDrawing();
        Raylib.ClearBackground(SkyBlue);

        // 3D rendering
        Raylib.BeginMode3D(_camera);
        Raylib.DrawPlane(Vector3.Zero, new Vector2(1000, 1000), Green);

        foreach (var entity in state.Entities)
        {
            var color = entity.Type == EntityType.Reader ? Red : Blue;
            var isActive = entity.Status is "Reading" or "Writing";

            // Highlight active entities
            var cubeColor = isActive ? Yellow : color;
            Raylib.DrawCube(entity.Position, CubeSideLen, CubeSideLen, CubeSideLen, cubeColor);
            Raylib.DrawCubeWires(entity.Position, CubeSideLen, CubeSideLen, CubeSideLen, entity.CurrentColor);
        }

        // central db
        Raylib.BeginBlendMode(BlendMode.Alpha);
        Raylib.DrawSphere(Vector3.Zero, DbRad, new Color(0, 0, 0, 100));
        Raylib.DrawSphereWires(Vector3.Zero, DbRad, 32, 32, Black);
        Raylib.EndBlendMode();

        Raylib.EndMode3D();

        foreach (var entity in state.Entities)
        {
            DrawEntityLabel(entity);
        }
        
        DrawDbLabel();
        
        DrawHud();
        
        Raylib.EndDrawing();
    }

    private void DrawDbLabel()
    {
        var screenPos = Raylib.GetWorldToScreen(new Vector3(0, DbRad + 10, 0), _camera);

        // Cut off rendering if the entity is off-screen
        var screenWidth = Raylib.GetScreenWidth();
        var screenHeight = Raylib.GetScreenHeight();

        if (screenPos.X < 0 || screenPos.X > screenWidth || screenPos.Y < 0 || screenPos.Y > screenHeight) return;

        // Draw a background rectangle for better readability
        var textSize = Raylib.MeasureText("Database", 40);
        var textX = (int)screenPos.X - textSize / 2;
        var textY = (int)screenPos.Y - 15;
        
        // Ensure the background rectangle stays within the screen bounds
        var rectX = Math.Clamp(textX - 5, 0, screenWidth - (textSize + 10));
        var rectY = Math.Clamp(textY, 0, screenHeight - 30);

        Raylib.DrawRectangle(
            rectX,
            rectY,
            textSize + 10,
            45,
            new Color(0, 0, 0, 128) // Semi-transparent black background
        );

        // Draw the status text
        Raylib.DrawText(
            "Database",
            Math.Clamp(textX, 0, screenWidth - textSize),
            Math.Clamp(textY + 5, 0, screenHeight - 20),
            40,
            White
        );
    }

    private void DrawEntityLabel(Entity entity)
    {
        // Calculate the screen position of the entity
        var screenPos = Raylib.GetWorldToScreen(
            entity.Position + new Vector3(0, 20, 0), // Offset above the entity
            _camera
        );

        // Clamp the screen position to ensure it's within the screen bounds
        var screenWidth = Raylib.GetScreenWidth();
        var screenHeight = Raylib.GetScreenHeight();

        if (screenPos.X < 0 || screenPos.X > screenWidth || screenPos.Y < 0 || screenPos.Y > screenHeight) return;

        // Draw a background rectangle for better readability
        var textSize = Raylib.MeasureText(entity.Status, 20);
        var textX = (int)screenPos.X - textSize / 2;
        var textY = (int)screenPos.Y - 15;

        // Ensure the background rectangle stays within the screen bounds
        var rectX = Math.Clamp(textX - 5, 0, screenWidth - (textSize + 10));
        var rectY = Math.Clamp(textY, 0, screenHeight - 30);

        Raylib.DrawRectangle(
            rectX,
            rectY,
            textSize + 10,
            30,
            new Color(0, 0, 0, 128) // Semi-transparent black background
        );

        // Draw the status text
        Raylib.DrawText(
            entity.Status,
            Math.Clamp(textX, 0, screenWidth - textSize),
            Math.Clamp(textY + 5, 0, screenHeight - 20),
            20,
            White
        );
    }

    private void DrawHud()
    {
        var statDict = state.Entities
            .GroupBy(e => e.Status)
            .ToDictionary(g => g.Key, g => g.Count());

        var statsText = string.Join('\n', statDict.Select(pair => $"{pair.Key}: {pair.Value}"));

        Raylib.DrawText($"Pos: {_camera.Position}", 10, 10, 20, Black);
        Raylib.DrawText(statsText, 10, 30, 20, Black);
    }
}