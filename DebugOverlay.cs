using OpenTK.Mathematics;

// Debug drawing on top of the world: chunk outlines, block outline and the HUD text
public class DebugOverlay
{
    private World world;
    private Overlay overlay;
    private OverlayFont font;
    private BlockPicker picker;
    private BlockEditor editor;

    // Press C to show the outline of every loaded chunk
    public bool showAllChunks = false;

    // World position under the mouse, set every frame by the game
    public double mouseWorldX = 0;
    public double mouseWorldY = 0;

    private static readonly Color4 FaintColor = new Color4(1f, 1f, 1f, 0.25f);
    private static readonly Color4 ChunkColor = new Color4(1f, 0.85f, 0.2f, 0.95f);
    private static readonly Color4 BlockColor = new Color4(1f, 1f, 1f, 1f);
    private static readonly Color4 TextColor = new Color4(1f, 1f, 1f, 1f);
    private static readonly Color4 TextBackground = new Color4(0f, 0f, 0f, 0.6f);

    public DebugOverlay(World world, Overlay overlay, OverlayFont font, BlockPicker picker, BlockEditor editor)
    {
        this.world = world;
        this.overlay = overlay;
        this.font = font;
        this.picker = picker;
        this.editor = editor;
    }

    // Overlay.Begin must be called before this
    public void Draw()
    {
        if (showAllChunks)
        {
            foreach (Chunk c in world.core.chunks.Values)
            {
                DrawChunkOutline(true, -1, c.chunkColumn, c.chunkRow, FaintColor, 1);
            }

            if (world.bands != null)
            {
                foreach (Band b in world.bands)
                {
                    foreach (Chunk c in b.chunks.Values)
                    {
                        DrawChunkOutline(false, b.band, c.chunkColumn, c.chunkRow, FaintColor, 1);
                    }
                }
            }
        }

        BlockPick pick = picker.PickAt(mouseWorldX, mouseWorldY);

        if (pick.valid)
        {
            DrawChunkOutline(pick.inCore, pick.band, pick.chunkA, pick.chunkB, ChunkColor, 2);
            DrawBlockOutline(pick, BlockColor, 2);
        }

        DrawHud(pick);
    }

    private void Line(double x0, double y0, double x1, double y1, double thickness, Color4 color)
    {
        overlay.WorldLine(x0, y0, x1, y1, thickness, color);
    }

    // Outline of one block, in polar bands the sides follow the angle and radius
    private void DrawBlockOutline(BlockPick p, Color4 color, double thickness)
    {
        if (p.inCore)
        {
            double x = p.blockA, y = p.blockB;
            Line(x, y, x + 1, y, thickness, color);
            Line(x + 1, y, x + 1, y + 1, thickness, color);
            Line(x + 1, y + 1, x, y + 1, thickness, color);
            Line(x, y + 1, x, y, thickness, color);
            return;
        }

        Band band = world.bands[p.band];
        double a0 = p.blockA * band.delta;
        double a1 = (p.blockA + 1) * band.delta;
        double r0 = band.innerRadius + p.blockB;
        double r1 = r0 + 1;

        double x00 = Math.Sin(a0) * r0, y00 = Math.Cos(a0) * r0;
        double x10 = Math.Sin(a1) * r0, y10 = Math.Cos(a1) * r0;
        double x11 = Math.Sin(a1) * r1, y11 = Math.Cos(a1) * r1;
        double x01 = Math.Sin(a0) * r1, y01 = Math.Cos(a0) * r1;

        Line(x00, y00, x10, y10, thickness, color);
        Line(x10, y10, x11, y11, thickness, color);
        Line(x11, y11, x01, y01, thickness, color);
        Line(x01, y01, x00, y00, thickness, color);
    }

    // Outline of one chunk, in polar bands the top and bottom edges are drawn as arcs
    private void DrawChunkOutline(bool inCore, int bandIndex, int chunkA, int chunkB, Color4 color, double thickness)
    {
        int size = world.chunkSize;

        if (inCore)
        {
            world.core.GetChunkOrigin(chunkA, chunkB, out double ox, out double oy);
            Line(ox, oy, ox + size, oy, thickness, color);
            Line(ox + size, oy, ox + size, oy + size, thickness, color);
            Line(ox + size, oy + size, ox, oy + size, thickness, color);
            Line(ox, oy + size, ox, oy, thickness, color);
            return;
        }

        Band band = world.bands[bandIndex];
        double r0 = band.GetRowRadius(chunkB);
        double r1 = r0 + size;
        double startAngle = band.GetChunkAngle(chunkA);
        double span = size * band.delta;
        const int steps = 16;

        for (int k = 0; k < steps; k++)
        {
            double a = startAngle + span * k / steps;
            double b = startAngle + span * (k + 1) / steps;

            Line(Math.Sin(a) * r0, Math.Cos(a) * r0, Math.Sin(b) * r0, Math.Cos(b) * r0, thickness, color);
            Line(Math.Sin(a) * r1, Math.Cos(a) * r1, Math.Sin(b) * r1, Math.Cos(b) * r1, thickness, color);
        }

        double endAngle = startAngle + span;
        Line(Math.Sin(startAngle) * r0, Math.Cos(startAngle) * r0, Math.Sin(startAngle) * r1, Math.Cos(startAngle) * r1, thickness, color);
        Line(Math.Sin(endAngle) * r0, Math.Cos(endAngle) * r0, Math.Sin(endAngle) * r1, Math.Cos(endAngle) * r1, thickness, color);
    }

    // Text in the top left corner
    private void DrawHud(BlockPick p)
    {
        var lines = new List<string>();

        // FormattableString.Invariant keeps the decimal point a dot on every language setting
        lines.Add(FormattableString.Invariant($"WORLD X {mouseWorldX:F1} Y {mouseWorldY:F1}"));

        if (!p.valid)
        {
            lines.Add("NONE");
        }
        else if (p.inCore)
        {
            lines.Add(FormattableString.Invariant($"CORE X {p.blockA} Y {p.blockB}"));
            lines.Add(FormattableString.Invariant($"CHUNK X {p.chunkA} Y {p.chunkB}"));
            lines.Add(FormattableString.Invariant($"LOCAL X {p.localX} Y {p.localY}"));
        }
        else
        {
            lines.Add(FormattableString.Invariant($"BAND {p.band} COL {p.blockA} ROW {p.blockB}"));
            lines.Add(FormattableString.Invariant($"CHUNK COL {p.chunkA} ROW {p.chunkB}"));
            lines.Add(FormattableString.Invariant($"LOCAL X {p.localX} Y {p.localY}"));
        }

        Chunk? hovered = picker.GetChunk(p);
        lines.Add(hovered == null
            ? "BLOCK NONE"
            : FormattableString.Invariant($"BLOCK {hovered.GetBlock(p.localX, p.localY)}"));

        if (editor.lastEdit != "") lines.Add(editor.lastEdit);

        const double scale = 2;
        const double pad = 8;
        double lineHeight = 10 * scale;

        double maxWidth = 0;
        foreach (string line in lines)
        {
            maxWidth = Math.Max(maxWidth, OverlayFont.TextWidth(line, scale));
        }

        overlay.Rect(pad / 2, pad / 2, maxWidth + pad * 2, lines.Count * lineHeight + pad, TextBackground);

        for (int i = 0; i < lines.Count; i++)
        {
            font.Text(lines[i], pad * 1.5, pad + i * lineHeight, scale, TextColor);
        }
    }
}