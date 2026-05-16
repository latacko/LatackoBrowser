using Browser;
using Units;
using Vulkan;

public class BrowserUI
{
    public RuntimeModelData TopBar;
    public RuntimeModelData LeftBar;
    public RuntimeModelData BottomBar;

    public void Create()
    {
        TopBar = ObjectsManager.AddObject(BrowserWindow.loadedShaders[0], VulkanManager.Instance.primitiveModelsDb.Get(PrimitiveUIModel.Quad))
        .SetPosition(new(0, 0))
        .SetSize(new(100, 45, UnitType.lvw, UnitType.px))
        .SetProperties(properties=>properties.SetBackgroundColor255(41, 44, 48, 255));

        BottomBar = ObjectsManager.AddObject(BrowserWindow.loadedShaders[0], VulkanManager.Instance.primitiveModelsDb.Get(PrimitiveUIModel.Quad))
        .SetPosition(new(0, 45))
        .SetSize(new(100, 45, UnitType.lvw, UnitType.px))
        .SetProperties(properties=>properties.SetBackgroundColor255(65, 68, 71, 255));
        // TopBar.SetTransform(TopBar.Transform.SetScale(1).SetRotation(new(0,2,3)));

        // LeftBar = ObjectsManager.AddObject(BrowserWindow.loadedShaders[0], VulkanManager.Instance.primitiveModelsDb.Get(PrimitiveUIModel.Quad));
        // LeftBar.SetPosition(new(0, 0));
        // LeftBar.SetSize(new(50, 100, UnitType.lvw, UnitType.lvh));
        // LeftBar.SetProperties(LeftBar.Properties.SetBackgroundColor255(0, 255, 0, 255));

        // BottomBar = ObjectsManager.AddObject(BrowserWindow.loadedShaders[0], VulkanManager.Instance.primitiveModelsDb.Get(PrimitiveUIModel.Quad));
        // BottomBar.SetPosition(new(0, 50, yType: UnitType.lvh));
        // BottomBar.SetSize(new(100, 50, UnitType.lvw, UnitType.lvh));
        // BottomBar.SetProperties(BottomBar.Properties.SetBackgroundColor255(0, 0, 255, 255));
    }
}