public class Camera
{
    private World world;

    // Camera position in world coordinates
    public double x;
    public double y;

    // Which way the camera looks, clockwise from world up (radians)
    // In the polar region it follows the planet, in the core it is turned by the player
    public double angle = 0;

    // World units visible vertically, width comes from the screen aspect ratio
    public double viewHeight = 400;
    public double viewWidth = 400;
    public double minViewHeight = 20;
    public double maxViewHeight = 20000;

    // Framebuffer size in pixels
    public int screenWidth = 1;
    public int screenHeight = 1;

    // (sin, cos) of the camera angle
    public double sin = 0;
    public double cos = 1;

    // Half of the screen diagonal in world units, used for culling
    public double halfDiagonal = 0;

    // If set the camera follows this entity (usually the player), if null the camera is free
    public Entity? target = null;

    public Camera(World world, double x, double y)
    {
        this.world = world;
        this.x = x;
        this.y = y;

        Refresh();
    }

    // Calculates everything that depends on angle, zoom and screen size
    private void Refresh()
    {
        viewWidth = viewHeight * screenWidth / screenHeight;
        sin = Math.Sin(angle);
        cos = Math.Cos(angle);
        halfDiagonal = 0.5 * Math.Sqrt(viewWidth * viewWidth + viewHeight * viewHeight);
    }

    public void SetViewport(int width, int height)
    {
        screenWidth = Math.Max(width, 1);
        screenHeight = Math.Max(height, 1);
        Refresh();
    }

    // Scroll up zooms in, scroll down zooms out
    public void Zoom(double scroll)
    {
        viewHeight *= Math.Pow(0.9, scroll);
        viewHeight = Math.Clamp(viewHeight, minViewHeight, maxViewHeight);
        Refresh();
    }

    public void Turn(double radians)
    {
        angle += radians;
        Refresh();
    }

    // True when the camera is outside of the core of a planet
    public bool InPolarRegion()
    {
        double coreRadius = world.coreRadius;
        return world.worldType == "Polar" && x * x + y * y >= coreRadius * coreRadius;
    }

    public void Update(double dt)
    {
        if (target != null)
        {
            x = target.x;
            y = target.y;
        }

        if (InPolarRegion())
        {
            // Up on the screen points away from the center of the planet
            double goal = Math.Atan2(x, y);

            // Turn the short way around, eased so entering the polar region does not snap
            double diff = goal - angle;
            diff = Math.Atan2(Math.Sin(diff), Math.Cos(diff));

            if (Math.Abs(diff) < 1e-4)
            {
                angle = goal;
            }
            else
            {
                angle += diff * Math.Min(1.0, dt * 12);
            }
        }

        Refresh();
    }

    // Converts a mouse position (in window units) to a position in the world
    public void ScreenToWorld(double mouseX, double mouseY, int windowWidth, int windowHeight, out double worldX, out double worldY)
    {
        // Offset from the screen center in world units, screen y goes down but view y goes up
        double viewX = (mouseX / windowWidth - 0.5) * viewWidth;
        double viewY = -(mouseY / windowHeight - 0.5) * viewHeight;

        // Turn it back by the camera angle
        worldX = x + viewX * cos + viewY * sin;
        worldY = y - viewX * sin + viewY * cos;
    }

    // Position relative to the camera, turned by the camera angle (up on screen is +y)
    public void WorldToView(double worldX, double worldY, out double viewX, out double viewY)
    {
        double dx = worldX - x;
        double dy = worldY - y;

        viewX = dx * cos - dy * sin;
        viewY = dx * sin + dy * cos;
    }

    // False if a circle at this position can not be on screen
    public bool IsVisible(double worldX, double worldY, double radius)
    {
        double dx = worldX - x;
        double dy = worldY - y;

        return Math.Sqrt(dx * dx + dy * dy) <= halfDiagonal + radius;
    }
}