using CsvHelper;
using CsvHelper.Configuration;
using SafeRoute.Models;
using System.Globalization;

namespace SafeRoute.Services;

public class CrimeDataService
{
    private readonly List<CrimeIncident> _incidents;

    public CrimeDataService(IWebHostEnvironment env)
    {
        var csvPath = Path.Combine(env.ContentRootPath, "Data", "saps_crime_data.csv");
        _incidents = LoadFromCsv(csvPath);
    }

    public List<CrimeIncident> GetIncidentsNear(double lat, double lng, double radiusKm)
    {
        return _incidents
            .Where(i => HaversineKm(lat, lng, i.Latitude, i.Longitude) <= radiusKm)
            .OrderByDescending(i => i.Count)
            .ToList();
    }

    public int GetCrimeScoreNear(double lat, double lng, double radiusKm)
    {
        var nearby = GetIncidentsNear(lat, lng, radiusKm);
        if (!nearby.Any()) return 0;
        // Weighted score: murders * 5 + robberies * 2 + sexual offences * 4
        return nearby.Sum(i => i.Category switch
        {
            "Murder" => i.Count * 5,
            "Sexual offences" => i.Count * 4,
            _ => i.Count * 2
        });
    }

    private static List<CrimeIncident> LoadFromCsv(string path)
    {
        if (!File.Exists(path)) return new List<CrimeIncident>();
        var config = new CsvConfiguration(CultureInfo.InvariantCulture) { HasHeaderRecord = true };
        using var reader = new StreamReader(path);
        using var csv = new CsvReader(reader, config);
        return csv.GetRecords<CrimeIncident>().ToList();
    }

    private static double HaversineKm(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371;
        var dLat = ToRad(lat2 - lat1);
        var dLon = ToRad(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
              + Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2))
              * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }

    private static double ToRad(double deg) => deg * Math.PI / 180;
}
