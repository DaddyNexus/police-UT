using Rocket.API;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using SDG.Unturned;
using System.Collections.Generic;
using UnityEngine;

namespace nexusUT
{
    public class CommandCuff : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Player;
        public string Name => "cuff";
        public string Help => "Cuffs a player.";
        public string Syntax => "<player>";
        public List<string> Aliases => new List<string>();
        public List<string> Permissions => new List<string> { "UPI.cuff" };

        public void Execute(IRocketPlayer caller, string[] command)
        {
            if (command.Length != 1)
            {
                UnturnedChat.Say(caller, $"Correct usage: /cuff {Syntax}", Color.red);
                return;
            }

            var player = (UnturnedPlayer)caller;
            var targetPlayer = UnturnedPlayer.FromName(command[0]);

            if (targetPlayer == null)
            {
                UnturnedChat.Say(player, "Player not found.", Color.red);
                return;
            }

            targetPlayer.Player.animator.sendGesture(EPlayerGesture.ARREST_START, true);

            UnturnedChat.Say(player, $"You have cuffed {targetPlayer.CharacterName}.", Color.white);
            UnturnedChat.Say(targetPlayer, "You have been cuffed.", Color.white);
        }
    }
}