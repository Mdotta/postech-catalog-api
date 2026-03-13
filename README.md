# catalog-api — Microsserviço de Catálogo (FCG)

Microsserviço de **Catálogo** da FIAP Cloud Games (Tech Challenge). Responsável pelo CRUD de jogos, início do fluxo de compra e pela biblioteca do usuário após pagamento aprovado.

## Finalidade

- **CRUD de jogos**: listar, criar, atualizar e remover jogos.
- **Iniciar fluxo de compra**: receber requisição de compra e publicar `OrderPlacedEvent` na fila (RabbitMQ).
- **Biblioteca do usuário**: consumir `PaymentProcessedEvent` e, se status `Approved`, adicionar o jogo à biblioteca; expor endpoint para consultar a biblioteca.

## Tecnologias

- .NET 9, Minimal API
- PostgreSQL (EF Core)
- MassTransit + RabbitMQ (mensageria)
- Docker (multi-stage), Kubernetes (Deployment, Service, ConfigMap, Secret)

## Estrutura

- `Interfaces/` — contratos dos serviços
- `Services/` — implementações (Game, Order, UserLibrary)
- `Models/` — Game, UserLibraryItem
- `Data/` — AppDbContext
- `Events/` — OrderPlacedEvent, PaymentProcessedEvent
- `Consumers/` — PaymentProcessedConsumer

## Variáveis de ambiente

| Variável | Descrição | Exemplo |
|----------|-----------|---------|
| `ConnectionStrings__Default` | Connection string PostgreSQL | `Host=localhost;Database=catalog;Username=postgres;Password=postgres` |
| `RabbitMQ__Host` | Host do RabbitMQ | `localhost` ou `rabbitmq`|
| `RabbitMQ__Username` | Usuário RabbitMQ | `guest` |
| `RabbitMQ__Password` | Senha RabbitMQ | `guest` |
| `ASPNETCORE_ENVIRONMENT` | Ambiente | `Development` / `Production` |


## Como rodar localmente

Não é necessário instalar PostgreSQL nem RabbitMQ na máquina. Use Docker para subir as dependências:

```bash
# 1. Sobe PostgreSQL e RabbitMQ (portas 5432 e 5672)
docker-compose up -d

# 2. Roda a API (connection string e RabbitMQ em appsettings.json já batem com o compose)
dotnet run
```

A API sobe na porta definida no `Properties/launchSettings.json` (ex.: **http://localhost:5050**). Abra no navegador:
- http://localhost:5050/health — health check
- http://localhost:5050/swagger — documentação e testes (quando `ASPNETCORE_ENVIRONMENT=Development`)

O `appsettings.json` já está configurado para `localhost` (PostgreSQL em 5432, RabbitMQ em 5672). Se quiser outras credenciais, altere o `docker-compose.yml` e o `appsettings.json` em conjunto.

## Imagem da API (Docker)

Para containerizar a própria API (ex.: deploy):

```bash
docker build -t catalog-api:latest .
docker run -p 8080:8080 -e ConnectionStrings__Default="Host=host.docker.internal;..." -e RabbitMQ__Host=host.docker.internal catalog-api:latest
```

## Endpoints

| Método | Rota | Descrição |
|--------|------|-----------|
| GET | `/health` | Health check |
| GET | `/games` | Lista jogos |
| GET | `/games/{id}` | Jogo por ID |
| POST | `/games` | Criar jogo |
| PUT | `/games/{id}` | Atualizar jogo |
| DELETE | `/games/{id}` | Remover jogo |
| POST | `/orders` | Iniciar compra (body: `{ "userId": "guid", "gameId": "guid" }`) — publica OrderPlacedEvent |
| GET | `/users/{userId}/library` | Biblioteca do usuário |

## Eventos (mensageria)

- **Publica**: `OrderPlacedEvent` (UserId, GameId, Price, OrderId) — consumido pelo PaymentsAPI.
- **Consome**: `PaymentProcessedEvent` (OrderId, UserId, GameId, Status) — se `Approved`, adiciona jogo à biblioteca.
