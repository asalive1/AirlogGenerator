using AirlogGenerator.Models;
using AirlogGenerator.Services;

public class PartnerIdAirLogQuery : IAirLogQueryStrategy
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
            // Events: same as Standard, but Artist = PartnerId
            if (AirLogRowHelpers.IsNonMediaEvent(row))
            {
                row.Artist = string.IsNullOrWhiteSpace(row.PartnerId)
                    ? row.Artist
                    : row.PartnerId;
                result.Add(row);
                continue;
            }

            // Non-rotator media: same as Standard, but Artist = PartnerId
            if (!AirLogRowHelpers.IsRotator(row))
            {
                row.Artist = string.IsNullOrWhiteSpace(row.PartnerId)
                    ? row.Artist
                    : row.PartnerId;
                result.Add(row);
                continue;
            }

            // ROTATOR: same as RotatorAirLogQuery, but Artist = PartnerId
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