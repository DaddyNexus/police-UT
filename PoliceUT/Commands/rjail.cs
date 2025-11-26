using nexusUT;
using Rocket.API;
using Rocket.Unturned.Player;
using UnityEngine;
using System.Collections.Generic;

namespace nexusUT
{
    public class RemoveJailCommand : IRocketCommand
    {
        private readonly PoliceUT plugin;
        public RemoveJailCommand(PoliceUT mainPlugin) { plugin = mainPlugin; }

        public AllowedCaller AllowedCaller => AllowedCaller.Player;
        public string Name => "rjail";
        public string Help => "Removes an existing jail cell.";
        public string Syntax => "/rjail <name>";
        public List<string> Aliases => new List<string> { "deljail" };
        public List<string> Permissions => new List<string> { "upi.removejail" };
        public void Execute(IRocketPlayer caller, string[] command)
        {
            var admin = (UnturnedPlayer)caller;
            if (command.Length < 1)
            {
                Messaging.Say(admin, plugin.Translate("command_usage", Name, Syntax), Color.white);
                return;
            }
            string jailName = command[0];
            if (plugin.RemoveJail(jailName))
            {
                Messaging.Say(admin, plugin.Translate("jail_cell_removed", jailName), Color.white);
            }
            else
            {
                Messaging.Say(admin, plugin.Translate("jail_cell_not_found", jailName), Color.white);
            }
        }
    }
}