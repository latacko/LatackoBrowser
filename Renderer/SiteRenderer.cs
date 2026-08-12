using System;
using Silk.NET.Vulkan;
using Vulkan;

namespace Renderer;

public class SiteRenderer : IDisposable
{
    public readonly uint UniqueId;

    static uint LastUniqueId;

    uint width;
    uint height;

    Image image;
    DeviceMemory deviceMemory;
    ImageView imageView;

    public SiteRenderer(uint width, uint height)
    {
        UniqueId = LastUniqueId++;

        UpdateSize(width, height);
    }

    //TODO - Clear prev image and render to new texture
    public void UpdateSize(uint width, uint height)
    {
        this.width = width;
        this.height = height;

        ImageHelper.CreateImage(width, height, Format.B8G8R8A8Srgb, ImageTiling.Optimal, ImageUsageFlags.TransferSrcBit | ImageUsageFlags.SampledBit, MemoryPropertyFlags.DeviceLocalBit, 1, ref image, ref deviceMemory);
        ImageHelper.CreateImageView(image, Format.B8G8R8A8Srgb, ImageAspectFlags.ColorBit);
    }


    //TODO - Render to texture
    public void Render()
    {
        Vulkan.CreateVulkan.vk.WaitForFences(LogicalDevice.device, 1, in vulkanManager.fences[currentFrame], Vk.True, ulong.MaxValue);


        uint imageIndex;
        var _result = swapchain.khrSwapChain!.AcquireNextImage(LogicalDevice.device, swapchain.swapChain, ulong.MaxValue, vulkanManager.imageAcquiredSemaphores[currentFrame], default, &imageIndex);

        if (_result == Result.ErrorOutOfDateKhr)
        {
            swapchain.RecreateSwapChain(GetFrameBufferSize, OnWindowMinimized);
            StylesManager.AddFlag(StylesManager.DirtyFlag.ScreenSize);
            return;
        }
        else if (_result != Result.Success && _result != Result.SuboptimalKhr)
            throw new Exception("Failed to acquire swap chain image!");

        Vulkan.CreateVulkan.vk.ResetFences(LogicalDevice.device, 1, in vulkanManager.fences[currentFrame]);

        Vulkan.CreateVulkan.vk.ResetCommandBuffer(vulkanManager.commandBuffers[currentFrame], 0);



        UpdateUniformBuffer(currentFrame);
        coreManager.OnRender(currentFrame);
        StylesManager.ComputeStyles();

        RecordCommandBuffer(vulkanManager.commandBuffers[currentFrame], imageIndex);
        StylesManager.SetFrameAsNotDirty(currentFrame);


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

            if (Vulkan.CreateVulkan.vk.QueueSubmit(LogicalDevice.graphicsQueue, 1, &submitInfo, vulkanManager.fences[currentFrame]) != Result.Success)
            {
                throw new Exception("Failed to submit command buffer!");
            }

            currentFrame = (currentFrame + 1) % Vulkan.VulkanEngine.MAX_FRAMES_IN_FLIGHT;


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
                StylesManager.AddFlag(StylesManager.DirtyFlag.ScreenSize);
            }
            else if (_result != Result.Success)
            {
                throw new Exception("failed to present swap chain image!");
            }

        }
        diagnosticStopwatch.Stop();
        if (currentFrame == 0)
            renderMicroseconds = diagnosticStopwatch.Elapsed.Microseconds;
        else
            render2Microseconds = diagnosticStopwatch.Elapsed.Microseconds;

        _frameCount++;
        _frameCount++;
        _fpsTimer += deltaTime;
        _fpsTimer2 += deltaTime;

        if (_fpsTimer >= 1.0) // every second
        {
            lastSecondFps = _frameCount;
            _frameCount = 0;
            _fpsTimer = 0;
        }

        if (_fpsTimer2 >= 0.25)
        {
            _fpsTimer2 = 0;
            window.Title = $"FPS: {lastSecondFps} update: {updateMicroseconds}μs render: {renderMicroseconds}μs render2: {render2Microseconds}μs";
        }
    }

    public void Dispose()
    {
        throw new NotImplementedException();
    }
}
