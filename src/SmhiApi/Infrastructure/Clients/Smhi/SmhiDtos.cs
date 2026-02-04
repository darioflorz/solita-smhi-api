using System.Text.Json.Serialization;

namespace SmhiApi.Infrastructure.Clients.Smhi;

/// <summary>
/// Response from SMHI parameter endpoint containing list of stations
/// </summary>
public class SmhiParameterResponse
{
    [JsonPropertyName("station")]
    public SmhiStation[] Station { get; set; } = Array.Empty<SmhiStation>();
}

/// <summary>
/// Station information from SMHI API
/// </summary>
public class SmhiStation
{
    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("active")]
    public bool Active { get; set; }

    [JsonPropertyName("latitude")]
    public double Latitude { get; set; }

    [JsonPropertyName("longitude")]
    public double Longitude { get; set; }

    [JsonPropertyName("height")]
    public double Height { get; set; }
}

/// <summary>
/// Response from SMHI observation endpoint
/// </summary>
public class SmhiObservationResponse
{
    [JsonPropertyName("station")]
    public SmhiObservationStation Station { get; set; } = new();

    [JsonPropertyName("value")]
    public SmhiObservationValue[] Value { get; set; } = Array.Empty<SmhiObservationValue>();
}

/// <summary>
/// Station information within observation response
/// </summary>
public class SmhiObservationStation
{
    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// Individual observation value
/// </summary>
public class SmhiObservationValue
{
    /// <summary>
    /// Unix timestamp in milliseconds
    /// </summary>
    [JsonPropertyName("date")]
    public long Date { get; set; }

    /// <summary>
    /// Observation value as string (may be null or empty)
    /// </summary>
    [JsonPropertyName("value")]
    public string? Value { get; set; }

    /// <summary>
    /// Quality indicator
    /// </summary>
    [JsonPropertyName("quality")]
    public string? Quality { get; set; }
}
