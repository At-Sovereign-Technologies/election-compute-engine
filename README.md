# Election Compute Engine

## Descripción

Sistema electoral modular desarrollado en .NET 8 para procesamiento y custodia segura de votos. El proyecto implementa:

- Motor de cómputo electoral (Instant Runoff Voting / Alternative Vote)
- Bóveda criptográfica de votos
- Heartbeat criptográfico y sellado temporal
- Ceremonia de apertura multifirma

El sistema sigue una arquitectura monolítica modular basada en separación de responsabilidades y uso de interfaces.

---

# Arquitectura

```txt
ElectionComputeEngine.sln

├── Election.Core
├── Election.Engine
├── Election.VoteVault
├── Election.Api
└── Election.Tests
```

## Election.Core

Modelos e interfaces compartidas.

## Election.Engine

Implementación del motor de cómputo electoral.

## Election.VoteVault

Custodia criptográfica, sellado temporal y ceremonia multifirma.

## Election.Api

API REST y configuración HTTP.

## Election.Tests

Pruebas unitarias.

---

# Funcionalidades

## Motor Electoral

- Método Alternative Vote (IRV)
- Redistribución de votos
- Manejo de votos agotados
- Trazabilidad por rondas
- Generación de resultados auditables

## Bóveda Criptográfica

- Custodia de votos cifrados
- Cifrado RSA
- Política Zero Trust
- Bloqueo de lectura de payloads

## Heartbeat Criptográfico

- Generación periódica de hash raíz
- Sellado temporal
- Detección de alteraciones
- Registro de sellos históricos

## Ceremonia Multi-Firma

- Apertura por quorum
- Validación M de N
- Llave efímera en memoria
- Habilitación controlada de escrutinio

---

# Tecnologías

- .NET 8
- ASP.NET Core
- Swagger / OpenAPI
- RSA Encryption
- Background Services

---

# Requisitos

- .NET SDK 8+

Verificar instalación:

```bash
dotnet --version
```

---

# Instalación

## Clonar repositorio

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

## Compilar

```bash
dotnet build
```

---

# Ejecución

```bash
dotnet run --project Election.Api
```

Swagger:

```txt
http://localhost:5284/swagger
```

---

# Endpoints principales

## Election Engine

```txt
POST /api/election/vote
GET  /api/election/result
```

## Vote Vault

```txt
POST /api/vault/custody
GET  /api/vault/count
GET  /api/vault/votes
```

## Cryptographic Seals

```txt
GET /api/seals
```

## Opening Ceremony

```txt
POST /api/ceremony/open
GET  /api/ceremony/status
```

---

# Autor

Proyecto académico de Arquitectura de Software desarrollado en .NET 8.
