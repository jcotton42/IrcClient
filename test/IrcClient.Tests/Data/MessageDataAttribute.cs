using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace IrcClient.Tests.Data;

public class MessageDataAttribute<T>(string filename) : DataSourceGeneratorAttribute<T>
{
    public override IEnumerable<Func<T>> GenerateDataSources(DataGeneratorMetadata dataGeneratorMetadata)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Data", filename);
        using var file = File.OpenText(path);
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();
        var data = deserializer.Deserialize<TestDataRoot<T>>(file);

        foreach (var testRow in data.Tests)
        {
            yield return () => testRow;
        }
    }
}
