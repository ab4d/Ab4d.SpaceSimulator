using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.Vulkan;
using System;
using Ab4d.SharpEngine.Core;
using Ab4d.SharpEngine.Utilities;

namespace Ab4d.SpaceSimulator.Visualization;

public class PlanetTextureLoader
{
    public static Action<string, VulkanDevice, StandardMaterialBase>? CustomAsyncTextureLoader;

    private readonly VulkanDevice _gpuDevice;

    public PlanetTextureLoader(VulkanDevice gpuDevice)
    {
        _gpuDevice = gpuDevice;
    }

    public void LoadPlanetTextureAsync(string textureFileName, StandardMaterialBase planetMaterial)
    {
        if (CustomAsyncTextureLoader != null)
        {
            CustomAsyncTextureLoader(textureFileName, _gpuDevice, planetMaterial);
        }
        else
        {
            if (!System.IO.Path.Exists(textureFileName))
                return;

            TextureLoader.CreateTextureAsync(textureFileName, _gpuDevice, gpuImage =>
            {
                planetMaterial.DiffuseTexture = gpuImage;
                planetMaterial.DiffuseColor = Colors.White; // no texture filter color
            });
        }
    }
}