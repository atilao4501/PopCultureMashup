namespace PopCultureMashup.Infrastructure.External.DTOs;

public class RawgGameDto
{
    public int id { get; set; }
    public string name { get; set; } = "";
    public string? released { get; set; }
    public double? rating { get; set; }
    public string? description_raw { get; set; }

    public List<NamedValue> genres { get; set; } = new();
    public List<NamedValue>? developers { get; set; }

    public class NamedValue
    {
        public string name { get; set; } = "";
    }
}

public class RawgSearchPageDto
{
    public int count { get; set; }
    public List<RawgGameDto> results { get; set; } = new();
}