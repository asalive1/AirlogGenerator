using AirlogGenerator.Services;

namespace AirlogGenerator.Models
{
    public class StandardAirLogQuery : IAirLogQueryStrategy
    {
        public async Task<List<AirLogRow>> ExecuteAsync(
            DatabaseService db,
            string station,
            DateTime date)
        {
            var rows = await db.GetUnifiedRowsAsync(station, date);

            var result = new List<AirLogRow>();

            foreach (var row in rows)
            {
                if (AirLogRowHelpers.IsNonMediaEvent(row))
                {
                    result.Add(row);
                    continue;
                }

                if (!AirLogRowHelpers.IsRotator(row))
                {
                    result.Add(row);
                    continue;
                }

                // ROTATOR: keep only media asset, but prefix cart
                var abbrev = AirLogRowHelpers.GetTypeAbbreviation(row.PlaylistType);

                var mediaOnly = new AirLogRow
                {
                    AirDate = row.AirDate,
                    AirTimeMs = row.AirTimeMs,
                    Status = row.Status,

                    Cart = $"{abbrev}{row.MediaCart}",
                    Category = row.MediaCategory,
                    Title = row.MediaTitle,
                    Artist = row.MediaArtist,

                    LengthMs = row.LengthMs,
                    OriginalScheduledTime = row.OriginalScheduledTime,
                    EntryType = row.EntryType,
                    EntryDescription = row.EntryDescription,
                    PartnerId = row.PartnerId,

                    MediaCart = row.MediaCart,
                    MediaCategory = row.MediaCategory,
                    MediaTitle = row.MediaTitle,
                    MediaArtist = row.MediaArtist,
                    MediaClass = row.MediaClass
                };

                result.Add(mediaOnly);
            }

            return result;
        }
    }
}