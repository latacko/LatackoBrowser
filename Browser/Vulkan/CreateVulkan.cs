using Silk.NET.Core;
using Silk.NET.Core.Native;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.EXT;
namespace Vulkan;

public unsafe class CreateVulkan : IDisposable
{
    internal static Vk vk { get; private set; }
    internal static Instance vulkanInstance;

    private ExtDebugUtils? debugUtils;
    DebugUtilsMessengerEXT debugMessenger;

#if DEBUG
    public const bool ENABLE_VALIDATION_LAYERS = true;
#else
    public const bool ENABLE_VALIDATION_LAYERS = false;
#endif

    internal static readonly string[] ValidationLayers =
    [
        "VK_LAYER_KHRONOS_validation",
    ];

    public void Create(byte** requiredExtensions, uint count)
    {
        vk = Vk.GetApi();

        CreateInstance(requiredExtensions, count);
        SetUpDebugMessenger();
    }
    bool CheckValidationLayerSupport()
    {
        uint _layerCount = 0;
        vk.EnumerateInstanceLayerProperties(&_layerCount, null);

        LayerProperties[] _availableLayers = new LayerProperties[_layerCount];
        fixed (LayerProperties* layersPtr = _availableLayers)
        {
            vk.EnumerateInstanceLayerProperties(&_layerCount, _availableLayers);
        }

        var availableLayerNames = _availableLayers.Select(layer => SilkMarshal.PtrToString((IntPtr)layer.LayerName)).ToHashSet();

        return ValidationLayers.All(availableLayerNames.Contains);
    }
    void CreateInstance(byte** requiredExtensions, uint count)
    {
        if (ENABLE_VALIDATION_LAYERS && !CheckValidationLayerSupport())
        {
            throw new System.Exception("validation layers requested, but not available!");
        }

        ApplicationInfo applicationInfo = new()
        {
            SType = StructureType.ApplicationInfo,
            PApplicationName = (byte*)SilkMarshal.StringToPtr("Latacko Browser"),
            ApplicationVersion = new Version32(0, 1, 0),
            PEngineName = (byte*)SilkMarshal.StringToPtr("Latacko Engine"),
            EngineVersion = new Version32(0, 1, 0),
            ApiVersion = Vk.Version13,
        };

        InstanceCreateInfo createInfo = new()
        {
            SType = StructureType.InstanceCreateInfo,
            PApplicationInfo = &applicationInfo,
        };

        if (ENABLE_VALIDATION_LAYERS)
        {
            createInfo.EnabledLayerCount = (uint)ValidationLayers.Length;
            createInfo.PpEnabledLayerNames = (byte**)SilkMarshal.StringArrayToPtr(ValidationLayers);

            var _debugCreateInfo = PopulateDebugMessengerCreateInfo();
            createInfo.PNext = &_debugCreateInfo;
        }
        

        var _requiredExtensions = GetRequiredExtensions(requiredExtensions, count);
        createInfo.EnabledExtensionCount = (uint)_requiredExtensions.Length;
        createInfo.PpEnabledExtensionNames = (byte**)SilkMarshal.StringArrayToPtr(_requiredExtensions);


        var _result = vk.CreateInstance(ref createInfo, null, out vulkanInstance);

        if (_result != Result.Success)
        {
            throw new System.Exception("Coudn't initialize vulkan instance. Error: " + _result);
        }

        SilkMarshal.FreeString((nint)applicationInfo.PApplicationName);
        SilkMarshal.FreeString((nint)applicationInfo.PEngineName);
        SilkMarshal.Free((nint)createInfo.PpEnabledExtensionNames);
    }

    string[] GetRequiredExtensions(byte** requiredExtensions, uint count)
    {
        var extensions = SilkMarshal.PtrToStringArray((nint)requiredExtensions, (int)count);

        if (ENABLE_VALIDATION_LAYERS)
        {
            return extensions.Append(ExtDebugUtils.ExtensionName).ToArray();
        }

        return extensions;
    }

    #region Debug Manager

    internal static DebugUtilsMessengerCreateInfoEXT PopulateDebugMessengerCreateInfo()
    {
        return new DebugUtilsMessengerCreateInfoEXT()
        {
            SType = StructureType.DebugUtilsMessengerCreateInfoExt,
            MessageSeverity = DebugUtilsMessageSeverityFlagsEXT.VerboseBitExt | DebugUtilsMessageSeverityFlagsEXT.WarningBitExt | DebugUtilsMessageSeverityFlagsEXT.ErrorBitExt,
            MessageType = DebugUtilsMessageTypeFlagsEXT.GeneralBitExt | DebugUtilsMessageTypeFlagsEXT.ValidationBitExt | DebugUtilsMessageTypeFlagsEXT.PerformanceBitExt,
            PfnUserCallback = (DebugUtilsMessengerCallbackFunctionEXT)DebugCallback,
            PUserData = (void*)IntPtr.Zero
        };
    }
    private void SetUpDebugMessenger()
    {
        if (!ENABLE_VALIDATION_LAYERS) return;

        if (!vk!.TryGetInstanceExtension(vulkanInstance, out debugUtils)) throw new System.Exception("failed to get debug utils");

        var _createInfo = PopulateDebugMessengerCreateInfo();

        if (debugUtils!.CreateDebugUtilsMessenger(vulkanInstance, in _createInfo, null, out debugMessenger) != Result.Success)
        {
            throw new Exception("failed to set up debug messenger!");
        }
    }

    private static uint DebugCallback(DebugUtilsMessageSeverityFlagsEXT messageSeverity, DebugUtilsMessageTypeFlagsEXT messageTypes, DebugUtilsMessengerCallbackDataEXT* pCallbackData, void* pUserData)
    {
        if (messageSeverity < DebugUtilsMessageSeverityFlagsEXT.WarningBitExt)
        {
            return Vk.False;
        }
        Console.WriteLine($"validation layer:" + SilkMarshal.PtrToString((nint)pCallbackData->PMessage));

        return Vk.False;
    }

    #endregion

    public void Dispose()
    {

        if (ENABLE_VALIDATION_LAYERS)
        {
            //DestroyDebugUtilsMessenger equivilant to method DestroyDebugUtilsMessengerEXT from original tutorial.
            debugUtils!.DestroyDebugUtilsMessenger(vulkanInstance, debugMessenger, null);
        }
        vk.DestroyInstance(vulkanInstance, null);
    }
}