// FILE: Services/GoogleAuthService.cs
// Google OAuth 2.0 via PKCE + loopback redirect (no NuGet package needed).
// Replace CLIENT_ID with your actual Google OAuth 2.0 Client ID from
// https://console.cloud.google.com  (Application type: Desktop app)
//
// HOW IT WORKS
//  1. GoogleLoginAsync() opens the system browser with a Google sign-in URL.
//  2. A tiny local HTTP listener catches the redirect and extracts the auth code.
//  3. The auth code is exchanged for tokens.
//  4. SignOut() revokes the token so the next call to GoogleLoginAsync() shows
//     the account-picker again instead of silently reusing the last session.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace KapwaKuha.Services
{
    public static class GoogleAuthService
    {
        // ── CONFIGURE THESE ──────────────────────────────────────────────────
        private const string ClientId = "1004459749091-44lp7vkh1o7bt5k58eqk82k97g5vk4qo.apps.googleusercontent.com";

        // FIX: Removed "/callback" because Google Desktop clients only accept the root IP address
        private const string RedirectUri = "http://127.0.0.1:8080";

        private const string Scope = "openid email profile";
        // ─────────────────────────────────────────────────────────────────────
        // ─────────────────────────────────────────────────────────────────────
        // ─────────────────────────────────────────────────────────────────────

        private static string? _accessToken;
        private static string? _refreshToken;
        private static string? _idToken;

        public static bool IsSignedIn => !string.IsNullOrEmpty(_accessToken);

        // ── PUBLIC API ────────────────────────────────────────────────────────

        /// <summary>
        /// Opens Google sign-in in the system browser and waits for the callback.
        /// Returns (email, name) on success, throws on failure/cancellation.
        /// </summary>
        public static async Task<(string Email, string Name)> GoogleLoginAsync(
            CancellationToken ct = default)
        {
            var (verifier, challenge) = GeneratePkce();
            var state = RandomBase64Url(16);
            var authUrl = BuildAuthUrl(challenge, state);

            using var listener = new HttpListener();
            listener.Prefixes.Add(RedirectUri + "/");
            listener.Start();

            Process.Start(new ProcessStartInfo(authUrl) { UseShellExecute = true });

            var contextTask = listener.GetContextAsync();
            var tcs = new TaskCompletionSource<HttpListenerContext>();
            using (ct.Register(() => tcs.TrySetCanceled()))
            {
                var completed = await Task.WhenAny(contextTask, tcs.Task);
                if (completed == tcs.Task) throw new OperationCanceledException();
            }
            var context = await contextTask;

            var response = context.Response;
            var html = "<html><body style='font-family:Segoe UI;text-align:center;margin-top:80px'>" +
                       "<h2 style='color:#4CAF50'>&#10003; Login successful!</h2>" +
                       "<p>You can close this tab and return to KapwaKuha.</p></body></html>";
            var bytes = Encoding.UTF8.GetBytes(html);
            response.ContentType = "text/html; charset=utf-8";
            response.ContentLength64 = bytes.Length;
            response.OutputStream.Write(bytes, 0, bytes.Length);
            response.Close();
            listener.Stop();

            var query = context.Request.QueryString;
            if (query["error"] != null)
                throw new InvalidOperationException($"Google error: {query["error"]}");
            if (query["state"] != state)
                throw new InvalidOperationException("State mismatch — possible CSRF.");

            var code = query["code"] ?? throw new InvalidOperationException("No auth code returned.");

            var tokens = await ExchangeCodeAsync(code, verifier);
            _accessToken = tokens.AccessToken;
            _refreshToken = tokens.RefreshToken;
            _idToken = tokens.IdToken;

            var payload = DecodeJwtPayload(tokens.IdToken);
            var email = payload.GetValueOrDefault("email") as string ?? "";
            var name = payload.GetValueOrDefault("name") as string ?? "";

            return (email, name);
        }

        /// <summary>
        /// Revokes the current access token so the next login shows the account picker.
        /// </summary>
        public static async Task SignOut()
        {
            if (string.IsNullOrEmpty(_accessToken)) return;
            try
            {
                using var http = new HttpClient();
                await http.PostAsync(
                    $"https://oauth2.googleapis.com/revoke?token={Uri.EscapeDataString(_accessToken)}",
                    null);
            }
            catch { /* best-effort */ }
            finally
            {
                _accessToken = null;
                _refreshToken = null;
                _idToken = null;
            }
        }

        // ── HELPERS ───────────────────────────────────────────────────────────

        private static string BuildAuthUrl(string challenge, string state)
        {
            var sb = new StringBuilder("https://accounts.google.com/o/oauth2/v2/auth?");
            sb.Append("client_id=").Append(Uri.EscapeDataString(ClientId));
            sb.Append("&redirect_uri=").Append(Uri.EscapeDataString(RedirectUri));
            sb.Append("&response_type=code");
            sb.Append("&scope=").Append(Uri.EscapeDataString(Scope));
            sb.Append("&code_challenge=").Append(challenge);
            sb.Append("&code_challenge_method=S256");
            sb.Append("&state=").Append(state);
            // prompt=select_account forces the picker even if already signed in
            sb.Append("&prompt=select_account");
            sb.Append("&access_type=offline");
            return sb.ToString();
        }

        private static async Task<TokenResponse> ExchangeCodeAsync(string code, string verifier)
        {
            using var http = new HttpClient();
            var body = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = ClientId,
                ["redirect_uri"] = RedirectUri,
                ["grant_type"] = "authorization_code",
                ["code"] = code,
                ["code_verifier"] = verifier
            });
            var resp = await http.PostAsync("https://oauth2.googleapis.com/token", body);
            var json = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode)
                throw new InvalidOperationException($"Token exchange failed: {json}");

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            return new TokenResponse(
                root.GetProperty("access_token").GetString() ?? "",
                root.TryGetProperty("refresh_token", out var rt) ? rt.GetString() ?? "" : "",
                root.GetProperty("id_token").GetString() ?? ""
            );
        }

        private static Dictionary<string, object?> DecodeJwtPayload(string jwt)
        {
            var parts = jwt.Split('.');
            if (parts.Length < 2) return new();
            var payload = parts[1].Replace('-', '+').Replace('_', '/');
            while (payload.Length % 4 != 0) payload += '=';
            var bytes = Convert.FromBase64String(payload);
            using var doc = JsonDocument.Parse(bytes);
            var result = new Dictionary<string, object?>();
            foreach (var prop in doc.RootElement.EnumerateObject())
                result[prop.Name] = prop.Value.ValueKind == JsonValueKind.String
                    ? prop.Value.GetString() : prop.Value.GetRawText();
            return result;
        }

        private static (string Verifier, string Challenge) GeneratePkce()
        {
            var verifier = RandomBase64Url(32);
            var hash = SHA256.HashData(Encoding.ASCII.GetBytes(verifier));
            var challenge = Convert.ToBase64String(hash)
                .Replace('+', '-').Replace('/', '_').TrimEnd('=');
            return (verifier, challenge);
        }

        private static string RandomBase64Url(int byteLength)
        {
            var bytes = RandomNumberGenerator.GetBytes(byteLength);
            return Convert.ToBase64String(bytes)
                .Replace('+', '-').Replace('/', '_').TrimEnd('=');
        }

        private record TokenResponse(string AccessToken, string RefreshToken, string IdToken);
    }
}