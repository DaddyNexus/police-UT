using Rocket.API;
using Rocket.Unturned.Player;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using nexusUT;

namespace nexusUT
{
    public class ArrestLogCommand : IRocketCommand
    {
        private readonly PoliceUT plugin;
        public ArrestLogCommand(PoliceUT pluginInstance) { plugin = pluginInstance; }

        public AllowedCaller AllowedCaller => AllowedCaller.Player;
        public string Name => "alog";
        public string Help => "Logs a player arrest to Discord.";
        public string Syntax => "<PlayerName> <Reason> <FineAmount>";
        public List<string> Aliases => new List<string> { "alog" };
        public List<string> Permissions => new List<string> { "policeut.arrestlog" };

        public void Execute(IRocketPlayer caller, string[] command)
        {
            UnturnedPlayer issuer = (UnturnedPlayer)caller;
            if (command.Length < 3)
            {
                Messaging.Say(issuer, plugin.Translate("command_usage", Name, Syntax), Color.red);
                return;
            }

            UnturnedPlayer target = UnturnedPlayer.FromName(command[0]);
            if (target == null)
            {
                Messaging.Say(issuer, plugin.Translate("player_not_found"), Color.red);
                return;
            }

            string amountString = command[command.Length - 1];
            if (!uint.TryParse(amountString, out uint fineAmount))
            {
                Messaging.Say(issuer, "Invalid fine amount specified. It must be a number at the end of the command.", Color.red);
                return;
            }

            string reason = string.Join(" ", command.Skip(1).Take(command.Length - 2));
            if (string.IsNullOrEmpty(reason))
            {
                Messaging.Say(issuer, "You must provide a reason.", Color.red);
                return;
            }
            if (plugin.Configuration.Instance.EnableArrestLogWebhook && !string.IsNullOrEmpty(plugin.Configuration.Instance.ArrestLogWebhookUrl))
            {
                DiscordHelper.SendArrestLog(issuer.CharacterName, target.CharacterName, target.CSteamID, reason, fineAmount, plugin.Configuration.Instance.ArrestLogWebhookUrl);
                Messaging.Say(issuer, $"Successfully logged arrest for {target.CharacterName}.", Color.white);
            }
            else
            {
                Messaging.Say(issuer, "The arrest log webhook is not enabled or configured.", Color.red);
            }
        }
    }
}