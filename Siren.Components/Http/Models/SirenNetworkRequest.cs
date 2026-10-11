using System;
namespace Siren.Components.Http.Models
{
    public class SirenNetworkRequest
    {
        public Guid Id { get; } = Guid.NewGuid();

        public HttpRequest? Request { get; set; }

        public RequestResult? Response { get; set; }

        public string? DisplayText { get; set; }

        public bool IsSending { get; internal set; }

        public SirenNetworkRequest Copy()
        {
            return new SirenNetworkRequest
            {
                Request = Request?.Copy(),
                Response = Response?.Copy(),
                DisplayText = DisplayText
            };
        }
    }
}

