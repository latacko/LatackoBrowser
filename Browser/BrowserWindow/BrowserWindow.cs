using Silk.NET.Maths;
using Silk.NET.Input;
using Silk.NET.Vulkan;
using Silk.NET.Windowing;
using Semaphore = Silk.NET.Vulkan.Semaphore;
using Silk.NET.Windowing.Sdl;
using Silk.NET.Input.Sdl;
using Vulkan;
using GraphicsCore.Styles;
using System.Diagnostics;
using Vulkan;
using VulkanManager;
using VulkanManager.BufferManager;
using InstanceFinderCore;
using Renderer;

namespace Browser;

public unsafe partial class BrowserWindow
{
    internal IWindow window;
    private IInputContext inputContext;

    private const int WIDTH = 800;
    private const int HEIGHT = 800;

    uint frameInFlight = 0;

    public Action OnStart;

    TextureRenderer textureRenderer;

    RingBufferManager ringBufferManager;
    TransferCommandPoolManager transferCommandPoolManager;
    public FPSData FPS;
    double timePassed;
    Stopwatch frameTimeWatch = new();

    long acumulatedGCInThread = 0;

    int traceFrames;

    long targetTicks = Stopwatch.Frequency / 99999;
    readonly Stopwatch stopwatch = Stopwatch.StartNew();
    long nextFrameTicks = Stopwatch.GetTimestamp();

    public BrowserWindow()
    {
        CreateWindow("Browser");
    }

    public void Run()
    {
        SetupVulkan();
        OnStart?.Invoke();
        MainLoop();
        CleanUp();
    }

    public void CreateWindow(string title)
    {
        SdlWindowing.RegisterPlatform();
        SdlInput.RegisterPlatform();

        var options = WindowOptions.DefaultVulkan;
        // options.WindowBorder = WindowBorder.Hidden;
        options.Size = new Vector2D<int>(WIDTH, HEIGHT);
        options.Title = title;

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
        window.Render += OnRender;
        window.FramebufferResize += OnFramebufferResize;
    }

    public void SetTextureRenderer(TextureRenderer textureRenderer)
    {
        this.textureRenderer = textureRenderer;
        textureRenderer.CreateVulkanEngine();
        CreateCommandBuffers(textureRenderer.GetVulkanEngine());
        window.Update += textureRenderer.Update;
    }

    public void SetTitle(string title)
    {
        window.Title = title;
    }

    unsafe void CreateCommandBuffers(VulkanEngine vulkanEngine)
    {
        for (int i = 0; i < VulkanEngine.MAX_FRAMES_IN_FLIGHT; i++)
        {
            CommandBufferAllocateInfo commandBufferCI = new(
                commandPool: vulkanEngine.commandPools[i],
                commandBufferCount: 1
            );

            fixed (CommandBuffer* commandBufferPtr = &commandBuffers[i])
                CreateVulkan.vk.AllocateCommandBuffers(LogicalDevice.device, ref commandBufferCI, commandBufferPtr);
        }
    }
    private void Start()
    {
        // coreManager.Start();
        transferCommandPoolManager = InstanceRegistry.Get<TransferCommandPoolManager>();
        ringBufferManager = InstanceRegistry.Get<RingBufferManager>();
        Console.WriteLine("Init instances");

        textureRenderer.Init(swapchain.swapChainExtent.Width, swapchain.swapChainExtent.Height);

        Console.WriteLine("==============================  STARTING ADDING OBJECTS  ==============================");
        // browserUI.Create();
    }

    private void OnFramebufferResize(Vector2D<int> newSize)
    {
        framebufferResized = true;
        textureRenderer.Resize(swapchain.swapChainExtent.Width, swapchain.swapChainExtent.Height);
    }

    private void OnRender(double deltaTime)
    {
        if (++traceFrames == 600)
        {
            SpeedscopeTrace.Enabled = false;
            SpeedscopeTrace.Save("trace.speedscope.json");
            Console.WriteLine("Saved trace");
        }

        long _allocatedGB1 = GC.GetAllocatedBytesForCurrentThread();
        using var _frame = SpeedscopeTrace.Begin("OnRender");
        using (SpeedscopeTrace.Begin("WaitForFences"))
        {
            CreateVulkan.vk.WaitForFences(LogicalDevice.device, 1, in fences[frameInFlight], Vk.True, ulong.MaxValue);
        }
        frameTimeWatch.Stop();

        timePassed += deltaTime;

        CreateVulkan.vk.ResetFences(LogicalDevice.device, 1, in fences[frameInFlight]);
        frameTimeWatch.Restart();

        uint imageIndex;
        Result _result;

        using (SpeedscopeTrace.Begin("AcquireNextImage"))
        {
            _result = swapchain.khrSwapChain!.AcquireNextImage(LogicalDevice.device, swapchain.swapChain, ulong.MaxValue, imageAcquiredSemaphores[frameInFlight], default, &imageIndex);
        }

        if (_result == Result.ErrorOutOfDateKhr)
        {
            swapchain.RecreateSwapChain(GetFrameBufferSize, OnWindowMinimized);
            // StylesManager.AddFlag(StylesManager.DirtyFlag.ScreenSize);
            return;
        }
        else if (_result != Result.Success && _result != Result.SuboptimalKhr)
            throw new Exception("Failed to acquire swap chain image!");

        // Vulkan.CreateVulkan.vk.ResetFences(LogicalDevice.device, 1, in vulkanManager.fences[frameInFlight]);

        CreateVulkan.vk.ResetCommandBuffer(commandBuffers[frameInFlight], 0);



        // UpdateUniformBuffer(frameInFlight);
        // coreManager.OnRender(frameInFlight);
        // StylesManager.ComputeStyles();
        (Image image, Semaphore semaphore) _imageData;
        using (SpeedscopeTrace.Begin("Ring+Texture"))
        {
            ringBufferManager.BeginRecording(frameInFlight);
            using (SpeedscopeTrace.Begin("Get Image"))
            {
                _imageData = textureRenderer.GetImage(frameInFlight);
            }

            ringBufferManager.EndRecording(frameInFlight);
        }

        using (SpeedscopeTrace.Begin("TransferTick"))
        {
            transferCommandPoolManager.RenderTick(frameInFlight);
        }

        using (SpeedscopeTrace.Begin("RecordCommandBuffer"))
        {
            RecordCommandBuffer(commandBuffers[frameInFlight], imageIndex, _imageData.image);
        }
        // StylesManager.SetFrameAsNotDirty(frameInFlight);


        // UpdateUniformBufferPerspective(frameInFlight);

        using (SpeedscopeTrace.Begin("Submit"))
        {
            SubmitInfo2 submitInfo = new(
                waitSemaphoreInfoCount: 2
            );
            SemaphoreSubmitInfo* waitSemaphores = stackalloc SemaphoreSubmitInfo[]
            {
                new(
                    semaphore: imageAcquiredSemaphores[frameInFlight],
                    stageMask: PipelineStageFlags2.ColorAttachmentOutputBit
                ),
                new(
                    semaphore: _imageData.semaphore,
                    stageMask: PipelineStageFlags2.FragmentShaderBit
                )
            };
            submitInfo.PWaitSemaphoreInfos = waitSemaphores;

            submitInfo.CommandBufferInfoCount = 1;

            CommandBufferSubmitInfo _cbSubmitInfo = new(
                commandBuffer: commandBuffers[frameInFlight]
            );
            submitInfo.PCommandBufferInfos = &_cbSubmitInfo;

            submitInfo.SignalSemaphoreInfoCount = 1;

            SemaphoreSubmitInfo _signalRenderComplete = new(
                semaphore: renderCompleteSemaphores[frameInFlight]
            );
            submitInfo.PSignalSemaphoreInfos = &_signalRenderComplete;

            if (Vulkan.CreateVulkan.vk.QueueSubmit2(LogicalDevice.GraphicsQueue, 1, &submitInfo, fences[frameInFlight]) != Result.Success)
            {
                throw new Exception("Failed to submit command buffer!");
            }
        }

        using (SpeedscopeTrace.Begin("Present"))
        {
            fixed (SwapchainKHR* swapChainPtr = &swapchain.swapChain)
            fixed (Semaphore* renderCompleteSemaphoresPtr = &renderCompleteSemaphores[frameInFlight])
            {
                PresentInfoKHR presentInfo = new(
                    waitSemaphoreCount: 1,
                    pWaitSemaphores: renderCompleteSemaphoresPtr,

                    swapchainCount: 1,
                    pSwapchains: swapChainPtr,
                    pImageIndices: &imageIndex
                );

                _result = swapchain.khrSwapChain.QueuePresent(LogicalDevice.PresentQueue, &presentInfo);

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
        }

        double frameMs = deltaTime * 1000.0;
        FPS.worstFrameMs = Math.Max(FPS.worstFrameMs, frameMs);
        FPS.fpsTimer += deltaTime;
        FPS.fpsFrames++;

        long _allocatedGB2 = GC.GetAllocatedBytesForCurrentThread();
        acumulatedGCInThread += _allocatedGB2 - _allocatedGB1;

        if (FPS.fpsTimer >= 0.1)   // update twice per second so it stays readable
        {
            double fps = FPS.fpsFrames / FPS.fpsTimer;
            double avgMs = FPS.fpsTimer * 1000.0 / FPS.fpsFrames;
            window.Title = $"Login screen | {fps:F0} FPS | avg {avgMs:F2} ms | worst {FPS.worstFrameMs:F2} ms. Allocated garbage: " + acumulatedGCInThread + " bytes";
            acumulatedGCInThread = 0;
            FPS.fpsTimer = 0;
            FPS.fpsFrames = 0;
            FPS.worstFrameMs = 0;
        }

        frameInFlight = (frameInFlight + 1) % VulkanEngine.MAX_FRAMES_IN_FLIGHT;
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
        window.Run(() =>
        {
            // 1. Obliczamy punkt docelowy dla tej klatki
            nextFrameTicks += targetTicks;

            // 2. Precyzyjne uśpienie dostosowane do Linuxa
            while (Stopwatch.GetTimestamp() < nextFrameTicks)
            {
                long remainingTicks = nextFrameTicks - Stopwatch.GetTimestamp();
                // Konwersja pozostałych ticków na milisekunde
                double remainingMs = (double)remainingTicks / Stopwatch.Frequency * 1000.0;

                if (remainingMs > 1.0)
                {
                    // Linuxowy scheduler idealnie usypia wątek na ~1ms bez narzutu
                    Thread.Sleep(1);
                }
                else if (remainingMs > 0)
                {
                    // Końcówka klatki (poniżej 1ms) dociągana mikro-pauzą
                    Thread.SpinWait(5);
                }
            }

            // 3. OCHRONA PRZED 14% CPU (Zabezpieczenie przed nadrabianiem klatek)
            long currentTicks = Stopwatch.GetTimestamp();
            if (currentTicks > nextFrameTicks + targetTicks)
            {
                // Jeśli renderowanie lub system operacyjny złapał laga, 
                // resetujemy zegar do czasu obecnego, zamiast wpadać w pętlę 100% CPU.
                nextFrameTicks = currentTicks;
            }

            // 4. Cykl życia okna Silk.NET
            window.DoEvents();
            if (window.IsClosing) return;

            window.DoUpdate();
            window.DoRender();
        });

        Vulkan.CreateVulkan.vk.DeviceWaitIdle(LogicalDevice.device);
    }

    void CleanUp()
    {
        textureRenderer.DestroyVulkanEngine();
        // coreManager.Dispose();
        // primitiveModelsDb.Dispose();
        Dispose();
        window.Reset();
        window.Dispose();
    }
}