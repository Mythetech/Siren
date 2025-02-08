using System.Net;

namespace Siren.Components.Services;

public interface ICookieService
{
    public List<Cookie> GetCookies();
    
    public void SaveCookie(Cookie cookie);
    
    public void DeleteCookie(string cookieName);
    
    public void DeleteAllCookies();
    
    public List<Cookie> ParseCookies(HttpResponseMessage response);
}

public class CookieService : ICookieService
{
    private List<Cookie> _cookies = [];
    
    public List<Cookie> GetCookies()
    {
        return _cookies;
    }

    public void SaveCookie(Cookie cookie)
    {
        _cookies.Add(cookie);
    }

    public void DeleteCookie(string cookieName)
    {
        _cookies.RemoveAll(x => x.Name == cookieName);
    }

    public void DeleteAllCookies()
    {
        _cookies = [];
    }
    
    public List<Cookie> ParseCookies(HttpResponseMessage response)
    {
        var cookies = new List<Cookie>();

        if (response.Headers.TryGetValues("Set-Cookie", out var cookieHeaders))
        {
            foreach (string header in cookieHeaders)
            {
                try
                {
                    var cookie = new Cookie();
                    var parts = header.Split(';');

                    // Parse the name-value pair
                    var nameValue = parts[0].Split(new[] { '=' }, 2);
                    if (nameValue.Length != 2 || string.IsNullOrWhiteSpace(nameValue[0])) 
                        continue;

                    cookie.Name = nameValue[0].Trim();
                    cookie.Value = Uri.UnescapeDataString(nameValue[1].Trim());

                    // Parse attributes
                    foreach (var part in parts.Skip(1))
                    {
                        var attrParts = part.Split(new[] { '=' }, 2);
                        var attrName = attrParts[0].Trim().ToLowerInvariant();
                        var attrValue = attrParts.Length > 1 ? attrParts[1].Trim() : string.Empty;

                        switch (attrName)
                        {
                            case "domain":
                                cookie.Domain = attrValue;
                                break;
                            case "path":
                                cookie.Path = attrValue;
                                break;
                            case "expires":
                                if (DateTimeOffset.TryParse(attrValue, out var expires))
                                {
                                    cookie.Expires = expires.LocalDateTime;
                                }
                                break;
                            case "secure":
                                cookie.Secure = true;
                                break;
                            case "httponly":
                                cookie.HttpOnly = true;
                                break;
                        }
                    }

                    cookies.Add(cookie);
                }
                catch (CookieException)
                {
                    // Skip invalid cookies
                    continue;
                }
            }
        }

        foreach (var cookie in cookies.Where(cookie => !_cookies.Any(x => x.Name == cookie.Name && x.Value == cookie.Value)))
        {
            _cookies.Add(cookie);
        }

        return cookies;
    }
}