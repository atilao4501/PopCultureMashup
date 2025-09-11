using System.Text.Json.Serialization;
using PopCultureMashup.Infrastructure.External.Converters;

namespace PopCultureMashup.Infrastructure.External.DTOs;

public class OpenLibWorkDto
{
    public string key { get; set; } = "";               // "/works/OL12345W"
    public string title { get; set; } = "";
    public string? first_publish_date { get; set; }     // "YYYY" ou "YYYY-MM-DD"
    [JsonConverter(typeof(DescriptionUnionConverter))]
    public DescriptionUnion? description { get; set; }       // string ou { value = "" }
    public List<string>? subjects { get; set; }
    
    public class DescriptionUnion
    {
        public string? value { get; set; }
    }
}

public class OpenLibSearchPageDto
{
    public int numFound { get; set; }
    public List<Doc> docs { get; set; } = new();
    public class Doc
    {
        public string key { get; set; } = "";          // "/works/OL12345W"
        public string title { get; set; } = "";
        public int? first_publish_year { get; set; }
        public List<string>? author_name { get; set; }
        public List<string>? subject { get; set; }
    }
}