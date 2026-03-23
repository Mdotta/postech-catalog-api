using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using Postech.Catalog.Api.Application.DTOs;

namespace Postech.Catalog.Api.Extensions;

/// <summary>
/// Preenche <c>requestBody.content["application/json"].example</c> para o Scalar mostrar um JSON editável.
/// </summary>
internal static class CatalogOpenApiExamplesTransformer
{
    private static readonly JsonSerializerOptions JsonExampleOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public static Task ApplyAsync(OpenApiDocument document, OpenApiDocumentTransformerContext _, CancellationToken cancellationToken)
        => ApplyCoreAsync(document, cancellationToken);

    private static Task ApplyCoreAsync(OpenApiDocument document, CancellationToken _)
    {
        if (document.Paths is null)
            return Task.CompletedTask;

        foreach (var pathItem in document.Paths.Values)
        {
            if (pathItem?.Operations is null)
                continue;

            foreach (var operation in pathItem.Operations.Values.OfType<OpenApiOperation>())
            {
                if (string.IsNullOrEmpty(operation.OperationId))
                    continue;

                switch (operation.OperationId)
                {
                    case "CreateGame":
                        SetRequestBodyExample(operation, new CreateGameRequest(
                            Name: "CyberQuest 2077",
                            Description: "RPG de mundo aberto em cenário cyberpunk.",
                            Price: 199.90m,
                            Genre: "RPG",
                            ReleaseDate: new DateOnly(2025, 6, 15)));
                        break;
                    case "CreateOrder":
                        SetRequestBodyExample(operation, new PlaceOrderRequest(
                            GameId: Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee")));
                        break;
                    case "UpdateGame":
                        SetRequestBodyExample(operation, new UpdateGameRequest(
                            Id: Guid.Empty,
                            Name: "Novo nome do jogo",
                            Description: "Descrição atualizada (opcional).",
                            Price: 149.90m,
                            Genre: "Ação",
                            ReleaseDate: new DateOnly(2025, 12, 1)));
                        break;
                }
            }
        }

        return Task.CompletedTask;
    }

    private static void SetRequestBodyExample<T>(OpenApiOperation operation, T examplePayload)
    {
        var json = JsonSerializer.Serialize(examplePayload, JsonExampleOptions);
        var node = JsonNode.Parse(json);
        if (node is null)
            return;

        if (operation.RequestBody is not OpenApiRequestBody body)
        {
            body = new OpenApiRequestBody { Required = true };
            operation.RequestBody = body;
        }

        body.Content ??= new Dictionary<string, OpenApiMediaType>();
        if (!body.Content.TryGetValue("application/json", out var media))
        {
            media = new OpenApiMediaType();
            body.Content["application/json"] = media;
        }

        media.Example = node;
    }
}
