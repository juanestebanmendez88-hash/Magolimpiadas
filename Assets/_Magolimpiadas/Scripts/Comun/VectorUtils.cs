using UnityEngine;

public static class VectorUtils
{
    private const float MinSqrLength = 1e-4f;

    public static Vector3 Flatten(Vector3 vector)
    {
        vector.y = 0f;
        return vector;
    }

    public static Vector3 FlatDirection(Vector3 vector)
    {
        vector.y = 0f;
        return vector.sqrMagnitude > MinSqrLength ? vector.normalized : Vector3.forward;
    }
}
