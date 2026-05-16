using Browser;
using Units;

public class BrowserUI
{
    public RuntimeModelData TopBar;
    public RuntimeModelData LeftBar;
    public RuntimeModelData BottomBar;

    public void Create()
    {
        TopBar = ObjectsManager.AddObject(BrowserWindow.loadedShader[0], BrowserWindow.primitiveModelsDb.Get(PrimitiveUIModel.Quad));
        TopBar.SetPosition(new(50, 50, UnitType.lvw, UnitType.lvw));
        TopBar.SetSize(new(50, 50, UnitType.lvw, UnitType.lvw));
        TopBar.SetTransform(TopBar.Transform.SetTranslate(new(25, 25, UnitType.lvw, UnitType.lvw)));
        TopBar.SetProperties(TopBar.Properties.SetBackgroundColor255(255, 0, 0, 255));
        // TopBar.SetTransform(TopBar.Transform.SetScale(1).SetRotation(new(0,2,3)));

        LeftBar = ObjectsManager.AddObject(BrowserWindow.loadedShader[0], BrowserWindow.primitiveModelsDb.Get(PrimitiveUIModel.Quad));
        LeftBar.SetPosition(new(0, 0));
        LeftBar.SetSize(new(50, 100, UnitType.lvw, UnitType.lvh));
        LeftBar.SetProperties(LeftBar.Properties.SetBackgroundColor255(0, 255, 0, 255));

        BottomBar = ObjectsManager.AddObject(BrowserWindow.loadedShader[0], BrowserWindow.primitiveModelsDb.Get(PrimitiveUIModel.Quad));
        BottomBar.SetPosition(new(0, 50, yType: UnitType.lvh));
        BottomBar.SetSize(new(100, 50, UnitType.lvw, UnitType.lvh));
        BottomBar.SetProperties(BottomBar.Properties.SetBackgroundColor255(0, 0, 255, 255));
    }
}