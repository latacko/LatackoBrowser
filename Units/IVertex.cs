using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Silk.NET.Maths;
using Silk.NET.Vulkan;

namespace Units
{
    public interface IVertex
    {
        public static abstract VertexInputBindingDescription GetBindingDescription();

        public static abstract VertexInputAttributeDescription[] GetAttributeDescriptions();
    }
}