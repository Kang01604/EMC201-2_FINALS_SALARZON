using System.Collections.Generic;
using UnityEngine; // Required for Random.Range, though System.Random could also be used.

public static class ListExtensions
{
    /// <summary>
    /// Shuffles the elements of an IList using the Fisher-Yates algorithm.
    /// </summary>
    /// <typeparam name="T">The type of elements in the list.</typeparam>
    /// <param name="list">The list to be shuffled.</param>
    public static void Shuffle<T>(this IList<T> list)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = Random.Range(0, n + 1); // Use Unity's Random for consistency if you're in Unity environment
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }
}