using Microsoft.Extensions.Logging;

namespace Tgs.Payment.MetaPay;

public class MetaPaySigner
{
    private readonly ILogger<MetaPaySigner> _logger;

    public MetaPaySigner(ILogger<MetaPaySigner> logger)
    {
        _logger = logger;
    }

    public string CreateSign(
        IEnumerable<KeyValuePair<string, string?>> dict,
        string privateKey)
    {
        var sortDict =
            dict.Where(kvp => !string.IsNullOrEmpty(kvp.Value))
                .OrderBy(kvp => kvp.Key)
                .Select(p => $"{p.Value}");

        var origin = string.Join("", sortDict);

        var cryptoer = new MetaPayCryptoer();

        var sign = cryptoer.EncryptByPrivateKey(origin, privateKey);

        _logger.LogInformation("CreateSign origin:{origin}", origin);

        return sign;
    }

    public bool VerifySign(IDictionary<string, object> dict, string publickey)
    {
        var sortDict =
            dict.Where(kvp => kvp.Key != "sign")
                .OrderBy(kvp => kvp.Key, StringComparer.Ordinal)
                .Select(p => $"{p.Value}");

        var origin = string.Join("", sortDict);

        var cryptoer = new MetaPayCryptoer();

        var decryptText = cryptoer.DecryptByPublicKey(dict["sign"].ToString()!, publickey);

        _logger.LogInformation("VerifySign decryptText:{decryptText}", decryptText);

        return origin.Equals(decryptText, StringComparison.OrdinalIgnoreCase);
    }
}
