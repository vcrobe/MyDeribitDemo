using System.Text.Json;

namespace MyDeribitApiLibrary;

class LowerCaseJsonNamingPolicy : JsonNamingPolicy
{
    public override string ConvertName(string name) => name.ToLower();
}