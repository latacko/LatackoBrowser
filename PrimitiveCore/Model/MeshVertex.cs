using System.Net;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Silk.NET.Maths;
using Silk.NET.Vulkan;
using Units;

namespace PrimitiveCore.Model
{
    public struct MeshVertex(Vector3D<float> pos, Vector2D<float> textCoord) : IVertex
    {
        public Vector3D<float> Pos = pos;
        public Vector2D<float> TextCoord = textCoord;

        public static VertexInputBindingDescription GetBindingDescription()
        {
            VertexInputBindingDescription bindingDescription = new()
            {
                Binding = 0,
                Stride = (uint)Unsafe.SizeOf<MeshVertex>(),
                InputRate = VertexInputRate.Vertex,
            };

            return bindingDescription;
        }

        public static VertexInputAttributeDescription[] GetAttributeDescriptions()
        {
            var attributeDescriptions = new[]
            {
                new VertexInputAttributeDescription()
                {
                    Binding = 0,
                    Location = 0,
                    Format = Format.R32G32B32Sfloat,
                    Offset = (uint)Marshal.OffsetOf<MeshVertex>(nameof(Pos)),
                },
                new VertexInputAttributeDescription()
                {
                    Binding = 0,
                    Location = 1,
                    Format = Format.R32G32Sfloat,
                    Offset = (uint)Marshal.OffsetOf<MeshVertex>(nameof(TextCoord)),
                }
            };

            return attributeDescriptions;
        }
    }
}