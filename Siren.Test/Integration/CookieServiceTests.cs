using System.Net;
using FluentAssertions;
using Siren.Components.Services;

namespace Siren.Test.Integration;

public class CookieServiceTests
{
    private readonly CookieService _service;

    public CookieServiceTests()
    {
        _service = new CookieService();
    }

    [Fact]
    public void Can_SaveAndRetrieve_Cookie()
    {
        // Arrange
        var cookie = new Cookie("sessionId", "abc123", "/", "example.com");

        // Act
        _service.SaveCookie(cookie);
        var cookies = _service.GetCookies();

        // Assert
        cookies.Should().ContainSingle();
        cookies[0].Name.Should().Be("sessionId");
        cookies[0].Value.Should().Be("abc123");
    }

    [Fact]
    public void Can_DeleteSpecific_Cookie()
    {
        // Arrange
        _service.SaveCookie(new Cookie("keep", "value1", "/", "example.com"));
        _service.SaveCookie(new Cookie("delete", "value2", "/", "example.com"));

        // Act
        _service.DeleteCookie("delete");
        var cookies = _service.GetCookies();

        // Assert
        cookies.Should().ContainSingle();
        cookies[0].Name.Should().Be("keep");
    }

    [Fact]
    public void Can_DeleteAll_Cookies()
    {
        // Arrange
        _service.SaveCookie(new Cookie("cookie1", "value1", "/", "example.com"));
        _service.SaveCookie(new Cookie("cookie2", "value2", "/", "example.com"));

        // Act
        _service.DeleteAllCookies();

        // Assert
        _service.GetCookies().Should().BeEmpty();
    }

    [Fact]
    public void Can_Parse_SetCookieHeader()
    {
        // Arrange
        var response = new HttpResponseMessage();
        response.Headers.Add("Set-Cookie", "session=abc123; Domain=example.com; Path=/; Secure; HttpOnly");

        // Act
        var cookies = _service.ParseCookies(response);

        // Assert
        cookies.Should().ContainSingle();
        var cookie = cookies[0];
        cookie.Name.Should().Be("session");
        cookie.Value.Should().Be("abc123");
        cookie.Domain.Should().Be("example.com");
        cookie.Path.Should().Be("/");
        cookie.Secure.Should().BeTrue();
        cookie.HttpOnly.Should().BeTrue();
    }

    [Fact]
    public void Can_Parse_MultipleCookies()
    {
        // Arrange
        var response = new HttpResponseMessage();
        response.Headers.Add("Set-Cookie", new[]
        {
            "session=abc123; Domain=example.com; Path=/",
            "theme=dark; Path=/; Secure"
        });

        // Act
        var cookies = _service.ParseCookies(response);

        // Assert
        cookies.Should().HaveCount(2);
        cookies.Should().Contain(c => c.Name == "session" && c.Value == "abc123");
        cookies.Should().Contain(c => c.Name == "theme" && c.Value == "dark");
    }

    [Fact]
    public void ParseCookies_HandlesExpires()
    {
        // Arrange
        var response = new HttpResponseMessage();
        var futureDate = DateTime.UtcNow.AddDays(1).ToString("R"); // RFC1123 format
        response.Headers.Add("Set-Cookie", $"session=abc123; Expires={futureDate}");

        // Act
        var cookies = _service.ParseCookies(response);

        // Assert
        cookies.Should().ContainSingle();
        var cookie = cookies[0];
        cookie.Expires.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public void ParseCookies_SkipsInvalidCookies()
    {
        // Arrange
        var response = new HttpResponseMessage();
        response.Headers.Add("Set-Cookie", new[] 
        {
            "invalid-cookie",  // Missing =
            "valid=cookie",    // Should be included
            "=invalid-value"   // Missing name
        });

        // Act
        var cookies = _service.ParseCookies(response);

        // Assert
        cookies.Should().ContainSingle();
        cookies[0].Name.Should().Be("valid");
        cookies[0].Value.Should().Be("cookie");
    }
}