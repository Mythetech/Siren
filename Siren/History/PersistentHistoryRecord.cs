using System;
using Siren.Components.History;

namespace Siren.History
{
    public class PersistentHistoryRecord : HistoryRecord
    {
        public int DocumentId { get; set; }
        public static PersistentHistoryRecord Create(HistoryRecord record)
        {
            return new PersistentHistoryRecord()
            {
                RequestId = record.RequestId,
                RequestUri = record.RequestUri,
                HttpMethod = record.HttpMethod,
                Id = record.Id,
                StatusCode = record.StatusCode,
                Timestamp = record.Timestamp,
                Request = record.Request,
                Response = record.Response,
                DisplayText = record.DisplayText
            };
        }

        public HistoryRecord ToHistoryRecord()
        {
            return this;
        }
    }
}

