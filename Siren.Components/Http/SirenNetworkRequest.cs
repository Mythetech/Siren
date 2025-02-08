using System;
namespace Siren.Components.Http
{
    public class SirenNetworkRequest
    {
        public Guid Id { get; } = Guid.NewGuid();

        public string Name { get; set; } = "";

        public HttpRequest? Request { get; set; }

        public RequestResult? Response { get; set; }

        public string? DisplayText { get; set; }

        public SirenNetworkRequest Copy()
        {
            return new SirenNetworkRequest
            {
                Name = Name,
                Request = Request?.Copy(),
                Response = Response?.Copy(),
                DisplayText = DisplayText
            };
        }
    }
}

