using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Silk.NET.Maths;
using Silk.NET.Vulkan;
using Units;

namespace TextCore;

public struct TextVertex(Vector3D<float> pos, Vector2D<float> textCoord, uint charAscii) : IVertex
{
    public Vector3D<float> Pos = pos;
    public Vector2D<float> TextCoord = textCoord;
    public uint CharAscii = charAscii;

    public VertexInputBindingDescription GetBindingDescription()
    {
        VertexInputBindingDescription bindingDescription = new()
        {
            Binding = 0,
            Stride = (uint)Unsafe.SizeOf<TextVertex>(),
            InputRate = VertexInputRate.Vertex,
        };

        return bindingDescription;
    }

    public VertexInputAttributeDescription[] GetAttributeDescriptions()
    {
        var attributeDescriptions = new[]
        {
                new VertexInputAttributeDescription()
                {
                    Binding = 0,
                    Location = 0,
                    Format = Format.R32G32B32Sfloat,
                    Offset = (uint)Marshal.OffsetOf<TextVertex>(nameof(Pos)),
                },
                new VertexInputAttributeDescription()
                {
                    Binding = 0,
                    Location = 1,
                    Format = Format.R32G32Sfloat,
                    Offset = (uint)Marshal.OffsetOf<TextVertex>(nameof(TextCoord)),
                },
                new VertexInputAttributeDescription()
                {
                    Binding = 0,
                    Location = 2,
                    Format = Format.R32Uint,
                    Offset = (uint)Marshal.OffsetOf<TextVertex>(nameof(CharAscii)),
                }
            };

        return attributeDescriptions;
    }
}
