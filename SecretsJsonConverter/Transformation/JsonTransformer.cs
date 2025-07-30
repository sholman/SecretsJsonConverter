using System.Text.Json;
using System.Text.Json.Nodes;

namespace SecretsConverterApp.Services
{
    public class JsonTransformer
    {
        public string TransformSecretsToAzureAppService(string inputJson, string platform)
        {
            if (string.IsNullOrWhiteSpace(inputJson))
                return "[]";

            try
            {
                var jsonNode = JsonNode.Parse(inputJson);
                if (jsonNode == null)
                    throw new JsonException("Invalid JSON format.");

                var flatSettings = new List<(string Name, string Value)>();
                FlattenJson(jsonNode.AsObject(), "", flatSettings, platform);

                var output = new JsonArray();
                foreach (var setting in flatSettings)
                {
                    output.Add(new JsonObject
                    {
                        ["name"] = setting.Name,
                        ["value"] = setting.Value,
                        ["slotSetting"] = false
                    });
                }

                return JsonSerializer.Serialize(output, new JsonSerializerOptions { WriteIndented = true });
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException("Failed to parse JSON: " + ex.Message);
            }
        }

        private void FlattenJson(JsonObject node, string prefix, List<(string Name, string Value)> result, string platform)
        {
            foreach (var property in node)
            {
                string key = string.IsNullOrEmpty(prefix) ? property.Key : $"{prefix}:{property.Key}";
                if (platform == "Linux")
                    key = key.Replace(":", "__").Replace(".", "_");

                if (property.Value is JsonObject nestedObject)
                {
                    FlattenJson(nestedObject, key, result, platform);
                }
                else if (property.Value is JsonArray nestedArray)
                {
                    for (int i = 0; i < nestedArray.Count; i++)
                    {
                        string arrayKey = $"{key}:{i}";
                        if (platform == "Linux")
                            arrayKey = arrayKey.Replace(":", "__").Replace(".", "_");

                        if (nestedArray[i] is JsonObject arrayObject)
                        {
                            FlattenJson(arrayObject, arrayKey, result, platform);
                        }
                        else
                        {
                            result.Add((arrayKey, nestedArray[i]?.ToString() ?? ""));
                        }
                    }
                }
                else
                {
                    result.Add((key, property.Value?.ToString() ?? ""));
                }
            }
        }
    }
}