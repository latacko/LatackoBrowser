using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Silk.NET.Maths;
using Silk.NET.Vulkan;
using Units;

namespace ObjectCore
{
    public struct ObjectVertex(Vector3D<float> pos, Vector2D<float> textCoord) : IVertex
    {
        public Vector3D<float> Pos = pos;
        public Vector2D<float> TextCoord = textCoord;

        public VertexInputBindingDescription GetBindingDescription()
        {
            VertexInputBindingDescription bindingDescription = new()
            {
                Binding = 0,
                Stride = (uint)Unsafe.SizeOf<ObjectVertex>(),
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
                    Offset = (uint)Marshal.OffsetOf<ObjectVertex>(nameof(Pos)),
                },
                new VertexInputAttributeDescription()
                {
                    Binding = 0,
                    Location = 1,
                    Format = Format.R32G32Sfloat,
                    Offset = (uint)Marshal.OffsetOf<ObjectVertex>(nameof(TextCoord)),
                }
            };

            return attributeDescriptions;
        }
    }
}