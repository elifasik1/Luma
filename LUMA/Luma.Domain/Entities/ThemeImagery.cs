using System.Text.Json.Serialization;

namespace Luma.Domain.Entities;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ThemeImagery
{
    [JsonStringEnumMemberName("city")]
    City,
    [JsonStringEnumMemberName("nature")]
    Nature,
    [JsonStringEnumMemberName("ocean")]
    Ocean,
    [JsonStringEnumMemberName("campus")]
    Campus,
    [JsonStringEnumMemberName("workspace")]
    Workspace,
    [JsonStringEnumMemberName("bakery")]
    Bakery,
    [JsonStringEnumMemberName("travel")]
    Travel,
    [JsonStringEnumMemberName("home")]
    Home,
    [JsonStringEnumMemberName("books")]
    Books,
    [JsonStringEnumMemberName("technology")]
    Technology
}
