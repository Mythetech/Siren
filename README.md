# Siren

> Call to your endpoints

Siren is an open source cross-platform desktop http testing application built with Blazor and Hermes. Siren aims for simpler use cases targeted towards people who want something more lightweight and without any imposition of creating an account for more functionality.

Find out more at [mythetech.com/siren](https://www.mythetech.com/siren).

[![MIT License](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

<img width="1728" height="1052" alt="Siren" src="https://github.com/user-attachments/assets/f97287a8-03b2-40b5-a48f-580ef56638da" />

---

<img width="1728" height="1052" alt="Siren Dark Mode" src="https://github.com/user-attachments/assets/2d305365-176e-4153-adf2-c7c18cbe465e" />

## Features

### HTTP Client

- Make requests to your favorite HTTP based endpoints
- Test your local running WebApi projects
- Send GET, POST, PUT, DELETE, PATCH, OPTIONS, HEAD and QUERY requests
- Set query parameters and headers, and view request and response cookies
- Send raw, form-data, multipart or binary request bodies
- Work on several requests at once in tabs
- History of requests
- Save collections of related requests

### Authentication

- Bearer token
- Basic auth
- API key sent as a header

### Environments and Variables

- Define variables globally or per environment and switch the active environment
- Reference variables as `{{variableName}}` in URLs, headers, bodies and form data
- Generate a fresh value on each request with `{{$uuid}}`, `{{$timestamp}}`, `{{$isoDate}}`, `{{$randomInt}}` and `{{$randomString}}`
- Mark a variable as secret to hide its value
- Set a variable to `$secret:keyName` to pull its value from your operating system's secret store or 1Password
- Preview the resolved values before sending

### Mock Server

- Create mock server configurations, each on its own local port, and start or stop them from the app
- Define endpoints by method and route pattern, for example `/api/example/{id}`
- Set the status code, content type, body and response delay for each endpoint
- Create a mock endpoint from a request in your history or a collection

### Import and Export

- Import a request from a cURL command and export the active request as cURL
- Import a collection from an OpenAPI spec URL, a Postman collection or a Bruno collection
- Export collections to JSON
- Export the last request or your whole history as HAR
- Save a request or response to a file and import a saved request

### Response Inspection

- View the body as JSON, plain text or rendered HTML, or switch to the response headers
- Format, search and copy the response body
- See where the time went: DNS, TCP, TLS, request, wait and download
- See network details: local and remote address, protocol, cipher and certificate
- See request and response size split into headers and body

### MCP Server

Siren includes a [Model Context Protocol](https://modelcontextprotocol.io) server so an AI assistant can work with your requests. With it an assistant can:

- Send an HTTP request and read the response (`send_http_request`)
- List your history and get the full details of a past request (`list_history`, `get_http_request_details`)
- List your collections and add a request to one (`list_collections`, `add_request_to_collection`)
- List your environments and their variables, with secret values masked (`list_variable_environments`, `list_variables`)
- Read your current settings (`list_settings`)

The MCP server is off by default. There are two ways to reach it:

- **Stdio.** Start Siren with the `--mcp` flag and it runs without a window, serving MCP over standard input and output. Point your MCP client at the Siren executable:

  ```json
  {
    "mcpServers": {
      "siren": {
        "command": "/path/to/Siren",
        "args": ["--mcp"]
      }
    }
  }
  ```

  From a source checkout the equivalent is:

  ```bash
  dotnet run --project Siren -- --mcp
  ```

- **HTTP.** The desktop app is configured to serve MCP at `http://localhost:3333/mcp`, on localhost only. This listener does not start with the app, and the current menus have no item to start it, so use `--mcp` for now.

### Development Experience

- Command palette for navigation and common actions (Ctrl+K, or Cmd+K on macOS)
- Keyboard shortcuts, listed below
- Native application menus for file, edit, tools and plugin actions
- Session stats for the current session: total requests, success streak, fastest response and top domain
- Settings for request defaults (timeout, retries, user agent, default method), response display, history storage and the default environment
- Plugins: import a plugin DLL, then enable or disable it from the Plugins menu
- An update indicator in the title bar when a new version is available, plus Check for Updates in the Tools menu
- Light/Dark Mode, or follow the system setting
- Local machine persistence
- No Sign Up / Online Account

### Keyboard Shortcuts

Use Cmd in place of Ctrl on macOS.

| Shortcut | Action |
| --- | --- |
| Ctrl+Enter | Run the active request |
| Ctrl+T | New request tab |
| Ctrl+U | Focus the URI input |
| Ctrl+M | Focus the HTTP method |
| Ctrl+S | Open settings |
| Ctrl+K | Open the command palette |
| Ctrl+Shift+H | Open the history panel |
| Ctrl+Shift+C | Open the collections panel |
| Ctrl+Shift+V | Open the variables panel |
| Ctrl+Shift+R | Go to the requests page |
| Ctrl+Shift+M | Go to the mock server page |

### Key Technologies

- .NET 11
- Blazor Hybrid
- Hermes
- MudBlazor & Mythetech Framework
- BlazorMonaco for the request and response editors
- LiteDB for local storage
- Velopack for installers and updates

## Development Setup

### Prerequisites

- .NET 11 SDK; the exact build is pinned in [global.json](global.json)

### Getting Started

1. Clone the repository

   ```bash
   git clone https://github.com/mythetech/siren.git
   cd siren
   ```

2. Install dependencies

   ```bash
   dotnet restore Siren/Siren.sln
   ```

3. Build the solution

   ```bash
   dotnet build Siren/Siren.sln
   ```

4. Run the application

   ```bash
   dotnet run --project Siren
   ```

5. Call to your endpoints

### Testing

Run the test suite with the same command CI uses:

```bash
dotnet test Siren.Test/Siren.Test.csproj
```

### Project Structure

- **Siren**: The desktop host. Core logic, services, repositories, data persistence, the MCP tools and native menus.
- **Siren.Components**: UI components (Blazor `.razor` files), UI interfaces and presentation logic.
- **Siren.Test**: Unit tests and integration tests.

The solution file is `Siren/Siren.sln`.

## Contributing

We welcome contributions! Please read [CONTRIBUTING.md](CONTRIBUTING.md) for how to build, test and submit a pull request, and follow our [Code of Conduct](CODE_OF_CONDUCT.md).

## Project Status

Siren is currently in alpha. While it's stable enough for learning and experimentation, we recommend against production use.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
