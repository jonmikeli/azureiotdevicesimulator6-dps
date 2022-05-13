using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;

using System.Net.Http.Headers;
using System.Text;

namespace JSON.Dynamic.Tools
{
    public class JSONDynamicContent
    {
        public async Task<JSchema> LoadSchemaAsync(string schemaUrl)
        {
            if (string.IsNullOrEmpty(schemaUrl))
                throw new ArgumentNullException(nameof(schemaUrl));

            JSchema schema = null;

            //HTTP GET-download the schema file
            string schemaContent = string.Empty;
            using (HttpClient httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Accept.Clear();
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                httpClient.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("utf-8"));

                var buffer = await httpClient.GetByteArrayAsync(schemaUrl);

                if (buffer != null && buffer.Length > 0)
                    schemaContent = System.Text.Encoding.UTF8.GetString(buffer);
            }

            //Deserialize the schema file into a JObject
            if (!string.IsNullOrEmpty(schemaContent))
                schema = JSchema.Parse(schemaContent);

            return schema;
        }

        public async Task<bool> IsValid(JSchema schema)
        {
            return true;
        }

        public async Task BuildContentFromSchemaUrl(string schemaUrl)
        {
            JSchema schema = await LoadSchemaAsync(schemaUrl);
            await BuildContentFromSchema(schema);
        }

        public async Task<string> BuildContentFromSchema(JSchema schema)
        {
            if (!await IsValid(schema))
                throw new Exception("The schema is not valid");

            Random r = new Random(DateTime.UtcNow.Millisecond);
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("{");
            foreach (var item in schema.Properties)
            {
                switch (item.Value.Type)
                {
                    case JSchemaType.Array:
                        if (item.Value.Default != null)
                            stringBuilder.AppendLine($"\"{item.Key}\": [{item.Value.Default.Value}]");
                        else
                            stringBuilder.AppendLine($"\"{item.Key}\": \"{item.Value.Default}\"");
                        break;
                    case JSchemaType.Boolean:
                        if (item.Value.Default != null)
                            stringBuilder.AppendLine($"\"{item.Key}\": {((bool)item.Value.Default)}");
                        else
                            stringBuilder.AppendLine($"\"{item.Key}\": \"{item.Value.Default}\"");
                        break;
                    case JSchemaType.Integer:
                        if (item.Value.Default != null)
                            stringBuilder.AppendLine($"\"{item.Key}\": {((bool)item.Value.Default)}");
                        else
                            stringBuilder.AppendLine($"\"{item.Key}\": \"{item.Value.Default}\"");
                        break;
                    case JSchemaType.Number:
                        if (item.Value.Default != null)
                            stringBuilder.AppendLine($"\"{item.Key}\": {((bool)item.Value.Default)}");
                        else
                            stringBuilder.AppendLine($"\"{item.Key}\": \"{item.Value.Default}\"");
                        break;
                    case JSchemaType.Object:
                        if (item.Value.Default != null)
                            stringBuilder.AppendLine($"\"{item.Key}\": {((bool)item.Value.Default)}");
                        else
                            stringBuilder.AppendLine($"\"{item.Key}\": \"{item.Value.Default}\"");
                        break;
                    case JSchemaType.String:
                        if (item.Value.Default != null)
                            stringBuilder.AppendLine($"\"{item.Key}\": {((bool)item.Value.Default)}");
                        else
                            stringBuilder.AppendLine($"\"{item.Key}\": \"{item.Value.Default}\"");
                        break;
                    default:
                        break;
                }
            }
            stringBuilder.AppendLine("}");
        }
    }
}