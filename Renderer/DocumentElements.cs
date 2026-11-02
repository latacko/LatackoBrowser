using System;
using InstanceFinderCore;
using Vulkan;

namespace Renderer;

public static class DocumentElements
{
    public static uint Div => InstanceFinder.GetInstance<PrimitiveModelsDb>().Get(PrimitiveUIModel.Quad).AssetId;
}
