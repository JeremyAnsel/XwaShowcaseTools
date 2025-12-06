using JeremyAnsel.DirectX.D3D11;
using JeremyAnsel.DirectX.Dxgi;
using JeremyAnsel.DirectX.DXMath;
using JeremyAnsel.DirectX.GameWindow;
using JeremyAnsel.Xwa.HooksConfig;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace XwaMissionBackdropsPreview;

[StructLayout(LayoutKind.Sequential)]
internal struct CubeMapsConstantBufferData
{
    public XMFloat4X4 World;

    public XMFloat4X4 View;

    public XMFloat4X4 Projection;

    public static readonly uint Size = (uint)Marshal.SizeOf<CubeMapsConstantBufferData>();
}

internal class CubeMapData
{
    public const int MaxMissionRegions = 4;

    public bool bEnabled = false;
    public bool bRenderAllRegions = false;
    public bool bAllRegionsIllum = false;
    public bool bAllRegionsOvr = false;

    public readonly bool[] bRenderInThisRegion = new bool[MaxMissionRegions] { false, false, false, false };
    public readonly bool[] bRenderIllumInThisRegion = new bool[MaxMissionRegions] { false, false, false, false };
    public readonly bool[] bRenderOvrInThisRegion = new bool[MaxMissionRegions] { false, false, false, false };

    public float allRegionsSpecular = 0.7f;
    public float allRegionsAmbientInt = 0.15f;
    public float allRegionsAmbientMin = 0.0f;
    public float allRegionsDiffuseMipLevel = 5;
    public float allRegionsIllumDiffuseMipLevel = 5;
    public float allRegionsAngX = 0.0f, allRegionsAngY = 0.0f, allRegionsAngZ = 0.0f;
    public float allRegionsOvrAngX = 0, allRegionsOvrAngY = 0, allRegionsOvrAngZ = 0;
    public XMFloat4 allRegionsR, allRegionsU, allRegionsF;
    public float allRegionsTexRes = -1;
    public float allRegionsIllumTexRes = -1;
    public float allRegionsMipRes = 16.0f;
    public float allRegionsIllumMipRes = 16.0f;

    public readonly float[] regionSpecular = new float[MaxMissionRegions];
    public readonly float[] regionAmbientInt = new float[MaxMissionRegions];
    public readonly float[] regionAmbientMin = new float[MaxMissionRegions];
    public readonly float[] regionDiffuseMipLevel = new float[MaxMissionRegions];
    public readonly float[] regionIllumDiffuseMipLevel = new float[MaxMissionRegions];
    public readonly float[] regionAngX = new float[MaxMissionRegions] { 0, 0, 0, 0 };
    public readonly float[] regionAngY = new float[MaxMissionRegions] { 0, 0, 0, 0 };
    public readonly float[] regionAngZ = new float[MaxMissionRegions] { 0, 0, 0, 0 };
    public readonly float[] regionOvrAngX = new float[MaxMissionRegions] { 0, 0, 0, 0 };
    public readonly float[] regionOvrAngY = new float[MaxMissionRegions] { 0, 0, 0, 0 };
    public readonly float[] regionOvrAngZ = new float[MaxMissionRegions] { 0, 0, 0, 0 };
    public readonly float[] regionTexRes = new float[MaxMissionRegions] { -1, -1, -1, -1 };
    public readonly float[] regionIllumTexRes = new float[MaxMissionRegions] { -1, -1, -1, -1 };
    public readonly float[] regionMipRes = new float[MaxMissionRegions];
    public readonly float[] regionIllumMipRes = new float[MaxMissionRegions];

    public D3D11ShaderResourceView allRegionsSRV;
    public D3D11ShaderResourceView allRegionsIllumSRV;
    public D3D11ShaderResourceView allRegionsOvrSRV;
    public readonly D3D11ShaderResourceView[] regionSRV = new D3D11ShaderResourceView[MaxMissionRegions];
    public readonly D3D11ShaderResourceView[] regionIllumSRV = new D3D11ShaderResourceView[MaxMissionRegions];
    public readonly D3D11ShaderResourceView[] regionOvrSRV = new D3D11ShaderResourceView[MaxMissionRegions];

    public void Release()
    {
        D3D11Utils.DisposeAndNull(ref allRegionsSRV);
        D3D11Utils.DisposeAndNull(ref allRegionsIllumSRV);
        D3D11Utils.DisposeAndNull(ref allRegionsOvrSRV);

        for (int i = 0; i < MaxMissionRegions; i++)
        {
            D3D11Utils.DisposeAndNull(ref regionSRV[i]);
            D3D11Utils.DisposeAndNull(ref regionIllumSRV[i]);
            D3D11Utils.DisposeAndNull(ref regionOvrSRV[i]);
        }
    }

    public bool RenderCubeMapInThisRegion(int region_out)
    {
        bool validRegion = region_out >= 0 && region_out < MaxMissionRegions;
        return validRegion && bRenderInThisRegion[region_out];
    }

    public int GetCurrentCubeMapRegion(int region)
    {
        bool regionEnabled = RenderCubeMapInThisRegion(region);
        return regionEnabled ? region : -1;
    }

    public bool RenderIllumCubeMapInThisRegion(int region)
    {
        bool validRegion = region >= 0 && region < MaxMissionRegions;
        return validRegion && bRenderIllumInThisRegion[region];
    }

    public bool RenderOverlayCubeMapInThisRegion(int region)
    {
        bool validRegion = region >= 0 && region < MaxMissionRegions;
        return validRegion && bRenderOvrInThisRegion[region];
    }
}

internal class CubeMapsData
{
    public int s_region;
    public bool s_drawCubeMap;
    public XMMatrix s_cubeMapRot;
    public bool s_drawOvrCubeMap;
    public XMMatrix s_ovrCubeMapRot;

    public XMMatrix world;
    public XMMatrix view;
    public XMMatrix projection;

    public D3D11Buffer s_constantBuffer;
    public D3D11RasterizerState s_rasterizerState;
    public D3D11SamplerState s_samplerState;
    public D3D11BlendState s_blendState;
    public D3D11DepthStencilState s_depthState;
    public D3D11InputLayout s_inputLayout;
    public D3D11VertexShader s_vertexShader;
    public D3D11PixelShader s_pixelShader;
    public D3D11Buffer s_vertexBuffer;
    public D3D11Buffer s_indexBuffer;

    public void Release()
    {
        D3D11Utils.DisposeAndNull(ref s_constantBuffer);
        D3D11Utils.DisposeAndNull(ref s_rasterizerState);
        D3D11Utils.DisposeAndNull(ref s_samplerState);
        D3D11Utils.DisposeAndNull(ref s_blendState);
        D3D11Utils.DisposeAndNull(ref s_depthState);
        D3D11Utils.DisposeAndNull(ref s_inputLayout);
        D3D11Utils.DisposeAndNull(ref s_vertexShader);
        D3D11Utils.DisposeAndNull(ref s_pixelShader);
        D3D11Utils.DisposeAndNull(ref s_vertexBuffer);
        D3D11Utils.DisposeAndNull(ref s_indexBuffer);
    }
}

internal class CubeMaps
{
    private readonly CubeMapData g_CubeMaps = new();
    public readonly CubeMapsData g_cubeMapsData = new();

    // Backdrops
    private readonly Dictionary<int, bool> g_StarfieldGroupIdImageIdMap = new();
    private readonly Dictionary<int, bool> g_DisabledGroupIdImageIdMap = new();
    private readonly Dictionary<int, bool> g_EnabledOvrGroupIdImageIdMap = new();

    private readonly D3D11Texture2D[] cubeTextures = new D3D11Texture2D[CubeMapData.MaxMissionRegions];
    private readonly D3D11Texture2D[] cubeTexturesIllum = new D3D11Texture2D[CubeMapData.MaxMissionRegions];
    private readonly D3D11Texture2D[] cubeTexturesOvr = new D3D11Texture2D[CubeMapData.MaxMissionRegions];
    private D3D11Texture2D allRegionsCubeTexture;
    private D3D11Texture2D allRegionsIllumCubeTexture;
    private D3D11Texture2D allRegionsOvrCubeTexture;

    public void Release()
    {
        g_CubeMaps.Release();
        g_cubeMapsData.Release();

        for (int i = 0; i < CubeMapData.MaxMissionRegions; i++)
        {
            D3D11Utils.DisposeAndNull(ref cubeTextures[i]);
            D3D11Utils.DisposeAndNull(ref cubeTexturesIllum[i]);
            D3D11Utils.DisposeAndNull(ref cubeTexturesOvr[i]);
        }

        D3D11Utils.DisposeAndNull(ref allRegionsCubeTexture);
        D3D11Utils.DisposeAndNull(ref allRegionsIllumCubeTexture);
        D3D11Utils.DisposeAndNull(ref allRegionsOvrCubeTexture);
    }

    private static List<string> ListFiles(string directoryPath)
    {
        if (!Directory.Exists(directoryPath))
        {
            return new();
        }

        List<(int id, string path)> result = new();

        foreach (string fullName in Directory.EnumerateFiles(directoryPath))
        {
            string name = Path.GetFileNameWithoutExtension(fullName);
            string key = Path.GetExtension(name).Substring(1);

            if (!int.TryParse(key, CultureInfo.InvariantCulture, out int keyId))
            {
                continue;
            }

            result.Add((keyId, fullName));
        }

        List<string> files = result.OrderBy(t => t.id).Select(t => t.path).ToList();
        return files;
    }

    private static bool IsInMap(Dictionary<int, bool> map, int key)
    {
        return map.ContainsKey(key);
    }

    private static int MakeKeyFromGroupIdImageId(int groupId, int imageId)
    {
        return (groupId << 16) | (imageId);
    }

    private void PopulateStarfieldMap()
    {
        // Populate the standard starfield map.
        // This code is not an exhaustive list of starfields. Custom DAT files can be
        // added (TFTC does this), but this is a good starting point for "standard" XWAU.
        g_StarfieldGroupIdImageIdMap.Clear();
        g_StarfieldGroupIdImageIdMap[MakeKeyFromGroupIdImageId(6104, 0)] = true;

        g_StarfieldGroupIdImageIdMap[MakeKeyFromGroupIdImageId(6079, 2)] = true;
        g_StarfieldGroupIdImageIdMap[MakeKeyFromGroupIdImageId(6079, 3)] = true;
        g_StarfieldGroupIdImageIdMap[MakeKeyFromGroupIdImageId(6079, 4)] = true;
        g_StarfieldGroupIdImageIdMap[MakeKeyFromGroupIdImageId(6079, 5)] = true;
        g_StarfieldGroupIdImageIdMap[MakeKeyFromGroupIdImageId(6079, 6)] = true;

        g_StarfieldGroupIdImageIdMap[MakeKeyFromGroupIdImageId(6034, 3)] = true;
        g_StarfieldGroupIdImageIdMap[MakeKeyFromGroupIdImageId(6034, 4)] = true;
        g_StarfieldGroupIdImageIdMap[MakeKeyFromGroupIdImageId(6034, 5)] = true;
        g_StarfieldGroupIdImageIdMap[MakeKeyFromGroupIdImageId(6034, 6)] = true;

        g_StarfieldGroupIdImageIdMap[MakeKeyFromGroupIdImageId(6042, 1)] = true;
        g_StarfieldGroupIdImageIdMap[MakeKeyFromGroupIdImageId(6042, 2)] = true;
        g_StarfieldGroupIdImageIdMap[MakeKeyFromGroupIdImageId(6042, 3)] = true;
        g_StarfieldGroupIdImageIdMap[MakeKeyFromGroupIdImageId(6042, 4)] = true;
        g_StarfieldGroupIdImageIdMap[MakeKeyFromGroupIdImageId(6042, 5)] = true;
        g_StarfieldGroupIdImageIdMap[MakeKeyFromGroupIdImageId(6042, 6)] = true;

        g_StarfieldGroupIdImageIdMap[MakeKeyFromGroupIdImageId(6094, 1)] = true;
        g_StarfieldGroupIdImageIdMap[MakeKeyFromGroupIdImageId(6094, 2)] = true; // Cap
        g_StarfieldGroupIdImageIdMap[MakeKeyFromGroupIdImageId(6094, 3)] = true;
        g_StarfieldGroupIdImageIdMap[MakeKeyFromGroupIdImageId(6094, 4)] = true; // Cap
        g_StarfieldGroupIdImageIdMap[MakeKeyFromGroupIdImageId(6094, 5)] = true;
        g_StarfieldGroupIdImageIdMap[MakeKeyFromGroupIdImageId(6094, 6)] = true; // Cap

        g_StarfieldGroupIdImageIdMap[MakeKeyFromGroupIdImageId(6083, 2)] = true;
        g_StarfieldGroupIdImageIdMap[MakeKeyFromGroupIdImageId(6083, 3)] = true; // Cap
        g_StarfieldGroupIdImageIdMap[MakeKeyFromGroupIdImageId(6083, 5)] = true;

        g_StarfieldGroupIdImageIdMap[MakeKeyFromGroupIdImageId(6084, 1)] = true;
        g_StarfieldGroupIdImageIdMap[MakeKeyFromGroupIdImageId(6084, 2)] = true; // Cap
        g_StarfieldGroupIdImageIdMap[MakeKeyFromGroupIdImageId(6084, 4)] = true;
        g_StarfieldGroupIdImageIdMap[MakeKeyFromGroupIdImageId(6084, 6)] = true; // Cap

        g_StarfieldGroupIdImageIdMap[MakeKeyFromGroupIdImageId(6094, 2)] = true; // Cap

        g_StarfieldGroupIdImageIdMap[MakeKeyFromGroupIdImageId(6104, 5)] = true; // Cap
    }

    private static void PopulateBackdropsMap(string list, Dictionary<int, bool> map)
    {
        IList<string> tokens = XwaHooksConfig.Tokennize(list);

        foreach (string token in tokens)
        {
            int index = token.IndexOf('-');

            if (index != -1)
            {
                if (!int.TryParse(token.AsSpan(0, index), CultureInfo.InvariantCulture, out int groupId))
                {
                    continue;
                }

                if (!int.TryParse(token.AsSpan(index + 1), CultureInfo.InvariantCulture, out int imageId))
                {
                    continue;
                }

                int key = MakeKeyFromGroupIdImageId(groupId, imageId);
                map[key] = true;
                continue;
            }

            if (string.Equals(token, "all", StringComparison.OrdinalIgnoreCase))
            {
                map[-1] = true;
                continue;
            }
        }
    }

    private void ParseCubeMapMissionIni(IList<string> lines)
    {
        g_DisabledGroupIdImageIdMap.Clear();
        g_EnabledOvrGroupIdImageIdMap.Clear();

        string DisabledBackdropList = XwaHooksConfig.GetFileKeyValue(lines, "DisabledBackdrops");
        PopulateBackdropsMap(DisabledBackdropList, g_DisabledGroupIdImageIdMap);

        string EnabledBackdropList = XwaHooksConfig.GetFileKeyValue(lines, "EnabledBackdrops");
        PopulateBackdropsMap(EnabledBackdropList, g_EnabledOvrGroupIdImageIdMap);

        g_CubeMaps.allRegionsSpecular = XwaHooksConfig.GetFileKeyValueFloat(lines, "AllRegionsSpecular", 0.55f);
        g_CubeMaps.allRegionsAmbientInt = XwaHooksConfig.GetFileKeyValueFloat(lines, "AllRegionsAmbientInt", 0.50f);
        g_CubeMaps.allRegionsAmbientMin = XwaHooksConfig.GetFileKeyValueFloat(lines, "AllRegionsAmbientMin", 0.01f);
        g_CubeMaps.allRegionsAngX = XwaHooksConfig.GetFileKeyValueFloat(lines, "AllRegionsRotationX", 0.0f);
        g_CubeMaps.allRegionsAngY = XwaHooksConfig.GetFileKeyValueFloat(lines, "AllRegionsRotationY", 0.0f);
        g_CubeMaps.allRegionsAngZ = XwaHooksConfig.GetFileKeyValueFloat(lines, "AllRegionsRotationZ", 0.0f);

        g_CubeMaps.allRegionsOvrAngX = XwaHooksConfig.GetFileKeyValueFloat(lines, "AllRegionsOverlayRotationX", g_CubeMaps.allRegionsAngX);
        g_CubeMaps.allRegionsOvrAngY = XwaHooksConfig.GetFileKeyValueFloat(lines, "AllRegionsOverlayRotationY", g_CubeMaps.allRegionsAngY);
        g_CubeMaps.allRegionsOvrAngZ = XwaHooksConfig.GetFileKeyValueFloat(lines, "AllRegionsOverlayRotationZ", g_CubeMaps.allRegionsAngZ);

        string[] regionSpecNames = new string[CubeMapData.MaxMissionRegions] { "Region0Specular", "Region1Specular", "Region2Specular", "Region3Specular" };
        string[] regionAmbientIntNames = new string[CubeMapData.MaxMissionRegions] { "Region0AmbientInt", "Region1AmbientInt", "Region2AmbientInt", "Region3AmbientInt" };
        string[] regionAmbientMinNames = new string[CubeMapData.MaxMissionRegions] { "Region0AmbientMin", "Region1AmbientMin", "Region2AmbientMin", "Region3AmbientMin" };

        string[] regionAngX = new string[CubeMapData.MaxMissionRegions] { "Region0RotationX", "Region1RotationX", "Region2RotationX", "Region3RotationX" };
        string[] regionAngY = new string[CubeMapData.MaxMissionRegions] { "Region0RotationY", "Region1RotationY", "Region2RotationY", "Region3RotationY" };
        string[] regionAngZ = new string[CubeMapData.MaxMissionRegions] { "Region0RotationZ", "Region1RotationZ", "Region2RotationZ", "Region3RotationZ" };

        string[] regionOvrAngX = new string[CubeMapData.MaxMissionRegions] { "Region0OverlayRotationX", "Region1OverlayRotationX", "Region2OverlayRotationX", "Region3OverlayRotationX" };
        string[] regionOvrAngY = new string[CubeMapData.MaxMissionRegions] { "Region0OverlayRotationY", "Region1OverlayRotationY", "Region2OverlayRotationY", "Region3OverlayRotationY" };
        string[] regionOvrAngZ = new string[CubeMapData.MaxMissionRegions] { "Region0OverlayRotationZ", "Region1OverlayRotationZ", "Region2OverlayRotationZ", "Region3OverlayRotationZ" };

        for (int i = 0; i < CubeMapData.MaxMissionRegions; i++)
        {
            g_CubeMaps.regionSpecular[i] = XwaHooksConfig.GetFileKeyValueFloat(lines, regionSpecNames[i], 0.70f);
            g_CubeMaps.regionAmbientInt[i] = XwaHooksConfig.GetFileKeyValueFloat(lines, regionAmbientIntNames[i], 0.15f);
            g_CubeMaps.regionAmbientMin[i] = XwaHooksConfig.GetFileKeyValueFloat(lines, regionAmbientMinNames[i], 0.01f);

            g_CubeMaps.regionAngX[i] = XwaHooksConfig.GetFileKeyValueFloat(lines, regionAngX[i], 0.0f);
            g_CubeMaps.regionAngY[i] = XwaHooksConfig.GetFileKeyValueFloat(lines, regionAngY[i], 0.0f);
            g_CubeMaps.regionAngZ[i] = XwaHooksConfig.GetFileKeyValueFloat(lines, regionAngZ[i], 0.0f);

            g_CubeMaps.regionOvrAngX[i] = XwaHooksConfig.GetFileKeyValueFloat(lines, regionOvrAngX[i], g_CubeMaps.regionAngX[i]);
            g_CubeMaps.regionOvrAngY[i] = XwaHooksConfig.GetFileKeyValueFloat(lines, regionOvrAngY[i], g_CubeMaps.regionAngY[i]);
            g_CubeMaps.regionOvrAngZ[i] = XwaHooksConfig.GetFileKeyValueFloat(lines, regionOvrAngZ[i], g_CubeMaps.regionAngZ[i]);
        }
    }

    public bool CubeMapsSkipBackdrop(int groupId, int imageId)
    {
        if (!g_CubeMaps.bEnabled)
        {
            return false;
        }

        int key = MakeKeyFromGroupIdImageId(groupId, imageId);

        // If this GroupId-ImageId is disabled...
        if (!IsInMap(g_EnabledOvrGroupIdImageIdMap, -1) && // "EnabledBackdrops = ALL" enables all backdrops
            (IsInMap(g_DisabledGroupIdImageIdMap, -1) || // Are all backdrops disabled?
                IsInMap(g_StarfieldGroupIdImageIdMap, key) || // Is it a known (default) backdrop in XWAU?
                IsInMap(g_DisabledGroupIdImageIdMap, key)) && // Is it explicitly disabled?
                                                              // ... but is not in the enabled-override list...
            !IsInMap(g_EnabledOvrGroupIdImageIdMap, key))
        // Then this is a skippable backdrop (starfield)
        {
            return true;
        }

        return false;
    }

    private void RenderDefaultBackground(int region, out bool drawCubeMap, out XMMatrix cubeMapRot, out bool drawOvrCubeMap, out XMMatrix ovrCubeMapRot)
    {
        bool renderCubeMapInThisRegion = g_CubeMaps.RenderCubeMapInThisRegion(region);
        bool renderIllumCubeMapInThisRegion = g_CubeMaps.RenderIllumCubeMapInThisRegion(region);
        bool renderOvrCubeMapInThisRegion = g_CubeMaps.RenderOverlayCubeMapInThisRegion(region);
        bool renderCubeMap = (renderCubeMapInThisRegion || g_CubeMaps.bRenderAllRegions);
        bool renderOvrCubeMap = (renderOvrCubeMapInThisRegion || g_CubeMaps.bAllRegionsOvr);

        float angX = g_CubeMaps.allRegionsAngX;
        float angY = g_CubeMaps.allRegionsAngY;
        float angZ = g_CubeMaps.allRegionsAngZ;
        float ovrAngX = g_CubeMaps.allRegionsOvrAngX;
        float ovrAngY = g_CubeMaps.allRegionsOvrAngY;
        float ovrAngZ = g_CubeMaps.allRegionsOvrAngZ;

        if (renderCubeMapInThisRegion)
        {
            angX = g_CubeMaps.regionAngX[region];
            angY = g_CubeMaps.regionAngY[region];
            angZ = g_CubeMaps.regionAngZ[region];
            ovrAngX = g_CubeMaps.regionOvrAngX[region];
            ovrAngY = g_CubeMaps.regionOvrAngY[region];
            ovrAngZ = g_CubeMaps.regionOvrAngZ[region];
        }

        XMMatrix Rx = XMMatrix.RotationX(XMMath.ConvertToRadians(-angX));
        XMMatrix Ry = XMMatrix.RotationY(XMMath.ConvertToRadians(-angY));
        XMMatrix Rz = XMMatrix.RotationZ(XMMath.ConvertToRadians(-angZ));
        XMMatrix ovrRx = XMMatrix.RotationX(XMMath.ConvertToRadians(-ovrAngX));
        XMMatrix ovrRy = XMMatrix.RotationY(XMMath.ConvertToRadians(-ovrAngY));
        XMMatrix ovrRz = XMMatrix.RotationZ(XMMath.ConvertToRadians(-ovrAngZ));
        XMMatrix mapRot = Rz * Ry * Rx;
        XMMatrix ovrMapRot = ovrRx * ovrRy * ovrRz;

        drawCubeMap = renderCubeMap;
        cubeMapRot = mapRot;
        drawOvrCubeMap = renderOvrCubeMap;
        ovrCubeMapRot = ovrMapRot;
    }

    private static D3D11Texture2D CreateTexture(D3D11Device device, string filename)
    {
        if (!File.Exists(filename))
        {
            return null;
        }

        string ext = Path.GetExtension(filename).ToUpperInvariant();

        switch (ext)
        {
            case ".BMP":
            case ".PNG":
            case ".JPG":
            case ".JPEG":
            case ".GIF":
                {
                    using var file = new Bitmap(filename);
                    var rect = new Rectangle(0, 0, file.Width, file.Height);
                    using var bitmap = file.Clone(rect, PixelFormat.Format32bppArgb);
                    var data = bitmap.LockBits(rect, ImageLockMode.ReadOnly, bitmap.PixelFormat);
                    byte[] textureBytes = new byte[data.Width * data.Height * 4];

                    try
                    {
                        Marshal.Copy(data.Scan0, textureBytes, 0, textureBytes.Length);
                    }
                    finally
                    {
                        bitmap.UnlockBits(data);
                    }

                    D3D11Texture2DDesc desc = new(DxgiFormat.B8G8R8A8UNorm, (uint)data.Width, (uint)data.Height, 1, 1);
                    D3D11SubResourceData[] textureData = new D3D11SubResourceData[]
                    {
                        new(textureBytes,  (uint)data.Width * 4)
                    };

                    return device.CreateTexture2D(desc, textureData);
                }

            default:
                throw new ArgumentOutOfRangeException(nameof(filename));
        }
    }

    private static bool LoadCubeMap(
        D3D11Device device,
        D3D11DeviceContext context,
        string path,
        out D3D11Texture2D cubeTexture,
        out D3D11ShaderResourceView cubeTextureSRV)
    {
        cubeTexture = null;
        cubeTextureSRV = null;

        path = AppSettings.WorkingDirectory + path;

        if (!Directory.Exists(path))
        {
            return false;
        }

        List<string> fileNames = ListFiles(path);
        bool cubeMapComplete = fileNames.Count == 6;

        if (!cubeMapComplete)
        {
            return false;
        }

        D3D11Texture2D cubeFace = null;

        // 0: Right
        // 1: Left
        // 2: Top
        // 3: Down
        // 4: Fwd
        // 5: Back

        uint totalMipLevels = 0;

        D3D11Box box = new(0, 0, 0, 1, 1, 1);

        for (int cubeFaceIdx = 0; cubeFaceIdx < fileNames.Count; cubeFaceIdx++)
        {
            D3D11Utils.DisposeAndNull(ref cubeFace);

            try
            {
                cubeFace = CreateTexture(device, fileNames[cubeFaceIdx]);
            }
            catch
            {
                D3D11Utils.DisposeAndNull(ref cubeFace);
                return false;
            }

            // When the first face of the cube is loaded, we get the size of the texture
            // and we use that to re-create the cubeTexture and cubeTextureSRV:

            if (cubeFaceIdx == 0)
            {
                D3D11Texture2DDesc desc = cubeFace.Description;
                box.Right = desc.Width;
                box.Bottom = desc.Height;
                uint size = desc.Width;

                D3D11Texture2DDesc cubeDesc = default;
                D3D11ShaderResourceViewDesc cubeSRVDesc = default;
                cubeDesc.Width = size;
                cubeDesc.Height = size;
                cubeDesc.MipLevels = 0;
                cubeDesc.ArraySize = 6;
                cubeDesc.Format = desc.Format; // Use the texture's format
                cubeDesc.Usage = D3D11Usage.Default;
                cubeDesc.BindOptions = D3D11BindOptions.ShaderResource | D3D11BindOptions.RenderTarget;
                cubeDesc.MiscOptions = D3D11ResourceMiscOptions.TextureCube | D3D11ResourceMiscOptions.GenerateMips;
                cubeDesc.CpuAccessOptions = 0;
                cubeDesc.SampleDesc = new(1, 0);

                cubeSRVDesc.Format = cubeDesc.Format;
                cubeSRVDesc.ViewDimension = D3D11SrvDimension.TextureCube;
                cubeSRVDesc.TextureCube = new D3D11TextureCubeSrv()
                {
                    MipLevels = uint.MaxValue,
                    MostDetailedMip = 0
                };

                D3D11Utils.DisposeAndNull(ref cubeTexture);
                D3D11Utils.DisposeAndNull(ref cubeTextureSRV);

                cubeTexture = device.CreateTexture2D(cubeDesc);
                cubeTextureSRV = device.CreateShaderResourceView(cubeTexture, cubeSRVDesc);
                totalMipLevels = cubeTexture.Description.MipLevels;
            }

            // The destination subresource is an array, so we need to use cubeFaceIdx...
            uint dstSubResourceIdx = D3D11Utils.CalcSubresource(0, (uint)cubeFaceIdx, totalMipLevels);
            // ... but the source is not an array, so the array index is always 0:
            uint srcSubResourceIdx = D3D11Utils.CalcSubresource(0, 0, 1);
            context.CopySubresourceRegion(cubeTexture, dstSubResourceIdx, 0, 0, 0,
                cubeFace, srcSubResourceIdx, box);
        }

        D3D11Utils.DisposeAndNull(ref cubeFace);
        context.GenerateMips(cubeTextureSRV);
        return true;
    }

    /// <summary>
    /// Check if the current mission has changed, and if so, load new cube maps and set
    /// global flags (g_bRenderCubeMapInThisRegion) and SRVs (g_cubeTextureSRV).
    /// This is currently called from D3dOptCreateTextureColorLight() while a mission
    /// is being loaded.
    /// </summary>
    public void LoadMissionCubeMaps(DeviceResources deviceResources, string missionFilename)
    {
        g_CubeMaps.bEnabled = true;

        if (!g_CubeMaps.bEnabled)
        {
            return;
        }

        PopulateStarfieldMap();

        string path = XwaHooksConfig.GetStringWithoutExtension(missionFilename);
        IList<string> lines = XwaHooksConfig.GetFileLines(path + "_SkyBoxes.txt");

        if (lines.Count == 0)
        {
            lines = XwaHooksConfig.GetFileLines(path + ".ini", "SkyBoxes");
        }

        D3D11Device device = deviceResources.D3DDevice;
        D3D11DeviceContext context = deviceResources.D3DContext;

        // Disable all cubemaps as soon as a new mission is loaded.
        // We'll re-enable them if we find the relevant settings in the .ini file.
        g_CubeMaps.bRenderAllRegions = false;
        for (int i = 0; i < CubeMapData.MaxMissionRegions; i++)
        {
            g_CubeMaps.bRenderInThisRegion[i] = false;
            g_CubeMaps.bRenderIllumInThisRegion[i] = false;
            g_CubeMaps.bRenderOvrInThisRegion[i] = false;
        }

        if (lines.Count == 0)
        {
            g_CubeMaps.bEnabled = false;
            return;
        }

        string illumStr = "_illum";
        string ovrStr = "_overlay";
        string allRegionsPath = XwaHooksConfig.GetFileKeyValue(lines, "AllRegions");
        string allRegionsIllumPath = XwaHooksConfig.GetFileKeyValue(lines, "AllRegionsIllum", allRegionsPath.Length > 0 ? allRegionsPath + illumStr : "");
        string allRegionsOvrPath = XwaHooksConfig.GetFileKeyValue(lines, "AllRegionsOverlay", allRegionsPath.Length > 0 ? allRegionsPath + ovrStr : "");

        string[] regionPath = new string[4];
        string[] regionIllumPath = new string[4];
        string[] regionOvrPath = new string[4];

        regionPath[0] = XwaHooksConfig.GetFileKeyValue(lines, "Region0");
        regionPath[1] = XwaHooksConfig.GetFileKeyValue(lines, "Region1");
        regionPath[2] = XwaHooksConfig.GetFileKeyValue(lines, "Region2");
        regionPath[3] = XwaHooksConfig.GetFileKeyValue(lines, "Region3");

        regionIllumPath[0] = XwaHooksConfig.GetFileKeyValue(lines, "Region0Illum", regionPath[0].Length > 0 ? regionPath[0] + illumStr : "");
        regionIllumPath[1] = XwaHooksConfig.GetFileKeyValue(lines, "Region1Illum", regionPath[1].Length > 0 ? regionPath[1] + illumStr : "");
        regionIllumPath[2] = XwaHooksConfig.GetFileKeyValue(lines, "Region2Illum", regionPath[2].Length > 0 ? regionPath[2] + illumStr : "");
        regionIllumPath[3] = XwaHooksConfig.GetFileKeyValue(lines, "Region3Illum", regionPath[3].Length > 0 ? regionPath[3] + illumStr : "");

        regionOvrPath[0] = XwaHooksConfig.GetFileKeyValue(lines, "Region0Overlay", regionPath[0].Length > 0 ? regionPath[0] + ovrStr : "");
        regionOvrPath[1] = XwaHooksConfig.GetFileKeyValue(lines, "Region1Overlay", regionPath[1].Length > 0 ? regionPath[1] + ovrStr : "");
        regionOvrPath[2] = XwaHooksConfig.GetFileKeyValue(lines, "Region2Overlay", regionPath[2].Length > 0 ? regionPath[2] + ovrStr : "");
        regionOvrPath[3] = XwaHooksConfig.GetFileKeyValue(lines, "Region3Overlay", regionPath[3].Length > 0 ? regionPath[3] + ovrStr : "");

        ParseCubeMapMissionIni(lines);

        if (allRegionsPath.Length > 0)
        {
            g_CubeMaps.bRenderAllRegions = LoadCubeMap(device, context, allRegionsPath,
                out allRegionsCubeTexture, out g_CubeMaps.allRegionsSRV);
        }

        if (allRegionsIllumPath.Length > 0)
        {
            g_CubeMaps.bAllRegionsIllum = LoadCubeMap(device, context, allRegionsIllumPath,
                out allRegionsIllumCubeTexture, out g_CubeMaps.allRegionsIllumSRV);
        }

        if (allRegionsOvrPath.Length > 0)
        {
            g_CubeMaps.bAllRegionsOvr = LoadCubeMap(device, context, allRegionsOvrPath,
                out allRegionsOvrCubeTexture, out g_CubeMaps.allRegionsOvrSRV);
        }

        for (int i = 0; i < CubeMapData.MaxMissionRegions; i++)
        {
            if (regionPath[i].Length > 0)
            {
                g_CubeMaps.bRenderInThisRegion[i] = LoadCubeMap(device, context, regionPath[i],
                    out cubeTextures[i], out g_CubeMaps.regionSRV[i]);
            }

            if (regionIllumPath[i].Length > 0)
            {
                g_CubeMaps.bRenderIllumInThisRegion[i] = LoadCubeMap(device, context, regionIllumPath[i],
                    out cubeTexturesIllum[i], out g_CubeMaps.regionIllumSRV[i]);
            }

            if (regionIllumPath[i].Length > 0)
            {
                g_CubeMaps.bRenderOvrInThisRegion[i] = LoadCubeMap(device, context, regionOvrPath[i],
                    out cubeTexturesOvr[i], out g_CubeMaps.regionOvrSRV[i]);
            }
        }

        ReloadCubeMapsResources(deviceResources);
    }

    private void ReloadCubeMapsResources(DeviceResources deviceResources)
    {
        D3D11Device device = deviceResources.D3DDevice;
        D3D11DeviceContext context = deviceResources.D3DContext;

        // Constant buffer
        var constantBufferDesc = new D3D11BufferDesc(CubeMapsConstantBufferData.Size, D3D11BindOptions.ConstantBuffer);
        g_cubeMapsData.s_constantBuffer = deviceResources.D3DDevice.CreateBuffer(constantBufferDesc);

        // Rasterizer state
        D3D11RasterizerDesc rsDesc = default;
        rsDesc.FillMode = D3D11FillMode.Solid;
        rsDesc.CullMode = D3D11CullMode.Front;
        rsDesc.IsFrontCounterClockwise = false;
        rsDesc.DepthBias = 0;
        rsDesc.DepthBiasClamp = 0.0f;
        rsDesc.SlopeScaledDepthBias = 0.0f;
        rsDesc.IsDepthClipEnabled = false;
        rsDesc.IsScissorEnabled = false;
        rsDesc.IsMultisampleEnabled = true;
        rsDesc.IsAntialiasedLineEnabled = false;
        g_cubeMapsData.s_rasterizerState = device.CreateRasterizerState(rsDesc);

        // Sampler state
        D3D11SamplerDesc samplerDesc = default;
        samplerDesc.Filter = D3D11Filter.Anisotropic;
        samplerDesc.MaxAnisotropy = deviceResources.D3DFeatureLevel > D3D11FeatureLevel.FeatureLevel91 ? D3D11Constants.DefaultMaxAnisotropy : D3D11Constants.FeatureLevel91DefaultMaxAnisotropy;
        samplerDesc.AddressU = D3D11TextureAddressMode.Mirror;
        samplerDesc.AddressV = D3D11TextureAddressMode.Mirror;
        samplerDesc.AddressW = D3D11TextureAddressMode.Mirror;
        samplerDesc.MipLodBias = 0.0f;
        samplerDesc.MinLod = 0;
        samplerDesc.MaxLod = float.MaxValue;
        samplerDesc.ComparisonFunction = D3D11ComparisonFunction.Never;
        samplerDesc.BorderColorR = 0.0f;
        samplerDesc.BorderColorG = 0.0f;
        samplerDesc.BorderColorB = 0.0f;
        samplerDesc.BorderColorA = 0.0f;
        g_cubeMapsData.s_samplerState = device.CreateSamplerState(samplerDesc);

        // Blend state
        D3D11BlendDesc blendDesc = D3D11BlendDesc.Default;
        blendDesc.IsAlphaToCoverageEnabled = false;
        blendDesc.IsIndependentBlendEnabled = false;
        var blendDescRenderTargets = blendDesc.GetRenderTargets();
        blendDescRenderTargets[0].IsBlendEnabled = false;
        blendDescRenderTargets[0].SourceBlend = D3D11BlendValue.One;
        blendDescRenderTargets[0].DestinationBlend = D3D11BlendValue.Zero;
        blendDescRenderTargets[0].BlendOperation = D3D11BlendOperation.Add;
        blendDescRenderTargets[0].SourceBlendAlpha = D3D11BlendValue.One;
        blendDescRenderTargets[0].DestinationBlendAlpha = D3D11BlendValue.Zero;
        blendDescRenderTargets[0].BlendOperationAlpha = D3D11BlendOperation.Add;
        blendDescRenderTargets[0].RenderTargetWriteMask = D3D11ColorWriteEnables.All;
        blendDesc.SetRenderTargets(blendDescRenderTargets);
        g_cubeMapsData.s_blendState = device.CreateBlendState(blendDesc);

        // Depth stencil state
        D3D11DepthStencilDesc depthDesc = default;
        depthDesc.IsDepthEnabled = false;
        depthDesc.DepthWriteMask = D3D11DepthWriteMask.Zero;
        depthDesc.DepthFunction = D3D11ComparisonFunction.Always;
        depthDesc.IsStencilEnabled = false;
        g_cubeMapsData.s_depthState = device.CreateDepthStencilState(depthDesc);

        var loader = new BasicLoader(deviceResources.D3DDevice);

        // Input layout
        D3D11InputElementDesc[] vertexLayoutDesc = new D3D11InputElementDesc[]
        {
            new()
            {
                SemanticName = "POSITION",
                SemanticIndex = 0,
                Format = DxgiFormat.R32G32B32Float,
                InputSlot = 0,
                AlignedByteOffset = 0,
                InputSlotClass = D3D11InputClassification.PerVertexData,
                InstanceDataStepRate = 0
            }
        };

        // Vertex shader
        loader.LoadShader("XwaMissionBackdropsPreview_Shaders\\CubeMapsVertexShader.cso", vertexLayoutDesc, out g_cubeMapsData.s_vertexShader, out g_cubeMapsData.s_inputLayout);

        // Pixel shader
        loader.LoadShader("XwaMissionBackdropsPreview_Shaders\\CubeMapsPixelShader.cso", out g_cubeMapsData.s_pixelShader);

        // Vertex buffer
        XMFloat4[] vertices = new XMFloat4[]
        {
            new(-1.0f, 1.0f, -1.0f, 1.0f),
            new(1.0f, 1.0f, -1.0f, 1.0f),
            new(1.0f, 1.0f, 1.0f, 1.0f),
            new (-1.0f, 1.0f, 1.0f, 1.0f),
            new (-1.0f, -1.0f, -1.0f, 1.0f),
            new(1.0f, -1.0f, -1.0f, 1.0f),
            new(1.0f, -1.0f, 1.0f, 1.0f),
            new(-1.0f, -1.0f, 1.0f, 1.0f),
        };

        D3D11BufferDesc vertexBufferDesc = D3D11BufferDesc.From(vertices, D3D11BindOptions.VertexBuffer);
        g_cubeMapsData.s_vertexBuffer = device.CreateBuffer(vertexBufferDesc, vertices, 0, 0);

        // Index buffer
        ushort[] indices = new ushort[]
        {
            3, 1, 0,
            2, 1, 3,
            0, 5, 4,
            1, 5, 0,
            3, 4, 7,
            0, 4, 3,
            1, 6, 5,
            2, 6, 1,
            2, 7, 6,
            3, 7, 2,
            6, 4, 5,
            7, 4, 6,
        };

        D3D11BufferDesc indexBufferDesc = D3D11BufferDesc.From(indices, D3D11BindOptions.IndexBuffer);
        g_cubeMapsData.s_indexBuffer = device.CreateBuffer(indexBufferDesc, indices, 0, 0);
    }

    public void RenderDefaultBackground(DeviceResources deviceResources, int region, in XMMatrix world, in XMMatrix view, in XMMatrix projection)
    {
        g_cubeMapsData.s_region = g_CubeMaps.GetCurrentCubeMapRegion(region);

        g_cubeMapsData.world = world;
        g_cubeMapsData.view = view;
        g_cubeMapsData.projection = projection;

        RenderDefaultBackground(g_cubeMapsData.s_region, out g_cubeMapsData.s_drawCubeMap, out g_cubeMapsData.s_cubeMapRot, out g_cubeMapsData.s_drawOvrCubeMap, out g_cubeMapsData.s_ovrCubeMapRot);
        RenderCubeMaps(deviceResources);
    }

    private void RenderCubeMaps(DeviceResources deviceResources)
    {
        D3D11Device device = deviceResources.D3DDevice;
        D3D11DeviceContext context = deviceResources.D3DContext;

        //D3D11Buffer[] oldVSConstantBuffer = context.VertexShaderGetConstantBuffers(0, 1);
        //D3D11Buffer[] oldPSConstantBuffer = context.PixelShaderGetConstantBuffers(0, 1);
        //D3D11ShaderResourceView[] oldVSSRV = context.VertexShaderGetShaderResources(0, 3);

        context.VertexShaderSetConstantBuffers(0, new[] { g_cubeMapsData.s_constantBuffer });
        context.PixelShaderSetConstantBuffers(0, new[] { g_cubeMapsData.s_constantBuffer });
        context.RasterizerStageSetState(g_cubeMapsData.s_rasterizerState);
        context.PixelShaderSetSamplers(0, new[] { g_cubeMapsData.s_samplerState });
        context.OutputMergerSetBlendState(g_cubeMapsData.s_blendState, new float[] { 0.0f, 0.0f, 0.0f, 0.0f }, 0xFFFFFFFF);
        context.OutputMergerSetDepthStencilState(g_cubeMapsData.s_depthState, 0);

        context.InputAssemblerSetPrimitiveTopology(D3D11PrimitiveTopology.TriangleList);
        context.InputAssemblerSetInputLayout(g_cubeMapsData.s_inputLayout);
        context.VertexShaderSetShader(g_cubeMapsData.s_vertexShader, null);
        context.PixelShaderSetShader(g_cubeMapsData.s_pixelShader, null);

        context.InputAssemblerSetVertexBuffers(
            0,
            new[] { g_cubeMapsData.s_vertexBuffer },
            new uint[] { (uint)Marshal.SizeOf<XMFloat4>() },
            new uint[] { 0 });

        context.InputAssemblerSetIndexBuffer(g_cubeMapsData.s_indexBuffer, DxgiFormat.R16UInt, 0);

        CubeMapsConstantBufferData constants = default;
        constants.World = g_cubeMapsData.world.Transpose();
        constants.View = g_cubeMapsData.view.Transpose();
        constants.Projection = g_cubeMapsData.projection.Transpose();

        if (g_cubeMapsData.s_drawCubeMap)
        {
            constants.World = (g_cubeMapsData.s_cubeMapRot * g_cubeMapsData.world).Transpose();
            context.UpdateSubresource(g_cubeMapsData.s_constantBuffer, 0, null, constants, 0, 0);

            if (g_cubeMapsData.s_region != -1)
            {
                context.PixelShaderSetShaderResources(0, new[] { g_CubeMaps.regionSRV[g_cubeMapsData.s_region] });
            }
            else if (g_CubeMaps.bRenderAllRegions)
            {
                context.PixelShaderSetShaderResources(0, new[] { g_CubeMaps.allRegionsSRV });
            }
            else
            {
                context.PixelShaderSetShaderResources(0, null);
            }

            context.DrawIndexed(36, 0, 0);
        }

        if (g_cubeMapsData.s_drawOvrCubeMap)
        {
            constants.World = (g_cubeMapsData.s_ovrCubeMapRot * g_cubeMapsData.world).Transpose();
            context.UpdateSubresource(g_cubeMapsData.s_constantBuffer, 0, null, constants, 0, 0);

            if (g_cubeMapsData.s_region != -1)
            {
                context.PixelShaderSetShaderResources(0, new[] { g_CubeMaps.regionOvrSRV[g_cubeMapsData.s_region] });
            }
            else if (g_CubeMaps.bRenderAllRegions)
            {
                context.PixelShaderSetShaderResources(0, new[] { g_CubeMaps.allRegionsOvrSRV });
            }
            else
            {
                context.PixelShaderSetShaderResources(0, null);
            }

            context.DrawIndexed(36, 0, 0);
        }

        //context.VertexShaderSetConstantBuffers(0, oldVSConstantBuffer);
        //context.PixelShaderSetConstantBuffers(0, oldPSConstantBuffer);
        //context.VertexShaderSetShaderResources(0, oldVSSRV);
    }
}
