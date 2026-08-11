using System.ComponentModel.DataAnnotations;
using BinTool.Api.Controllers;
using BinTool.Application.Abstractions;
using BinTool.Application.Models.Classification;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace BinTool.Tests;

// The controller's contract: what it passes to the service, what it returns, and the request
// validation that [ApiController] turns into a 400 before the action runs. The matching rules
// themselves are covered by the service tests.
public class BinControllerTests
{
    private readonly Mock<IBinClassificationService> _service = new(MockBehavior.Strict);

    private BinController CreateController() => new(_service.Object);

    private static IReadOnlyList<ValidationResult> Validate(BinClassificationRequest request)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(request, new ValidationContext(request), results, true);
        return results;
    }

    [Fact]
    public async Task Classify_returns_the_service_result()
    {
        var expected = new BinClassificationResult
        {
            Bin = "400001",
            Matched = true,
            MatchedPrefix = "400001",
            CardScheme = "Visa"
        };

        _service
            .Setup(s => s.ClassifyAsync("400001", It.IsAny<decimal?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await CreateController()
            .Classify(new BinClassificationRequest { Bin = "400001" }, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeSameAs(expected);
    }

    [Fact]
    public async Task Classify_returns_200_when_nothing_matched()
    {
        _service
            .Setup(s => s.ClassifyAsync(It.IsAny<string>(), It.IsAny<decimal?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BinClassificationResult { Bin = "999999", Matched = false });

        var result = await CreateController()
            .Classify(new BinClassificationRequest { Bin = "999999" }, CancellationToken.None);

        var value = result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeOfType<BinClassificationResult>().Subject;

        value.Matched.Should().BeFalse();
    }

    [Theory]
    [InlineData("400001")]
    [InlineData("4000012345678901")]
    public void A_valid_bin_passes_request_validation(string bin)
    {
        Validate(new BinClassificationRequest { Bin = bin }).Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("40000")]
    [InlineData("4000 01")]
    [InlineData("4000a1")]
    [InlineData("40000123456789012345")]
    public void An_invalid_bin_fails_request_validation(string bin)
    {
        Validate(new BinClassificationRequest { Bin = bin }).Should().NotBeEmpty();
    }
}
