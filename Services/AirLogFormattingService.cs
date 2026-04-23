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

            // Apply optional offset to every raw row timestamp.
            var correctedRows = rows
                .Select(r =>
                {
                    var serverTime = r.AirDate.Date.AddMilliseconds(r.AirTimeMs);

                    var corrected = useOffset
                        ? AirLogProcessor.ApplyOffset(serverTime, offsetHours)
                        : serverTime;

                    return (Corrected: corrected, Row: r);
                })
                .ToList();

            var expandedRows = new List<(DateTime Corrected, AirLogRow Row)>();

            // Non-media events continue to be included by AIR DATE.
            foreach (var item in correctedRows.Where(x => AirLogRowHelpers.IsNonMediaEvent(x.Row)))
            {
                if (item.Row.AirDate.Date == targetDate.Date)
                    expandedRows.Add(item);
            }

            // Media rows are grouped by logical output row identity so STARTED and terminal
            // records can be paired per item (including rotator/media-asset expansions).
            var mediaGroups = correctedRows
                .Where(x => !AirLogRowHelpers.IsNonMediaEvent(x.Row))
                .GroupBy(x => new
                {
                    x.Row.PlaylistEntryId,
                    x.Row.Cart,
                    x.Row.Category,
                    x.Row.Title,
                    x.Row.Artist,
                    x.Row.EntryType,
                    x.Row.EntryDescription,
                    x.Row.PartnerId
                });

            foreach (var group in mediaGroups)
            {
                var groupRows = group.OrderBy(x => x.Corrected).ToList();

                var start = groupRows
                    .Where(x => x.Row.Status == "STARTED")
                    .OrderBy(x => x.Corrected)
                    .FirstOrDefault();

                (DateTime Corrected, AirLogRow Row) primary = default;

                // Prefer a terminal row when present, otherwise use STARTED.
                foreach (var preferredStatus in new[] { "COMPLETED", "SKIPPED", "FAILED", "STARTED" })
                {
                    primary = groupRows
                        .Where(x => x.Row.Status == preferredStatus)
                        .OrderBy(x => x.Corrected)
                        .FirstOrDefault();

                    if (primary.Row != null)
                        break;
                }

                if (primary.Row != null)
                {
                    var publishTime = start.Row != null ? start.Corrected : primary.Corrected;

                    // For terminal rows, carry actual played time when STARTED exists.
                    if (start.Row != null &&
                        primary.Corrected > start.Corrected &&
                        (primary.Row.Status == "COMPLETED" ||
                         primary.Row.Status == "SKIPPED" ||
                         primary.Row.Status == "FAILED"))
                    {
                        primary.Row.LengthMs = (int)(primary.Corrected - start.Corrected).TotalMilliseconds;
                    }

                    if (AirLogProcessor.BelongsToBroadcastDay(publishTime, targetDate))
                        expandedRows.Add((publishTime, primary.Row));
                }

                var stop = groupRows
                    .Where(x => x.Row.Status == "STOPPED")
                    .OrderBy(x => x.Corrected)
                    .FirstOrDefault();

                // Keep SHORT at stop timestamp; duration is computed from STARTED -> STOPPED.
                if (start.Row != null && stop.Row != null && stop.Corrected > start.Corrected)
                {
                    var shortRow = AirLogProcessor.BuildShortRow(start.Row, stop.Row);

                    if (AirLogProcessor.BelongsToBroadcastDay(stop.Corrected, targetDate))
                        expandedRows.Add((stop.Corrected, shortRow));
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