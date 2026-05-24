using System;
using Units;

namespace TextCore;

public class TextObject
{
    public ushort characters;
    public required Vertex[] Vertices;
    public required uint[] Indices;
    public Slot Slot;
    public bool[] Dirty = new bool[Vulkan.VulkanManager.MAX_FRAMES_IN_FLIGHT];

    public TextObject()
    {
        Array.Fill(Dirty, true);
    }
}


public record struct Slot(
    uint VertexOffset,
    uint IndexOffset,
    BucketSize Bucket
);

public enum BucketSize : uint
{
    Tiny = 16,
    Small = 32,
    Medium = 64,
    Large = 128,
    Huge = 256,
}
