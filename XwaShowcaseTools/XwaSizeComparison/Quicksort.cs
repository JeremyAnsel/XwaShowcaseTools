using System;
using System.Collections.Generic;

namespace XwaSizeComparison
{
    static class Quicksort
    {
        public static void Sort<T>(IList<T> array)
            where T : IComparable<T>
        {
            QuickSortInternal(array, 0, array.Count - 1);
            QuickSortInternal(array, 0, array.Count - 1);
        }

        private static void QuickSortInternal<T>(IList<T> array, int left, int right)
            where T : IComparable<T>
        {
            if (left >= right || IsArraySorted(array, left, right))
            {
                return;
            }

            Swap(array, left, (left + right) / 2);
            int last = left;
            for (int current = left + 1; current <= right; ++current)
            {
                if (array[current].CompareTo(array[left]) < 0)
                {
                    ++last;
                    Swap(array, last, current);
                }
            }

            Swap(array, left, last);

            QuickSortInternal(array, left, last - 1);
            QuickSortInternal(array, last + 1, right);
        }

        private static bool IsArraySorted<T>(IList<T> arr, int left, int right)
            where T : IComparable<T>
        {
            for (int i = left; i < right; i++)
            {
                if (arr[i].CompareTo(arr[i + 1]) > 0)
                {
                    return false;
                }
            }

            return true;
        }

        private static void Swap<T>(IList<T> arr, int i, int j)
        {
            (arr[j], arr[i]) = (arr[i], arr[j]);
        }
    }
}
