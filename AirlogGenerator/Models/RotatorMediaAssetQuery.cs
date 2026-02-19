using AirlogGenerator.Services;

namespace AirlogGenerator.Models
{

    public class RotatorMediaAssetQuery : IAirLogQueryStrategy
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
                // ⭐ Events pass through unchanged
                if (AirLogRowHelpers.IsNonMediaEvent(row))
                {
                    result.Add(row);
                    continue;
                }

                // ⭐ Non-rotator media pass through unchanged
                if (!AirLogRowHelpers.IsRotator(row))
                {
                    result.Add(row);
                    continue;
                }

                var abbrev = AirLogRowHelpers.GetTypeAbbreviation(row.PlaylistType);

                // ⭐ ROTATOR: add playlist cut FIRST
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

                // ⭐ ROTATOR: add media asset SECOND
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