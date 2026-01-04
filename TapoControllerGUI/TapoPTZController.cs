using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Security.Cryptography;

namespace TapoControllerGUI
{
    public class TapoPTZController
    {
        private readonly string _host;
        private readonly string _username;
        private readonly string _password;
        private readonly HttpClient _httpClient;
        private string? _stok;

        public TapoPTZController(string host, string username, string password)
        {
            _host = host;
            _username = username;
            _password = password;
            
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            };
            _httpClient = new HttpClient(handler);
            _httpClient.Timeout = TimeSpan.FromSeconds(10);
        }

        public async Task<bool> ConnectAsync()
        {
            try
            {
                // Authenticate with Tapo camera
                var loginUrl = $"https://{_host}";
                
                // Step 1: Handshake
                var handshakePayload = new
                {
                    method = "login",
                    parameters = new
                    {
                        username = _username,
                        password = _password
                    }
                };

                var json = JsonSerializer.Serialize(handshakePayload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PostAsync(loginUrl, content);
                var responseContent = await response.Content.ReadAsStringAsync();
                
                // Extract stok token from response
                if (responseContent.Contains("stok="))
                {
                    var stokStart = responseContent.IndexOf("stok=") + 5;
                    var stokEnd = responseContent.IndexOf("/", stokStart);
                    if (stokEnd > stokStart)
                    {
                        _stok = responseContent.Substring(stokStart, stokEnd - stokStart);
                        return true;
                    }
                }
                
                return false;
            }
            catch
            {
                return false;
            }
        }

        private async Task<bool> SendPTZCommand(string method, object? parameters = null)
        {
            try
            {
                if (string.IsNullOrEmpty(_stok))
                {
                    if (!await ConnectAsync())
                        return false;
                }

                var url = $"https://{_host}/stok={_stok}/ds";
                
                var payload = new
                {
                    method = method,
                    parameters = parameters ?? new { }
                };

                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PostAsync(url, content);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task MoveAsync(string direction)
        {
            // Tapo motor_move command
            await SendPTZCommand("do", new
            {
                motor = new
                {
                    move = new
                    {
                        x_coord = direction == "right" ? "1" : direction == "left" ? "-1" : "0",
                        y_coord = direction == "up" ? "1" : direction == "down" ? "-1" : "0"
                    }
                }
            });
        }

        public async Task StopAsync()
        {
            // Stop command - send zero movement
            await SendPTZCommand("do", new
            {
                motor = new
                {
                    stop = new { }
                }
            });
        }

        public async Task MoveUpAsync(float speed = 0.5f) => await MoveAsync("up");
        public async Task MoveDownAsync(float speed = 0.5f) => await MoveAsync("down");
        public async Task MoveLeftAsync(float speed = 0.5f) => await MoveAsync("left");
        public async Task MoveRightAsync(float speed = 0.5f) => await MoveAsync("right");
        
        public async Task ZoomInAsync(float speed = 0.5f)
        {
            // Tapo cameras don't typically support zoom via API
            await Task.CompletedTask;
        }
        
        public async Task ZoomOutAsync(float speed = 0.5f)
        {
            // Tapo cameras don't typically support zoom via API
            await Task.CompletedTask;
        }

        public async Task<bool> GotoPresetAsync(int presetNumber)
        {
            try
            {
                // Tapo preset command
                return await SendPTZCommand("do", new
                {
                    preset = new
                    {
                        goto_preset = new
                        {
                            id = (presetNumber + 1).ToString()
                        }
                    }
                });
            }
            catch
            {
                return false;
            }
        }

        public void Disconnect()
        {
            _stok = null;
        }
    }
}
