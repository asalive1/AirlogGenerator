using AirlogGenerator.Services;

namespace AirlogGenerator.Models
{
    public class RotatorPartnerIdQuery : IAirLogQueryStrategy
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
                    row.Artist = string.IsNullOrWhiteSpace(row.PartnerId)
                        ? row.Artist
                        : row.PartnerId;
                    result.Add(row);
                    continue;
                }

                if (!AirLogRowHelpers.IsRotator(row))
                {
                    row.Artist = string.IsNullOrWhiteSpace(row.PartnerId)
                        ? row.Artist
                        : row.PartnerId;
                    result.Add(row);
                    continue;
                }

                var abbrev = AirLogRowHelpers.GetTypeAbbreviation(row.PlaylistType);

                var playlistOnly = new AirLogRow
                {
                    AirDate = row.AirDate,
                    AirTimeMs = row.AirTimeMs,
                    Status = row.Status,

                    Cart = $"{abbrev}{row.PlaylistCart}",
                    Category = row.PlaylistCategory,
                    Title = row.PlaylistTitle,
                    Artist = string.IsNullOrWhiteSpace(row.PartnerId)
                        ? row.PlaylistArtist
                        : row.PartnerId,

                    LengthMs = row.LengthMs,
                    OriginalScheduledTime = row.OriginalScheduledTime,
                    EntryType = row.EntryType,
                    EntryDescription = row.EntryDescription,
                    PartnerId = row.PartnerId
                };

                result.Add(playlistOnly);
            }

            return result;
        }
    }
}