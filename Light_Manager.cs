using IL.MoreSlugcats;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Rain_World_GameSense
{
    public static class Light_Manager
    {
        // Every variable currently visible on the keyboard
        public static string SlugCatName = string.Empty;
        public static string RegionName = string.Empty;
        public static int FoodPips = -1;
        public static int KarmaLevel = -1;
        public static float RainTimer = -1;
        public static float BreathTimer = -1;
        public static float Hypothermia = -1;
        public static bool KarmaProtected = false;
        public static bool Dead = false;
        public static bool Stunned = false;
        public static bool Muddy = false;
        public static Dictionary<string, (int red, int green, int blue)> ScugColors;
        public static Dictionary<string, (int red, int green, int blue)> RegionColors;

        private static async Task<Dictionary<string, (int, int, int)>> LoadRGBValues(string filename)
        {
            string json = await Json_Manager.GetConfig(filename);
            if (string.IsNullOrEmpty(json))
            {
                return null;
            }

            try
            {
                var dict = JsonConvert.DeserializeObject<Dictionary<string, int[]>>(json);
                await Debug.Log($"SUCCESS: Fetched RGB Values From: {filename}");
                var result = new Dictionary<string, (int, int, int)>();
                foreach (var kvp in dict)
                {
                    result[kvp.Key] = (kvp.Value[0], kvp.Value[1], kvp.Value[2]);
                }
                return result;
            }
            catch(Exception ex)
            {
                await Debug.Log($"ERROR: Failed to parse the contents of {filename}: {ex.Message}");
                return null;
            }
        }

        public async static Task UpdateRGB(string newSlugCatName, string newRegionName, int newFoodPips, int newKarmaLevel, float newRainTimer, float newBreathTimer, float newHypothermia, bool newKarmaProtected, bool newDead, bool newStunned, bool newMuddy, bool forcedUpdated)
        {
            GameSense_Mod.UpdatingRGB = true;
            try
            {
                if (forcedUpdated || newSlugCatName != SlugCatName)
                {
                    SlugCatName = newSlugCatName ?? "default";
                    var (red, green, blue) = ScugColors.TryGetValue(SlugCatName, out var colorValue) ? colorValue : (0, 0, 0);
                    await SendZoneUpdate("SLUGCAT", "slugcatColor", red, green, blue);
                }

                if (forcedUpdated || newRegionName != RegionName && SlugCatName != string.Empty)
                {
                    RegionName = newRegionName ?? "default";
                    var (red, green, blue) = RegionColors.TryGetValue(RegionName, out var colorValue) ? colorValue : (0, 0, 0);
                    if (SlugCatName == "Saint" && RegionName != "HR")
                    {
                        red = green = blue = 110;
                    }
                    await SendZoneUpdate("REGION", "regionColor", red, green, blue);
                }

                if (forcedUpdated || newKarmaProtected != KarmaProtected)
                {
                    KarmaProtected = newKarmaProtected;
                    if (KarmaProtected == true)
                    {
                        await SendZoneUpdate("KARMA_PROTECTED", "karmaProtectedColor", 255, 180, 0);
                    }
                    else
                    {
                        await SendZoneUpdate("KARMA_PROTECTED", "karmaProtectedColor", 0, 0, 0);
                    }
                }

                if (forcedUpdated || newFoodPips != FoodPips)
                {
                    FoodPips = newFoodPips;
                    await SendCountUpdate("FOOD", FoodPips);
                }

                if (forcedUpdated || newKarmaLevel != KarmaLevel)
                {
                    KarmaLevel = newKarmaLevel;
                    await SendCountUpdate("KARMA", KarmaLevel + 1);
                }

                if (forcedUpdated || Math.Abs(newRainTimer - RainTimer) >= 0.01f)
                {
                    RainTimer = newRainTimer;
                    await SendPercentUpdate("RAIN_TIMER", RainTimer);
                }

                if (forcedUpdated || Math.Abs(newBreathTimer - BreathTimer) >= 0.01f)
                {
                    BreathTimer = newBreathTimer;
                    await SendPercentUpdate("BREATH", BreathTimer);
                }

                if (forcedUpdated || Math.Abs(newHypothermia - Hypothermia) >= 0.1f && SlugCatName != string.Empty)
                {
                    Hypothermia = newHypothermia;
                    var value = (int)Math.Round(Hypothermia * 100f);
                    value = Mathf.Clamp(value, 0, 100);
                    value = 100 - value;
                    if (SlugCatName != "Saint")
                    {
                        value = 0;
                    }
                    await SendPercentUpdate("HYPOTHERMIA", value / 100f);
                }

                if (forcedUpdated || newDead != Dead)
                {
                    Dead = newDead;
                    if (Dead)
                    {
                        await SendZoneUpdate("SLUGCAT", "slugcatColor", 255, 0, 0);
                    }
                    else
                    {
                        var (red, green, blue) = ScugColors.TryGetValue(SlugCatName, out var colorValue) ? colorValue : (0, 0, 0);
                        await SendZoneUpdate("SLUGCAT", "slugcatColor", red, green, blue);
                    }
                }

                if (forcedUpdated || newMuddy != Muddy)
                {
                    Muddy = newMuddy;
                    if (Muddy)
                    {
                        await SendZoneUpdate("SLUGCAT", "slugcatColor", 85, 50, 25);
                    }
                    else
                    {
                        var (red, green, blue) = ScugColors.TryGetValue(SlugCatName, out var colorValue) ? colorValue : (0, 0, 0);
                        await SendZoneUpdate("SLUGCAT", "slugcatColor", red, green, blue);
                    }
                }

                if (forcedUpdated || newStunned != Stunned)
                {
                    Stunned = newStunned;
                    if (Stunned && !Dead)
                    {
                        var (red, green, blue) = ScugColors.TryGetValue(SlugCatName, out var colorValue) ? colorValue : (0, 0, 0);
                        _ = Task.Run(async () =>
                        {
                            for (int i = 0; i < 2; i++)
                            {
                                await SendZoneUpdate("SLUGCAT", "slugcatColor", 255, 255, 255);
                                await Task.Delay(200);
                                if (Muddy)
                                {
                                    await SendZoneUpdate("SLUGCAT", "slugcatColor", 85, 50, 25);
                                }
                                else
                                {
                                    await SendZoneUpdate("SLUGCAT", "slugcatColor", red, green, blue);
                                }
                                await Task.Delay(200);
                            }
                        });
                    }
                }
            }
            finally
            {
                GameSense_Mod.UpdatingRGB = false;
            }
        }

        private static async Task SendCountUpdate(string steelSeriesEvent, int count)
        {
            var payload = new
            {
                game = "RAINWORLD",
                @event = steelSeriesEvent,
                data = new
                {
                    value = count
                }
            };

            string json = JsonConvert.SerializeObject(payload);
            await Json_Manager.SendRawJson(json, "game_event");
        }

        private static async Task SendPercentUpdate(string steelSeriesEvent, float percentf)
        {
            int percent = (int)Math.Round(percentf * 100f);
            percent = Mathf.Clamp(percent, 0, 100);

            var payload = new
            {
                game = "RAINWORLD",
                @event = steelSeriesEvent,
                data = new
                {
                    value = percent
                }
            };

            string json = JsonConvert.SerializeObject(payload);
            await Json_Manager.SendRawJson(json, "game_event");
        }

        private static async Task SendZoneUpdate(string steelSeriesEvent, string fieldName, int red, int green, int blue)
        {
            var payload = new
            {
                game = "RAINWORLD",
                @event = steelSeriesEvent,
                data = new
                {
                    frame = new Dictionary<string, object>
                    {
                        [fieldName] = new { red, green, blue }
                    }
                }
            };

            string json = JsonConvert.SerializeObject(payload);
            await Json_Manager.SendRawJson(json, "game_event");
        }

        public static async Task<bool> Init()
        {
            ScugColors = await LoadRGBValues("slugcat_color_list.json");
            RegionColors = await LoadRGBValues("region_color_list.json");
            if (ScugColors == null || RegionColors == null)
            {
                return false;
            }
            return true;
        }
    }
}
