using Shouldly;
using Siren.Components.Http;
using Siren.Components.Http.Models;
using Siren.Test.Infrastructure;

namespace Siren.Test.Services;

public class RequestTabsStateTests
{
    private static readonly BinaryAttachment Attachment = new() { FileName = "avatar.png" };

    private static RequestTabsState CreateState(string? defaultMethod = null)
        => TestState.Tabs(TestState.Settings(http => http.DefaultHttpMethod = defaultMethod));

    private static SirenNetworkRequest Tab(string uri) => new()
    {
        Request = new HttpRequest { RequestUri = uri, Method = HttpMethod.Get }
    };

    private sealed class Recorder
    {
        public int Tabs;
        public int Active;
        public int Request;
        public int Response;
        public int Sending;

        public Recorder(RequestTabsState state)
        {
            state.TabsChanged += () => Tabs++;
            state.ActiveTabChanged += _ => Active++;
            state.RequestChanged += _ => Request++;
            state.ResponseChanged += _ => Response++;
            state.SendingChanged += _ => Sending++;
        }
    }

    [Fact(DisplayName = "A new state has one blank tab, and it is active")]
    public void NewState_HasOneActiveBlankTab()
    {
        var state = CreateState();

        var tab = state.Tabs.ShouldHaveSingleItem();
        state.Active.ShouldBeSameAs(tab);
        var request = tab.Request.ShouldNotBeNull();
        request.RequestUri.ShouldBe("");
        request.Method.ShouldBeNull();
    }

    [Fact(DisplayName = "The first tab uses the default method setting")]
    public void FirstTab_UsesDefaultMethod()
    {
        CreateState("POST").Active.Request.ShouldNotBeNull().Method.ShouldBe(HttpMethod.Post);
    }

    [Fact(DisplayName = "A new tab uses the default method setting and becomes active")]
    public void OpenNew_UsesDefaultMethodAndActivates()
    {
        var state = CreateState("PUT");
        var recorder = new Recorder(state);

        var tab = state.OpenNew();

        tab.Request.ShouldNotBeNull().Method.ShouldBe(HttpMethod.Put);
        state.Tabs.Count.ShouldBe(2);
        state.Active.ShouldBeSameAs(tab);
        recorder.Tabs.ShouldBe(1);
        recorder.Active.ShouldBe(1);
    }

    [Fact(DisplayName = "Opening a tab adds it and makes it active")]
    public void Open_AddsAndActivates()
    {
        var state = CreateState();
        var tab = Tab("https://api.example.com/users");

        state.Open(tab).ShouldBeSameAs(tab);

        state.Tabs.Count.ShouldBe(2);
        state.Tabs[^1].ShouldBeSameAs(tab);
        state.Active.ShouldBeSameAs(tab);
    }

    [Fact(DisplayName = "Opening a tab that is already open activates it without adding it again")]
    public void Open_AlreadyOpen_OnlyActivates()
    {
        var state = CreateState();
        var first = state.Active;
        var tab = state.Open(Tab("https://api.example.com/users"));
        state.Activate(first.Id);
        var recorder = new Recorder(state);

        state.Open(tab);

        state.Tabs.Count.ShouldBe(2);
        state.Active.ShouldBeSameAs(tab);
        recorder.Tabs.ShouldBe(0);
        recorder.Active.ShouldBe(1);
    }

    [Fact(DisplayName = "Opening a tab without a request gives it an empty one")]
    public void Open_WithoutRequest_GivesEmptyRequest()
    {
        var state = CreateState();

        var tab = state.Open(new SirenNetworkRequest());

        tab.Request.ShouldNotBeNull().RequestUri.ShouldBe("");
    }

    [Fact(DisplayName = "Duplicating a tab copies its request and response into a new active tab")]
    public void Duplicate_CopiesRequestAndResponse()
    {
        var state = CreateState();
        var source = state.Open(new SirenNetworkRequest
        {
            Request = new HttpRequest { RequestUri = "https://api.example.com/users", Method = HttpMethod.Get },
            Response = new RequestResult { StatusCode = 200 },
            DisplayText = "{}",
        });

        var copy = state.Duplicate(source.Id).ShouldNotBeNull();

        copy.ShouldNotBeSameAs(source);
        copy.Id.ShouldNotBe(source.Id);
        var copiedRequest = copy.Request.ShouldNotBeNull();
        copiedRequest.RequestUri.ShouldBe("https://api.example.com/users");
        copiedRequest.ShouldNotBeSameAs(source.Request);
        copy.Response.ShouldNotBeNull().StatusCode.ShouldBe(200);
        copy.DisplayText.ShouldBe("{}");
        state.Tabs[^1].ShouldBeSameAs(copy);
        state.Active.ShouldBeSameAs(copy);
    }

    [Fact(DisplayName = "Duplicating a tab that has no method works")]
    public void Duplicate_WithoutMethod_Works()
    {
        var state = CreateState();

        var copy = state.Duplicate(state.Active.Id).ShouldNotBeNull();

        copy.Request.ShouldNotBeNull().Method.ShouldBeNull();
    }

    [Fact(DisplayName = "Duplicating a tab that is not open does nothing")]
    public void Duplicate_UnknownTab_DoesNothing()
    {
        var state = CreateState();

        state.Duplicate(Guid.NewGuid()).ShouldBeNull();

        state.Tabs.Count.ShouldBe(1);
    }

    [Fact(DisplayName = "Closing the active tab activates the tab that takes its place")]
    public void Close_ActiveTab_ActivatesTabInItsPlace()
    {
        var state = CreateState();
        var a = state.Open(Tab("https://api.example.com/a"));
        var b = state.Open(Tab("https://api.example.com/b"));
        state.Activate(a.Id);
        var recorder = new Recorder(state);

        state.Close(a.Id);

        state.Tabs.ShouldNotContain(a);
        state.Active.ShouldBeSameAs(b);
        recorder.Tabs.ShouldBe(1);
        recorder.Active.ShouldBe(1);
    }

    [Fact(DisplayName = "Closing the active tab at the end of the list activates the one before it")]
    public void Close_LastActiveTab_ActivatesPrevious()
    {
        var state = CreateState();
        var a = state.Open(Tab("https://api.example.com/a"));
        var b = state.Open(Tab("https://api.example.com/b"));

        state.Close(b.Id);

        state.Active.ShouldBeSameAs(a);
    }

    [Fact(DisplayName = "Closing the only tab opens a blank one")]
    public void Close_OnlyTab_OpensBlankTab()
    {
        var state = CreateState("GET");
        var only = state.Active;

        state.Close(only.Id);

        var replacement = state.Tabs.ShouldHaveSingleItem();
        replacement.ShouldNotBeSameAs(only);
        replacement.Request.ShouldNotBeNull().Method.ShouldBe(HttpMethod.Get);
        state.Active.ShouldBeSameAs(replacement);
    }

    [Fact(DisplayName = "Closing another tab leaves the active tab active")]
    public void Close_OtherTab_KeepsActiveTab()
    {
        var state = CreateState();
        var a = state.Open(Tab("https://api.example.com/a"));
        var b = state.Open(Tab("https://api.example.com/b"));
        var recorder = new Recorder(state);

        state.Close(a.Id);

        state.Active.ShouldBeSameAs(b);
        recorder.Active.ShouldBe(0);
    }

    [Fact(DisplayName = "Closing a tab that is not open does nothing")]
    public void Close_UnknownTab_DoesNothing()
    {
        var state = CreateState();
        var recorder = new Recorder(state);

        state.Close(Guid.NewGuid());

        state.Tabs.Count.ShouldBe(1);
        recorder.Tabs.ShouldBe(0);
    }

    [Fact(DisplayName = "Activating another tab raises ActiveTabChanged once")]
    public void Activate_OtherTab_RaisesOnce()
    {
        var state = CreateState();
        var first = state.Active;
        state.OpenNew();
        var recorder = new Recorder(state);

        state.Activate(first.Id);

        state.Active.ShouldBeSameAs(first);
        recorder.Active.ShouldBe(1);
    }

    [Fact(DisplayName = "Activating the active tab raises nothing")]
    public void Activate_ActiveTab_RaisesNothing()
    {
        var state = CreateState();
        var recorder = new Recorder(state);

        state.Activate(state.Active.Id);

        recorder.Active.ShouldBe(0);
    }

    [Fact(DisplayName = "Activating a tab that is not open does nothing")]
    public void Activate_UnknownTab_DoesNothing()
    {
        var state = CreateState();
        var active = state.Active;
        var recorder = new Recorder(state);

        state.Activate(Guid.NewGuid());

        state.Active.ShouldBeSameAs(active);
        recorder.Active.ShouldBe(0);
    }

    public static TheoryData<string> Setters => new()
    {
        nameof(RequestTabsState.SetUri),
        nameof(RequestTabsState.SetMethod),
        nameof(RequestTabsState.SetBodyType),
        nameof(RequestTabsState.SetRawBody),
        nameof(RequestTabsState.SetHeaders),
        nameof(RequestTabsState.SetFormData),
        nameof(RequestTabsState.SetAttachments),
    };

    private static void Set(string setter, RequestTabsState state, Guid id)
    {
        switch (setter)
        {
            case nameof(RequestTabsState.SetUri): state.SetUri(id, "https://api.example.com/changed"); break;
            case nameof(RequestTabsState.SetMethod): state.SetMethod(id, HttpMethod.Delete); break;
            case nameof(RequestTabsState.SetBodyType): state.SetBodyType(id, RequestBodyType.FormData); break;
            case nameof(RequestTabsState.SetRawBody): state.SetRawBody(id, """{"changed":true}"""); break;
            case nameof(RequestTabsState.SetHeaders): state.SetHeaders(id, [new("Accept", "text/plain")]); break;
            case nameof(RequestTabsState.SetFormData): state.SetFormData(id, new Dictionary<string, string> { ["name"] = "Ada" }); break;
            case nameof(RequestTabsState.SetAttachments): state.SetAttachments(id, [Attachment]); break;
            default: throw new ArgumentOutOfRangeException(nameof(setter), setter, "Unknown setter");
        }
    }

    [Theory(DisplayName = "A setter raises RequestChanged once and never changes the active tab")]
    [MemberData(nameof(Setters))]
    public void Setter_RaisesRequestChangedOnce(string setter)
    {
        var state = CreateState();
        var recorder = new Recorder(state);

        Set(setter, state, state.Active.Id);

        recorder.Request.ShouldBe(1);
        recorder.Active.ShouldBe(0);
    }

    [Theory(DisplayName = "A setter given the value the tab already has raises nothing")]
    [MemberData(nameof(Setters))]
    public void Setter_SameValue_RaisesNothing(string setter)
    {
        var state = CreateState();
        Set(setter, state, state.Active.Id);
        var recorder = new Recorder(state);

        Set(setter, state, state.Active.Id);

        recorder.Request.ShouldBe(0);
    }

    [Theory(DisplayName = "A setter for a tab that is not open does nothing")]
    [MemberData(nameof(Setters))]
    public void Setter_UnknownTab_DoesNothing(string setter)
    {
        var state = CreateState();
        var recorder = new Recorder(state);

        Set(setter, state, Guid.NewGuid());

        recorder.Request.ShouldBe(0);
    }

    [Fact(DisplayName = "The setters store their values on the tab's request")]
    public void Setters_StoreValues()
    {
        var state = CreateState();
        var id = state.Active.Id;

        state.SetUri(id, "https://api.example.com/changed");
        state.SetMethod(id, HttpMethod.Delete);
        state.SetBodyType(id, RequestBodyType.Raw);
        state.SetRawBody(id, """{"changed":true}""");
        state.SetHeaders(id, [new("Accept", "text/plain")]);
        state.SetFormData(id, new Dictionary<string, string> { ["name"] = "Ada" });
        state.SetAttachments(id, [Attachment]);

        var request = state.Active.Request.ShouldNotBeNull();
        request.RequestUri.ShouldBe("https://api.example.com/changed");
        request.Method.ShouldBe(HttpMethod.Delete);
        request.BodyType.ShouldBe(RequestBodyType.Raw);
        request.RawBody.ShouldBe("""{"changed":true}""");
        request.Headers.ShouldBe([new KeyValuePair<string, string>("Accept", "text/plain")]);
        request.FormData.ShouldBe(new Dictionary<string, string> { ["name"] = "Ada" });
        request.BinaryAttachments.ShouldBe([Attachment]);
    }

    [Fact(DisplayName = "Setting no method clears the method")]
    public void SetMethod_Null_ClearsMethod()
    {
        var state = CreateState("GET");

        state.SetMethod(state.Active.Id, null);

        state.Active.Request.ShouldNotBeNull().Method.ShouldBeNull();
    }

    [Fact(DisplayName = "Replacing a request swaps it and raises RequestChanged")]
    public void ReplaceRequest_SwapsRequest()
    {
        var state = CreateState();
        var recorder = new Recorder(state);
        var replacement = new HttpRequest { RequestUri = "https://api.example.com/loaded" };

        state.ReplaceRequest(state.Active.Id, replacement);

        state.Active.Request.ShouldBeSameAs(replacement);
        recorder.Request.ShouldBe(1);
        recorder.Active.ShouldBe(0);
    }

    [Fact(DisplayName = "Marking a tab as sending raises SendingChanged once")]
    public void SetSending_RaisesOnce()
    {
        var state = CreateState();
        var recorder = new Recorder(state);

        state.SetSending(state.Active.Id, true);
        state.SetSending(state.Active.Id, true);

        state.Active.IsSending.ShouldBeTrue();
        recorder.Sending.ShouldBe(1);
    }

    [Fact(DisplayName = "A duplicate of a tab that is sending is not sending")]
    public void Duplicate_OfSendingTab_IsNotSending()
    {
        var state = CreateState();
        state.SetSending(state.Active.Id, true);

        state.Duplicate(state.Active.Id).ShouldNotBeNull().IsSending.ShouldBeFalse();
    }

    [Fact(DisplayName = "Storing a response keeps it on the tab and raises ResponseChanged")]
    public void SetResponse_StoresResponse()
    {
        var state = CreateState();
        var recorder = new Recorder(state);
        var response = new RequestResult { StatusCode = 200 };

        state.SetResponse(state.Active.Id, response, "{}").ShouldBeTrue();

        state.Active.Response.ShouldBeSameAs(response);
        state.Active.DisplayText.ShouldBe("{}");
        recorder.Response.ShouldBe(1);
    }

    [Fact(DisplayName = "Storing a response for a closed tab does nothing")]
    public void SetResponse_ClosedTab_DoesNothing()
    {
        var state = CreateState();
        var closed = state.Open(Tab("https://api.example.com/users"));
        state.Close(closed.Id);
        var recorder = new Recorder(state);

        state.SetResponse(closed.Id, new RequestResult(), "{}").ShouldBeFalse();

        closed.Response.ShouldBeNull();
        recorder.Response.ShouldBe(0);
    }

    [Fact(DisplayName = "A list taken from Tabs does not change when a tab is added")]
    public void Tabs_Snapshot_DoesNotChange()
    {
        var state = CreateState();
        var before = state.Tabs;

        state.OpenNew();

        before.Count.ShouldBe(1);
        state.Tabs.Count.ShouldBe(2);
    }
}
