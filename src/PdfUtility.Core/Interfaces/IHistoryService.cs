using System.Collections.Generic;
using PdfUtility.Core.Models;

namespace PdfUtility.Core.Interfaces
{
    public interface IHistoryService
    {
        IEnumerable<HistoryEntry> GetHistory();
        void AddEntry(HistoryEntry entry);
        void ClearHistory();
    }
}
