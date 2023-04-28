using System.Text;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Encodings;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Security;

namespace Tgs.Payment.MetaPay;

internal class MetaPayCryptoer
{
    public string EncryptByPrivateKey(string origin, string privateKey)
    {
        IAsymmetricBlockCipher engine = new Pkcs1Encoding(new RsaEngine());

        byte[] privateInfoByte = Convert.FromBase64String(privateKey);

        var priKey = PrivateKeyFactory.CreateKey(privateInfoByte);

        engine.Init(true, priKey);
        var byteData = Encoding.UTF8.GetBytes(origin);

        var result = Array.Empty<byte>();
        for (int i = 0, j = byteData.Length / 117 + (byteData.Length % 117 == 0 ? 0 : 1); i < j; i++)
        {
            var getData = byteData.Skip(i * 117).Take(117).ToArray();
            var ResultData = engine.ProcessBlock(getData, 0, getData.Length);
            result = result.Concat(ResultData).ToArray();
        }

        return Convert.ToBase64String(result);
    }

    public string DecryptByPublicKey(string sign, string publicKey)
    {                
        IAsymmetricBlockCipher engine = new Pkcs1Encoding(new RsaEngine());
        
        var publicInfoByte = Convert.FromBase64String(publicKey);
        var pubKey = PublicKeyFactory.CreateKey(publicInfoByte);

        engine.Init(false, pubKey);
        var byteData = Convert.FromBase64String(sign);

        var decryptText = "";

        for (int i = 0, j = byteData.Length / 128 + (byteData.Length % 128 == 0 ? 0 : 1); i < j; i++)
        {
            var getData = byteData.Skip(i * 128).Take(128).ToArray();
            var ResultData = engine.ProcessBlock(getData, 0, getData.Length);
            decryptText += Encoding.UTF8.GetString(ResultData);
        }
        return decryptText;
    }
}