using UnityEngine;

public static class ExtraPhysics
{
    public static int RaycastAllNonAlloc(Ray ray, float maxDistance, RaycastHit[] results)
    {
        int count = 0;

        var currentDistance = maxDistance;

        while (true)
        {
            if (count >= results.Length)
            {
                break;
            }

            if (Physics.Raycast(ray, out var currentHit, currentDistance))
            {
                ray.origin = currentHit.point;

                results[count++] = currentHit;

                currentDistance -= currentHit.distance;
            }
            else
            {
                break;
            }
        }

        return count;
    }
}
