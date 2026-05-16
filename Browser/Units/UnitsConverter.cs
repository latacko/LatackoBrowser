namespace Units;

public static class UnitsConverter {
    readonly static Dictionary<UnitType, float> unitToPx = new(){
        [UnitType.px] = 1,

        [UnitType.lvw] = 0,
        [UnitType.lvh] = 0,
    };

    public static void Update(float windowWidth, float windowHeight)
    {
        unitToPx[UnitType.lvw] = windowWidth/100;
        unitToPx[UnitType.lvh] = windowHeight/100;
    }

    public static float Get(UnitType unitType)
    {
        return unitToPx[unitType];
    }
}