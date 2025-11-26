using nexusUT;
using Rocket.API;
using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using System.Collections.Generic;
using UnityEngine;

namespace nexusUT
{
    public class FriskCommand : IRocketCommand
    {
        private readonly PoliceUT plugin;

        public FriskCommand(PoliceUT mainPlugin)
        {
            plugin = mainPlugin;
        }

        public AllowedCaller AllowedCaller => AllowedCaller.Player;
        public string Name => "frisk";
        public string Help => "Frisk a player by name or by looking at them.";
        public string Syntax => "/frisk [player]";
        public List<string> Aliases => new();
        public List<string> Permissions => new() { "UPI.Frisk" };

        public void Execute(IRocketPlayer caller, string[] command)
        {
            var officer = (UnturnedPlayer)caller;
            UnturnedPlayer target = null;

            if (command.Length > 0)
            {
                target = UnturnedPlayer.FromName(command[0]);
            }
            else
            {
                target = GetPlayerFromRaycast(officer, plugin.Configuration.Instance.MaxFriskDistance);
                if (target == null)
                {
                    Messaging.Say(officer, plugin.Translate("frisk_not_looking"), Color.white);
                    return;
                }
            }

            if (target == null)
            {
                Messaging.Say(officer, plugin.Translate("player_not_found"), Color.white);
                return;
            }

            if (target.CSteamID == officer.CSteamID)
            {
                Messaging.Say(officer, plugin.Translate("cannot_target_self"), Color.white);
                return;
            }

            float maxDistance = plugin.Configuration.Instance.MaxFriskDistance;
            if (Vector3.Distance(officer.Position, target.Position) > maxDistance)
            {
                Messaging.Say(officer, plugin.Translate("frisk_too_far", target.CharacterName), Color.white);
                return;
            }

            if (plugin.Configuration.Instance.RequireLooking && !IsLookingAt(officer.Player, target.Player, maxDistance))
            {
                Messaging.Say(officer, plugin.Translate("frisk_not_looking"), Color.white);
                return;
            }

            if (!IsHandcuffed(target.Player))
            {
                Messaging.Say(officer, plugin.Translate("frisk_not_cuffed"), Color.white);
                return;
            }

            plugin.StartCoroutine(plugin.DropItemsCoroutine(target));

            Messaging.Say(officer, plugin.Translate("frisk_success_officer", target.CharacterName), Color.white);
            Messaging.Say(target, plugin.Translate("frisk_notification_target", officer.CharacterName), Color.white);
        }


        private UnturnedPlayer GetPlayerFromRaycast(UnturnedPlayer observer, float maxDistance)
        {
            Transform observerTransform = observer.Player.look.aim;

            if (Physics.Raycast(observerTransform.position, observerTransform.forward, out RaycastHit hit, maxDistance, RayMasks.PLAYER_INTERACT))
            {
                Player hitPlayer = hit.transform.GetComponentInParent<Player>();
                if (hitPlayer != null)
                {
                    return UnturnedPlayer.FromPlayer(hitPlayer);
                }
            }
            return null;
        }

        private bool IsHandcuffed(Player player)
        {
            return player.animator.captorID != CSteamID.Nil;
        }

        private bool IsLookingAt(Player observer, Player target, float maxDistance)
        {
            Transform observerTransform = observer.look.aim;
            if (Physics.Raycast(observerTransform.position, observerTransform.forward, out RaycastHit hit, maxDistance, RayMasks.PLAYER_INTERACT))
            {
                Player hitPlayer = hit.transform.GetComponentInParent<Player>();
                if (hitPlayer != null && hitPlayer == target)
                {
                    return true;
                }
            }
            return false;
        }
    }
}