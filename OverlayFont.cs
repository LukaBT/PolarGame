using OpenTK.Mathematics;

// 5x7 pixel font drawn as rectangles into an Overlay
public class OverlayFont
{
    private readonly Overlay overlay;

    // Rows go from top to bottom, 1 is a lit pixel
    private static readonly Dictionary<char, string[]> glyphs = BuildGlyphs();

    private static Dictionary<char, string[]> BuildGlyphs()
    {
        var source = new Dictionary<char, string>
        {
            ['0'] = "01110/10001/10011/10101/11001/10001/01110",
            ['1'] = "00100/01100/00100/00100/00100/00100/01110",
            ['2'] = "01110/10001/00001/00010/00100/01000/11111",
            ['3'] = "11110/00001/00001/01110/00001/00001/11110",
            ['4'] = "00010/00110/01010/10010/11111/00010/00010",
            ['5'] = "11111/10000/11110/00001/00001/10001/01110",
            ['6'] = "00110/01000/10000/11110/10001/10001/01110",
            ['7'] = "11111/00001/00010/00100/01000/01000/01000",
            ['8'] = "01110/10001/10001/01110/10001/10001/01110",
            ['9'] = "01110/10001/10001/01111/00001/00010/01100",
            ['-'] = "00000/00000/00000/11111/00000/00000/00000",
            ['.'] = "00000/00000/00000/00000/00000/01100/01100",
            [':'] = "00000/01100/01100/00000/01100/01100/00000",
            [','] = "00000/00000/00000/00000/01100/00100/01000",
            [' '] = "00000/00000/00000/00000/00000/00000/00000",
            ['A'] = "01110/10001/10001/11111/10001/10001/10001",
            ['B'] = "11110/10001/10001/11110/10001/10001/11110",
            ['C'] = "01110/10001/10000/10000/10000/10001/01110",
            ['D'] = "11110/10001/10001/10001/10001/10001/11110",
            ['E'] = "11111/10000/10000/11110/10000/10000/11111",
            ['H'] = "10001/10001/10001/11111/10001/10001/10001",
            ['K'] = "10001/10010/10100/11000/10100/10010/10001",
            ['L'] = "10000/10000/10000/10000/10000/10000/11111",
            ['N'] = "10001/11001/10101/10101/10011/10001/10001",
            ['O'] = "01110/10001/10001/10001/10001/10001/01110",
            ['P'] = "11110/10001/10001/11110/10000/10000/10000",
            ['R'] = "11110/10001/10001/11110/10100/10010/10001",
            ['U'] = "10001/10001/10001/10001/10001/10001/01110",
            ['W'] = "10001/10001/10001/10101/10101/11011/10001",
            ['X'] = "10001/10001/01010/00100/01010/10001/10001",
            ['Y'] = "10001/10001/01010/00100/00100/00100/00100",
        };

        var result = new Dictionary<char, string[]>();
        foreach (var pair in source)
        {
            result[pair.Key] = pair.Value.Split('/');
        }
        return result;
    }

    public OverlayFont(Overlay overlay)
    {
        this.overlay = overlay;
    }

    // Position is the top left corner in pixels
    public void Text(string text, double x, double y, double scale, Color4 color)
    {
        double cursor = x;

        foreach (char ch in text.ToUpperInvariant())
        {
            if (glyphs.TryGetValue(ch, out string[]? rows))
            {
                for (int r = 0; r < 7; r++)
                {
                    for (int c = 0; c < 5; c++)
                    {
                        if (rows[r][c] == '1')
                        {
                            overlay.Rect(cursor + c * scale, y + r * scale, scale, scale, color);
                        }
                    }
                }
            }

            cursor += 6 * scale;
        }
    }

    // Width of a text in pixels
    public static double TextWidth(string text, double scale) => text.Length * 6 * scale;
}