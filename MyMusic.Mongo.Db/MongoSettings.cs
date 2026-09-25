namespace MyMusic.Mongo.Db;

public class MongoSettings
{
    public const string SectionName = "MongoDB";

    public string ConnectionString { get; set; } = string.Empty;

    public string Database { get; set; } = "MyMusicDB";
}
