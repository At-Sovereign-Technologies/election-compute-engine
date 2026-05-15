namespace Election.Core.Models;

// Tipo de asistencia declarado por el jurado al registrar la sesión asistida.
// Sirve para el conteo agregado en el acta de mesa (US-SE-M3-05 CA #5).
public enum TipoAsistencia
{
    Discapacidad,
    EdadAvanzada,
    Analfabetismo,
    Otra
}
