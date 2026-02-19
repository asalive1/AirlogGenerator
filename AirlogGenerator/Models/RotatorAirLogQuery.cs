using AirlogGenerator.Services;

namespace AirlogGenerator.Models
{
    public class RotatorAirLogQuery : IAirLogQueryStrategy
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

                var abbrev = AirLogRowHelpers.GetTypeAbbreviation(row.PlaylistType);

                var playlistOnly = new AirLogRow
                {
                    AirDate = row.AirDate,
                    AirTimeMs = row.AirTimeMs,
                    Status = row.Status,

                    Cart = $"{abbrev}{row.PlaylistCart}",
                    Category = row.PlaylistCategory,
                    Title = row.PlaylistTitle,
                    Artist = row.PlaylistArtist,

                    LengthMs = row.LengthMs,
                    OriginalScheduledTime = row.OriginalScheduledTime,
                    EntryType = row.EntryType,
                    EntryDescription = row.EntryDescription,
                    PartnerId = row.PartnerId,

                    PlaylistCart = row.PlaylistCart,
                    PlaylistCategory = row.PlaylistCategory,
                    PlaylistTitle = row.PlaylistTitle,
                    PlaylistArtist = row.PlaylistArtist,
                    PlaylistClass = row.PlaylistClass,
                    PlaylistOriginalType = row.PlaylistOriginalType,
                    PlaylistType = row.PlaylistType
                };

                result.Add(playlistOnly);
            }

            return result;
        }
    }
}