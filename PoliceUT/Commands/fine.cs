using Rocket.API;
using Rocket.Unturned.Player;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using nexusUT;

namespace nexusUT
{
    public class FineCommand : IRocketCommand
    {
        private readonly PoliceUT plugin;
        public FineCommand(PoliceUT pluginInstance) { plugin = pluginInstance; }

        public AllowedCaller AllowedCaller => AllowedCaller.Player;
        public string Name => "fine";
        public string Help => "Fines a player a specific amount of experience.";
        public string Syntax => "<PlayerName> <Amount> [Reason]";
        public List<string> Aliases => new List<string>();
        public List<string> Permissions => new List<string> { "policeut.fine" };

        public void Execute(IRocketPlayer caller, string[] command)
        {
            UnturnedPlayer officer = (UnturnedPlayer)caller;
            if (command.Length < 2)
            {
                Messaging.Say(officer, plugin.Translate("command_usage",Name, Syntax), Color.red);
                return;
            }

            UnturnedPlayer target = UnturnedPlayer.FromName(command[0]);
            if (target == null)
            {
                Messaging.Say(officer, plugin.Translate("player_not_found"), Color.red);
                return;
            }

            if (!uint.TryParse(command[1], out uint amount) || amount == 0)
            {
                Messaging.Say(officer, "Invalid amount specified.", Color.red);
                return;
            }

            if (target.Experience < amount)
            {
                Messaging.Say(officer, $"<color=#17A2B8>[CPolice]</color> {target.CharacterName} does not have enough moeny to pay that fine. (Required: ${amount}, Has: ${target.Experience})", Color.white);
                return;
            }

            string reason = command.Length > 2 ? string.Join(" ", command.Skip(2)) : "No reason provided.";

            target.Experience -= amount;

            Messaging.Say(officer, $"<color=#17A2B8>[CPolice]</color> You fined {target.CharacterName} ${amount} for: {reason}", Color.white);
            Messaging.Say(target, $"<color=#17A2B8>[CPolice]</color> You have been fined ${amount} by {officer.CharacterName} for: {reason}", Color.white);
            if (plugin.Configuration.Instance.EnableFineWebhook && !string.IsNullOrEmpty(plugin.Configuration.Instance.FineWebhookUrl))
            {
                DiscordHelper.SendFineLog(officer.CharacterName, target.CharacterName, target.CSteamID, amount, reason, plugin.Configuration.Instance.FineWebhookUrl);
            }
        }
    }
}