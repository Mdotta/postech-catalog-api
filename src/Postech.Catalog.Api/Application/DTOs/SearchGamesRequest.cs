namespace Postech.Catalog.Api.Application.DTOs;

public record SearchGamesRequest(string Q, int Fuzziness = 2);
