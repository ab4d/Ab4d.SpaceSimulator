using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.Vulkan;
using System;
using System.IO;
using Ab4d.SharpEngine.Core;
using Ab4d.SharpEngine.Utilities;
using Avalonia.Platform;

namespace Ab4d.SpaceSimulator.Visualization;

public class PlanetTextureLoader
{
    public static Action<string, VulkanDevice, StandardMaterialBase>? CustomAsyncTextureLoader;

    private readonly VulkanDevice _gpuDevice;

    private readonly string _assetsPath;

    public PlanetTextureLoader(VulkanDevice gpuDevice)
    {
        _gpuDevice = gpuDevice;
        _assetsPath = $"avares://{typeof(PlanetTextureLoader).Assembly.GetName().Name}/Assets/";
    }

    public void LoadPlanetTextureAsync(string textureFileName, StandardMaterialBase planetMaterial)
    {
        if (CustomAsyncTextureLoader != null)
        {
            CustomAsyncTextureLoader(textureFileName, _gpuDevice, planetMaterial);
        }
        else
        {
            try
            {
                var bitmapStream = AssetLoader.Open(new Uri(_assetsPath + textureFileName));
                
                if (bitmapStream != null)
                {
                    var gpuImage = TextureLoader.CreateTexture(bitmapStream, textureFileName, _gpuDevice);
                    planetMaterial.DiffuseTexture = gpuImage;
                    planetMaterial.DiffuseColor = Colors.White; // no texture filter color
                        
                    bitmapStream.Close();
                }
            }
            catch (FileNotFoundException)
            {
                // pass
            }
        }
    }
}