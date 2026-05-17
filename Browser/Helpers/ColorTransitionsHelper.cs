using Silk.NET.Maths;

public static class ColorTransitionsHelper
{

    class TransitionData
    {
        public float Duration;
        public Vector4D<float> OriginalColor;
        public Vector4D<float> TargetColor;
        public float Elapsed;
    }
    static Dictionary<RuntimeModelData, TransitionData> elements = new();

    public static void StartTransition(RuntimeModelData element, Vector4D<float> originalColor, Vector4D<float> targetColor, float duration)
    {
        if (elements.TryGetValue(element, out var value))
        {
            value.OriginalColor = originalColor;
            value.TargetColor = targetColor;
            value.Duration = duration;
            value.Elapsed = 0;
        }
        else
        {
            elements.Add(element, new()
            {
                OriginalColor = originalColor,
                TargetColor = targetColor,
                Duration = duration,
            });
        }
    }

    public static void Update(float deltaTime)
    {
        foreach (var item in new Dictionary<RuntimeModelData, TransitionData>(elements))
        {
            item.Value.Elapsed += deltaTime;
            float t = Math.Clamp(item.Value.Elapsed / item.Value.Duration, 0f, 1f);

            t = t * t * (3f - 2f * t);

            if (t >= 0.998f)
            {
                item.Key.SetProperties(item.Key.Properties.SetBackgroundColorWithoutTransitionInLinear(item.Value.TargetColor.X, item.Value.TargetColor.Y, item.Value.TargetColor.Z, item.Value.TargetColor.W));
                elements.Remove(item.Key);
            }
            else
            {
                var _currentColor = Vector4D.Lerp(item.Value.OriginalColor, item.Value.TargetColor, t);
                item.Key.SetProperties(item.Key.Properties.SetBackgroundColorWithoutTransitionInLinear(_currentColor.X, _currentColor.Y, _currentColor.Z, _currentColor.W));
            }
        }
    }


}