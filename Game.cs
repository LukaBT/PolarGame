using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

public class Game : GameWindow
{
    private World world = null!;
    private Camera camera = null!;
    private Renderer renderer = null!;
    private ChunkLoader loader = null!;
    private BlockPicker picker = null!;
    private BlockEditor editor = null!;
    private DebugOverlay debug = null!;

    public Game()
        : base(GameWindowSettings.Default, new NativeWindowSettings
        {
            ClientSize = new Vector2i(1280, 720),
            Title = "Planet",
            APIVersion = new Version(3, 3),
            Profile = ContextProfile.Core
        })
    {
    }

    protected override void OnLoad()
    {
        base.OnLoad();

        GL.Viewport(0, 0, FramebufferSize.X, FramebufferSize.Y);

        // type, radius, coreRadius, startingColumnChunks, chunkSize, bandCount
        world = new World("Polar", 50000, 384, 28, 128, 15);

        camera = new Camera(world, 0, 50000); // Planet surface at the top
        camera.SetViewport(FramebufferSize.X, FramebufferSize.Y);

        renderer = new Renderer(world);
        loader = new ChunkLoader(world);
        picker = new BlockPicker(world);
        editor = new BlockEditor(world, picker);
        debug = new DebugOverlay(world, renderer.overlay, renderer.font, picker, editor);

        loader.LoadCore(world.coreRadiusChunks);
    }

    // The viewport must use the framebuffer size in pixels. The window size can be different on
    // scaled displays, which would draw the world smaller than the mouse math expects.
    protected override void OnFramebufferResize(FramebufferResizeEventArgs e)
    {
        base.OnFramebufferResize(e);
        GL.Viewport(0, 0, e.Width, e.Height);
        camera?.SetViewport(e.Width, e.Height);
    }

    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        base.OnMouseWheel(e);
        camera.Zoom(e.OffsetY);
    }

    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        base.OnUpdateFrame(args);

        if (KeyboardState.IsKeyDown(Keys.Escape)) Close();

        // Free camera, until there is a player to follow (camera.target)
        if (camera.target == null)
        {
            MoveFreeCamera(args.Time);
        }

        // Core or cartesian world, arrow keys turn the camera freely
        if (!camera.InPolarRegion())
        {
            const double turnSpeed = 1.5; // radians per second
            if (KeyboardState.IsKeyDown(Keys.Right)) camera.Turn(turnSpeed * args.Time);
            if (KeyboardState.IsKeyDown(Keys.Left)) camera.Turn(-turnSpeed * args.Time);
        }

        camera.Update(args.Time);

        if (KeyboardState.IsKeyPressed(Keys.D1)) editor.selectedBlock = 1; // grass
        if (KeyboardState.IsKeyPressed(Keys.D2)) editor.selectedBlock = 2; // stone
        if (KeyboardState.IsKeyPressed(Keys.D3)) editor.selectedBlock = 3; // dirt
        if (KeyboardState.IsKeyPressed(Keys.C)) debug.showAllChunks = !debug.showAllChunks;
        Title = "Planet - selected block " + editor.selectedBlock;

        loader.LoadAround(camera.x, camera.y);

        // Hold left mouse to break blocks, hold right mouse to place blocks
        camera.ScreenToWorld(MousePosition.X, MousePosition.Y, ClientSize.X, ClientSize.Y, out double mx, out double my);
        debug.mouseWorldX = mx;
        debug.mouseWorldY = my;
        editor.Edit(MouseState.IsButtonDown(MouseButton.Left), MouseState.IsButtonDown(MouseButton.Right), mx, my);
    }

    // WASD moves relative to the screen, so W is always up on the screen
    private void MoveFreeCamera(double dt)
    {
        double speed = camera.viewHeight * dt * 0.8;
        double moveRight = 0, moveUp = 0;
        if (KeyboardState.IsKeyDown(Keys.W)) moveUp += speed;
        if (KeyboardState.IsKeyDown(Keys.S)) moveUp -= speed;
        if (KeyboardState.IsKeyDown(Keys.D)) moveRight += speed;
        if (KeyboardState.IsKeyDown(Keys.A)) moveRight -= speed;

        // Screen right is (cos, -sin) and screen up is (sin, cos) in the world
        camera.x += moveRight * camera.cos + moveUp * camera.sin;
        camera.y += -moveRight * camera.sin + moveUp * camera.cos;
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);

        renderer.Render(camera, debug);

        SwapBuffers();
    }

    protected override void OnUnload()
    {
        renderer.Dispose();
        base.OnUnload();
    }
}