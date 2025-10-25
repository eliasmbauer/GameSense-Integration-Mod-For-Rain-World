using BepInEx;
using System.Security.Permissions;
using System.Threading.Tasks;

// Allows access to private members
#pragma warning disable CS0618
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618

namespace Rain_World_GameSense
{
    [BepInPlugin("eliasmbauer.rwgamesense", "GameSense for Steelseries", "1.0.0")]
    public class GameSense_Mod : BaseUnityPlugin
    {
        private string SlugCatName, RegionName;
        private int FoodPips, KarmaLevel, MudLevel;
        private float RainTimer, BreathTimer, Hypothermia;
        private bool KarmaProtected, Stunned, Dead;
        private static int UpdateCount;
        private static readonly int RGBUpdateRate = 10; // determines how many in game ticks before the light manager get's an update (A rate of 10 means 1 RGB update per 10 in-game updates)
        private bool IsInit;
        // I swear there is a reason for three seperate locks.
        public static bool UpdateLock = false; // stopper variable that handles game closing and reopening
        public static bool InitComplete = false; // stopper variable for all updates in the event of a fatal error
        public static bool UpdatingRGB = false; // stopper variable for Light_Manager.UpdateRGB

        private void OnEnable()
        {
            On.RainWorld.OnModsInit += OnModsInit;
            On.RainWorldGame.ExitToMenu += RainWorldGame_ExitToMenu;
            On.RainWorldGame.ctor += RainWorldGame_Ctor;
            On.RainWorldGame.Update += RainWorldGame_Update;
        }

        private void OnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self)
        {
            orig(self);

            if (IsInit) return;
            IsInit = true;

            _ = Task.Run(Init);
        }

        private void RainWorldGame_ExitToMenu(On.RainWorldGame.orig_ExitToMenu orig, RainWorldGame self)
        {
            orig(self);
            UpdateLock = true;
            _ = Light_Manager.UpdateRGB(string.Empty, string.Empty, -1, -1, -1, -1, -1, false, false, false, false, true);
        }

        private void RainWorldGame_Ctor(On.RainWorldGame.orig_ctor orig, RainWorldGame self, ProcessManager manager)
        {
            orig(self, manager);
            UpdateLock = false;
        }

        private void RainWorldGame_Update(On.RainWorldGame.orig_Update orig, RainWorldGame self)
        {
            orig(self);
            if(UpdateCount % RGBUpdateRate == 0 && !UpdateLock && !UpdatingRGB && InitComplete)
            {
                Player player = self?.FirstRealizedPlayer ?? null;
                SlugCatName = player?.slugcatStats?.name?.ToString() ?? string.Empty;
                RegionName = self?.world?.region?.name ?? string.Empty;
                FoodPips = player?.CurrentFood ?? 0;
                KarmaLevel = player?.Karma ?? 0;
                RainTimer = self?.world?.rainCycle?.AmountLeft ?? 0;
                BreathTimer = player?.airInLungs ?? 0;
                Hypothermia = player?.Hypothermia ?? 0;
                KarmaProtected = player?.KarmaIsReinforced ?? false;
                Dead = player?.dead ?? false;
                Stunned = player?.Stunned ?? false;
                MudLevel = player?.muddy ?? 0;
                bool muddy = MudLevel > 0;
                _ = Light_Manager.UpdateRGB(SlugCatName, RegionName, FoodPips, KarmaLevel, RainTimer, BreathTimer, Hypothermia, KarmaProtected, Dead, Stunned, muddy, false);
            }
            UpdateCount++;
        }

        private async Task Init()
        {
            await Debug.Clear();
            await Debug.Log("--------------------INIT STARTED--------------------");

            if (!await Json_Manager.Init())
            {
                InitComplete = false;
                await Debug.Log("FATAL: Json_Manager Init Failed!");
                await Debug.Log("--------------------INIT FAILED--------------------");
                return;
            }
            if (!await Light_Manager.Init()) 
            {
                InitComplete = false;
                await Debug.Log("FATAL: Light_Manager Init Failed!");
                await Debug.Log("--------------------INIT FAILED--------------------");
                return;
            }
            string[] payloadlist = { "region", "karma", "rain_timer", "food", "breath", "slugcat", "hypothermia", "karma_protected"};
            foreach (string payload in payloadlist)
            {
                string json = await Json_Manager.GetConfig(payload + ".json");
                if (json != null) await Json_Manager.SendRawJson(json, "bind_game_event");
            }

            InitComplete = true;
            await Debug.Log("--------------------INIT SUCCESS--------------------");
        }
    }
}