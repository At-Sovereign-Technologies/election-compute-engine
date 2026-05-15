namespace Election.VoteVault.Interfaces;

public interface IServicioFirmaDigital
{
    // Firma un payload con la clave privada del sistema y devuelve la firma en base64.
    string Firmar(string payload);

    // Verifica que la firma corresponda al payload usando la clave pública del sistema.
    bool Verificar(string payload, string firmaBase64);

    // Clave pública del sistema en formato PEM, para que verificadores externos
    // puedan validar comprobantes sin acceso a la clave privada.
    string ObtenerClavePublicaPem();
}
