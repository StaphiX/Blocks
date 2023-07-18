using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class RandomUtil
{
    public static Color RandomColor()
    {
        return new Color(Random.value, Random.value, Random.value, 1.0f);
    }

    public static Vector3 RandomVector3(Vector3 min, Vector3 max)
    {
        return new Vector3(Random.Range(min.x, max.x), Random.Range(min.y, max.y), Random.Range(min.z, max.z));
    }
}
