using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Silk.NET.Maths;
using Silk.NET.Vulkan;

namespace Units
{
    public interface IVertex
    {
        public VertexInputBindingDescription GetBindingDescription();

        public VertexInputAttributeDescription[] GetAttributeDescriptions();
    }
}