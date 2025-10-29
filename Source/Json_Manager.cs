using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Rain_World_GameSense
{
    public static class Json_Manager
    {
        private static readonly string ConfigDirectory = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(typeof(Json_Manager).Assembly.Location), "..", "config"));
        private static string SteelseriesAddress;
        public static bool HeartbeatRunning = false;
        private static readonly HttpClient Client = new HttpClient();

        //Sends raw json data to Steelseries address
        public static async Task SendRawJson(string json, string address)
        {
            string debugMessage = null;
            try
            {
                var jsonDebug = JObject.Parse(json);
                string gameEvent = jsonDebug["event"]?.ToString() ?? "—";
                string dataDebug = jsonDebug["data"]?["value"]?.ToString() ?? "—";
                debugMessage = $"Event: {gameEvent} | Data: {dataDebug}";
            }
            catch (Exception ex)
            {
                await Debug.Log($"ERROR: Failed to parse json: {ex.Message}");
                return;
            }
            
            try
            {
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await Client.PostAsync($"http://{SteelseriesAddress}/{address}", content);
                await Debug.Log($"POST: {address} | {debugMessage} | {(int)response.StatusCode} | {response.ReasonPhrase}");
            }
            catch (Exception ex)
            {
                await Debug.Log($"ERROR: Problem sending json: {ex.Message} | {debugMessage}");
            }
        }
        
        // Pulls json data from the config directory in the mod folder
        public static async Task<string> GetConfig(string fileName)
        {
            string filePath = Path.Combine(ConfigDirectory, fileName);

            if (!File.Exists(filePath))
            {
                await Debug.Log($"ERROR: config file not found: {fileName}");
                return null;
            }

            using (var reader = new StreamReader(filePath, Encoding.UTF8))
            {
               string json = await reader.ReadToEndAsync();
               return json;
            }
        }
        
        // Fetch the Steelseries address either from the coreprops.json file the steelseries software ships with, or the manually entered steelseries-address.json
        private static async Task<string> GetSteelSeriesAddress()
        {
            string corePropsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), @"SteelSeries/SteelSeries Engine 3/coreProps.json");
            string manualAddressPath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(typeof(Json_Manager).Assembly.Location), "..", "config/steelseries-address.json"));

            // This block will run if the user has put anything into the custom address json
            try
            {
                string manualAddressJson = await GetConfig("steelseries-address.json");
                if (!string.IsNullOrEmpty(manualAddressJson))
                {
                    var manualAddressJsonObject = JObject.Parse(manualAddressJson);
                    string manualAddress = manualAddressJsonObject["address"]?.ToString();

                    if (!string.IsNullOrEmpty(manualAddress))
                    {
                        await Debug.Log($"SUCCESS: Found SteelSeries address (steelseries-address.json): {manualAddress}");
                        return manualAddress;
                    }
                    else
                    {
                        await Debug.Log("WARNING: No manual entry in steelseries-address.json");
                    }
                }
            }
            catch (Exception ex)
            {
                await Debug.Log($"WARNING: Failed to parse steelseries-address.json ({ex.Message})");
            }

            await Debug.Log("Searching alternatively for coreprops.json...");

            // This block will run if the user disn't put in custom address, or if parsing the custom address has failed
            if (!File.Exists(corePropsPath))
            {
                await Debug.Log($"ERROR: Steelseries coreProps.json not found at {corePropsPath}");
                return null;
            }
            try
            {
                string defaultAddressJson;
                using (var reader = new StreamReader(corePropsPath))
                {
                    defaultAddressJson = await reader.ReadToEndAsync();
                }
                var defaultAddressJsonObject = JObject.Parse(defaultAddressJson);
                string defaultAddress = defaultAddressJsonObject["address"].ToString();
                await Debug.Log($"SUCCESS: Found SteelSeries address (coreprops.json): {defaultAddress}");
                return defaultAddress;
            }
            catch (Exception ex)
            {
                await Debug.Log($"ERROR: coreProps.json didn't parse properly: {ex.Message}");
                return null;
            }
        }

        // Periodically sends a heartbeat to Steelseries to keep it from going to sleep (every 14 seconds)
        private static async Task StartKeepAlive()
        {
            HeartbeatRunning = true;
            var json = "{\"game\":\"RAINWORLD\"}";
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            while (HeartbeatRunning)
            {
                await Client.PostAsync($"http://{SteelseriesAddress}/game_heartbeat", content);
                await Task.Delay(14000);
            }
        }

        // I highly doubt these two methods will ever see the light of day
        public static async void RemoveEvent(string eventName)
        {
            var payload = new
            {
                game = "RAINWORLD",
                @event = eventName
            };

            string json = JsonConvert.SerializeObject(payload);
            await SendRawJson(json, "remove_game_event");
        }

        public static async void RemoveGame()
        {
            var payload = new
            {
                game = "RAINWORLD"
            };

            string json = JsonConvert.SerializeObject(payload);
            await SendRawJson(json, "remove_game");
        }

        public static async Task<bool> Init()
        {
            SteelseriesAddress = await GetSteelSeriesAddress();
            if (SteelseriesAddress == null) return false;
            string registerJson = await GetConfig("register.json");
            if (registerJson == null) return false; 
            await SendRawJson(registerJson, "game_metadata");
            if (!HeartbeatRunning) _ = Task.Run(StartKeepAlive);
            return true;
        }
    }
}
