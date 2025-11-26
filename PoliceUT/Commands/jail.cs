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
        public string Help => "Jail management command. Use: /jail player, /jail add, /jail remove";
        public string Syntax => "player <PlayerName> <CellName> <TimeSeconds> <BailAmount> [Reason] | add <name> [radius] | remove <name>";
        public List<string> Aliases => new List<string>();
        public List<string> Permissions => new List<string> { "policeut.jail" };

        public void Execute(IRocketPlayer caller, string[] command)
        {
            UnturnedPlayer officer = (UnturnedPlayer)caller;

            if (command.Length == 0)
            {
                Messaging.Say(officer, $"Usage: /jail player <PlayerName> <CellName> <TimeSeconds> <BailAmount> [Reason]", Color.red);
                Messaging.Say(officer, $"       /jail add <name> [radius]", Color.red);
                Messaging.Say(officer, $"       /jail remove <name>", Color.red);
                return;
            }

            string subcommand = command[0].ToLower();

            switch (subcommand)
            {
                case "player":
                    HandleJailPlayer(officer, command.Skip(1).ToArray());
                    break;
                case "add":
                    HandleAddJail(officer, command.Skip(1).ToArray());
                    break;
                case "remove":
                    HandleRemoveJail(officer, command.Skip(1).ToArray());
                    break;
                default:
                    Messaging.Say(officer, $"Unknown subcommand '{subcommand}'. Use: player, add, or remove", Color.red);
                    break;
            }
        }

        private void HandleJailPlayer(UnturnedPlayer officer, string[] args)
        {
            if (args.Length < 4)
            {
                Messaging.Say(officer, "Usage: /jail player <PlayerName> <CellName> <TimeSeconds> <BailAmount> [Reason]", Color.red);
                return;
            }

            UnturnedPlayer target = UnturnedPlayer.FromName(args[0]);
            if (target == null)
            {
                Messaging.Say(officer, plugin.Translate("player_not_found"), Color.red);
                return;
            }

            string cellName = args[1];
            JailCell chosenCell = plugin.Jails.FirstOrDefault(c => c.Name.Equals(cellName, StringComparison.OrdinalIgnoreCase));
            if (chosenCell == null)
            {
                Messaging.Say(officer, $"Jail cell '{cellName}' not found. Use /jails to see a list.", Color.red);
                return;
            }

            if (!int.TryParse(args[2], out int seconds))
            {
                Messaging.Say(officer, plugin.Translate("jail_invalid_time"), Color.red);
                return;
            }

            if (!uint.TryParse(args[3], out uint bailAmount))
            {
                Messaging.Say(officer, "Invalid bail amount specified.", Color.red);
                return;
            }

            string reason = args.Length > 4 ? string.Join(" ", args.Skip(4)) : "No reason provided.";

            if (plugin.Jails.Count == 0)
            {
                Messaging.Say(officer, plugin.Translate("jail_no_cells"), Color.red);
                return;
            }

            plugin.JailPlayer(officer, target, chosenCell, seconds, bailAmount, reason);
        }

        private void HandleAddJail(UnturnedPlayer admin, string[] args)
        {
            if (args.Length < 1)
            {
                Messaging.Say(admin, "Usage: /jail add <name> [radius]", Color.red);
                return;
            }

            string jailName = args[0];
            Vector3 position = admin.Position;
            float radius = 0f;

            if (args.Length > 1)
            {
                if (!float.TryParse(args[1], out radius) || radius < 0)
                {
                    Messaging.Say(admin, plugin.Translate("setjail_invalid_radius"), Color.white);
                    return;
                }
            }

            plugin.AddJail(jailName, position, radius);
            Messaging.Say(admin, plugin.Translate("jail_cell_created", jailName, radius), Color.white);
        }

        private void HandleRemoveJail(UnturnedPlayer admin, string[] args)
        {
            if (args.Length < 1)
            {
                Messaging.Say(admin, "Usage: /jail remove <name>", Color.red);
                return;
            }

            string jailName = args[0];
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