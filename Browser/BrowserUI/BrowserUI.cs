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
        TopBar = ObjectsManager.AddObject(BrowserWindow.loadedShaders[0], BrowserWindow.Instance.primitiveModelsDb.Get(PrimitiveUIModel.Quad))
        .SetLayout(
            (layout) => layout
                .SetLeft(new(0)).SetTop(new(0))
                .SetWidth(new(100, UnitType.lvw)).SetHeight(new(100, UnitType.px))
        )
        .SetProperties(
            (properties) => properties
                .SetBackgroundColor255(41, 44, 48, 255)
                .SetTransition(.25f)
                .SetBorderRadius(new(20, UnitType.px))
        );

        Karta = ObjectsManager.AddObject(BrowserWindow.loadedShaders[0], BrowserWindow.Instance.primitiveModelsDb.Get(PrimitiveUIModel.Quad), TopBar)
        .SetLayout(
            (layout) => layout
                .SetLeft(new(-10)).SetTop(new(-10))
                .SetWidth(new(100, UnitType.px)).SetHeight(new(100, UnitType.percentageHeight))
        )
        .SetProperties(
            (properties) => properties
                .SetCursor(Properties.CursorType.pointer)
                .SetBackgroundColor255(65, 68, 71, 255)
                .SetBorderRadius(new(25))
                .SetTransition(5f)
        ).SetEvents(
            events => events
                .AddOnClick((element) =>
                {
                    Console.WriteLine("Klikam element jakiś");
                })
                .AddOnMouseOver(element =>
                {
                    element.SetProperties(element.Properties
                        .SetBackgroundColor255(60, 60, 78, 255)
                    );
                })
                .AddOnMouseOut(element =>
                {
                    element.SetProperties(element.Properties
                        .SetBackgroundColor255(65, 68, 71, 255)
                    );
                })
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