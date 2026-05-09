# Election Compute Engine

## Descripción

Election Compute Engine es un motor de cómputo electoral desacoplado desarrollado en .NET 8. El sistema implementa el método electoral de Voto Alternativo (Instant Runoff Voting - IRV), permitiendo procesar preferencias de votantes, redistribuir votos entre rondas y generar resultados auditables.

El proyecto fue diseñado siguiendo principios de arquitectura limpia y extensibilidad, permitiendo incorporar nuevos métodos electorales sin modificar el núcleo del sistema.

---

# Características principales

- Implementación del método Voto Alternativo (IRV)
- Procesamiento de preferencias ordenadas por votante
- Redistribución automática de votos
- Manejo de votos agotados
- Trazabilidad completa por rondas
- Arquitectura desacoplada mediante interfaces
- Inyección de dependencias
- API REST con Swagger
- Operación completamente en memoria
- Uso de precisión decimal para cálculos electorales

---

# Arquitectura

El sistema se encuentra dividido en varios proyectos:

```txt
ElectionComputeEngine.sln

├── Election.Core
│   ├── Interfaces
│   └── Models
│
├── Election.Engine
│   └── Methods
│       └── AlternativeVote
│
├── Election.Api
│   └── Controllers
│
└── Election.Tests
```

## Election.Core

Contiene:

- Interfaces base del sistema
- Modelos de dominio
- Contratos del motor electoral

## Election.Engine

Contiene:

- Implementaciones de métodos electorales
- Lógica de negocio del algoritmo IRV

## Election.Api

Contiene:

- API REST
- Configuración de Swagger
- Endpoints HTTP

## Election.Tests

Contiene:

- Pruebas unitarias del sistema

---

# Principios arquitectónicos aplicados

## Strategy Pattern

Cada método electoral implementa la interfaz:

```csharp
IMetodoElectoral
```

Esto permite incorporar nuevos métodos sin alterar el motor existente.

## Inversión de Dependencias

El sistema utiliza inyección de dependencias para desacoplar el API de las implementaciones concretas.

## Open/Closed Principle

El motor está abierto para extensión y cerrado para modificación.

## Stateless Processing

El motor opera completamente en memoria y no tiene dependencias directas con bases de datos.

---

# Método Electoral Implementado

## Voto Alternativo (Instant Runoff Voting)

El algoritmo implementado sigue el siguiente flujo:

1. Lectura de preferencias ordenadas de cada votante.
2. Conteo de primeras preferencias activas.
3. Verificación de mayoría absoluta (>50%).
4. Eliminación del candidato con menor cantidad de votos.
5. Redistribución de votos hacia la siguiente preferencia activa.
6. Repetición del proceso hasta encontrar un ganador.
7. Registro de auditoría por ronda.

---

# Modelos principales

## Voto

Representa el voto de un ciudadano y sus preferencias ordenadas.

## Resultado

Representa el resultado final de la elección.

## RondaResultado

Representa la trazabilidad de cada ronda del algoritmo.

## Acta

Representa el acta generada por el método electoral.

---

# Tecnologías utilizadas

- .NET 8
- ASP.NET Core
- Swagger / OpenAPI
- C#
- Dependency Injection

---

# Requisitos

Antes de ejecutar el proyecto se debe tener instalado:

- .NET SDK 8.0 o superior
- Visual Studio Code (opcional)
- Git

Verificar instalación:

```bash
dotnet --version
```

---

# Instalación

## Clonar el repositorio

```bash
git clone <URL_DEL_REPOSITORIO>
```

## Entrar al proyecto

```bash
cd election-compute-engine
```

## Restaurar dependencias

```bash
dotnet restore
```

## Compilar el proyecto

```bash
dotnet build
```

---

# Ejecución

## Ejecutar la API

```bash
dotnet run --project Election.Api
```

## Ejecutar en modo watch

```bash
dotnet watch --project Election.Api
```

---

# Swagger

Una vez iniciada la aplicación, Swagger estará disponible en:

```txt
http://localhost:5284/swagger
```

El puerto puede variar dependiendo de la configuración local.

---

# Endpoints

## Registrar voto

### POST

```txt
/api/election/vote
```

### Ejemplo de body

```json
{
  "votanteId": "1",
  "preferencias": {
    "A": 1,
    "B": 2,
    "C": 3
  }
}
```

---

## Obtener resultados

### GET

```txt
/api/election/result
```

---

# Ejemplo de respuesta

```json
{
  "ganador": "B",
  "porcentaje": 0.6667,
  "totales": {
    "B": 2,
    "C": 1
  },
  "rondas": [
    {
      "numeroRonda": 1,
      "conteo": {
        "A": 1,
        "B": 1,
        "C": 1
      },
      "candidatoEliminado": "A",
      "votosAgotados": 0
    },
    {
      "numeroRonda": 2,
      "conteo": {
        "B": 2,
        "C": 1
      },
      "candidatoEliminado": null,
      "votosAgotados": 0
    }
  ]
}
```

---