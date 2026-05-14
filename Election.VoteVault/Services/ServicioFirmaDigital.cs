using System.Security.Cryptography;
using System.Text;
using Election.VoteVault.Interfaces;

namespace Election.VoteVault.Services;

public class ServicioFirmaDigital : IServicioFirmaDigital
{
    private readonly RSA _rsa;

    public ServicioFirmaDigital()
    {
        _rsa = RSA.Create(2048);
    }

    public string Firmar(string payload)
    {
        byte[] data = Encoding.UTF8.GetBytes(payload);
        byte[] firma = _rsa.SignData(
            data,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pss);
        return Convert.ToBase64String(firma);
    }

    public bool Verificar(string payload, string firmaBase64)
    {
        byte[] data = Encoding.UTF8.GetBytes(payload);
        byte[] firma = Convert.FromBase64String(firmaBase64);
        return _rsa.VerifyData(
            data,
            firma,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pss);
    }

    public string ObtenerClavePublicaPem()
    {
        return _rsa.ExportRSAPublicKeyPem();
    }
}
