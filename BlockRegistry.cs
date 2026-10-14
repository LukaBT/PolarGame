// Knows every block: its id and its name. The ids come from atlas_index.txt, so the id stored in
// chunk.blockData is also the layer of the block texture array.
// Id 0 is air and has no texture.
public class BlockRegistry
{
    public const string AtlasFileName = "block_atlas.png";

    public readonly AtlasSheet sheet;

    // Highest id + 1, also the number of layers in the texture array
    public readonly int Size;

    private readonly string?[] names;
    private readonly Dictionary<string, ushort> ids = new Dictionary<string, ushort>();

    public BlockRegistry(AtlasIndex index)
    {
        sheet = index.GetSheet(AtlasFileName);

        int maxId = 0;
        foreach (AtlasEntry entry in sheet.Entries)
        {
            if (entry.Id <= 0 || entry.Id > ushort.MaxValue)
            {
                throw new InvalidDataException("Block '" + entry.Name + "' has id " + entry.Id + ", ids must be 1 to " + ushort.MaxValue);
            }

            maxId = Math.Max(maxId, entry.Id);
        }

        Size = maxId + 1;
        names = new string?[Size];

        foreach (AtlasEntry entry in sheet.Entries)
        {
            if (names[entry.Id] != null)
            {
                throw new InvalidDataException("Block id " + entry.Id + " is used twice");
            }

            if (!ids.TryAdd(entry.Name, (ushort)entry.Id))
            {
                throw new InvalidDataException("Block name '" + entry.Name + "' is used twice");
            }

            names[entry.Id] = entry.Name;
        }
    }

    // Block definitions are the same on every planet, so there is one registry for the whole game.
    // Call Initialize once at startup, before any world is created.
    private static BlockRegistry? instance;

    public static BlockRegistry Instance =>
        instance ?? throw new InvalidOperationException("BlockRegistry.Initialize() must be called before it is used");

    public static void Initialize()
    {
        instance = new BlockRegistry(AtlasIndex.Load(AssetPaths.Get("atlas_index.txt")));
    }

    // Throws on a typo, so a wrong name is found at startup and not as a magenta block later
    public ushort GetId(string name)
    {
        if (ids.TryGetValue(name, out ushort id)) return id;

        throw new KeyNotFoundException("Unknown block '" + name + "', it is not in atlas_index.txt");
    }

    public string GetName(ushort id)
    {
        if (id == 0) return "air";
        if (id < Size && names[id] != null) return names[id]!;
        return "unknown";
    }
}