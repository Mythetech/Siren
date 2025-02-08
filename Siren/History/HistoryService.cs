using System;
using LiteDB;
using Microsoft.Extensions.Logging;
using Siren.Components.History;

namespace Siren.History
{
    public class HistoryService : IHistoryService
    {
        private readonly ILogger<HistoryService> _logger;

        public HistoryService(ILogger<HistoryService> logger)
        {
            _logger = logger;
        }

        private List<HistoryRecord>? _history;

        public ILogger<HistoryService> Logger => _logger;

        public event Action<List<HistoryRecord>>? HistoryRecordsChanged;

        private List<HistoryRecord> LoadHistory()
        {
            if (_history != null)
                return _history;

            return _history = HistoryRepository.GetHistoryRecords();
        }

        public void AddHistoryRecord(HistoryRecord record)
        {
            _history ??= LoadHistory();

            try
            {
                _history.Add(record);

                HistoryRepository.UpsertHistoryRecord(record);

                HistoryRecordsChanged?.Invoke(_history);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error upserting history record");
            }

            _logger.LogInformation("Added history record");
        }

        public List<HistoryRecord> GetHistory()
        {
            _history ??= LoadHistory();

            return _history.OrderByDescending(x => x.Timestamp).ToList();
        }

        public void DeleteAllHistoryRecords()
        {
            _history ??= LoadHistory();

            try
            {
                _history.Clear();

                HistoryRepository.DeleteAllHistoryRecords();

                HistoryRecordsChanged?.Invoke(_history);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting all history records");
            }

            _logger.LogInformation("Deleted all history records");
        }
    }
}

