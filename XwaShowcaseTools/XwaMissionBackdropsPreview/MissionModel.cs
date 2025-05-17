using JeremyAnsel.Xwa.Dat;
using JeremyAnsel.Xwa.HooksConfig;
using JeremyAnsel.Xwa.Mission;
using JeremyAnsel.Xwa.Workspace;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System;

namespace XwaMissionBackdropsPreview;

internal sealed class MissionModel
{
    public MissionModel(string missionFileName, int missionRegion, bool createRenderBackdrops, Action<DatImage, BackdropEntry, bool> createCallback)
    {
        if (AppSettings.WorkingSpace is null)
        {
            throw new InvalidOperationException("WorkingSpace not found");
        }

        MissionFileName = missionFileName;
        MissionRegion = missionRegion;
        MissionFile = TieFile.FromFile(missionFileName);
        LoadResdataPlanets(missionFileName);
        LoadBackdropScales(missionFileName);
        LoadMapBackdrops(missionRegion);
        CreateBackdrops(createRenderBackdrops, createCallback);
    }

    public string MissionFileName { get; }

    public int MissionRegion { get; }

    public TieFile MissionFile { get; }

    public SortedDictionary<(int, int), string> BackdropModels { get; } = new();

    public SortedDictionary<(int, int), int> BackdropScales { get; } = new();

    public int[] BackdropsCountPerRegion { get; } = new int[5];

    public BackdropEntry[] BackdropsEntries { get; } = new BackdropEntry[160];

    private void LoadResdataPlanets(string fileName)
    {
        BackdropModels.Clear();

        string basePath = Path.Combine(Path.GetDirectoryName(fileName), Path.GetFileNameWithoutExtension(fileName));

        var lines = XwaHooksConfig.GetFileLines(basePath + "_Resdata.txt");

        if (lines.Count == 0)
        {
            lines = XwaHooksConfig.GetFileLines(basePath + ".ini", "Resdata");
        }

        foreach (string line in XwaHooksConfig.GetFileLines(AppSettings.WorkingDirectory + "Resdata.txt"))
        {
            lines.Add(line);
        }

        foreach (string line in lines)
        {
            DatFile dat = DatFile.FromFile(AppSettings.WorkingDirectory + line, false);

            foreach (DatImage image in dat.Images)
            {
                (int, int) key = (image.GroupId, image.ImageId);

                if (BackdropModels.ContainsKey(key))
                {
                    continue;
                }

                BackdropModels.Add(key, dat.FileName);
            }
        }
    }

    private void LoadBackdropScales(string fileName)
    {
        BackdropScales.Clear();

        string mission = XwaHooksConfig.GetStringWithoutExtension(fileName);

        var file = XwaHooksConfig.GetFileLines(mission + "_BackdropScales.txt");

        if (file.Count == 0)
        {
            file = XwaHooksConfig.GetFileLines(mission + ".ini", "BackdropScales");
        }

        string scalesFileName = XwaHooksConfig.GetFileKeyValue(file, "ScalesFileName");

        if (!string.IsNullOrEmpty(scalesFileName))
        {
            var scales = XwaHooksConfig.GetFileLines(AppSettings.WorkingDirectory + "Resdata\\" + scalesFileName);

            foreach (string scale in scales)
            {
                file.Add(scale);
            }
        }

        var defaultScales = XwaHooksConfig.GetFileLines(AppSettings.WorkingDirectory + "Resdata\\BackdropScales.txt");

        if (defaultScales.Count != 0)
        {
            foreach (string scale in defaultScales)
            {
                file.Add(scale);
            }
        }

        var lines = XwaHooksConfig.GetFileListValues(file);

        foreach (var line in lines)
        {
            if (line.Count < 2)
            {
                continue;
            }

            int backdropIndex;
            int imageNumber;
            int scale;

            if (line.Count == 2)
            {
                backdropIndex = XwaHooksConfig.ToInt32(line[0]);
                imageNumber = -1;
                scale = XwaHooksConfig.ToInt32(line[1]);
            }
            else
            {
                backdropIndex = XwaHooksConfig.ToInt32(line[0]);
                imageNumber = XwaHooksConfig.ToInt32(line[1]);
                scale = XwaHooksConfig.ToInt32(line[2]);
            }

            (int, int) key = (backdropIndex, imageNumber);

            if (BackdropScales.ContainsKey(key))
            {
                continue;
            }

            BackdropScales.Add(key, scale);
        }
    }

    public float GetBackdropScale(int backdropIndex, int imageNumber)
    {
        if (BackdropScales.TryGetValue((backdropIndex, imageNumber), out int scale))
        {
            return scale;
        }

        if (BackdropScales.TryGetValue((backdropIndex, -1), out scale))
        {
            return scale;
        }

        return AppSettings.BackdropsScale;
    }

    private void LoadMapBackdrops(int region)
    {
        if (region < 0 || region >= 4)
        {
            return;
        }

        for (int flightGroupIndex = 0; flightGroupIndex < MissionFile.FlightGroups.Count; flightGroupIndex++)
        {
            var flightGroup = MissionFile.FlightGroups[flightGroupIndex];

            int startRegion = flightGroup.StartPointRegions[0];
            int craftId = flightGroup.CraftId;
            int positionX = flightGroup.StartPoints[0].PositionX;
            int positionY = -flightGroup.StartPoints[0].PositionY;
            int positionZ = flightGroup.StartPoints[0].PositionZ;
            int planetId = flightGroup.PlanetId;

            if (startRegion != region)
            {
                continue;
            }

            if (planetId == 0)
            {
                continue;
            }

            if (craftId != 183)
            {
                continue;
            }

            bool isDefaultPlanet = planetId >= 1 && planetId < AppSettings.ExePlanets.Length;
            bool isExtraPlanet = planetId >= 104 && planetId <= 255;

            if (!isDefaultPlanet && !isExtraPlanet)
            {
                continue;
            }

            bool isDsFire = isDefaultPlanet && AppSettings.ExePlanets[planetId].ModelIndex == 487;

            var planet = isDefaultPlanet
                ? AppSettings.ExePlanets[planetId]
                : new()
                {
                    ModelIndex = 0,
                    DataIndex1 = (short)(6304 + planetId - 104),
                    DataIndex2 = 0,
                    Flags = 5
                };

            if (isDefaultPlanet)
            {
                if (planet.ModelIndex == 0)
                {
                    continue;
                }

                var planetObject = AppSettings.WorkingSpace.ObjectTable.Entries[planet.ModelIndex];

                if (!planetObject.GameOptions.HasFlag(XwaExeObjectGameOptions.IsBackdrop))
                {
                    continue;
                }
            }

            if (BackdropsCountPerRegion[startRegion] >= 32)
            {
                continue;
            }

            int currentBackdropsCount = BackdropsCountPerRegion[startRegion];
            BackdropsCountPerRegion[startRegion]++;

            int backdropIndex = startRegion * 32 + currentBackdropsCount;
            var backdrop = new BackdropEntry();
            BackdropsEntries[backdropIndex] = backdrop;

            backdrop.DataIndex1 = planet.DataIndex1;
            backdrop.DataIndex2 = planet.DataIndex2;
            backdrop.WorldX = positionX;
            backdrop.WorldY = positionY;
            backdrop.WorldZ = positionZ;

            int worldXAbs = Math.Abs(backdrop.WorldX);
            int worldYAbs = Math.Abs(backdrop.WorldY);
            int worldZAbs = Math.Abs(backdrop.WorldZ);

            if (worldYAbs >= worldXAbs && worldYAbs >= worldZAbs)
            {
                if (backdrop.WorldY > 0)
                {
                    backdrop.Side = 0;
                }
                else
                {
                    backdrop.Side = 1;
                }
            }
            else if (worldXAbs >= worldYAbs && worldXAbs >= worldZAbs)
            {
                if (backdrop.WorldX > 0)
                {
                    backdrop.Side = 3;
                }
                else
                {
                    backdrop.Side = 2;
                }
            }
            else
            {
                if (backdrop.WorldZ >= 0)
                {
                    backdrop.Side = 4;
                }
                else
                {
                    backdrop.Side = 5;
                }
            }

            backdrop.Flags = planet.Flags;
            backdrop.IsEnvironment = (backdrop.Flags & 0x04) == 0;

            float size = 1.0f;

            if (!string.IsNullOrEmpty(flightGroup.SpecialCargo))
            {
                size = float.Parse(flightGroup.SpecialCargo, CultureInfo.InvariantCulture);

                if (size == 15.5f)
                {
                    size = 1.0f;
                    backdrop.Flags |= 0x02;
                }
            }

            float colorI = 0.0f;
            float colorR = 0.0f;
            float colorG = 0.0f;
            float colorB = 0.0f;

            if (isDsFire)
            {
                // ModelIndex_487_6250_0_ResData_DsFire
                backdrop.ImageNumber = 1;
            }
            else
            {
                if (!string.IsNullOrEmpty(flightGroup.Cargo))
                {
                    colorI = float.Parse(flightGroup.Cargo, CultureInfo.InvariantCulture);
                }

                if (!string.IsNullOrEmpty(flightGroup.Name))
                {
                    string[] parts = flightGroup.Name.Split();

                    if (parts.Length == 3)
                    {
                        colorR = float.Parse(parts[0], CultureInfo.InvariantCulture);
                        colorG = float.Parse(parts[1], CultureInfo.InvariantCulture);
                        colorB = float.Parse(parts[2], CultureInfo.InvariantCulture);
                    }
                }

                if ((backdrop.Flags & 0x01) != 0)
                {
                    backdrop.ImageNumber = (byte)(flightGroup.GlobalCargoIndex + 1);
                }
                else
                {
                    backdrop.ImageNumber = 1;
                }
            }

            int imageNumber;
            if (backdrop.Side <= 3)
            {
                imageNumber = (backdrop.Flags & 0x01) != 0 ? backdrop.ImageNumber - 1 : planet.DataIndex2;
            }
            else
            {
                imageNumber = (backdrop.Flags & 0x01) != 0 ? backdrop.ImageNumber - 1 : 0;
            }

            imageNumber = Math.Max(imageNumber, 0);

            backdrop.Scale = size * GetBackdropScale(planet.DataIndex1, imageNumber);
            backdrop.ColorIntensity = colorI;
            backdrop.ColorR = colorR;
            backdrop.ColorG = colorG;
            backdrop.ColorB = colorB;

            if (isDsFire)
            {
                // ModelIndex_487_6250_0_ResData_DsFire
                BackdropsCountPerRegion[startRegion]++;
            }
        }
    }

    private void CreateBackdrops(bool createRenderBackdrops, Action<DatImage, BackdropEntry, bool> createCallback)
    {
        for (int regionBackdropIndex = 0; regionBackdropIndex < BackdropsCountPerRegion[MissionRegion]; regionBackdropIndex++)
        {
            var backdrop = BackdropsEntries[MissionRegion * 32 + regionBackdropIndex];

            if (backdrop is null)
            {
                continue;
            }

            if (!createRenderBackdrops && !backdrop.IsEnvironment)
            {
                continue;
            }

            bool isWrap = (backdrop.Flags & 0x02) != 0 || backdrop.Side <= 3;

            int imageNumber;

            if (isWrap)
            {
                imageNumber = (backdrop.Flags & 0x01) != 0 ? backdrop.ImageNumber - 1 : backdrop.DataIndex2;
            }
            else
            {
                imageNumber = (backdrop.Flags & 0x01) != 0 ? backdrop.ImageNumber - 1 : 0;
            }

            imageNumber = Math.Max(imageNumber, 0);

            (int, int) planetImageKey = (backdrop.DataIndex1, imageNumber);

            if (!BackdropModels.TryGetValue(planetImageKey, out string planetFileName))
            {
                continue;
            }

            DatImage planetImage = DatFile.GetImageDataById(planetFileName, (short)planetImageKey.Item1, (short)planetImageKey.Item2);

            if (planetImage is null)
            {
                continue;
            }

            createCallback(planetImage, backdrop, isWrap);
        }
    }
}
