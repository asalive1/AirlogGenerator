using AirlogGenerator.Models;

public static class AirLogRowHelpers
{
    public static bool IsRotatorPair(AirLogRow a, AirLogRow b)
    {
        if (a == null || b == null)
            return false;

        // Must share exact timestamp
        if (a.AirDate != b.AirDate || a.AirTimeMs != b.AirTimeMs)
            return false;

        // Must have different carts
        if (string.Equals(a.Cart, b.Cart, StringComparison.OrdinalIgnoreCase))
            return false;

        // Titles or artists must differ
        bool titleDiffers = !string.Equals(a.Title, b.Title, StringComparison.OrdinalIgnoreCase);
        bool artistDiffers = !string.Equals(a.Artist, b.Artist, StringComparison.OrdinalIgnoreCase);

        return titleDiffers || artistDiffers;
    }
}