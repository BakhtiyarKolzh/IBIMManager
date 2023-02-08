using System.Collections.Generic;
using System.Collections.ObjectModel;


namespace IBIMTool.RevitExtensions
{
    internal static class DataStructureExtension
    {
        public static IEnumerable<T> Distinct<T>(IEnumerable<T> source)
        {
            HashSet<T> uniques = new HashSet<T>();
            foreach (T item in source)
            {
                if (uniques.Add(item))
                {
                    yield return item;
                }
            }
        }


        public static IDictionary<T, U> Update<T, U>(this IDictionary<T, U> target, IDictionary<T, U> source, int capacity = 10)
        {
            if (source != null && source.Count > 0)
            {
                target ??= new Dictionary<T, U>(capacity);
                foreach (KeyValuePair<T, U> item in source)
                {
                    target[item.Key] = item.Value;
                }
            }
            return target;
        }


        public static ObservableCollection<T> ToObservableCollection<T>(this IEnumerable<T> source)
        {
            return source != null ? new ObservableCollection<T>(source) : new ObservableCollection<T>();
        }

    }
}
