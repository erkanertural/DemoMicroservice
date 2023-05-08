namespace Library;

public interface IConnectionStringsKeys
{
    public string Default { get; set; }
}

public interface IAppSettings
{
    public string Secret { get; set; }
    public string WildDuckUrl { get; set; }
    public string WildDuckAccessToken { get; set; }
    public string AesCryptKey { get; set; }
    public string QuotaByte { get; set; }
    public string KafkaIP { get; set; }
    public string MailDomain { get; set; }
}

public class AppSettings : IAppSettings
{
    public string Secret { get; set; }
    public string WildDuckUrl { get; set; }
    public string WildDuckAccessToken { get; set; }
    public string AesCryptKey { get; set; }
    public string QuotaByte { get; set; }
    public string KafkaIP { get; set; }
    public string MailDomain { get; set; }
    public string ApplicationUrl { get; set; }
}
