using System.Diagnostics;
using System.Runtime.InteropServices;

namespace RvB.Graphs;

public static class GraphViz {
    public static bool GenerateImage(string dot, string fileName) {
        if (GenerateImage(dot, out var image)) {
            try {
                File.WriteAllBytes(fileName, CollectionsMarshal.AsSpan(image));
                return true;
            } catch {
                return false;
            }
        }
        return false;
    }

    public static bool GenerateImage(string dot, out List<byte> image) {
        var location = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        var exe = Path.Combine(location, @"Graphviz\bin\dot.exe");

        var proc = new Process() {
            StartInfo = new() {
                FileName = exe,
                Arguments = $"-Tpng -Gdpi=300",
                UseShellExecute = false,
                RedirectStandardInput = true,
                //StandardInputEncoding = System.Text.Encoding.UTF8,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            }
        };
        image = [];

        proc.Start();
        var binReader = new BinaryReader(proc.StandardOutput.BaseStream);
        proc.StandardInput.WriteLine(dot);
        proc.StandardInput.Close();

        byte[] data;
        const int ChunkSize = 8192;
        do {
            data = binReader.ReadBytes(ChunkSize);
            image.AddRange(data);
        } while (data.Length == ChunkSize);
        return proc.WaitForExit(2_000);
    }
}
