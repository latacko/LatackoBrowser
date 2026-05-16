using Silk.NET.Maths;

public static class ColorTransitionsHelper
{
    class TransitionData
    {
        public RuntimeModelData Element;
        public float Duration;
        public Vector4D<float> OriginalColor;
        public Vector4D<float> TargetColor;
        public float Elapsed;
    }
    static List<TransitionData> elements = new();

    public TransitionData StartTransition(RuntimeModelData element, Vector4D<float> originalColor, Vector4D<float> targetColor, float duration)
    {
        
    }
}