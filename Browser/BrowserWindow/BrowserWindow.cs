using Silk.NET.Maths;
using Silk.NET.Input;
using Silk.NET.Vulkan;
using Silk.NET.Windowing;
using Semaphore = Silk.NET.Vulkan.Semaphore;
using Silk.NET.Input.Sdl;
using PrimitiveCore;
using GraphicsCore.Styles;
using System.Diagnostics;
using Vulkan;
using VulkanManager;

namespace Browser;

public unsafe partial class BrowserWindow
{
    internal IWindow window;
    private IInputContext inputContext;

    private const int WIDTH = 800;
    private const int HEIGHT = 800;

    uint currentFrame = 0;

    public Action OnStart;

    TextureRenderer textureRenderer;

    public BrowserWindow()
    {
    }

    public void Run(TextureRenderer textureRenderer)
    {
        textureRenderer.CreateVulkanEngine();
        CreateCommandBuffers(textureRenderer.GetVulkanEngine());
        SetupVulkan();
        CreateWindow("Browser");
        OnStart?.Invoke();
        MainLoop();
        CleanUp();
    }

    void CreateWindow(string title)
    {
        var options = WindowOptions.DefaultVulkan;
        // options.WindowBorder = WindowBorder.Hidden;
        options.Size = new Vector2D<int>(WIDTH, HEIGHT);
        options.Title = title;
        options.UpdatesPerSecond = 60;
        options.FramesPerSecond = 60;

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
        // Console.WriteLine($"Windowing backend: {window.GetType().Name}");

        if (window.VkSurface is null)
        {
            throw new Exception("Windowing platform doesn't support Vulkan.");
        }

        // Vulkan.VulkanEngine.Instance.CreateBuffers += CreateBuffers;

        //Assign events.
        OnStart += Start;
        window.Update += textureRenderer.Update;
        window.Render += OnRender;
        window.FramebufferResize += OnFramebufferResize;
    }

    public void SetTitle(string title)
    {
        window.Title = title;
    }

    unsafe void CreateCommandBuffers(VulkanEngine vulkanEngine)
    {
        for (int i = 0; i < VulkanEngine.MAX_FRAMES_IN_FLIGHT; i++)
        {
            CommandBufferAllocateInfo commandBufferCI = new()
            {
                SType = StructureType.CommandBufferAllocateInfo,
                CommandPool = vulkanEngine.commandPools[i],
                CommandBufferCount = 1
            };

            fixed (CommandBuffer* commandBufferPtr = &commandBuffers[i])
                CreateVulkan.vk.AllocateCommandBuffers(LogicalDevice.device, ref commandBufferCI, commandBufferPtr);
        }
    }
    private void Start()
    {
        // coreManager.Start();
        Console.WriteLine("==============================  STARTING ADDING OBJECTS  ==============================");
        // browserUI.Create();
    }

    private void OnFramebufferResize(Vector2D<int> newSize)
    {
        framebufferResized = true;
    }

    int lastSecondFps = 0;
    private void OnRender(double deltaTime)
    {
        // diagnosticStopwatch.Restart();
        CreateVulkan.vk.WaitForFences(LogicalDevice.device, 1, in fences[currentFrame], Vk.True, ulong.MaxValue);
        CreateVulkan.vk.ResetFences(LogicalDevice.device, 1, in fences[currentFrame]);


        uint imageIndex;
        var _result = swapchain.khrSwapChain!.AcquireNextImage(LogicalDevice.device, swapchain.swapChain, ulong.MaxValue, imageAcquiredSemaphores[currentFrame], default, &imageIndex);

        if (_result == Result.ErrorOutOfDateKhr)
        {
            swapchain.RecreateSwapChain(GetFrameBufferSize, OnWindowMinimized);
            // StylesManager.AddFlag(StylesManager.DirtyFlag.ScreenSize);
            return;
        }
        else if (_result != Result.Success && _result != Result.SuboptimalKhr)
            throw new Exception("Failed to acquire swap chain image!");

        // Vulkan.CreateVulkan.vk.ResetFences(LogicalDevice.device, 1, in vulkanManager.fences[currentFrame]);

        CreateVulkan.vk.ResetCommandBuffer(commandBuffers[currentFrame], 0);



        // UpdateUniformBuffer(currentFrame);
        // coreManager.OnRender(currentFrame);
        // StylesManager.ComputeStyles();
        var _imageData = textureRenderer.GetImage();
        RecordCommandBuffer(commandBuffers[currentFrame], imageIndex, _imageData.image);
        // StylesManager.SetFrameAsNotDirty(currentFrame);


        // UpdateUniformBufferPerspective(currentFrame);

        SubmitInfo2 submitInfo = new()
        {
            SType = StructureType.SubmitInfo,
        };

        submitInfo.WaitSemaphoreInfoCount = 2;
        SemaphoreSubmitInfo* waitSemaphores = stackalloc SemaphoreSubmitInfo[]
        {
            new()
            {
                SType = StructureType.SemaphoreSubmitInfo,
                Semaphore = imageAcquiredSemaphores[currentFrame],
                StageMask = PipelineStageFlags2.ColorAttachmentOutputBit,
            },
            new()
            {
                SType = StructureType.SemaphoreSubmitInfo,
                Semaphore = _imageData.semaphore,
                StageMask = PipelineStageFlags2.FragmentShaderBit,
            }
        };
        submitInfo.PWaitSemaphoreInfos = waitSemaphores;

        submitInfo.CommandBufferInfoCount = 1;

        CommandBufferSubmitInfo _cbSubmitInfo = new()
        {
            CommandBuffer = commandBuffers[currentFrame],
        };
        submitInfo.PCommandBufferInfos = &_cbSubmitInfo;

        submitInfo.SignalSemaphoreInfoCount = 1;

        SemaphoreSubmitInfo _signalRenderComplete = new()
        {
            Semaphore = renderCompleteSemaphores[currentFrame]
        };
        submitInfo.PSignalSemaphoreInfos = &_signalRenderComplete;

        if (Vulkan.CreateVulkan.vk.QueueSubmit2(LogicalDevice.graphicsQueue, 1, &submitInfo, fences[currentFrame]) != Result.Success)
        {
            throw new Exception("Failed to submit command buffer!");
        }


        fixed (SwapchainKHR* swapChainPtr = &swapchain.swapChain)
        fixed (Semaphore* renderCompleteSemaphoresPtr = &renderCompleteSemaphores[currentFrame])
        {
            PresentInfoKHR presentInfo = new()
            {
                SType = StructureType.PresentInfoKhr,

                WaitSemaphoreCount = 1,
                PWaitSemaphores = renderCompleteSemaphoresPtr,

                SwapchainCount = 1,
                PSwapchains = swapChainPtr,
                PImageIndices = &imageIndex
            };

            _result = swapchain.khrSwapChain.QueuePresent(LogicalDevice.presentQueue, &presentInfo);

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
        currentFrame = (currentFrame + 1) % VulkanEngine.MAX_FRAMES_IN_FLIGHT;
    }

    Vector2D<int> GetFrameBufferSize()
    {
        return window.FramebufferSize;
    }

    void OnWindowMinimized()
    {
        window.DoEvents();
    }

    void MainLoop()
    {
        window.Run();

        Vulkan.CreateVulkan.vk.DeviceWaitIdle(LogicalDevice.device);
    }

    void CleanUp()
    {
        textureRenderer.DestroyVulkanEngine();
        // coreManager.Dispose();
        // primitiveModelsDb.Dispose();
        Dispose();
        window.Dispose();
    }
}