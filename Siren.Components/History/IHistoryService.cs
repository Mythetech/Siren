using System;
using Siren.Components.History;

namespace Siren.Components.History
{
    public interface IHistoryService
    {
        public List<HistoryRecord> GetHistory();

        public void AddHistoryRecord(HistoryRecord record);

        public event Action<List<HistoryRecord>>? HistoryRecordsChanged;

        void DeleteAllHistoryRecords();
    }
}

