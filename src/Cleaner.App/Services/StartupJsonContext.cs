using System.Text.Json.Serialization;
using Cleaner.Core.Services;
namespace Cleaner.App.Services;
[JsonSourceGenerationOptions(WriteIndented=true,PropertyNameCaseInsensitive=true)]
[JsonSerializable(typeof(StartupBaseline))]
[JsonSerializable(typeof(List<StartupChange>))]
internal partial class StartupJsonContext : JsonSerializerContext { }
