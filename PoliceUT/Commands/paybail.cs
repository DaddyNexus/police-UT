using Rocket.API;
using Rocket.Unturned.Player;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using nexusUT;

namespace nexusUT
{
    public class PayBailCommand : IRocketCommand
    {
        private readonly PoliceUT plugin;
        public PayBailCommand(PoliceUT pluginInstance) { plugin = pluginInstance; }

        public AllowedCaller AllowedCaller => AllowedCaller.Player;
        public string Name => "paybail";
        public string Help => "Pays a player's bail to release them from jail.";
        public string Syntax => "<JailedPlayerName>";
        public List<string> Aliases => new List<string>();
        public List<string> Permissions => new List<string> { "policeut.paybail" };

        public void Execute(IRocketPlayer caller, string[] command)
        {
            UnturnedPlayer bailer = (UnturnedPlayer)caller;
            if (command.Length != 1)
            {
                Messaging.Say(bailer, plugin.Translate("command_usage", Name, Syntax), Color.red);
                return;
            }

            UnturnedPlayer target = UnturnedPlayer.FromName(command[0]);
            if (target == null)
            {
                Messaging.Say(bailer, plugin.Translate("player_not_found"), Color.red);
                return;
            }

            if (!plugin.JailedPlayers.TryGetValue(target.CSteamID, out JailedPlayerData data))
            {
                Messaging.Say(bailer, plugin.Translate("bail_not_jailed", target.CharacterName), Color.red);
                return;
            }

            if (data.BailAmount <= 0)
            {
                Messaging.Say(bailer, plugin.Translate("bail_no_bail_set", target.CharacterName), Color.red);
                return;
            }

            if (bailer.Experience < data.BailAmount)
            {
                Messaging.Say(bailer, plugin.Translate("bail_insufficient_funds", data.BailAmount, bailer.Experience), Color.red);
                return;
            }

            bailer.Experience -= data.BailAmount;
            plugin.UnjailPlayer(bailer, target, true);
        }
    }
}