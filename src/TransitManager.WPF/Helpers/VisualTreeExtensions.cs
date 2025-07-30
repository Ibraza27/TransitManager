using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace TransitManager.WPF.Helpers
{
    public static class VisualTreeExtensions
    {
        public static IEnumerable<T> Descendants<T>(this DependencyObject parent) where T : DependencyObject
        {
            if (parent == null) yield break;

            var queue = new Queue<DependencyObject>();
            queue.Enqueue(parent);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                var childrenCount = VisualTreeHelper.GetChildrenCount(current);

                for (int i = 0; i < childrenCount; i++)
                {
                    var child = VisualTreeHelper.GetChild(current, i);
                    if (child is T typedChild)
                        yield return typedChild;
                    
                    queue.Enqueue(child);
                }
            }
        }
    }
}