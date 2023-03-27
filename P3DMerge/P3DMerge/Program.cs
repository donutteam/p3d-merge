using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace P3DMerge
{
    internal class Program
    {
        private const uint P3DSignature = 0xFF443350;
        private static readonly ReadOnlyCollection<uint> ChunkPriorities = new ReadOnlyCollection<uint>(new uint[] {
            0x7000,     // History
            0x7030,     // Export Info
            0x19001,    // Image
            0x19000,    // Texture
            0x3000110,  // Set
            0x11000,    // Shader
            0x22000,    // Texture Font
            0x22002,    // Image Font
            0x19005,    // Sprite
            0x121304,   // Vertex Anim Key
            0x121000,   // Animation
            0x13000,    // Light
            0x2380,     // Light Group
            0x13005,    // Photon Map
            0x2200,     // Camera
            0x4500,     // Skeleton
            0x16006,    // Lens Flare Group
            0x15800,    // Particle System Factory
            0x15801,    // Particle System
            0x17001,    // Old Billboard Quad
            0x17002,    // Old Billboard Quad Group
            0x10000,    // Mesh
            0x7011000,  // Physics Object
            0x7010000,  // Collision Object
            0x3F0000E,  // Anim Dyna Phys
            0x3F00008,  // Anim Coll
            0x3F0000F,  // Anim Dyna Phys Wrapper
            0x3001000,  // Breakable Object
            0x3001001,  // Inst Particle System
            0x3F0000C,  // Anim
            0x3000005,  // Locator
            0x3000007,  // Spline
            0x10001,    // Skin
            0x21001,    // Expression Group
            0x21002,    // Expression Mixer
            0x3000000,  // Wall
            0x3000001,  // Fenceline
            0x3000004,  // Intersection
            0x3000009,  // Road Segment Data
            0x3000002,  // Road Segment
            0x3000003,  // Road
            0x300000B,  // Path
            0x4512,     // Composite Drawable
            0x20000,    // Animated Object Factory
            0x20001,    // Animated Object
            0x120100,   // Scenegraph
            0x121200,   // Old Frame Controller
            0x48A0,     // Multi Controller
            0x14000,    // Locator 3
            0x12000,    // Game Attr
            0x8010000,  // Smart Prop
            0x8020000,  // State Prop
        });

        private static void WriteHelp()
        {
            Console.WriteLine("P3D Merge help:");
            //Console.WriteLine($"\t{Path.GetFileName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName)} \"Path\\To\\File 1.p3d\" \"Path\\To\\File 2.p3d\" \"Path\\To\\File 3.p3d\" -o \"Path\\To\\Output.p3d\"");
            Console.WriteLine("\tP3DMerge.exe \"Path\\To\\File 1.p3d\" \"Path\\To\\File 2.p3d\" \"Path\\To\\File 3.p3d\" -o \"Path\\To\\Output.p3d\"");
            Console.WriteLine();
            Console.WriteLine("Optional arguments:");
            Console.WriteLine("\t\"-append\"\t- Append to output file if it already exists");
            Console.WriteLine("\t\"-dedupe\"\t- Check merged files for duplicate chunks");
            Console.WriteLine("\t\"-nohistory\"\t- Don't add a history chunk to the merged file");
            Console.WriteLine("\t\"-overwrite\"\t- Force overwriting if output file exists");
            Console.WriteLine("\t\"-pause\"\t- Pauses after successful file merge");
            Console.WriteLine("\t\"-sort\"\t- Sorts chunks of all files into the correct order");
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey(true);
        }

        private struct Chunk
        {
            public readonly uint Id;
            public List<byte> Data;
            public Chunk(uint Id, int Size)
            {
                this.Id = Id;
                Data = new List<byte>(Size);
            }
        }

        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                WriteHelp();
                return;
            }

            List<string> inputs = new List<string>();
            string output = string.Empty;

            bool isOutput = false;
            bool append = false;
            bool dedupe = false;
            bool noHistory = false;
            bool forceOverwrite = false;
            bool pauseOnExit = false;
            bool sort = false;
            foreach (string arg in args)
            {
                if (arg.StartsWith("-"))
                {
                    switch (arg.ToLower())
                    {
                        case "-i":
                        case "-in":
                        case "--in":
                            isOutput = false;
                            break;
                        case "-o":
                        case "-out":
                        case "--out":
                            isOutput = true;
                            break;
                        case "-?":
                        case "-help":
                        case "--help":
                            WriteHelp();
                            return;
                        case "-append":
                        case "--append":
                            append = true;
                            break;
                        case "-dedupe":
                        case "--dedupe":
                            dedupe = true;
                            break;
                        case "-nohistory":
                        case "--nohistory":
                            noHistory = true;
                            break;
                        case "-force":
                        case "-overwrite":
                        case "--force":
                        case "--overwrite":
                            forceOverwrite = true;
                            break;
                        case "-pause":
                        case "--pause":
                            pauseOnExit = true;
                            break;
                        case "-sort":
                        case "--sort":
                            sort = true;
                            break;
                        default:
                            Console.WriteLine($"Unknown argument: {arg}");
                            break;
                    }
                }
                else if (isOutput)
                {
                    if (output.Length > 0)
                    {
                        Console.WriteLine($"You may only specify one output file.");
                        Console.WriteLine("Press any key to exit...");
                        Console.ReadKey(true);
                        return;
                    }

                    try
                    {
                        FileInfo fi = new FileInfo(arg);
                        if (IsFileNameInvalid(fi.Name))
                        {
                            Console.WriteLine($"Specified output file '{fi.FullName}' contains invalid characters.");
                            Console.WriteLine("Press any key to exit...");
                            Console.ReadKey(true);
                            return;
                        }
                        output = fi.FullName;
                    }
                    catch
                    {
                        Console.WriteLine($"Specified output file '{arg}' is invalid.");
                        Console.WriteLine("Press any key to exit...");
                        Console.ReadKey(true);
                        return;
                    }
                }
                else if (File.Exists(arg))
                {
                    inputs.Add(arg);
                }
                else
                {
                    Console.WriteLine($"Specified input file '{arg}' not found.");
                    Console.WriteLine("Press any key to exit...");
                    Console.ReadKey(true);
                    return;
                }
            }

            if (output.Length == 0)
            {
                Console.WriteLine("No output file specified.");
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey(true);
                return;
            }

            if (inputs.Count < 2)
            {
                Console.WriteLine($"Less than 2 input files specified.");
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey(true);
                return;
            }

            if (append && forceOverwrite)
            {
                Console.WriteLine("You cannot specify both '-append' and '-force'.");
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey(true);
                return;
            }

            if (!forceOverwrite && File.Exists(output))
            {
                if (append)
                {
                    inputs.Insert(0, output);
                }
                else
                {
                    ConsoleKeyInfo answer;
                    Console.WriteLine($"Output file '{output}' already exists. Choose an action:\n\t[A]ppend\n\t[O]verwrite\n\t[E]xit");
                    do
                    {
                        answer = Console.ReadKey(true);
                    } while (answer.Key != ConsoleKey.A && answer.Key != ConsoleKey.O && answer.Key != ConsoleKey.E);

                    switch (answer.Key)
                    {
                        case ConsoleKey.E:
                            Console.WriteLine($"Not overwriting existing file.");
                            Console.WriteLine("Press any key to exit...");
                            Console.ReadKey(true);
                            return;
                        case ConsoleKey.A:
                            inputs.Insert(0, output);
                            Console.WriteLine("Appending to existing file...");
                            break;
                        case ConsoleKey.O:
                            Console.WriteLine("Overwriting existing file...");
                            break;
                    }
                    Console.WriteLine();
                }
            }

            Console.WriteLine($"Inputs: {string.Join("; ", inputs.ToArray())}");
            Console.WriteLine($"Output: {output}");

            Console.WriteLine();
            Console.WriteLine("Parsing inputs...");
            Dictionary<uint, List<Chunk>> chunks = new Dictionary<uint, List<Chunk>>();
            List<byte> data = new List<byte>();
            foreach (string input in inputs)
            {
                Console.WriteLine($"Checking: '{input}'...");
                try
                {
                    using (FileStream fs = new FileStream(input, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        if (fs.Length < 12)
                        {
                            Console.WriteLine($"Specified input file '{input}' is not a valid P3D.");
                            Console.WriteLine("Press any key to exit...");
                            Console.ReadKey(true);
                            return;
                        }

                        using (BinaryReader br = new BinaryReader(fs))
                        {
                            byte[] fileBytes = LZR_Compression.DecompressFile(br);
                            uint signature = BitConverter.ToUInt32(fileBytes, 0);

                            if (signature != P3DSignature)
                            {
                                Console.WriteLine($"Specified input file '{input}' is not a valid P3D.");
                                Console.WriteLine("Press any key to exit...");
                                Console.ReadKey(true);
                                return;
                            }

                            int pos = 12;
                            while (pos < fileBytes.Length)
                            {
                                uint id = BitConverter.ToUInt32(fileBytes, pos);
                                int size = BitConverter.ToInt32(fileBytes, pos + 8);

                                if (!chunks.ContainsKey(id))
                                    chunks.Add(id, new List<Chunk>());

                                Chunk c = new Chunk(id, size);
                                data.Capacity += size;
                                for (int i = 0; i < size; i++)
                                {
                                    c.Data.Add(fileBytes[pos + i]);
                                }
                                pos += size;

                                if (dedupe)
                                {
                                    bool match = false;
                                    foreach (Chunk chunk in chunks[id])
                                    {
                                        if (CompareLists(chunk.Data, c.Data))
                                        {
                                            match = true;
                                            break;
                                        }
                                    }
                                    if (match)
                                        continue;
                                }

                                data.AddRange(c.Data);
                                chunks[id].Add(c);

                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error reading file '{input}': {ex}");
                    Console.WriteLine("Press any key to exit...");
                    Console.ReadKey(true);
                    return;
                }
            }

            Console.WriteLine($"Merging files into '{output}'...");
            try
            {
                using (FileStream fs = new FileStream(output, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
                using (BinaryWriter bw = new BinaryWriter(fs))
                {
                    bw.Write(P3DSignature);
                    bw.Write(12);
                    bw.Write(12);

                    if (!noHistory)
                    {
                        bw.Write(0x7000u);
                        bw.Write(12);
                        bw.Write(12);

                        Assembly assembly = Assembly.GetExecutingAssembly();
                        FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
                        string version = fvi.FileVersion;
                        string[] history = new string[]
                        {
                            $"P3DMerge version {version}",
                            $"P3DMerge.exe \"{string.Join("\" \"", args)}\"",
                            $"Run at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} (UTC)"
                        };

                        List<string> history2 = new List<string>();
                        int maxStringLength = 252;
                        foreach (string line in history)
                        {
                            for (int i = 0; i < line.Length; i += maxStringLength)
                                history2.Add(PadString(line.Substring(i, Math.Min(maxStringLength, line.Length - i))));
                        }

                        bw.Write((short)history2.Count);
                        foreach (string line in history2)
                        {
                            bw.Write((byte)line.Length);
                            bw.Write(line.ToCharArray());
                        }

                        bw.Seek(16, SeekOrigin.Begin);
                        int size = (int)fs.Length - 12;
                        bw.Write(size);
                        bw.Write(size);
                        bw.Seek(0, SeekOrigin.End);
                    }

                    if (sort)
                    {

                        foreach (uint id in ChunkPriorities)
                        {
                            if (chunks.ContainsKey(id))
                            {
                                foreach (Chunk chunk in chunks[id])
                                {
                                    bw.Write(chunk.Data.ToArray());
                                }
                                chunks.Remove(id);
                            }
                        }
                        foreach (KeyValuePair<uint, List<Chunk>> kvp in chunks)
                        {
                            foreach (Chunk chunk in kvp.Value)
                            {
                                bw.Write(chunk.Data.ToArray());
                            }
                        }
                    }
                    else
                    {
                        bw.Write(data.ToArray());
                    }

                    fs.Seek(8, SeekOrigin.Begin);
                    bw.Write((uint)fs.Length);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error writing file '{output}': {ex}");
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey(true);
                return;
            }

            Console.WriteLine("Done.");

            if (pauseOnExit)
            {
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey(true);
            }
        }

        private static bool IsFileNameInvalid(string fileName)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                if (fileName.IndexOf(c) != -1)
                    return true;
            }
            return false;
        }

        private static bool CompareLists<T>(List<T> list1, List<T> list2)
        {
            if (list1 == null)
                return list2 == null;

            if (list2 == null)
                return false;

            if (list1.Count != list2.Count)
                return false;

            for (int i = 0; i < list1.Count; i++)
            {
                if (!list1[i].Equals(list2[i]))
                    return false;
            }

            return true;
        }

        private static string PadString(string str)
        {
            if (str == null)
                return null;

            int diff = str.Length & 3;
            if (diff == 0)
                return str;

            return str + new string('\0', 4 - diff);
        }
    }
}
