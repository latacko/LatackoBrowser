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
        CreateVulkan();
        MainLoop();
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
    private void OnUpdate(double deltaTime)
    {
        // runtimeModelData.SetBackgroundColor(0, 0, (float)(runtimeModelData.BackgroundColor.Z + deltaTime) % 1, 1);

        // runtimeModelData.SetRotation((float)(runtimeModelData.RotationX + deltaTime * speed), 0, 0);
    }

    private void OnFramebufferResize(Vector2D<int> newSize)
    {
        framebufferResized = true;
    }

    private void OnRender(double deltaTime)
    {
        vk.WaitForFences(device, 1, in inFlightFences[currentFrame], Vk.True, ulong.MaxValue);


        uint imageIndex;
        var _result = khrSwapChain!.AcquireNextImage(device, swapChain, ulong.MaxValue, imageAvailableSemaphores[currentFrame], default, &imageIndex);

        if (_result == Result.ErrorOutOfDateKhr)
        {
            RecreateSwapChain();
            return;
        }
        else if (_result != Result.Success && _result != Result.SuboptimalKhr)
            throw new Exception("Failed to acquire swap chain image!");

        vk.ResetFences(device, 1, in inFlightFences[currentFrame]);

        vk.ResetCommandBuffer(commandBuffers[currentFrame], 0);

        RecordCommandBuffer(commandBuffers[currentFrame], imageIndex);


        UpdateUniformBuffer(currentFrame);

        // UpdateUniformBufferPerspective(currentFrame);

        SubmitInfo submitInfo = new()
        {
            SType = StructureType.SubmitInfo
        };

        Semaphore[] waitSemaphores = [imageAvailableSemaphores[currentFrame]];
        Semaphore[] signalSemaphores = [renderFinishedSemaphores[currentFrame]];
        PipelineStageFlags[] waitStages = [PipelineStageFlags.ColorAttachmentOutputBit];
        submitInfo.WaitSemaphoreCount = 1;
        SwapchainKHR[] swapChains = [swapChain];

        fixed (Semaphore* waitSemaphoresPtr = waitSemaphores)
        fixed (Semaphore* signalSemaphoresPtr = signalSemaphores)
        fixed (PipelineStageFlags* waitStagesPtr = waitStages)
        fixed (CommandBuffer* cmdBufferPtr = &commandBuffers[currentFrame])
        fixed (SwapchainKHR* swapChainsPtr = swapChains)
        {
            submitInfo.PWaitSemaphores = waitSemaphoresPtr;
            submitInfo.PWaitDstStageMask = waitStagesPtr;

            submitInfo.CommandBufferCount = 1;
            submitInfo.PCommandBuffers = cmdBufferPtr;

            submitInfo.SignalSemaphoreCount = 1;
            submitInfo.PSignalSemaphores = signalSemaphoresPtr;

            if (vk.QueueSubmit(graphicsQueue, 1, &submitInfo, inFlightFences[currentFrame]) != Result.Success)
            {
                throw new Exception("Failed to submit command buffer!");
            }


            PresentInfoKHR presentInfo = new()
            {
                SType = StructureType.PresentInfoKhr,

                WaitSemaphoreCount = 1,
                PWaitSemaphores = signalSemaphoresPtr,

                SwapchainCount = 1,
                PSwapchains = swapChainsPtr,
                PImageIndices = &imageIndex
            };

            _result = khrSwapChain.QueuePresent(presentQueue, &presentInfo);

            if (_result == Result.ErrorOutOfDateKhr || _result == Result.SuboptimalKhr || framebufferResized)
            {
                framebufferResized = false;
                RecreateSwapChain();
            }
            else if (_result != Result.Success)
            {
                throw new Exception("failed to present swap chain image!");
            }

            currentFrame = (currentFrame + 1) % MAX_FRAMES_IN_FLIGHT;
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

    void UpdateUniformBuffer(uint currentImage)
    {
        var time = (float)window!.Time;

        UICameraUBO ubo = new()
        {
            Proj = Matrix4X4.CreateOrthographicOffCenter<float>(0, swapChainExtent.Width, 0, swapChainExtent.Height, -1000, 1000),
        };

        cameraBuffer.Update(currentImage, ubo.Proj);


        static float Radians(float angle) => angle * MathF.PI / 180f;
    }

    void UpdateUniformBufferPerspective(uint currentImage)
    {
        var time = (float)window!.Time;

        UniformBufferObject ubo = new()
        {
            Model = Matrix4X4<float>.Identity * Matrix4X4.CreateFromAxisAngle<float>(new Vector3D<float>(0, 0, 1), time * Radians(90.0f)),
            View = Matrix4X4.CreateLookAt(new Vector3D<float>(2, 2, 2), new Vector3D<float>(0, 0, 0), new Vector3D<float>(0, 0, 1)),
            Proj = Matrix4X4.CreatePerspectiveFieldOfView(Radians(45.0f), (float)swapChainExtent.Width / swapChainExtent.Height, 0.1f, 10.0f),
        };
        ubo.Proj.M22 *= -1;

        void* data;
        vk!.MapMemory(device, CameraBuffer.Instance._uniformMemory[currentImage], 0, (ulong)Unsafe.SizeOf<UICameraUBO>(), 0, &data);
        new Span<UniformBufferObject>(data, 1)[0] = ubo;
        vk!.UnmapMemory(device, CameraBuffer.Instance._uniformMemory[currentImage]);

        static float Radians(float angle) => angle * MathF.PI / 180f;
    }

    void MainLoop()
    {
        window.Run();

        vk.DeviceWaitIdle(device);
    }

    void CleanUp()
    {
        CleanUpVulcan();
        window.Dispose();
    }
}