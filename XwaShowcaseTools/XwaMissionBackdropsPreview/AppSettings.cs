using JeremyAnsel.Xwa.Workspace;
using System.IO;
using System.Linq;
using System.Text;

namespace XwaMissionBackdropsPreview;

public static class AppSettings
{
    private static string _workingDirectory;

    public static string WorkingDirectory
    {
        get
        {
            return _workingDirectory;
        }

        set
        {
            if (value == _workingDirectory)
            {
                return;
            }

            if (!string.IsNullOrEmpty(value))
            {
                if (!value.EndsWith(Path.DirectorySeparatorChar) && !value.EndsWith(Path.AltDirectorySeparatorChar))
                {
                    value += "\\";
                }
            }

            _workingDirectory = value;

            SetData();
        }
    }

    public static XwaWorkspace WorkingSpace { get; private set; }

    public static PlanetEntry[] ExePlanets { get; private set; }

    public static float BackdropsScale { get; private set; }

    private static void ResetData()
    {
        WorkingSpace = null;
        ExePlanets = null;
        BackdropsScale = 1.0f;
    }

    private static void SetData()
    {
        ResetData();

        if (string.IsNullOrEmpty(WorkingDirectory) || !Directory.Exists(WorkingDirectory))
        {
            return;
        }

        string xwaExeFilePath = Path.Combine(WorkingDirectory, XwaWorkspace.ExeName);

        if (!File.Exists(xwaExeFilePath) || !XwaExeVersion.IsMatch(xwaExeFilePath))
        {
            return;
        }

        WorkingSpace = new(WorkingDirectory);
        ExePlanets = ReadExePlanets(xwaExeFilePath);

        using (BinaryReader file = new(new FileStream(xwaExeFilePath, FileMode.Open, FileAccess.Read), Encoding.ASCII))
        {
            file.BaseStream.Seek(0x1A83AC, SeekOrigin.Begin);
            BackdropsScale = file.ReadSingle();
        }
    }

    private static PlanetEntry[] ReadExePlanets(string path)
    {
        var planets = new PlanetEntry[104];

        using (BinaryReader file = new(new FileStream(path, FileMode.Open, FileAccess.Read), Encoding.ASCII))
        {
            file.BaseStream.Seek(0x1AFD40, SeekOrigin.Begin);

            for (int i = 0; i < planets.Length; i++)
            {
                var entry = new PlanetEntry
                {
                    ModelIndex = file.ReadUInt16(),
                    Flags = file.ReadByte()
                };

                var obj = WorkingSpace.ObjectTable.Entries.ElementAtOrDefault(entry.ModelIndex);

                if (obj != null)
                {
                    entry.DataIndex1 = obj.DataIndex1;
                    entry.DataIndex2 = obj.DataIndex2;
                }

                planets[i] = entry;
            }
        }

        return planets;
    }
}
