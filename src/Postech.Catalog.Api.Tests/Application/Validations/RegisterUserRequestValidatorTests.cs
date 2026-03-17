using FluentAssertions;
using Postech.Catalog.Api.Application.DTOs;
using Postech.Catalog.Api.Application.Validations;
using Postech.Catalog.Api.Domain.Enums;
using Postech.Catalog.Api.Domain.Errors;

namespace Postech.Catalog.Api.Tests.Application.Validations;

public class RegisterUserRequestValidatorTests
{
    [Fact]
    public void Validate_ShouldPassForValidRequest()
    {
        // Arrange
        var request = new RegisterUserRequest("valid@email.com", "Valid User", "StrongP@ssw0rd!", UserRoles.User);
        
        // Act
        var result = RegisterUserRequestValidator.Validate(request);
        
        // Assert
        result.IsError.Should().BeFalse();
    }

    [Fact]
    public void Validate_ShouldReturnExpectedErrors()
    {
        // Arrange
        var request = new RegisterUserRequest("invalid@email", "", "weakPass", UserRoles.User);
        
        // Act
        var result = RegisterUserRequestValidator.Validate(request);
        
        // Assert
        result.Errors.Should().Contain(Errors.User.UnsafePassword);
        result.Errors.Should().Contain(Errors.User.InvalidEmail);
        result.Errors.Should().Contain(Errors.User.NameRequired);
    }
}