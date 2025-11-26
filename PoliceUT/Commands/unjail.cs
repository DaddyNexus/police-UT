using nexusUT;
using Rocket.API;
using Rocket.Unturned.Player;
using UnityEngine;
using System.Collections.Generic;

namespace nexusUT
{
    public class UnjailCommand : IRocketCommand
    {
        private readonly PoliceUT plugin;
        public UnjailCommand(PoliceUT mainPlugin) { plugin = mainPlugin; }

        public AllowedCaller AllowedCaller => AllowedCaller.Player;
        public string Name => "unjail";
        public string Help => "Manually releases a player from jail.";
        public string Syntax => "/unjail <player>";
        public List<string> Aliases => new List<string>();
        public List<string> Permissions => new List<string> { "upi.unjail" };
        public void Execute(IRocketPlayer caller, string[] command)
        {
            var admin = (UnturnedPlayer)caller;
            if (command.Length < 1) { Messaging.Say(admin, plugin.Translate("command_usage", Name, Syntax), Color.white); return; }
            var target = UnturnedPlayer.FromName(command[0]);
            if (target == null) { Messaging.Say(admin, plugin.Translate("player_not_found"), Color.white); return; }
            plugin.UnjailPlayer(admin, target);
        }
    }
}