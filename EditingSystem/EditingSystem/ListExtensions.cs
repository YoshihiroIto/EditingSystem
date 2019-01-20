using System.Collections.Generic;

namespace EditingSystem
{
    public static class ListExtensions
    {
        public static void ClearEx<T>(this IList<T> self, History history)
        {
            history.BeginBatch();

            while (self.Count != 0)
                self.RemoveAt(self.Count - 1);

            history.EndBatch();
        }
    }
}