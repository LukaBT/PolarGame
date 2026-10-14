using System.Globalization;

// Where game assets live. Assets/ is copied next to the executable by the csproj.
public static class AssetPaths
{
    public static readonly string Directory = Path.Combine(AppContext.BaseDirectory, "Assets");

    public static string Get(string fileName)
    {
        return Path.Combine(Directory, fileName);
    }
}

// One texture inside an atlas image, position is in pixels from the top left
public record AtlasEntry(int Id, string Name, int X, int Y, int Width, int Height);

// One atlas image and the textures it contains
public class AtlasSheet
{
    public string FileName = "";
    public int Width;
    public int Height;
    public List<AtlasEntry> Entries = new List<AtlasEntry>();
}

// Reads atlas_index.txt:
//   [block_atlas.png] 2048x2048
//   1 stone 16 0 16 16        (id name x y width height)
// Lines starting with # are comments. The x and y of every entry are used as written,
// so the index decides where a texture is, not the id.
public class AtlasIndex
{
    public List<AtlasSheet> sheets = new List<AtlasSheet>();

    public AtlasSheet GetSheet(string fileName)
    {
        foreach (AtlasSheet sheet in sheets)
        {
            if (sheet.FileName == fileName) return sheet;
        }

        throw new KeyNotFoundException("atlas_index.txt has no [" + fileName + "] section");
    }

    public static AtlasIndex Load(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("Atlas index not found. Is the Assets folder copied to the output folder?", path);
        }

        var index = new AtlasIndex();
        AtlasSheet? current = null;
        string[] lines = File.ReadAllLines(path);

        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            int lineNumber = i + 1;

            if (line.Length == 0 || line[0] == '#') continue;

            if (line[0] == '[')
            {
                current = ParseHeader(line, lineNumber);
                index.sheets.Add(current);
                continue;
            }

            if (current == null)
            {
                throw Error(lineNumber, "texture entry before any [sheet] header");
            }

            current.Entries.Add(ParseEntry(line, current, lineNumber));
        }

        return index;
    }

    // [block_atlas.png] 2048x2048
    private static AtlasSheet ParseHeader(string line, int lineNumber)
    {
        int close = line.IndexOf(']');
        if (close < 0) throw Error(lineNumber, "missing ]");

        string[] size = line.Substring(close + 1).Trim().Split('x');
        if (size.Length != 2) throw Error(lineNumber, "expected WIDTHxHEIGHT after the sheet name");

        return new AtlasSheet
        {
            FileName = line.Substring(1, close - 1),
            Width = ParseInt(size[0], lineNumber),
            Height = ParseInt(size[1], lineNumber)
        };
    }

    // 1 stone 16 0 16 16
    private static AtlasEntry ParseEntry(string line, AtlasSheet sheet, int lineNumber)
    {
        string[] parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 6) throw Error(lineNumber, "expected: id name x y width height");

        var entry = new AtlasEntry(
            ParseInt(parts[0], lineNumber), parts[1],
            ParseInt(parts[2], lineNumber), ParseInt(parts[3], lineNumber),
            ParseInt(parts[4], lineNumber), ParseInt(parts[5], lineNumber));

        if (entry.Width <= 0 || entry.Height <= 0 ||
            entry.X < 0 || entry.Y < 0 ||
            entry.X + entry.Width > sheet.Width || entry.Y + entry.Height > sheet.Height)
        {
            throw Error(lineNumber, "texture '" + entry.Name + "' is outside of the sheet");
        }

        return entry;
    }

    private static int ParseInt(string text, int lineNumber)
    {
        if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value))
        {
            throw Error(lineNumber, "'" + text + "' is not a number");
        }

        return value;
    }

    private static FormatException Error(int lineNumber, string message)
    {
        return new FormatException("atlas_index.txt line " + lineNumber + ": " + message);
    }
}