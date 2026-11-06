using System;
using InstanceFinderCore;
using Vulkan;

namespace Renderer;

public static class DocumentElements
{
    public static uint Div => InstanceRegistry.Get<PrimitiveModelsDb>().Get(PrimitiveUIModel.Quad).AssetId;
}
