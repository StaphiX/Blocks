using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class CodeUtils
{
    public static void For(int length, Action<Int32> action)
    {
        for (int index = 0; index < length; ++index)
        {
            action(index);
        }
    }

    public static void ForReverse<T>(this T[] array, Action<Int32, T> action)
    {
        for (int index = array.Length-1; index >= 0; --index)
        {
            action(index, array[index]);
        }
    }

    public static void For<T>(this T[] array, Action<Int32, T> action)
    {
        for (int index = 0; index < array.Length; ++index)
        {
            action(index, array[index]);
        }
    }
}

public static class CodeUtilsDebug
{
    public static void Run()
    {
        string[] stringArray = { "Apples", "Oranges", "Pears", "Lemons", "Grapes" };
        stringArray.ForReverse((index, value) =>
        {
            Debug.Log(value);
        });

        stringArray.For((index, value) =>
        {
            Debug.Log(value);
        });
    }
}