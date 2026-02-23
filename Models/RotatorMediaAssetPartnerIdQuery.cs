using AirlogGenerator.Services;

namespace AirlogGenerator.Models
{
    public class RotatorMediaAssetPartnerIdQuery : IAirLogQueryStrategy
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
                // ⭐ EVENTS — pass through, but Artist = PartnerId when available
                if (AirLogRowHelpers.IsNonMediaEvent(row))
                {
                    row.Artist = string.IsNullOrWhiteSpace(row.PartnerId)
                        ? row.Artist
                        : row.PartnerId;

                    result.Add(row);
                    continue;
                }

                // ⭐ NON‑ROTATOR MEDIA — same as Standard, but Artist = PartnerId when available
                if (!AirLogRowHelpers.IsRotator(row))
                {
                    row.Artist = string.IsNullOrWhiteSpace(row.PartnerId)
                        ? row.Artist
                        : row.PartnerId;

                    result.Add(row);
                    continue;
                }

                // ⭐ ROTATOR — we output TWO rows:
                // 1. Playlist cut
                // 2. Media asset cut

                var abbrev = AirLogRowHelpers.GetTypeAbbreviation(row.PlaylistType);

                // -----------------------------
                // 1️⃣ PLAYLIST CUT (first)
                // -----------------------------
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

                // -----------------------------
                // 2️⃣ MEDIA ASSET CUT (second)
                // -----------------------------
                var mediaOnly = new AirLogRow
                {
                    AirDate = row.AirDate,
                    AirTimeMs = row.AirTimeMs,
                    Status = row.Status,

                    Cart = $"{abbrev}{row.MediaCart}",
                    Category = row.MediaCategory,
                    Title = row.MediaTitle,
                    Artist = string.IsNullOrWhiteSpace(row.PartnerId)
                        ? row.MediaArtist
                        : row.PartnerId,

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