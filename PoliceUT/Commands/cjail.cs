using nexusUT;
using Rocket.API;
using Rocket.Unturned.Player;
using UnityEngine;
using System.Collections.Generic;

namespace nexusUT
{
    public class SetJailCommand : IRocketCommand
    {
        private readonly PoliceUT plugin;
        public SetJailCommand(PoliceUT mainPlugin) { plugin = mainPlugin; }

        public AllowedCaller AllowedCaller => AllowedCaller.Player;
        public string Name => "setjail";
        public string Help => "Creates a new jail cell with an optional radius.";
        public string Syntax => "/setjail <name> [radius]";
        public List<string> Aliases => new List<string> { "cjail" };
        public List<string> Permissions => new List<string> { "upi.setjail" };

        public void Execute(IRocketPlayer caller, string[] command)
        {
            var admin = (UnturnedPlayer)caller;
            if (command.Length < 1)
            {
                Messaging.Say(admin, plugin.Translate("command_usage", Syntax), Color.red);
                return;
            }

            string jailName = command[0];
            Vector3 position = admin.Position;
            float radius = 0f;

            if (command.Length > 1)
            {
                if (!float.TryParse(command[1], out radius) || radius < 0)
                {
                    Messaging.Say(admin, plugin.Translate("setjail_invalid_radius"), Color.white);
                    return;
                }
            }

            plugin.AddJail(jailName, position, radius);
            Messaging.Say(admin, plugin.Translate("jail_cell_created", jailName, radius), Color.white);
        }
    }
}