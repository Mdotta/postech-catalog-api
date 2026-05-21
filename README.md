# Catalog API — Microsservico de Catalogo (FCG)

Microsservico de **Catalogo** da FIAP Cloud Games (Tech Challenge). Responsavel pelo CRUD de jogos, inicio do fluxo de compra e biblioteca do usuario apos pagamento aprovado.

## Finalidade

- **CRUD de jogos** — listar, criar, atualizar e remover jogos.
- **Dual-write document store** — dados expandidos (tags, screenshots, developer, publisher) salvos em MongoDB (local) ou DynamoDB (producao AWS).
- **Cache com Redis** — lista de jogos cacheada por 5 minutos.
- **Publica `OrderPlacedEvent`** via SNS ao criar pedido.
- **Consome `PaymentProcessedEvent`** via SQS — aprova e adiciona o jogo a biblioteca do usuario.

## Tecnologias / Dependencias

| Recurso | Local (dev) | AWS (producao) |
|---------|------------|----------------|
| Runtime | .NET 10 / C# | .NET 10 / C# |
| Banco primario | PostgreSQL 16 | RDS PostgreSQL 16 |
| Document store | MongoDB 7 | DynamoDB |
| Cache | Redis 7 | ElastiCache Redis 7 |
| Mensageria (pub) | SNS (localstack opcional) | SNS |
| Mensageria (sub) | SQS (localstack opcional) | SQS |
| Logs | Console / arquivo | CloudWatch Logs |
| Metricas | `/metrics` (Prometheus) | `/metrics` (Prometheus) |
| API docs | Scalar | Scalar |
| Auth | JWT (API Gateway pass-through) | Cognito JWT via API Gateway |

Pacotes NuGet principais: `AWSSDK.DynamoDBv2`, `AWSSDK.SQS`, `AWSSDK.SimpleNotificationService`, `MongoDB.Driver`, `StackExchange.Redis`, `prometheus-net.AspNetCore`, `Serilog.AspNetCore`, `Scalar.AspNetCore`, `ErrorOr`.

## Como rodar localmente

```bash
# 1. Subir dependencias (PostgreSQL, MongoDB, Redis, RabbitMQ) — use o compose do projeto de orquestracao:
cd ../postech-orchestration/docker
docker compose up -d postgresql mongodb redis

# 2. Rodar a API
cd ../../postech-catalog-api/src/Postech.Catalog.Api
dotnet run
```

API disponivel em **http://localhost:5050**. Abra no navegador:

- `http://localhost:5050/health` — health check
- `http://localhost:5050/scalar/v1` — documentacao e testes
- `http://localhost:5050/metrics` — metricas Prometheus

Alternativamente, suba apenas as dependencias locais com o `docker-compose.yml` no proprio repositorio:

```bash
docker compose up -d
```

## Variaveis de ambiente

| Variavel | Descricao | Default (local) |
|----------|-----------|-----------------|
| `ConnectionStrings__DefaultConnection` | PostgreSQL | `Host=localhost;Port=5432;Database=postech_catalog;Username=postgres;Password=postgres` |
| `MongoDB__ConnectionString` | MongoDB (local) | `mongodb://postgres:postgres@localhost:27017` |
| `MongoDB__DatabaseName` | MongoDB database | `postech_catalog` |
| `DynamoDB__UseDynamoDB` | Usar DynamoDB em vez de MongoDB | `false` |
| `DynamoDB__TableName` | Nome da tabela DynamoDB | `postech_catalog_games` |
| `Redis__ConnectionString` | Redis | `localhost:6379` |
| `AWS__Region` | Regiao AWS | `us-east-1` |
| `AWS__ServiceURL` | LocalStack (opcional) | — |
| `AWS__SnsTopicArn` | ARN do topico SNS para OrderPlacedEvent | — |
| `AWS__SqsQueueUrl` | URL da fila SQS para PaymentProcessedEvent | — |

## Endpoints

| Metodo | Rota | Descricao |
|--------|------|-----------|
| `GET` | `/health` | Health check |
| `GET` | `/health/alive` | Liveness probe |
| `GET` | `/metrics` | Metricas Prometheus |
| `GET` | `/games` | Listar todos os jogos (cache Redis) |
| `GET` | `/games/{id}` | Buscar jogo por ID |
| `POST` | `/games` | Criar jogo |
| `PUT` | `/games/{id}` | Atualizar jogo |
| `DELETE` | `/games/{id}` | Remover jogo |
| `POST` | `/games/create-order` | Criar pedido (publica `OrderPlacedEvent`) |
| `GET` | `/games/library` | Biblioteca do usuario autenticado |

## Eventos

- **Publica:** `OrderPlacedEvent` (OrderId, UserId, GameId, Price) via SNS.
- **Consome:** `PaymentProcessedEvent` (OrderId, UserId, GameId, Status) via SQS — se aprovado, adiciona a biblioteca.

## Estrutura do projeto

```
src/Postech.Catalog.Api/
  Application/            # DTOs, Services (GameService, OrderService), Validations
    Consumers/            # SQS consumers (desacoplados do MassTransit)
  Domain/                 # Entities (Game, Order), Enums, Errors
  Endpoints/              # Minimal API endpoints
  Extensions/             # DI registration, auth, pipeline
  Infrastructure/
    Cache/                # RedisCacheService
    Data/                 # CatalogDbContext (EF Core / Postgres)
    DynamoDB/             # DynamoDbSettings, GameDynamoRepository
    Messaging/            # SnsEventPublisher, SqsOrderEventConsumer
    MongoDB/              # MongoDbSettings, GameDocument, GameMongoRepository
    Repositories/         # IGameRepository, IGameDocumentRepository, etc.
  Middleware/             # CorrelationIdMiddleware
  Migrations/             # EF Core migrations
```

## Como atualizar imagem no ECR

```bash
ACCOUNT=$(aws sts get-caller-identity --query Account --output text)
ECR="${ACCOUNT}.dkr.ecr.us-east-1.amazonaws.com/tf-postech-postech-catalog-api"

aws ecr get-login-password --region us-east-1 | docker login --username AWS --password-stdin "${ACCOUNT}.dkr.ecr.us-east-1.amazonaws.com"

docker build -t "${ECR}:latest" -f Dockerfile .
docker push "${ECR}:latest"
```

Apos o push, a nova imagem sera usada na proxima inicializacao da instancia EC2 (via user_data). Para forcar a atualizacao imediata, acesse a EC2 e execute:

```bash
ssh -i postech-key.pem ec2-user@<catalog-eip>
docker pull <ecr-url>:latest
docker rm -f tf-postech-catalog-api
# re-execute o comando docker run do user_data original
```
