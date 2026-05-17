using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Browser.DataTypes;
using Silk.NET.Core;
using Silk.NET.Core.Native;
using Silk.NET.Maths;
using Silk.NET.Input;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.EXT;
using Silk.NET.Vulkan.Extensions.KHR;
using Silk.NET.Windowing;
using Semaphore = Silk.NET.Vulkan.Semaphore;
using System.Numerics;
using Silk.NET.Input.Sdl;

namespace Browser;

public unsafe partial class BrowserWindow
{
    public static BrowserWindow Instance;
    private static IWindow window;
    private IInputContext inputContext;

    private const int WIDTH = 800;
    private const int HEIGHT = 800;

    uint currentFrame = 0;

    private double _fpsTimer = 0;
    private int _frameCount = 0;

    private BrowserUI browserUI = new();

    public Action OnStart;

    public BrowserWindow()
    {
        Instance = this;
    }

    public void Run()
    {
        CreateWindow();
        Console.WriteLine("Creating vulkan");
        CreateVulkan();
        Console.WriteLine("Created vulkan");
        MainLoop();
        Console.WriteLine("Ended main loop");
        CleanUp();
    }

    void CreateWindow()
    {
        Window.PrioritizeSdl();
        Silk.NET.Windowing.Sdl.SdlWindowing.Use();
        SdlInput.RegisterPlatform();

        var options = WindowOptions.DefaultVulkan;
        // options.WindowBorder = WindowBorder.Hidden;
        options.Size = new Vector2D<int>(WIDTH, HEIGHT);
        options.Title = "LearnOpenGL with Silk.NET";
        options.UpdatesPerSecond = 0;
        options.FramesPerSecond = 0;

        window = Window.Create(options);
        CursorManager.Init(Silk.NET.SDL.Sdl.GetApi());


        window.Initialize();
        inputContext = window.CreateInput();
        foreach (var keyboard in inputContext.Keyboards)
        {
            keyboard.KeyDown += OnKeyDown;
            keyboard.KeyUp += OnKeyUp;
        }

        foreach (var mouse in inputContext.Mice)
        {
            mouse.MouseDown += OnMouseDown;
            mouse.Click += OnMouseClick;
            mouse.MouseMove += OnMouseMove;
            mouse.Scroll += OnMouseScroll;
        }
        Console.WriteLine($"Windowing backend: {window.GetType().Name}");

        if (window.VkSurface is null)
        {
            throw new Exception("Windowing platform doesn't support Vulkan.");
        }

        //Assign events.
        OnStart += Start;
        window.Update += OnUpdate;
        window.Render += OnRender;
        window.FramebufferResize += OnFramebufferResize;
    }

    private void Start()
    {
        browserUI.Create();
        Console.WriteLine("Create model on load");
    }

    float speed = 0.2f;
    float DegToRad(float deg) => deg * (MathF.PI / 180f);
    float timeFromStart = 0f;
    private void OnUpdate(double deltaTime)
    {
        timeFromStart += (float)deltaTime;
        ColorTransitionsHelper.Update((float)deltaTime);

        browserUI.TopBar.SetLayout(browserUI.TopBar.Layout.SetTop(browserUI.TopBar.CreateUnit(100 + MathF.Sin(timeFromStart)*100, Units.UnitType.px)));
        // browserUI.LeftBar?.SetTransform(browserUI.LeftBar.Transform.SetRotationZ(browserUI.LeftBar.Transform.Rotation.Z + (float)deltaTime));
        // browserUI.BottomBar?.SetTransform(browserUI.BottomBar.Transform.SetRotationZ(browserUI.BottomBar.Transform.Rotation.Z + (float)deltaTime));
        // runtimeModelData.SetBackgroundColor(0, 0, (float)(runtimeModelData.BackgroundColor.Z + deltaTime) % 1, 1);

        // runtimeModelData.SetRotation((float)(runtimeModelData.RotationX + deltaTime * speed), 0, 0);
    }

    private void OnFramebufferResize(Vector2D<int> newSize)
    {
        framebufferResized = true;
    }

    private void OnRender(double deltaTime)
    {
        Vulkan.CreateVulkan.vk.WaitForFences(Vulkan.LogicalDevice.device, 1, in vulkanManager.fences[currentFrame], Vk.True, ulong.MaxValue);


        uint imageIndex;
        var _result = swapchain.khrSwapChain!.AcquireNextImage(Vulkan.LogicalDevice.device, swapchain.swapChain, ulong.MaxValue, vulkanManager.imageAcquiredSemaphores[currentFrame], default, &imageIndex);

        if (_result == Result.ErrorOutOfDateKhr)
        {
            swapchain.RecreateSwapChain(GetFrameBufferSize, OnWindowMinimized);
            return;
        }
        else if (_result != Result.Success && _result != Result.SuboptimalKhr)
            throw new Exception("Failed to acquire swap chain image!");

        Vulkan.CreateVulkan.vk.ResetFences(Vulkan.LogicalDevice.device, 1, in vulkanManager.fences[currentFrame]);

        Vulkan.CreateVulkan.vk.ResetCommandBuffer(vulkanManager.commandBuffers[currentFrame], 0);

        RecordCommandBuffer(vulkanManager.commandBuffers[currentFrame], imageIndex);


        UpdateUniformBuffer(currentFrame);

        // UpdateUniformBufferPerspective(currentFrame);

        SubmitInfo submitInfo = new()
        {
            SType = StructureType.SubmitInfo,
        };

        PipelineStageFlags waitStages = PipelineStageFlags.ColorAttachmentOutputBit;

        fixed (Semaphore* waitSemaphoresPtr = &vulkanManager.imageAcquiredSemaphores[currentFrame])
        fixed (CommandBuffer* commandBufferPtr = &vulkanManager.commandBuffers[currentFrame])
        fixed (Semaphore* renderCompleteSemaphoresPtr = &vulkanManager.renderCompleteSemaphores[imageIndex])
        fixed (SwapchainKHR* swapChainPtr = &swapchain.swapChain)
        {
            submitInfo.WaitSemaphoreCount = 1;
            submitInfo.PWaitSemaphores = waitSemaphoresPtr;

            submitInfo.PWaitDstStageMask = &waitStages;

            submitInfo.CommandBufferCount = 1;
            submitInfo.PCommandBuffers = commandBufferPtr;

            submitInfo.SignalSemaphoreCount = 1;
            submitInfo.PSignalSemaphores = renderCompleteSemaphoresPtr;

            if (Vulkan.CreateVulkan.vk.QueueSubmit(Vulkan.LogicalDevice.graphicsQueue, 1, &submitInfo, vulkanManager.fences[currentFrame]) != Result.Success)
            {
                throw new Exception("Failed to submit command buffer!");
            }

            currentFrame = (currentFrame + 1) % Vulkan.VulkanManager.MAX_FRAMES_IN_FLIGHT;


            PresentInfoKHR presentInfo = new()
            {
                SType = StructureType.PresentInfoKhr,

                WaitSemaphoreCount = 1,
                PWaitSemaphores = renderCompleteSemaphoresPtr,

                SwapchainCount = 1,
                PSwapchains = swapChainPtr,
                PImageIndices = &imageIndex
            };

            _result = swapchain.khrSwapChain.QueuePresent(Vulkan.LogicalDevice.presentQueue, &presentInfo);

            if (_result == Result.ErrorOutOfDateKhr || _result == Result.SuboptimalKhr || framebufferResized)
            {
                framebufferResized = false;
                swapchain.RecreateSwapChain(GetFrameBufferSize, OnWindowMinimized);
            }
            else if (_result != Result.Success)
            {
                throw new Exception("failed to present swap chain image!");
            }

        }

        _frameCount++;
        _fpsTimer += deltaTime;

        if (_fpsTimer >= 1.0) // every second
        {
            window.Title = $"FPS: {_frameCount}";
            _frameCount = 0;
            _fpsTimer = 0;
        }
    }

    Vector2D<int> GetFrameBufferSize()
    {
        return window.FramebufferSize;
    }

    void OnWindowMinimized()
    {
        window.DoEvents();
    }

    void UpdateUniformBuffer(uint currentImage)
    {
        var time = (float)window!.Time;

        Vulkan.UICameraUBO ubo = new()
        {
            Proj = Matrix4X4.CreateOrthographicOffCenter<float>(0, swapchain.swapChainExtent.Width, 0, swapchain.swapChainExtent.Height, -1000, 1000),
        };

        vulkanManager.UpdateCameraBuffer(currentImage, ubo.Proj);
    }

    void UpdateUniformBufferPerspective(uint currentImage)
    {
        var time = (float)window!.Time;

        Vulkan.UICameraUBO ubo = new()
        {
            Proj = Matrix4X4.CreatePerspectiveFieldOfView(Radians(45.0f), (float)swapchain.swapChainExtent.Width / swapchain.swapChainExtent.Height, 0.1f, 10.0f),
        };
        ubo.Proj.M22 *= -1;

        vulkanManager.UpdateCameraBuffer(currentImage, ubo.Proj);

        static float Radians(float angle) => angle * MathF.PI / 180f;
    }

    void MainLoop()
    {
        window.Run();

        Vulkan.CreateVulkan.vk.DeviceWaitIdle(Vulkan.LogicalDevice.device);
    }

    void CleanUp()
    {
        CleanUpVulcan();
        window.Dispose();
    }
}