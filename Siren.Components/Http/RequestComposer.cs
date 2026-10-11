using System.Net.Http.Headers;
using Siren.Components.Http.Models;
using Siren.Components.Services;

namespace Siren.Components.Http;

public interface IRequestComposer
{
    /// <summary>
    /// Builds the request that goes on the wire from a tab's request: variables are substituted and
    /// the body is turned into content. The template is left as it is.
    /// </summary>
    Task<ComposedRequest> ComposeAsync(HttpRequest template, string? environment);
}

/// <param name="Request">The request to send</param>
/// <param name="ResolutionErrors">Variables that could not be resolved, each listed once</param>
public sealed record ComposedRequest(HttpRequest Request, IReadOnlyList<string> ResolutionErrors);

public class RequestComposer : IRequestComposer
{
    private const string DefaultContentType = "application/json";
    private const string FormContentType = "application/x-www-form-urlencoded";
    private const string MultipartContentType = "multipart/form-data";
    private const string ContentTypeHeader = "Content-Type";
    private const string FilePartName = "file";

    private readonly IVariableSubstitutionService _variables;

    public RequestComposer(IVariableSubstitutionService variables)
    {
        _variables = variables;
    }

    public async Task<ComposedRequest> ComposeAsync(HttpRequest template, string? environment)
    {
        var errors = new List<string>();

        async Task<string> ResolveAsync(string? value)
        {
            var result = await _variables.SubstituteVariablesAsync(value ?? "", environment);
            errors.AddRange(result.Errors);
            return result.Value;
        }

        var contentType = DefaultContentType;
        HttpContent? content = null;

        switch (template.BodyType)
        {
            case RequestBodyType.FormData:
            {
                var fields = new Dictionary<string, string>();
                foreach (var field in template.FormData ?? new Dictionary<string, string>())
                {
                    var name = await ResolveAsync(field.Key);
                    fields[name] = await ResolveAsync(field.Value);
                }

                contentType = FormContentType;
                content = new FormUrlEncodedContent(fields);
                break;
            }
            case RequestBodyType.MultipartFormData:
            {
                var multipart = new MultipartFormDataContent();
                foreach (var field in template.FormData ?? new Dictionary<string, string>())
                {
                    var name = await ResolveAsync(field.Key);
                    multipart.Add(new StringContent(await ResolveAsync(field.Value)), name);
                }

                AddFiles(multipart, template.BinaryAttachments ?? []);
                contentType = multipart.Headers.ContentType?.ToString() ?? MultipartContentType;
                content = multipart;
                break;
            }
            case RequestBodyType.Raw:
            {
                var contentTypeRow = (template.Headers ?? []).FirstOrDefault(h =>
                    string.Equals(h.Key, ContentTypeHeader, StringComparison.OrdinalIgnoreCase));
                if (contentTypeRow.Key != null)
                    contentType = contentTypeRow.Value;

                content = RawBodyContent.Create(await ResolveAsync(template.RawBody), contentType);
                break;
            }
            case RequestBodyType.Binary:
            {
                var attachments = template.BinaryAttachments ?? [];
                if (attachments.Count == 1)
                {
                    contentType = attachments[0].ContentType;
                    content = FileContent(attachments[0]);
                }
                else if (attachments.Count > 1)
                {
                    var multipart = new MultipartFormDataContent();
                    AddFiles(multipart, attachments);
                    contentType = multipart.Headers.ContentType?.ToString() ?? MultipartContentType;
                    content = multipart;
                }

                break;
            }
        }

        var requestUri = await ResolveAsync(template.RequestUri);

        var headers = new List<KeyValuePair<string, string>>();
        foreach (var header in template.Headers ?? [])
        {
            var name = await ResolveAsync(header.Key);
            headers.Add(new KeyValuePair<string, string>(name, await ResolveAsync(header.Value)));
        }

        var request = new HttpRequest
        {
            ContentType = contentType,
            RequestUri = requestUri,
            Method = template.Method ?? HttpMethod.Get,
            Headers = headers,
            FormData = new Dictionary<string, string>(template.FormData ?? new Dictionary<string, string>()),
            BinaryAttachments = (template.BinaryAttachments ?? []).Select(a => a.Copy()).ToList(),
            Content = content!,
            BodyType = template.BodyType,
            RawBody = template.RawBody ?? "",
        };

        return new ComposedRequest(request, errors.Distinct().ToList());
    }

    private static void AddFiles(MultipartFormDataContent multipart, List<BinaryAttachment> attachments)
    {
        foreach (var attachment in attachments)
            multipart.Add(FileContent(attachment), FilePartName, attachment.FileName);
    }

    private static ByteArrayContent FileContent(BinaryAttachment attachment)
    {
        var content = new ByteArrayContent(attachment.Data ?? []);
        content.Headers.ContentType = new MediaTypeHeaderValue(attachment.ContentType);
        return content;
    }
}
