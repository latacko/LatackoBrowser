using Browser;
using Units;
using Vulkan;

public class BrowserUI
{
    public RuntimeModelData TopBar;
    public RuntimeModelData Karta;
    public RuntimeModelData LeftBar;
    public RuntimeModelData BottomBar;

    public void Create()
    {
        TopBar = ObjectsManager.AddObject(BrowserWindow.loadedShaders[0], VulkanManager.Instance.primitiveModelsDb.Get(PrimitiveUIModel.Quad))
        .SetLayout(
            (layout, self) => layout
                .SetLeft(self.CreateUnit(0)).SetTop(self.CreateUnit(0))
                .SetWidth(self.CreateUnit(100, UnitType.lvw)).SetHeight(self.CreateUnit(45, UnitType.px))
        )
        .SetProperties(
            (properties, self) => properties
                .SetBackgroundColor255(41, 44, 48, 255)
                .SetTransition(.25f)
                .SetCursor(Properties.CursorType.none)
                .SetBorderRadius(self.CreateUnit(100))
        );

        Karta = ObjectsManager.AddObject(BrowserWindow.loadedShaders[0], VulkanManager.Instance.primitiveModelsDb.Get(PrimitiveUIModel.Quad), TopBar)
        .SetLayout(
            (layout, self) => layout
                .SetLeft(self.CreateUnit(0)).SetTop(self.CreateUnit(0))
                .SetWidth(self.CreateUnit(100, UnitType.px)).SetHeight(self.CreateUnit(100, UnitType.percentageHeight))
        )
        .SetProperties(
            (properties, self)=>properties
                .SetCursor(Properties.CursorType.pointer)
                .SetBackgroundColor255(65, 68, 71, 255)
                .SetBorderRadius(self.CreateUnit(25))
        );

        // BottomBar = ObjectsManager.AddObject(BrowserWindow.loadedShaders[0], VulkanManager.Instance.primitiveModelsDb.Get(PrimitiveUIModel.Quad))
        // .SetPosition(new(0, 45))
        // .SetSize(new(100, 45, UnitType.lvw, UnitType.px))
        // .SetProperties(properties=>properties.SetBackgroundColor255(65, 68, 71, 255));
        // clickableElements = [TopBar, BottomBar];

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