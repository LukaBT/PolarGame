using System;
using System.Diagnostics;

public class Program
{
    static int passed = 0;
    static int failed = 0;

    // Stops the compiler from removing the timed code
    static double sink = 0;

    public static void Main()
    {
        int radius = 50000;
        int startingChunks = 14;
        int chunkSize = 128;
        int coreRadius = 384;
        int bandCount = 8;
        String worldType = "Polar";

        // Runs first so the numbers are not mixed with the tests below
        MemoryTest(radius, coreRadius, startingChunks, chunkSize, bandCount);
        Console.WriteLine();

        World world = new World(worldType, radius, coreRadius, startingChunks, chunkSize, bandCount);

        Console.WriteLine("TESTS");
        TestBands(world);
        TestWrap(world);
        TestIndices(world);
        TestChunkStorage(world);
        TestBlocks(world);
        TestMesh(world);
        TestSeams(world);
        TestCartesianMesh(world);
        TestCore(world);
        TestPositions(world);
        TestTerrain(world);

        Console.WriteLine();
        Console.WriteLine($"Passed: {passed}  Failed: {failed}");
        Console.WriteLine();

        Console.WriteLine("TIMINGS");
        TimeMeshes(world);
        TimeChunks(world);
        TimeMath(world);
        TimeWorld(world);
        TimeTerrain(world);

        Console.WriteLine();
        Console.WriteLine($"(ignore this number) {sink}");
    }


    static void Check(String name, bool condition)
    {
        if (condition)
        {
            passed++;
            Console.WriteLine($"[PASS] {name}");
        }
        else
        {
            failed++;
            Console.WriteLine($"[FAIL] {name}");
        }
    }

    static void Time(String name, int iterations, Action<int> action)
    {
        // Warm up
        for (int i = 0; i < iterations / 10 + 1; i++)
        {
            action(i);
        }

        Stopwatch stopwatch = Stopwatch.StartNew();

        for (int i = 0; i < iterations; i++)
        {
            action(i);
        }

        stopwatch.Stop();

        double nanoseconds = stopwatch.Elapsed.TotalMilliseconds * 1000000.0 / iterations;

        if (nanoseconds >= 1000)
        {
            Console.WriteLine($"{name,-38} {nanoseconds / 1000:F3} μs per call");
        }
        else
        {
            Console.WriteLine($"{name,-38} {nanoseconds:F1} ns per call");
        }
    }


    // Tests

    static void TestBands(World world)
    {
        for (int b = 0; b < world.bandCount; b++)
        {
            Band band = world.bands[b];

            Check($"Band {b} height matches radii", band.bandChunkHeight == (band.outerRadius - band.innerRadius) / world.chunkSize);
            Check($"Band {b} column count", band.bandChunkColumns == world.startingColumnChunks << b);
            Check($"Band {b} delta covers full circle", Math.Abs(band.delta * band.blockColumns - Math.Tau) < 1e-9);

            if (b > 0)
            {
                Check($"Band {b} starts where band {b - 1} ends", world.bands[b - 1].outerRadius == band.innerRadius);
            }
        }

        // Numbers for the default settings
        Check("Band 6 has 192 rows", world.bands[6].bandChunkHeight == 192);
        Check("Band 6 has 896 columns", world.bands[6].bandChunkColumns == 896);
    }

    static void TestWrap(World world)
    {
        Band band = world.bands[6];

        Check("Wrap 0", band.WrapChunkColumn(0) == 0);
        Check("Wrap 895", band.WrapChunkColumn(895) == 895);
        Check("Wrap 896", band.WrapChunkColumn(896) == 0);
        Check("Wrap 900", band.WrapChunkColumn(900) == 4);
        Check("Wrap -1", band.WrapChunkColumn(-1) == 895);
        Check("Wrap -3", band.WrapChunkColumn(-3) == 893);
        Check("Wrap -896", band.WrapChunkColumn(-896) == 0);
        Check("Wrap block column -1", band.WrapBlockColumn(-1) == band.blockColumns - 1);
        Check("Wrap block column full circle", band.WrapBlockColumn(band.blockColumns) == 0);
    }

    static void TestIndices(World world)
    {
        int n = world.chunkSize + 1;

        Check("Index count", world.mesh.indices.Length == world.chunkSize * world.chunkSize * 6);

        int max = 0;
        for (int i = 0; i < world.mesh.indices.Length; i++)
        {
            max = Math.Max(max, world.mesh.indices[i]);
        }

        Check("Indices stay inside vertex count", max == n * n - 1);
        Check("First quad", world.mesh.indices[0] == 0 && world.mesh.indices[1] == n && world.mesh.indices[2] == 1
            && world.mesh.indices[3] == 1 && world.mesh.indices[4] == n && world.mesh.indices[5] == n + 1);
    }

    static void TestChunkStorage(World world)
    {
        Band band = world.bands[0];

        Check("Chunk not loaded at start", band.GetChunk(1, 0) == null);

        Chunk? chunk = band.LoadChunk(1, 0);
        Check("LoadChunk returns chunk", chunk != null);
        Check("GetChunk finds loaded chunk", band.GetChunk(1, 0) == chunk);
        Check("LoadChunk twice gives same chunk", band.LoadChunk(1, 0) == chunk);
        Check("GetChunk wraps column", band.GetChunk(1 + band.bandChunkColumns, 0) == chunk);
        Check("GetChunk wraps negative column", band.GetChunk(1 - band.bandChunkColumns, 0) == chunk);

        Check("Row below band is null", band.LoadChunk(0, -1) == null);
        Check("Row above band is null", band.LoadChunk(0, band.bandChunkHeight) == null);

        band.UnloadChunk(1, 0);
        Check("UnloadChunk removes chunk", band.GetChunk(1, 0) == null);
    }

    static void TestBlocks(World world)
    {
        Band band = world.bands[0];
        int chunkSize = world.chunkSize;

        Chunk? chunk = band.LoadChunk(1, 0);
        Chunk? lastChunk = band.LoadChunk(band.bandChunkColumns - 1, 0);

        Check("SetBlock on loaded chunk", band.SetBlock(130, 20, 5));
        Check("GetBlock reads it back", band.GetBlock(130, 20) == 5);
        Check("Chunk has it at local (2,20)", chunk != null && chunk.GetBlock(2, 20) == 5);

        Check("SetBlock on unloaded chunk fails", !band.SetBlock(5 * chunkSize, 0, 3));
        Check("GetBlock on unloaded chunk is 0", band.GetBlock(5 * chunkSize, 0) == 0);

        Check("SetBlock below band fails", !band.SetBlock(0, -1, 3));
        Check("GetBlock above band is 0", band.GetBlock(0, band.bandChunkHeight * chunkSize) == 0);

        Check("SetBlock wraps negative column", band.SetBlock(-1, 7, 9));
        Check("GetBlock sees wrapped block", band.GetBlock(band.blockColumns - 1, 7) == 9);
        Check("Last chunk has it at local (127,7)", lastChunk != null && lastChunk.GetBlock(chunkSize - 1, 7) == 9);

        band.UnloadChunk(1, 0);
        band.UnloadChunk(band.bandChunkColumns - 1, 0);
    }

    static void TestMesh(World world)
    {
        Band band = world.bands[0];
        int n = world.chunkSize + 1;

        float[] mesh = world.mesh.GetPolarMesh(band, 0);

        Check("Mesh size", mesh.Length == n * n * 2);
        Check("Mesh is cached", world.mesh.GetPolarMesh(band, 0) == mesh);
        Check("Corner vertex is (0,0)", mesh[0] == 0 && mesh[1] == 0);

        // Left edge goes straight up
        bool leftEdge = true;
        for (int j = 0; j < n; j++)
        {
            int index = j * n * 2;
            leftEdge = leftEdge && Math.Abs(mesh[index]) < 1e-4 && Math.Abs(mesh[index + 1] - j) < 1e-3;
        }
        Check("Left edge is a straight line up", leftEdge);

        // Bottom edge bends away from the corner
        int last = (n - 1) * 2;
        Check("Bottom right vertex has x > 0 and y < 0", mesh[last] > 0 && mesh[last + 1] < 0);
    }

    // World position of one mesh vertex of a chunk
    static void LocalToWorld(World world, Band band, int chunkColumn, int chunkRow, int i, int j, out double x, out double y)
    {
        float[] mesh = world.mesh.GetPolarMesh(band, chunkRow);
        int index = (j * (world.chunkSize + 1) + i) * 2;

        double angle = band.GetChunkAngle(chunkColumn);
        double sin = Math.Sin(angle);
        double cos = Math.Cos(angle);

        band.GetChunkOrigin(chunkColumn, chunkRow, out double originX, out double originY);

        x = mesh[index] * cos + mesh[index + 1] * sin + originX;
        y = -mesh[index] * sin + mesh[index + 1] * cos + originY;
    }

    // Edges of neighbouring chunks should land on the same world position
    static void TestSeams(World world)
    {
        // Band, column, row
        int[,] cases =
        {
            { 0, 0, 0 },
            { 0, 13, 1 },
            { 3, 10, 5 },
            { 6, 100, 50 },
            { 6, 895, 190 },
            { 7, 0, 0 }
        };

        for (int c = 0; c < cases.GetLength(0); c++)
        {
            Band band = world.bands[cases[c, 0]];
            int column = cases[c, 1];
            int row = cases[c, 2];

            // Right edge of this chunk against left edge of the next one
            double sideError = 0;
            for (int j = 0; j < world.chunkSize + 1; j++)
            {
                LocalToWorld(world, band, column, row, world.chunkSize, j, out double ax, out double ay);
                LocalToWorld(world, band, column + 1, row, 0, j, out double bx, out double by);

                sideError = Math.Max(sideError, Math.Max(Math.Abs(ax - bx), Math.Abs(ay - by)));
            }
            Check($"Side seam band {band.band} column {column} row {row} (error {sideError:E2})", sideError < 1e-3);

            // Top edge of this chunk against bottom edge of the chunk above
            if (row + 1 < band.bandChunkHeight)
            {
                double topError = 0;
                for (int i = 0; i < world.chunkSize + 1; i++)
                {
                    LocalToWorld(world, band, column, row, i, world.chunkSize, out double ax, out double ay);
                    LocalToWorld(world, band, column, row + 1, i, 0, out double bx, out double by);

                    topError = Math.Max(topError, Math.Max(Math.Abs(ax - bx), Math.Abs(ay - by)));
                }
                Check($"Top seam band {band.band} column {column} row {row} (error {topError:E2})", topError < 1e-3);
            }
        }
    }


    static void TestCartesianMesh(World world)
    {
        int n = world.chunkSize + 1;
        float[] mesh = world.mesh.cartesianMesh;

        Check("Cartesian mesh size", mesh.Length == n * n * 2);

        // Vertex (i, j) is at (i, j)
        bool grid = true;
        for (int j = 0; j < n; j++)
        {
            for (int i = 0; i < n; i++)
            {
                int index = (j * n + i) * 2;
                grid = grid && mesh[index] == i && mesh[index + 1] == j;
            }
        }
        Check("Cartesian mesh is a square grid", grid);

        // Chunk (0,0) is centered on the middle of the planet
        world.core.GetChunkOrigin(0, 0, out double x, out double y);
        Check("Cartesian chunk (0,0) starts at (-64,-64)", x == -world.chunkSize / 2 && y == -world.chunkSize / 2);

        world.core.GetChunkOrigin(1, -1, out x, out y);
        Check("Cartesian chunk (1,-1) starts at (64,-192)", x == world.chunkSize / 2 && y == -world.chunkSize - world.chunkSize / 2);

        // Block (0,0) is local (64,64) of chunk (0,0), so its bottom left corner is the planet center
        world.core.GetChunkOrigin(0, 0, out double originX, out double originY);
        int blockIndex = (world.chunkSize / 2 * n + world.chunkSize / 2) * 2;
        Check("Block (0,0) corner is the planet center", originX + mesh[blockIndex] == 0 && originY + mesh[blockIndex + 1] == 0);

        // GetMesh picks the right mesh for the chunk
        Chunk cartesianChunk = new Chunk(world, 0, 0);
        Chunk polarChunk = new Chunk(world, 2, 5, 3);
        Check("GetMesh for cartesian chunk", world.mesh.GetMesh(cartesianChunk) == mesh);
        Check("GetMesh for polar chunk", world.mesh.GetMesh(polarChunk) == world.mesh.GetPolarMesh(world.bands[2], 3));
    }

    static void TestCore(World world)
    {
        world.core.LoadChunk(0, 0);
        world.core.LoadChunk(-1, 0);

        Check("Core SetBlock at (10.5, 20.5)", world.SetBlock(10.5, 20.5, 7));
        Check("Core GetBlock reads it back", world.GetBlock(10.5, 20.5) == 7);
        Check("Core chunk (0,0) has it at local (74,84)", world.core.GetChunk(0, 0)!.GetBlock(74, 84) == 7);

        Check("Core SetBlock at (-0.5, -0.5)", world.SetBlock(-0.5, -0.5, 3));
        Check("Block (-1,-1) is local (63,63) of chunk (0,0)", world.core.GetChunk(0, 0)!.GetBlock(63, 63) == 3);

        Check("Core SetBlock at (-70.5, 0.5)", world.SetBlock(-70.5, 0.5, 4));
        Check("Block -71 is local 121 of chunk (-1,0)", world.core.GetChunk(-1, 0)!.GetBlock(121, 64) == 4);

        Check("Core SetBlock on unloaded chunk fails", !world.SetBlock(200.5, 0.5, 1));
        Check("Core GetBlock on unloaded chunk is 0", world.GetBlock(200.5, 0.5) == 0);

        // This point is inside coreRadius, but the center of its block (272,271) is outside
        world.core.LoadChunk(2, 2);
        Check("Block with center inside coreRadius is accepted", world.SetBlock(270.5, 270.5, 1));
        Check("Block with center outside coreRadius is refused", !world.SetBlock(272.01, 271.01, 1));
    }

    static void TestPositions(World world)
    {
        // Every block center must give back the same block
        bool roundTrip = true;
        int checkedBlocks = 0;

        for (int b = 0; b < world.bandCount; b++)
        {
            Band band = world.bands[b];
            int[] columns = { 0, 1, 2, band.blockColumns / 3, band.blockColumns / 2, band.blockColumns - 2, band.blockColumns - 1 };
            int[] rows = { 0, 1, band.bandChunkHeight * world.chunkSize / 2, band.bandChunkHeight * world.chunkSize - 1 };

            foreach (int column in columns)
            {
                foreach (int row in rows)
                {
                    band.GetBlockCenter(column, row, out double x, out double y);
                    int foundBand = world.PositionToBlock(x, y, out int foundColumn, out int foundRow);

                    checkedBlocks++;
                    if (foundBand != b || foundColumn != column || foundRow != row)
                    {
                        roundTrip = false;
                        Console.WriteLine($"  mismatch band {b} column {column} row {row} -> band {foundBand} column {foundColumn} row {foundRow}");
                    }
                }
            }
        }
        Check($"Position round trip for {checkedBlocks} blocks", roundTrip);

        Check("Position inside core is not in a band", world.PositionToBlock(10, 10, out _, out _) == -1);
        Check("Position far outside planet is not in a band", world.PositionToBlock(0, 1000000, out _, out _) == -1);

        // Set through world position, read through band
        Band band0 = world.bands[0];
        band0.LoadChunk(0, 0);
        band0.GetBlockCenter(5, 10, out double px, out double py);

        Check("Polar SetBlock by position", world.SetBlock(px, py, 11));
        Check("Polar GetBlock by position", world.GetBlock(px, py) == 11);
        Check("Band sees it at (5,10)", band0.GetBlock(5, 10) == 11);
        Check("Polar GetBlock by address", world.GetBlock(0, 5, 10) == 11);
        Check("Position on unloaded polar chunk is 0", world.GetBlock(0, 100000) == 0);

        // Below band 0 is the core
        world.core.LoadChunk(0, 3);
        band0.GetBlockCenter(0, -1, out double cx, out double cy);
        Check("SetBlock below band 0 goes into the core", world.SetBlock(0, 0, -1, 5));
        Check("GetBlock below band 0 reads the core", world.GetBlock(0, 0, -1) == 5);
        Check("Same block is found by position", world.GetBlock(cx, cy) == 5);

        // Above band 0 is band 1, a block there touches two blocks above so set both
        Band band1 = world.bands[1];
        band1.LoadChunk(0, 0);
        band1.SetBlock(0, 0, 6);
        band1.SetBlock(1, 0, 6);

        int top = band0.bandChunkHeight * world.chunkSize;
        Check("GetBlock above band 0 reads band 1", world.GetBlock(0, 0, top) == 6);

        int lastBand = world.bandCount - 1;
        int lastTop = world.bands[lastBand].bandChunkHeight * world.chunkSize;
        Check("GetBlock above the last band is 0", world.GetBlock(lastBand, 0, lastTop) == 0);
        Check("SetBlock above the last band fails", !world.SetBlock(lastBand, 0, lastTop, 1));
    }


    static int CountBlocks(Chunk chunk, ushort blockId)
    {
        int count = 0;
        for (int i = 0; i < chunk.blockData.Length; i++)
        {
            if (chunk.blockData[i] == blockId)
            {
                count++;
            }
        }
        return count;
    }

    static void TestTerrain(World world)
    {
        int chunkSize = world.chunkSize;
        int all = chunkSize * chunkSize;
        ushort stone = world.generator.stoneId;
        ushort air = world.generator.airId;

        Band band6 = world.bands[6];
        Band band7 = world.bands[7];

        // Band 6 ends at 49152 which is below sea level (50000)
        Chunk deep = band6.LoadChunk(100, band6.bandChunkHeight - 1)!;
        world.generator.GenerateChunk(deep);
        Check("Top chunk of band 6 is all stone", CountBlocks(deep, stone) == all);

        Chunk bottom = band7.LoadChunk(0, 0)!;
        world.generator.GenerateChunk(bottom);
        Check("Bottom chunk of band 7 is all stone", CountBlocks(bottom, stone) == all);

        // Sea level is 50000, band 7 starts at 49152 so rows 0 to 847 are stone and row 848 is air
        Chunk surface = band7.LoadChunk(0, 6)!;
        world.generator.GenerateChunk(surface);
        Check("Surface chunk has 80 rows of stone", CountBlocks(surface, stone) == 80 * chunkSize);
        Check("Block at row 847 is stone", surface.GetBlock(5, 79) == stone);
        Check("Block at row 848 is air", surface.GetBlock(5, 80) == air);

        Chunk sky = band7.LoadChunk(0, 7)!;
        world.generator.GenerateChunk(sky);
        Check("Chunk above surface is all air", CountBlocks(sky, air) == all);

        // Same result no matter which chunk or in what order
        Chunk other = band7.LoadChunk(500, 6)!;
        world.generator.GenerateChunk(other);
        Check("Other column gives identical chunk", surface.blockData.SequenceEqual(other.blockData));

        world.generator.GenerateChunk(surface);
        Check("Generating twice gives identical chunk", surface.blockData.SequenceEqual(other.blockData));

        // Through the world
        Check("World.GetBlock by address, stone", world.GetBlock(7, 0, 847) == stone);
        Check("World.GetBlock by address, air", world.GetBlock(7, 0, 848) == air);

        band7.GetBlockCenter(3, 100, out double x, out double y);
        Check("World.GetBlock by position, stone", world.GetBlock(x, y) == stone);
        band7.GetBlockCenter(3, 900 - 1, out x, out y);
        Check("World.GetBlock by position above surface is air", world.GetBlock(x, y) == 0);

        // Core is a disc of stone
        int stoneBlocks = 0;
        for (int chunkY = -3; chunkY <= 3; chunkY++)
        {
            for (int chunkX = -3; chunkX <= 3; chunkX++)
            {
                Chunk coreChunk = world.core.LoadChunk(chunkX, chunkY);
                world.generator.GenerateChunk(coreChunk);
                stoneBlocks += CountBlocks(coreChunk, stone);
            }
        }

        double discArea = Math.PI * world.coreRadius * world.coreRadius;
        double error = Math.Abs(stoneBlocks - discArea) / discArea;
        Check($"Core stone blocks {stoneBlocks} match a disc of area {discArea:F0} (error {error:P2})", error < 0.01);

        Chunk edge = world.core.GetChunk(3, 0)!;
        Check("Core block with center at 383.5 is stone", edge.GetBlock(63, 64) == stone);
        Check("Core block with center at 384.5 is air", edge.GetBlock(64, 64) == air);
        Check("Core corner chunk is all air", CountBlocks(world.core.GetChunk(3, 3)!, air) == all);
        Check("Center block is stone", world.core.GetChunk(0, 0)!.GetBlock(64, 64) == stone);

        // Cartesian only world is flat
        World flat = new World("Cartesian", chunkSize);
        Chunk flatChunk = flat.core.LoadChunk(0, 0);
        flat.generator.GenerateChunk(flatChunk);
        Check("Flat world, row below y = 0 is stone", flatChunk.GetBlock(10, 63) == flat.generator.stoneId);
        Check("Flat world, row at y = 0 is air", flatChunk.GetBlock(10, 64) == flat.generator.airId);
        Check("Flat world, half of chunk is stone", CountBlocks(flatChunk, flat.generator.stoneId) == all / 2);
    }


    // Timings

    static void TimeMeshes(World world)
    {
        Band band0 = world.bands[0];
        Band band6 = world.bands[6];

        // Clear first so the mesh is really built every time
        Time("Build mesh (band 0 row 0)", 2000, i =>
        {
            world.mesh.polarMeshes.Clear();
            sink += world.mesh.GetPolarMesh(band0, 0)[10];
        });

        Time("Build mesh (band 6 row 100)", 2000, i =>
        {
            world.mesh.polarMeshes.Clear();
            sink += world.mesh.GetPolarMesh(band6, 100)[10];
        });

        world.mesh.GetPolarMesh(band6, 100);
        Time("Get cached mesh", 1000000, i =>
        {
            sink += world.mesh.GetPolarMesh(band6, 100)[10];
        });
    }

    static void TimeChunks(World world)
    {
        Band band = world.bands[6];

        Time("Create empty Chunk", 10000, i =>
        {
            Chunk chunk = new Chunk(world, 6, 0, 0);
            sink += chunk.blockData.Length;
        });

        // Load a 32 x 32 patch of chunks, about 32 MB of block data
        Stopwatch stopwatch = Stopwatch.StartNew();

        for (int row = 0; row < 32; row++)
        {
            for (int column = 0; column < 32; column++)
            {
                band.LoadChunk(column, row);
            }
        }

        stopwatch.Stop();
        double totalMilliseconds = stopwatch.Elapsed.TotalMilliseconds;
        Console.WriteLine($"{"Load 1024 chunks",-38} {totalMilliseconds:F3} ms total, {totalMilliseconds * 1000 / 1024:F2} μs each");

        // Lookups with 1024 chunks loaded
        Time("GetChunk (1024 loaded)", 1000000, i =>
        {
            sink += band.GetChunk(i % 32, (i / 32) % 32) != null ? 1 : 0;
        });

        Time("GetBlock (1024 loaded)", 1000000, i =>
        {
            sink += band.GetBlock(i % 4096, (i / 4096) % 4096);
        });

        Time("SetBlock (1024 loaded)", 1000000, i =>
        {
            band.SetBlock(i % 4096, (i / 4096) % 4096, 1);
        });

        stopwatch.Restart();

        for (int row = 0; row < 32; row++)
        {
            for (int column = 0; column < 32; column++)
            {
                band.UnloadChunk(column, row);
            }
        }

        stopwatch.Stop();
        Console.WriteLine($"{"Unload 1024 chunks",-38} {stopwatch.Elapsed.TotalMilliseconds:F3} ms total");
    }

    static void TimeMath(World world)
    {
        Band band = world.bands[6];

        Time("WrapChunkColumn", 10000000, i =>
        {
            sink += band.WrapChunkColumn(-i);
        });

        Time("GetChunkOrigin", 1000000, i =>
        {
            band.GetChunkOrigin(i % 896, 5, out double x, out double y);
            sink += x + y;
        });
    }

    static void TimeWorld(World world)
    {
        Band band = world.bands[6];

        // Same 32 x 32 chunks as before
        for (int row = 0; row < 32; row++)
        {
            for (int column = 0; column < 32; column++)
            {
                band.LoadChunk(column, row);
            }
        }
        band.GetBlockCenter(0, 0, out double startX, out double startY);

        Time("PositionToBlock", 1000000, i =>
        {
            world.PositionToBlock(startX + (i % 1000) * 0.01, startY + (i % 777) * 0.01, out int c, out int r);
            sink += c + r;
        });

        Time("World.GetBlock by position (polar)", 1000000, i =>
        {
            sink += world.GetBlock(startX + (i % 1000) * 0.01, startY + (i % 777) * 0.01);
        });

        Time("World.GetBlock by position (core)", 1000000, i =>
        {
            sink += world.GetBlock((i % 300) + 0.5, 0.5);
        });
    }

    // Memory

    static void PrintMemory(String label)
    {
        // Collect first so garbage is not counted
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        Process process = Process.GetCurrentProcess();

        double managedMegabytes = GC.GetTotalMemory(true) / 1048576.0;
        double workingSetMegabytes = process.WorkingSet64 / 1048576.0;
        double privateMegabytes = process.PrivateMemorySize64 / 1048576.0;

        Console.WriteLine($"{label,-26} managed {managedMegabytes,8:F1} MB   working set {workingSetMegabytes,8:F1} MB   private {privateMegabytes,8:F1} MB");
    }

    static void MemoryTest(int radius, int coreRadius, int startingChunks, int chunkSize, int bandCount)
    {
        Console.WriteLine("MEMORY");
        PrintMemory("Program start");

        World world = new World("Polar", radius, coreRadius, startingChunks, chunkSize, bandCount);
        PrintMemory("World created");

        // Mesh for every row of bands 0 to 6
        int meshCount = 0;
        int lastBand = Math.Min(6, world.bandCount - 1);

        for (int b = 0; b <= lastBand; b++)
        {
            Band band = world.bands[b];

            for (int row = 0; row < band.bandChunkHeight; row++)
            {
                world.mesh.GetPolarMesh(band, row);
                meshCount++;
            }
        }
        PrintMemory($"{meshCount} meshes built");

        // Fixed seed so every run picks the same area, change it to get another one
        Random random = new Random(12345);

        // Area is 32 rows tall so only bands that have at least 32 rows can be used
        List<int> usableBands = new List<int>();
        for (int b = 0; b <= lastBand; b++)
        {
            if (world.bands[b].bandChunkHeight >= 32)
            {
                usableBands.Add(b);
            }
        }

        int areaBand = usableBands[random.Next(usableBands.Count)];
        Band areaBandObject = world.bands[areaBand];
        int startColumn = random.Next(areaBandObject.bandChunkColumns);
        int startRow = random.Next(areaBandObject.bandChunkHeight - 31);

        for (int row = startRow; row < startRow + 32; row++)
        {
            for (int column = startColumn; column < startColumn + 32; column++)
            {
                Chunk? chunk = areaBandObject.LoadChunk(column, row);

                // Fresh memory is zero already and the OS only counts pages that are touched, so write to it
                Array.Fill(chunk!.blockData, (ushort)1);
            }
        }
        Console.WriteLine($"Loaded 32 x 32 chunks in band {areaBand}, columns {startColumn} to {startColumn + 31}, rows {startRow} to {startRow + 31}");
        PrintMemory($"{areaBandObject.chunks.Count} chunks loaded");

        // Free everything and see how much comes back
        foreach (Band band in world.bands)
        {
            band.chunks.Clear();
        }
        world.mesh.polarMeshes.Clear();
        PrintMemory("Chunks and meshes cleared");

        GC.KeepAlive(world);
    }

    static void TimeTerrain(World world)
    {
        Chunk polar = world.bands[7].LoadChunk(0, 6)!;
        Chunk core = world.core.LoadChunk(0, 0);

        Time("Generate polar chunk", 100000, i =>
        {
            world.generator.GenerateChunk(polar);
            sink += polar.blockData[0];
        });

        Time("Generate core chunk", 10000, i =>
        {
            world.generator.GenerateChunk(core);
            sink += core.blockData[0];
        });
    }
}