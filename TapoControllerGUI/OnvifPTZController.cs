using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace TapoControllerGUI
{
    public class OnvifPTZController
    {
        private readonly string _host;
        private readonly string _username;
        private readonly string _password;
        private readonly HttpClient _httpClient;

        public OnvifPTZController(string host, string username, string password)
        {
            _host = host;
            _username = username;
            _password = password;
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(5);
        }

        public async Task<bool> ConnectAsync()
        {
            // Test connection with a simple HTTP request
            try
            {
                var response = await _httpClient.GetAsync($"http://{_host}");
                return true;
            }
            catch
            {
                return false;
            }
        }

        private async Task SendOnvifCommand(string soapBody)
        {
            try
            {
                var soapEnvelope = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<s:Envelope xmlns:s=""http://www.w3.org/2003/05/soap-envelope"" 
            xmlns:tptz=""http://www.onvif.org/ver20/ptz/wsdl"">
  <s:Body>
    {soapBody}
  </s:Body>
</s:Envelope>";

                var content = new StringContent(soapEnvelope, Encoding.UTF8, "application/soap+xml");
                
                // ONVIF typically uses port 80 or 8080 for PTZ
                var url = $"http://{_host}/onvif/ptz";
                
                var response = await _httpClient.PostAsync(url, content);
                var responseContent = await response.Content.ReadAsStringAsync();
                
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"ONVIF command failed: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ONVIF command error: {ex.Message}");
            }
        }

        public async Task MoveAsync(float panSpeed, float tiltSpeed, float zoomSpeed = 0)
        {
            var soapBody = $@"
    <tptz:ContinuousMove>
      <tptz:ProfileToken>profile_1</tptz:ProfileToken>
      <tptz:Velocity>
        <tt:PanTilt x=""{panSpeed}"" y=""{tiltSpeed}"" xmlns:tt=""http://www.onvif.org/ver10/schema""/>
        <tt:Zoom x=""{zoomSpeed}"" xmlns:tt=""http://www.onvif.org/ver10/schema""/>
      </tptz:Velocity>
    </tptz:ContinuousMove>";

            await SendOnvifCommand(soapBody);
        }

        public async Task StopAsync()
        {
            var soapBody = @"
    <tptz:Stop>
      <tptz:ProfileToken>profile_1</tptz:ProfileToken>
      <tptz:PanTilt>true</tptz:PanTilt>
      <tptz:Zoom>true</tptz:Zoom>
    </tptz:Stop>";

            await SendOnvifCommand(soapBody);
        }

        public async Task MoveUpAsync(float speed = 0.5f) => await MoveAsync(0, speed, 0);
        public async Task MoveDownAsync(float speed = 0.5f) => await MoveAsync(0, -speed, 0);
        public async Task MoveLeftAsync(float speed = 0.5f) => await MoveAsync(-speed, 0, 0);
        public async Task MoveRightAsync(float speed = 0.5f) => await MoveAsync(speed, 0, 0);
        public async Task ZoomInAsync(float speed = 0.5f) => await MoveAsync(0, 0, speed);
        public async Task ZoomOutAsync(float speed = 0.5f) => await MoveAsync(0, 0, -speed);

        public async Task<bool> GotoPresetAsync(int presetNumber)
        {
            try
            {
                var presetToken = $"preset_{presetNumber + 1}";
                var soapBody = $@"
    <tptz:GotoPreset>
      <tptz:ProfileToken>profile_1</tptz:ProfileToken>
      <tptz:PresetToken>{presetToken}</tptz:PresetToken>
      <tptz:Speed>
        <tt:PanTilt x=""1.0"" y=""1.0"" xmlns:tt=""http://www.onvif.org/ver10/schema""/>
        <tt:Zoom x=""1.0"" xmlns:tt=""http://www.onvif.org/ver10/schema""/>
      </tptz:Speed>
    </tptz:GotoPreset>";

                await SendOnvifCommand(soapBody);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public void Disconnect()
        {
            // Clean up if needed
        }
    }
}
