using System.ComponentModel.DataAnnotations;

namespace SafeRoute.Models;

public class RouteRequest
{
    [Required, Display(Name = "Origin (address or place)")]
    public string Origin { get; set; } = "";

    [Required, Display(Name = "Destination (address or place)")]
    public string Destination { get; set; } = "";

    [Display(Name = "Time of travel")]
    public TimeOfDay TimeOfDay { get; set; } = TimeOfDay.Evening;
}

public enum TimeOfDay
{
    Morning,
    Afternoon,
    Evening,
    Night
}
