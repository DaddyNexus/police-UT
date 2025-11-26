using Rocket.API;
using Rocket.Core.Plugins;
using Rocket.Unturned.Player;
using SDG.Unturned;
using System;
using System.Collections;
using System.Collections.Generic;
using Steamworks;
using UnityEngine;
using Logger = Rocket.Core.Logging.Logger;
using Rocket.API.Collections;
using Rocket.Unturned.Events;
using Newtonsoft.Json;
using System.IO;
using System.Linq;
using Rocket.Unturned;
using ShimmyMySherbet.DiscordWebhooks;
using nexusUT;

namespace nexusUT
{

    public static class Messaging
    {
        public static void Say(UnturnedPlayer player, string message, Color color)
        {
            if (player == null) return;
            ChatManager.serverSendMessage(message, color, null, player.SteamPlayer(), EChatMode.SAY, PoliceUT.Instance.Configuration.Instance.IconImageUrl, true);
        }
    }

    public partial class PoliceUT : RocketPlugin<PoliceUTConfiguration>
    {
        public static PoliceUT Instance { get; private set; }

        public Dictionary<CSteamID, JailedPlayerData> JailedPlayers { get; private set; }
        public List<JailCell> Jails { get; private set; }
        private string jailFilePath;
        private const short JAIL_UI_KEY = 9876;

        public List<PersistentJailInfo> PersistentlyJailedPlayers { get; private set; }
        private string persistentJailFilePath;

        private readonly Dictionary<ulong, ulong> dragLinks = new Dictionary<ulong, ulong>();
        private readonly Dictionary<ulong, DragState> draggedPlayerStates = new Dictionary<ulong, DragState>();
        private readonly Dictionary<ulong, DateTime> lastFailedDragAttempt = new Dictionary<ulong, DateTime>();

        protected override void Load()
        {
            Instance = this;
            InitializeDataStores();
            SubscribeEvents();
            RegisterCommands();
            StartCoroutine(CheckJailRadiusCoroutine());
            Logger.Log("-------[PoliceUT Loaded]--------");
        }

        protected override void Unload()
        {
            UnsubscribeEvents();
            StopAllCoroutines();
            Instance = null;
            Logger.Log("PoliceUT Unloaded!");
        }

        private void InitializeDataStores()
        {
            Jails = new List<JailCell>();
            JailedPlayers = new Dictionary<CSteamID, JailedPlayerData>();
            PersistentlyJailedPlayers = new List<PersistentJailInfo>();

            persistentJailFilePath = Path.Combine(Directory, "persistent_jails.json");
            jailFilePath = Path.Combine(Directory, "jails.json");

            LoadPersistentJails();
            LoadJails();
        }

        private void SubscribeEvents()
        {
            DamageTool.damagePlayerRequested += OnDamageRequested;
            PlayerAnimator.OnGestureChanged_Global += OnPlayerGesture_Drag;
            VehicleManager.onExitVehicleRequested += HandlePlayerExitVehicle_Drag;
            U.Events.OnPlayerDisconnected += OnPlayerDisconnected;
            U.Events.OnPlayerConnected += OnPlayerConnected;
        }

        private void UnsubscribeEvents()
        {
            DamageTool.damagePlayerRequested -= OnDamageRequested;
            PlayerAnimator.OnGestureChanged_Global -= OnPlayerGesture_Drag;
            VehicleManager.onExitVehicleRequested -= HandlePlayerExitVehicle_Drag;
            U.Events.OnPlayerDisconnected -= OnPlayerDisconnected;
            U.Events.OnPlayerConnected -= OnPlayerConnected;
        }

        private void RegisterCommands()
        {
            Rocket.Core.R.Commands.Register(new FriskCommand(this));
            Rocket.Core.R.Commands.Register(new JailCommand(this));
            Rocket.Core.R.Commands.Register(new UnjailCommand(this));
            Rocket.Core.R.Commands.Register(new PayBailCommand(this));
            Rocket.Core.R.Commands.Register(new FineCommand(this));
            Rocket.Core.R.Commands.Register(new ArrestLogCommand(this));
        }

        private void FixedUpdate()
        {
            if (dragLinks.Count == 0) return;
            var currentDraggers = new List<ulong>(dragLinks.Keys);
            foreach (var draggerId in currentDraggers)
            {
                if (!dragLinks.TryGetValue(draggerId, out ulong draggedId)) continue;
                UnturnedPlayer dragger = UnturnedPlayer.FromCSteamID((CSteamID)draggerId);
                UnturnedPlayer dragged = UnturnedPlayer.FromCSteamID((CSteamID)draggedId);
                if (dragger == null || dragged == null)
                {
                    if (dragger != null) StopDragging(dragger);
                    continue;
                }
                if (!draggedPlayerStates.TryGetValue(draggedId, out DragState state)) continue;
                if (state.IsInVehicle) continue;
                if (Vector3.Distance(dragger.Position, dragged.Position) > Configuration.Instance.MaxDragDistance)
                {
                    Messaging.Say(dragger, Translate("drag_too_far_dragger", dragged.DisplayName), Color.white);
                    Messaging.Say(dragged, Translate("drag_too_far_dragged", dragger.DisplayName), Color.white);
                    StopDragging(dragger);
                    continue;
                }
                Vector3 positionBehind = dragger.Position - (dragger.Player.transform.forward * 1.5f);
                dragged.Player.teleportToLocation(positionBehind, dragger.Rotation);
            }
        }





    }
}