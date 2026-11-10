using Microsoft.Extensions.Logging.Abstractions;
using MudBlazor;
using Mythetech.Framework.Infrastructure.Commands;
using Mythetech.Framework.Infrastructure.MessageBus;
using Mythetech.Framework.Infrastructure.Plugins;
using NSubstitute;
using Shouldly;
using Siren.Components.NativeMenu;

namespace Siren.Test.Components;

public class PluginMenuCallbackRegistryTests
{
    private readonly IMessageBus _bus = Substitute.For<IMessageBus>();
    private readonly PluginMenuCallbackRegistry _registry;
    private readonly PluginContext _context;

    public PluginMenuCallbackRegistryTests()
    {
        _registry = new PluginMenuCallbackRegistry(
            new PluginMenuItemInvoker(_bus, NullLogger<PluginMenuItemInvoker>.Instance));
        _context = new PluginContext
        {
            MessageBus = _bus,
            StateStore = new PluginStateStore(new PluginState()),
        };
    }

    [Fact(DisplayName = "A plugin menu callback that throws is contained and reported by plugin name")]
    public async Task TryInvoke_CallbackThrows_ReturnsFalseAndNotifies()
    {
        _registry.Register("item", _ => throw new InvalidOperationException("boom"), "Notes");

        var handled = await _registry.TryInvoke("item", _context);

        handled.ShouldBeFalse();
        await _bus.Received(1).PublishAsync(Arg.Is<AddNotification>(n =>
            n.Message == "Notes couldn't complete that action." && n.Severity == Severity.Error));
    }

    [Fact(DisplayName = "A menu item with no registered callback is not handled and does not throw")]
    public async Task TryInvoke_UnknownItem_ReturnsFalse()
    {
        var handled = await _registry.TryInvoke("missing", _context);

        handled.ShouldBeFalse();
        await _bus.DidNotReceiveWithAnyArgs().PublishAsync(Arg.Any<AddNotification>());
    }

    [Fact(DisplayName = "A plugin menu callback that succeeds runs and is handled")]
    public async Task TryInvoke_CallbackSucceeds_ReturnsTrue()
    {
        var ran = false;
        _registry.Register("item", _ => { ran = true; return Task.CompletedTask; }, "Notes");

        var handled = await _registry.TryInvoke("item", _context);

        handled.ShouldBeTrue();
        ran.ShouldBeTrue();
    }
}
