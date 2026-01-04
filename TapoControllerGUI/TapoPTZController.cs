using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace TapoControllerGUI
{
    public class TapoPTZController
    {
        private readonly string _host;
        private readonly string _username;
        private readonly string _password;
        private readonly HttpClient _httpClient;

        public TapoPTZController(string host, string username, string password)
        {
            _host = host;
            _username = username;
            _password = password;
            
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true,
                Credentials = new System.Net.NetworkCredential(username, password),
                PreAuthenticate = true
            };
            _httpClient = new HttpClient(handler);
            _httpClient.Timeout = TimeSpan.FromSeconds(5);
        }

        public async Task<bool> ConnectAsync()
        {
            // Test ONVIF connection on multiple possible endpoints
            var endpoints = new[]
            {
                $"http://{_host}:2020/onvif/device_service",
                $"http://{_host}:8000/onvif/device_service",
                $"http://{_host}:80/onvif/device_service",
                $"https://{_host}:443/onvif/device_service"
            };

            foreach (var endpoint in endpoints)
            {
                try
                {
                    var soapRequest = CreateONVIFRequest("GetDeviceInformation", "http://www.onvif.org/ver10/device/wsdl");
                    var content = new StringContent(soapRequest, Encoding.UTF8, "application/soap+xml");
                    
                    var response = await _httpClient.PostAsync(endpoint, content);
                    if (response.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"ONVIF connection successful: {endpoint}");
                        return true;
                    }
                }
                catch
                {
                    continue;
                }
            }
            
            return false;
        }

        private string CreateONVIFRequest(string action, string xmlns, string body = "")
        {
            return $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<s:Envelope xmlns:s=""http://www.w3.org/2003/05/soap-envelope"">
    <s:Body>
        <{action} xmlns=""{xmlns}"">
            {body}
        </{action}>
    </s:Body>
</s:Envelope>";
        }

        private async Task<bool> SendPTZCommand(string command, string parameters = "")
        {
            var endpoints = new[]
            {
                $"https://{_host}:2020/onvif/ptz_service",
                $"http://{_host}:2020/onvif/ptz_service",
                $"https://{_host}:443/onvif/ptz_service",
                $"http://{_host}:2020/onvif/ptz",
                $"http://{_host}:8000/onvif/ptz",
                $"http://{_host}:80/onvif/ptz"
            };

            var soapRequest = CreateONVIFRequest(command, "http://www.onvif.org/ver20/ptz/wsdl", parameters);
            var content = new StringContent(soapRequest, Encoding.UTF8, "application/soap+xml");

            foreach (var endpoint in endpoints)
            {
                try
                {
                    var response = await _httpClient.PostAsync(endpoint, content);
                    if (response.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"PTZ command sent successfully to: {endpoint}");
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"PTZ failed on {endpoint}: {ex.Message}");
                    continue;
                }
            }

            Console.WriteLine("PTZ command failed on all endpoints");
            return false;
        }

        public async Task MoveAsync(float panSpeed, float tiltSpeed, float zoomSpeed = 0)
        {
            var body = $@"
            <ProfileToken>profile_1</ProfileToken>
            <Velocity>
                <PanTilt x=""{panSpeed}"" y=""{tiltSpeed}"" xmlns=""http://www.onvif.org/ver10/schema""/>
                <Zoom x=""{zoomSpeed}"" xmlns=""http://www.onvif.org/ver10/schema""/>
            </Velocity>";

            await SendPTZCommand("ContinuousMove", body);
        }

        public async Task StopAsync()
        {
            var body = @"
            <ProfileToken>profile_1</ProfileToken>
            <PanTilt>true</PanTilt>
            <Zoom>true</Zoom>";

            await SendPTZCommand("Stop", body);
        }

        public async Task MoveUpAsync(float speed = 0.5f) => await MoveAsync(0, speed, 0);
        public async Task MoveDownAsync(float speed = 0.5f) => await MoveAsync(0, -speed, 0);
        public async Task MoveLeftAsync(float speed = 0.5f) => await MoveAsync(-speed, 0, 0);
        public async Task MoveRightAsync(float speed = 0.5f) => await MoveAsync(speed, 0, 0);
        public async Task ZoomInAsync(float speed = 0.5f) => await MoveAsync(0, 0, speed);
        public async Task ZoomOutAsync(float speed = 0.5f) => await MoveAsync(0, 0, -speed);

        public async Task<bool> GotoPresetAsync(int presetNumber)
        {
            var body = $@"
            <ProfileToken>profile_1</ProfileToken>
            <PresetToken>preset_{presetNumber + 1}</PresetToken>";

            return await SendPTZCommand("GotoPreset", body);
        }

        public void Disconnect()
        {
            // Clean up
        }
    }
}
