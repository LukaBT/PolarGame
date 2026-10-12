// Breaks and places blocks. Right now the mouse calls this, later the player will.
public class BlockEditor
{
    private World world;
    private BlockPicker picker;

    // Block that placing uses
    public ushort selectedBlock = 2;

    // Result of the last edit, shown on the HUD (BREAK 1 = worked, BREAK 0 = failed)
    public string lastEdit = "";

    public BlockEditor(World world, BlockPicker picker)
    {
        this.world = world;
        this.picker = picker;
    }

    // Breaks or places the block at a world position
    public void Edit(bool breaking, bool placing, double x, double y)
    {
        if (!breaking && !placing) return;

        // Edit exactly the block that was picked, instead of finding it again from the position
        BlockPick p = picker.PickAt(x, y);
        Chunk? chunk = picker.GetChunk(p);

        if (chunk == null)
        {
            lastEdit = "NONE";
            return;
        }

        ushort current = chunk.GetBlock(p.localX, p.localY);
        ushort newId;
        string action;

        if (breaking && current != 0)
        {
            newId = 0;
            action = "BREAK";
        }
        else if (placing && !breaking && current == 0)
        {
            newId = selectedBlock;
            action = "PLACE";
        }
        else
        {
            return;
        }

        bool ok = p.inCore
            ? world.core.SetBlock(p.blockA, p.blockB, newId)
            : world.bands[p.band].SetBlock(p.blockA, p.blockB, newId);

        // Make sure the renderer uploads the changed chunk again
        if (ok) chunk.dirty = true;

        lastEdit = action + (ok ? " 1" : " 0");
    }
}