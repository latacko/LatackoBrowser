using GraphicsCore;
using GraphicsCore.Buffers;
using GraphicsCore.Styles;
using Silk.NET.Vulkan;
using TextCore.Text;
using Units;
using Vulkan;
using VulkanManager;
using VulkanManager.BufferManager;
using VulkanManager.Helpers;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace TextCore;

public class TextManager : BufferManager, IRenderTick
{
    byte threadId;
    internal DynamicBuffer<TextVertex> vertexBuffer;
    internal DynamicBuffer<ushort> indicesBuffer;

    List<TextLine> activeTexts = new();
    public static uint LastCreatedIndex = 0;

    internal static Style TextDefaultStyle;


    internal TextLine AddModelText(ReadOnlyMemory<char> text, int leftRange, int rightRange, TextContainer parent)
    {
        uint objectIndex = LastCreatedIndex++;
        TextLine runtimeModelData = new(text, leftRange, rightRange, parent);
        if (parent != null)
        {
            parent.AddChild(runtimeModelData);
        }

        return runtimeModelData;
    }

    public TextContainer AddText(string text, BaseShader shader, VisualElement parent)
    {
        uint objectIndex = LastCreatedIndex++;
        TextContainer runtimeTextContainer = new(text, objectIndex, this, parent);
        parent?.AddChild(runtimeTextContainer);

        VisualElement.ObjectsToCompile.Add(new()
        {
            runtimeModel = runtimeTextContainer,
            shader = shader
        });

        Console.WriteLine("Adding text to the textShader." + shader.GetHashCode());

        return runtimeTextContainer;
    }

    public override void RegisterBuffer()
    {
        vertexBuffer = new(threadId, 4096, 2, BufferUsageFlags.VertexBufferBit);
        indicesBuffer = new(threadId, 4096, 2, BufferUsageFlags.IndexBufferBit);

        TextDefaultStyle = new Style("Default text style")
            .SetFontProperties((FontProperties) => FontProperties
                .SetFont("google-noto/NotoSerif-Regular.ttf")
                .SetFontSize(new(16))
            );
    }

    public void Update(TextLine text)
    {
        // Console.WriteLine("Vertex: ");
        vertexBuffer.Update(text.VertexSlotData);
        // Console.WriteLine("Indices: ");
        indicesBuffer.Update(text.IndicesSlotData);
    }

    public unsafe void RenderTick(uint frameInFlight)
    {
        // Console.WriteLine("Vertex:");
        vertexBuffer.RenderTick(frameInFlight);
        // Console.WriteLine("Indices:");
        indicesBuffer.RenderTick(frameInFlight);
        // foreach (var text in activeTexts)
        // {
        //     if (!text.dirty[frameInFlight].HasFlag(RuntimeModelData.DirtyFlags.Model)) continue;

        //     text.ModelData.Vertices.CopyTo(
        //         new Span<TextVertex>(((TextVertex*)vertexBuffer[frameInFlight].Mapped) + text.Slot.VertexOffset, (int)VertexsPerBucket(text.Slot.Bucket))
        //     );

        //     text.ModelData.Indices.CopyTo(
        //         new Span<ushort>((ushort*)indicesBuffer[frameInFlight].Mapped + text.Slot.IndexOffset, (int)IndicesPerBucket(text.Slot.Bucket))
        //     );

        //     text.RemoveFlag(RuntimeModelData.DirtyFlags.Model, frameInFlight);
        // }
    }

    public override unsafe void Dispose()
    {
        vertexBuffer.Dispose();
        indicesBuffer.Dispose();
    }
}
