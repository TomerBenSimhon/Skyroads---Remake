using UnityEngine;

public static class Helper
{
    // Method to map a value from one range to another
    public static float MapValue(float value, float fromMin, float fromMax, float toMin, float toMax)
    {
        // Ensure the value stays within the original range
        value = Mathf.Clamp(value, fromMin, fromMax);

        // Map the value to the new range
        return toMin + (toMax - toMin) * ((value - fromMin) / (fromMax - fromMin));
    }
}