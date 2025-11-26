using Rocket.API;

namespace nexusUT
{
    public class PoliceUTConfiguration : IRocketPluginConfiguration
    {
        public string IconImageUrl { get; set; }
        public float MaxDragDistance { get; set; }
        public bool RequireLooking { get; set; }
        public ushort TaserId { get; set; }
        public float TaserTime { get; set; }
        public bool EnforceJailRadius { get; set; }
        public float MaxFriskDistance { get; set; }
        public float JailReleaseX { get; set; }
        public float JailReleaseY { get; set; }
        public float JailReleaseZ { get; set; }
        public bool EnableJailWebhook { get; set; }
        public string JailWebhookUrl { get; set; }

        public bool EnableJailUI { get; set; }
        public ushort JailUI_ID { get; set; }

        public bool EnableFineWebhook { get; set; }
        public string FineWebhookUrl { get; set; }
        public bool EnableArrestLogWebhook { get; set; }
        public string ArrestLogWebhookUrl { get; set; }
        public void LoadDefaults()
        {
            IconImageUrl = "https://imgur.com/NktP6Sn.png";
            MaxDragDistance = 50f;
            TaserId = 15101;
            TaserTime = 5.0f;
            EnforceJailRadius = true;
            MaxFriskDistance = 5f;
            JailReleaseX = 0;
            JailReleaseY = 100;
            JailReleaseZ = 0;
            RequireLooking = true;

            EnableFineWebhook = true;
            FineWebhookUrl = "https://discord.com/api/webhooks/1399844443691159643/kbHfdAP-NHupB0Qr-ct3Yun-2TX8j9QyTYoT5GoGsV36avoltBhmxTiAyTUBRyWq1IKj";

            EnableArrestLogWebhook = true;
            ArrestLogWebhookUrl = "https://discord.com/api/webhooks/1411624811293048904/khi_AT2dZ3TH2hIoUL4_7NUjQqYQBgSTYqNm8LB1a91g8OOJV0qQWpDxXiZQn1qIM3CD";

            EnableJailUI = true;
            JailUI_ID = 22004;
            EnableJailWebhook = true;
            JailWebhookUrl = "https://discord.com/api/webhooks/1397993426405822506/8V1U5wL7V8A_susP3Rc2ETVJNeQOZmmdR7mLmpDD9Zuv5SYSx64VJcW7ZajC8X_OYbHw";
        }
    }
}