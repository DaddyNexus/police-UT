using Rocket.API;
using Rocket.Unturned.Player;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using nexusUT;

namespace nexusUT
{
    public class JailCommand : IRocketCommand
    {
        private readonly PoliceUT plugin;
        public JailCommand(PoliceUT pluginInstance) { plugin = pluginInstance; }

        public AllowedCaller AllowedCaller => AllowedCaller.Player;
        public string Name => "jail";
        public string Help => "Jails a player in a specific cell.";
        public string Syntax => "<PlayerName> <CellName> <TimeSeconds> <BailAmount> [Reason]";
        public List<string> Aliases => new List<string>();
        public List<string> Permissions => new List<string> { "policeut.jail" };

        public void Execute(IRocketPlayer caller, string[] command)
        {
            UnturnedPlayer officer = (UnturnedPlayer)caller;
            if (command.Length < 4)
            {
                Messaging.Say(officer, plugin.Translate("command_usage", Name, Syntax), Color.red);
                return;
            }

            UnturnedPlayer target = UnturnedPlayer.FromName(command[0]);
            if (target == null)
            {
                Messaging.Say(officer, plugin.Translate("player_not_found"), Color.red);
                return;
            }

            string cellName = command[1];
            JailCell chosenCell = plugin.Jails.FirstOrDefault(c => c.Name.Equals(cellName, StringComparison.OrdinalIgnoreCase));
            if (chosenCell == null)
            {
                Messaging.Say(officer, $"Jail cell '{cellName}' not found. Use /jails to see a list.", Color.red);
                return;
            }

            if (!int.TryParse(command[2], out int seconds))
            {
                Messaging.Say(officer, plugin.Translate("jail_invalid_time"), Color.red);
                return;
            }

            if (!uint.TryParse(command[3], out uint bailAmount))
            {
                Messaging.Say(officer, "Invalid bail amount specified.", Color.red);
                return;
            }

            string reason = command.Length > 4 ? string.Join(" ", command.Skip(4)) : "No reason provided.";

            if (plugin.Jails.Count == 0)
            {
                Messaging.Say(officer, plugin.Translate("jail_no_cells"), Color.red);
                return;
            }

            plugin.JailPlayer(officer, target, chosenCell, seconds, bailAmount, reason);
        }
    }
}