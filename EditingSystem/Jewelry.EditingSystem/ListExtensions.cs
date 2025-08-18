using System.Collections.Generic;

namespace Jewelry.EditingSystem;

public static class ListExtensions
{
    public static void ClearEx<T>(this IList<T> self, History history)
    {
        try
        {
            history.BeginBatch();
            
            while (self.Count is not 0)
                self.RemoveAt(self.Count - 1);
        }
        finally
        {
            history.EndBatch();
        }
    }
}