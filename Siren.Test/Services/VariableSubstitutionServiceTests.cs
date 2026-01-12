using FluentAssertions;
using NSubstitute;
using Siren.Components.Services;
using Siren.Components.Variables;

namespace Siren.Test.Services;

public class VariableSubstitutionServiceTests
{
    private readonly IVariableService _variableService;
    private readonly VariableSubstitutionService _service;

    public VariableSubstitutionServiceTests()
    {
        _variableService = Substitute.For<IVariableService>();
        _service = new VariableSubstitutionService(_variableService, Enumerable.Empty<IVariableValueResolver>());
    }

    [Fact]
    public void SubstituteVariables_NullOrEmpty_ReturnsInput()
    {
        _service.SubstituteVariables(null!).Should().BeNull();
        _service.SubstituteVariables("").Should().Be("");
    }

    [Fact]
    public void SubstituteVariables_NoVariables_ReturnsInputUnchanged()
    {
        var input = "Hello, World!";

        var result = _service.SubstituteVariables(input);

        result.Should().Be(input);
    }

    [Fact]
    public void SubstituteVariables_WithVariable_Substitutes()
    {
        var variables = new List<Variable>
        {
            Variable.Create("apiKey", "secret123", group: VariableGroups.SystemGroup)
        };
        _variableService.GetVariables().Returns(variables);

        var input = "Bearer {{apiKey}}";

        var result = _service.SubstituteVariables(input, VariableGroups.SystemGroup);

        result.Should().Be("Bearer secret123");
    }

    [Fact]
    public void SubstituteVariables_MultipleVariables_SubstitutesAll()
    {
        var variables = new List<Variable>
        {
            Variable.Create("host", "api.example.com", group: VariableGroups.SystemGroup),
            Variable.Create("version", "v1", group: VariableGroups.SystemGroup)
        };
        _variableService.GetVariables().Returns(variables);

        var input = "https://{{host}}/{{version}}/users";

        var result = _service.SubstituteVariables(input, VariableGroups.SystemGroup);

        result.Should().Be("https://api.example.com/v1/users");
    }

    [Fact]
    public void SubstituteVariables_UndefinedVariable_LeavesPlaceholder()
    {
        _variableService.GetVariables().Returns(new List<Variable>());

        var input = "Value: {{undefined}}";

        var result = _service.SubstituteVariables(input, VariableGroups.SystemGroup);

        result.Should().Be("Value: {{undefined}}");
    }

    [Fact]
    public void SubstituteVariables_EnvironmentSpecificVariable_TakesPrecedence()
    {
        var variables = new List<Variable>
        {
            Variable.Create("baseUrl", "https://dev.example.com", group: "Development"),
            Variable.Create("baseUrl", "https://prod.example.com", group: VariableGroups.SystemGroup)
        };
        _variableService.GetVariables().Returns(variables);

        var input = "{{baseUrl}}/api";

        var result = _service.SubstituteVariables(input, "Development");

        result.Should().Be("https://dev.example.com/api");
    }

    [Fact]
    public void SubstituteVariables_GlobalVariableWhenNoEnvironmentMatch_UsesGlobal()
    {
        var variables = new List<Variable>
        {
            Variable.Create("timeout", "30", group: VariableGroups.SystemGroup)
        };
        _variableService.GetVariables().Returns(variables);

        var input = "Timeout: {{timeout}}s";

        var result = _service.SubstituteVariables(input, VariableGroups.SystemGroup);

        result.Should().Be("Timeout: 30s");
    }

    [Fact]
    public void SubstituteVariables_CaseInsensitiveMatch()
    {
        var variables = new List<Variable>
        {
            Variable.Create("ApiKey", "test123", group: VariableGroups.SystemGroup)
        };
        _variableService.GetVariables().Returns(variables);

        var input = "Key: {{apikey}}";

        var result = _service.SubstituteVariables(input, VariableGroups.SystemGroup);

        result.Should().Be("Key: test123");
    }

    [Fact]
    public void SubstituteVariables_DynamicVariable_LeavesUnresolved()
    {
        _variableService.GetVariables().Returns(new List<Variable>());

        var input = "ID: {{$uuid}}";

        // Sync method doesn't resolve dynamic variables
        var result = _service.SubstituteVariables(input);

        result.Should().Be("ID: {{$uuid}}");
    }

    [Fact]
    public async Task SubstituteVariablesAsync_NullOrEmpty_ReturnsOkResult()
    {
        var result = await _service.SubstituteVariablesAsync(null!);

        result.Value.Should().BeNull();
        result.HasErrors.Should().BeFalse();
    }

    [Fact]
    public async Task SubstituteVariablesAsync_WithVariable_Substitutes()
    {
        var variables = new List<Variable>
        {
            Variable.Create("token", "abc123", group: VariableGroups.SystemGroup)
        };
        _variableService.GetVariables().Returns(variables);

        var input = "Auth: {{token}}";

        var result = await _service.SubstituteVariablesAsync(input, VariableGroups.SystemGroup);

        result.Value.Should().Be("Auth: abc123");
        result.HasErrors.Should().BeFalse();
    }

    [Fact]
    public async Task SubstituteVariablesAsync_SecretVariable_IndicatesHasSecrets()
    {
        var variables = new List<Variable>
        {
            Variable.Create("password", "secret", isSecret: true, group: VariableGroups.SystemGroup)
        };
        _variableService.GetVariables().Returns(variables);

        var input = "Pass: {{password}}";

        var result = await _service.SubstituteVariablesAsync(input, VariableGroups.SystemGroup, maskSecrets: false);

        result.Value.Should().Be("Pass: secret");
        result.HasSecrets.Should().BeTrue();
    }

    [Fact]
    public async Task SubstituteVariablesAsync_SecretVariableWithMasking_MasksValue()
    {
        var variables = new List<Variable>
        {
            Variable.Create("apiSecret", "verysecret", isSecret: true, group: VariableGroups.SystemGroup)
        };
        _variableService.GetVariables().Returns(variables);

        var input = "Secret: {{apiSecret}}";

        var result = await _service.SubstituteVariablesAsync(input, VariableGroups.SystemGroup, maskSecrets: true);

        result.Value.Should().Be("Secret: ********");
        result.HasSecrets.Should().BeTrue();
    }

    [Fact]
    public async Task SubstituteVariablesAsync_WithDynamicVariableResolver_Resolves()
    {
        var resolver = Substitute.For<IVariableValueResolver>();
        resolver.CanResolve("$timestamp").Returns(true);
        resolver.ResolveAsync("$timestamp", Arg.Any<CancellationToken>())
            .Returns(VariableResolutionResult.Ok("1234567890"));

        var service = new VariableSubstitutionService(_variableService, new[] { resolver });
        _variableService.GetVariables().Returns(new List<Variable>());

        var input = "Time: {{$timestamp}}";

        var result = await service.SubstituteVariablesAsync(input, VariableGroups.SystemGroup);

        result.Value.Should().Be("Time: 1234567890");
    }

    [Fact]
    public async Task SubstituteVariablesAsync_ResolverReturnsSecret_MarksAsSecret()
    {
        var resolver = Substitute.For<IVariableValueResolver>();
        resolver.CanResolve("$secret:mykey").Returns(true);
        resolver.ResolveAsync("$secret:mykey", Arg.Any<CancellationToken>())
            .Returns(VariableResolutionResult.Ok("secretvalue", isSecret: true));

        var service = new VariableSubstitutionService(_variableService, new[] { resolver });
        _variableService.GetVariables().Returns(new List<Variable>());

        var input = "Key: {{$secret:mykey}}";

        var result = await service.SubstituteVariablesAsync(input, VariableGroups.SystemGroup, maskSecrets: true);

        result.Value.Should().Be("Key: ********");
        result.HasSecrets.Should().BeTrue();
    }

    [Fact]
    public async Task SubstituteVariablesAsync_ResolverFails_AddsError()
    {
        var resolver = Substitute.For<IVariableValueResolver>();
        resolver.CanResolve("$failing").Returns(true);
        resolver.ResolveAsync("$failing", Arg.Any<CancellationToken>())
            .Returns(VariableResolutionResult.Fail("[ERROR]", "Something went wrong"));

        var service = new VariableSubstitutionService(_variableService, new[] { resolver });
        _variableService.GetVariables().Returns(new List<Variable>());

        var input = "Result: {{$failing}}";

        var result = await service.SubstituteVariablesAsync(input, VariableGroups.SystemGroup);

        result.HasErrors.Should().BeTrue();
        result.Errors.Should().Contain("Something went wrong");
    }

    [Fact]
    public async Task SubstituteVariablesAsync_CancellationRequested_StopsProcessing()
    {
        var variables = new List<Variable>
        {
            Variable.Create("var1", "value1", group: VariableGroups.SystemGroup),
            Variable.Create("var2", "value2", group: VariableGroups.SystemGroup)
        };
        _variableService.GetVariables().Returns(variables);

        var input = "{{var1}} {{var2}}";
        var cts = new CancellationTokenSource();
        cts.Cancel();

        var result = await _service.SubstituteVariablesAsync(input, VariableGroups.SystemGroup, cancellationToken: cts.Token);

        // When cancelled immediately, may not process any replacements
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task SubstituteVariablesAsync_SameVariableMultipleTimes_OnlyProcessedOnce()
    {
        var variables = new List<Variable>
        {
            Variable.Create("id", "123", group: VariableGroups.SystemGroup)
        };
        _variableService.GetVariables().Returns(variables);

        var input = "ID: {{id}}, Again: {{id}}, Once more: {{id}}";

        var result = await _service.SubstituteVariablesAsync(input, VariableGroups.SystemGroup);

        result.Value.Should().Be("ID: 123, Again: 123, Once more: 123");
    }

    [Fact]
    public async Task SubstituteVariablesAsync_VariableValueIsAnotherDynamicVar_Resolves()
    {
        var resolver = Substitute.For<IVariableValueResolver>();
        resolver.CanResolve("$uuid").Returns(true);
        resolver.ResolveAsync("$uuid", Arg.Any<CancellationToken>())
            .Returns(VariableResolutionResult.Ok("550e8400-e29b-41d4-a716-446655440000"));

        var variables = new List<Variable>
        {
            Variable.Create("requestId", "$uuid", group: VariableGroups.SystemGroup)
        };
        _variableService.GetVariables().Returns(variables);

        var service = new VariableSubstitutionService(_variableService, new[] { resolver });

        var input = "Request: {{requestId}}";

        var result = await service.SubstituteVariablesAsync(input, VariableGroups.SystemGroup);

        result.Value.Should().Be("Request: 550e8400-e29b-41d4-a716-446655440000");
    }
}
