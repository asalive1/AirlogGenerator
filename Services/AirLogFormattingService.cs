using AirlogGenerator.Models;

namespace AirlogGenerator.Services
{
    public class AirLogFormattingService
    {
        public List<string> GenerateLines(
    List<AirLogRow> rows,
    DateTime targetDate,
    double offsetHours,
    bool useOffset)
        {
            var output = new List<string>();

            // Apply offset and filter to broadcast day
            var correctedRows = rows
                .Select(r =>
                {
                    var serverTime = r.AirDate.Date.AddMilliseconds(r.AirTimeMs);

                    var corrected = useOffset
                        ? AirLogProcessor.ApplyOffset(serverTime, offsetHours)
                        : serverTime;

                    return (Corrected: corrected, Row: r);
                })
                .Where(x =>
                {
                    if (AirLogRowHelpers.IsNonMediaEvent(x.Row))
                    {
                        // Non-media events should be included based on their AIR DATE, not corrected timestamp
                        return x.Row.AirDate.Date == targetDate.Date;
                    }

                    return AirLogProcessor.BelongsToBroadcastDay(x.Corrected, targetDate);
                })
                .OrderBy(x => x.Corrected)
                .ToList();

            // Build a working list of rows we may expand (for SHORT)
            var expandedRows = new List<(DateTime Corrected, AirLogRow Row)>();

            foreach (var item in correctedRows)
            {
                var row = item.Row;

                // Do NOT output STOPPED rows directly — only output the SHORT row
                if (row.Status != "STOPPED")
                    expandedRows.Add(item);

                // If this is a STOPPED media row, try to synthesize a SHORT line
                if (row.Status == "STOPPED" && !AirLogRowHelpers.IsNonMediaEvent(row))
                {
                    // Match STOPPED to STARTED of the same playlist entry
                    var start = correctedRows
                        .Where(x =>
                            x.Row.PlaylistEntryId == row.PlaylistEntryId &&
                            x.Row.Status == "STARTED")
                        .OrderBy(x => x.Corrected)
                        .FirstOrDefault();

                    if (start.Row != null && start.Corrected < item.Corrected)
                    {
                        var shortRow = AirLogProcessor.BuildShortRow(start.Row, row);

                        // STOPPED timestamp → corrected timestamp
                        var stopServerTime = row.AirDate.Date.AddMilliseconds(row.AirTimeMs);
                        var stopCorrected = useOffset
                            ? AirLogProcessor.ApplyOffset(stopServerTime, offsetHours)
                            : stopServerTime;

                        expandedRows.Add((stopCorrected, shortRow));
                    }
                }
            }

            // Final sort:
            // 1. Timestamp
            // 2. Media rows grouped by playlist_entry_id
            // 3. ON-AIR before SHORT
            foreach (var item in expandedRows
                .OrderBy(x => x.Corrected)
                .ThenBy(x => AirLogRowHelpers.IsNonMediaEvent(x.Row) ? int.MaxValue : x.Row.PlaylistEntryId)
                .ThenBy(x => StatusSortKey(x.Row.Status)))
            {
                var row = item.Row;

                if (AirLogRowHelpers.IsNonMediaEvent(row))
                {
                    output.Add(AirLogProcessor.FormatNonMediaLine(item.Corrected, row));
                }
                else
                {
                    output.Add(AirLogProcessor.FormatAirLine(item.Corrected, row));
                }
            }

            return output;
        }

        private static int StatusSortKey(string status)
        {
            return status switch
            {
                "COMPLETED" => 0,   // ON-AIR
                "STARTED" => 0,   // ON-AIR fallback
                "SHORT" => 1,   // synthetic SHORT
                "STOPPED" => 2,   // should not appear directly
                _ => 3
            };
        }
    }
}