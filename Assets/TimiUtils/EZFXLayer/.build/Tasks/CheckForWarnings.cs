namespace BuildTasks
{
    using System;
    using System.Linq;
    using System.IO;
    using System.Text.Json;
    using Microsoft.Build.Framework;

    public class CheckForWarnings : Microsoft.Build.Utilities.Task
    {
        [Required]
        public string? CscErrorLog { get; set; }

        [Output]
        public bool HasWarnings { get; set; }

        public override bool Execute()
        {
            using JsonDocument doc = JsonDocument.Parse(
                File.ReadAllText(
                    CscErrorLog ?? throw new ArgumentNullException(nameof(CscErrorLog))));

            HasWarnings = doc.RootElement.GetProperty("runs")[0].GetProperty("results").EnumerateArray().Any(result =>
                result.GetProperty("level").ValueEquals("warning") &&
                    (!result.TryGetProperty("suppressionStates", out JsonElement suppressionStates) ||
                    suppressionStates.GetArrayLength() == 0));

            return true;
        }
    }
}
