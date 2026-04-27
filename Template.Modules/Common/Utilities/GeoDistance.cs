namespace Template.Modules.Common.Utilities;

public static class GeoDistance
{
    public static double HaversineMeters(decimal lat1, decimal lon1, decimal lat2, decimal lon2)
    {
        const double earthRadiusMeters = 6_371_000d;

        var latitude1 = DegreesToRadians((double)lat1);
        var latitude2 = DegreesToRadians((double)lat2);
        var deltaLatitude = DegreesToRadians((double)(lat2 - lat1));
        var deltaLongitude = DegreesToRadians((double)(lon2 - lon1));

        var a = Math.Sin(deltaLatitude / 2) * Math.Sin(deltaLatitude / 2) +
                Math.Cos(latitude1) * Math.Cos(latitude2) *
                Math.Sin(deltaLongitude / 2) * Math.Sin(deltaLongitude / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return earthRadiusMeters * c;
    }

    private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180d;
}
