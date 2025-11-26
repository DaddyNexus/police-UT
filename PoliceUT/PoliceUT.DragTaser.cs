using Rocket.API;
using Rocket.Unturned;
using Rocket.Unturned.Player;
using Rocket.API.Extensions;
using Rocket.Unturned.Extensions;
using System;
using SDG.Unturned;
using Steamworks;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace nexusUT
{
    public partial class PoliceUT
    {
        #region Drag System
        private void HandlePlayerExitVehicle_Drag(Player player, InteractableVehicle vehicle, ref bool shouldAllow, ref Vector3 pendingLocation, ref float pendingYaw)
        {
            ulong exitingPlayerId = player.channel.owner.playerID.steamID.m_SteamID;
            if (dragLinks.ContainsValue(exitingPlayerId))
            {
                shouldAllow = false;
                Messaging.Say(UnturnedPlayer.FromPlayer(player), Translate("cannot_exit_vehicle_dragged"), Color.white);
                return;
            }
            shouldAllow = true;
        }

        private void OnPlayerDisconnected_Drag(UnturnedPlayer player)
        {
            if (player == null) return;
            ulong playerId = player.CSteamID.m_SteamID;
            if (dragLinks.ContainsKey(playerId)) StopDragging(player);

            var pair = dragLinks.FirstOrDefault(kvp => kvp.Value == playerId);
            if (pair.Key != 0)
            {
                UnturnedPlayer dragger = UnturnedPlayer.FromCSteamID((CSteamID)pair.Key);
                if (dragger != null) StopDragging(dragger);
            }
        }

        private void OnPlayerGesture_Drag(PlayerAnimator animator, EPlayerGesture gesture)
        {
            if (gesture != EPlayerGesture.SURRENDER_START) return;
            var dragger = UnturnedPlayer.FromPlayer(animator.player);

            if (!HasPermission(dragger, "PoliceUT.Drag"))
            {
                return;
            }

            if (dragLinks.TryGetValue(dragger.CSteamID.m_SteamID, out ulong draggedId))
            {
                var dragged = UnturnedPlayer.FromCSteamID((CSteamID)draggedId);
                if (dragged == null) { StopDragging(dragger); return; }
                if (!draggedPlayerStates.TryGetValue(draggedId, out DragState state)) { StopDragging(dragger); return; }

                if (state.IsInVehicle) { PullFromVehicle(dragger, dragged); return; }
                if (RaycastForVehicle(dragger, out InteractableVehicle vehicle)) { PushToVehicle(dragger, dragged, vehicle); return; }

                StopDragging(dragger);
            }
            else
            {
                if (RaycastForPlayer(dragger, out UnturnedPlayer target))
                {
                    if (IsHandcuffed(target.Player)) StartDragging(dragger, target);                }
                else
                {
                    ulong draggerId = dragger.CSteamID.m_SteamID;
                    if (lastFailedDragAttempt.TryGetValue(draggerId, out DateTime lastAttempt) && (DateTime.UtcNow - lastAttempt).TotalSeconds < 2f) return;
                    lastFailedDragAttempt[draggerId] = DateTime.UtcNow;
                }
            }
        }

        private void StartDragging(UnturnedPlayer dragger, UnturnedPlayer target)
        {
            if (dragLinks.ContainsValue(target.CSteamID.m_SteamID))
            {
                Messaging.Say(dragger, Translate("target_already_dragged"), Color.white);
                return;
            }
            dragLinks[dragger.CSteamID.m_SteamID] = target.CSteamID.m_SteamID;
            draggedPlayerStates[target.CSteamID.m_SteamID] = new DragState();
            Messaging.Say(dragger, Translate("start_dragging_dragger", target.DisplayName), Color.white);
            Messaging.Say(target, Translate("start_dragging_dragged", dragger.DisplayName), Color.white);
        }

        private void StopDragging(UnturnedPlayer dragger)
        {
            if (dragger == null) return;
            if (dragLinks.TryGetValue(dragger.CSteamID.m_SteamID, out ulong draggedId))
            {
                var dragged = UnturnedPlayer.FromCSteamID((CSteamID)draggedId);
                dragLinks.Remove(dragger.CSteamID.m_SteamID);
                draggedPlayerStates.Remove(draggedId);
                if (dragged != null)
                {
                    Messaging.Say(dragger, Translate("stop_dragging_dragger", dragged.DisplayName), Color.white);
                    Messaging.Say(dragged, Translate("stop_dragging_dragged", dragger.DisplayName), Color.white);
                }
            }
        }

        private void PushToVehicle(UnturnedPlayer dragger, UnturnedPlayer dragged, InteractableVehicle vehicle)
        {
            if (vehicle.isLocked && vehicle.lockedOwner != dragger.CSteamID)
            {
                Messaging.Say(dragger, Translate("vehicle_locked"), Color.white);
                return;
            }
            if (VehicleManager.ServerForcePassengerIntoVehicle(dragged.Player, vehicle))
            {
                var state = draggedPlayerStates[dragged.CSteamID.m_SteamID];
                state.IsInVehicle = true;
                Messaging.Say(dragger, Translate("pushed_to_vehicle", dragged.DisplayName), Color.white);
            }
            else
            {
                Messaging.Say(dragger, Translate("vehicle_full"), Color.white);
            }
        }

        private void PullFromVehicle(UnturnedPlayer dragger, UnturnedPlayer dragged)
        {
            var vehicle = dragged.Player.movement.getVehicle();
            if (vehicle != null)
            {
                VehicleManager.forceRemovePlayer(vehicle, dragged.CSteamID);
                Vector3 positionBehind = dragger.Position - (dragger.Player.transform.forward * 2f);
                dragged.Player.teleportToLocation(positionBehind, dragger.Rotation);
                var state = draggedPlayerStates[dragged.CSteamID.m_SteamID];
                state.IsInVehicle = false;
                Messaging.Say(dragger, Translate("pulled_from_vehicle", dragged.DisplayName), Color.white);
            }
        }

        private bool IsHandcuffed(Player player) => player.animator.captorID != CSteamID.Nil;

        private bool RaycastForPlayer(UnturnedPlayer source, out UnturnedPlayer target)
        {
            Ray ray = new Ray(source.Player.look.aim.position, source.Player.look.aim.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, 4f, RayMasks.PLAYER))
            {
                if (hit.transform.TryGetComponent<Player>(out Player p) && p.channel.owner.playerID.steamID != source.CSteamID)
                {
                    target = UnturnedPlayer.FromPlayer(p);
                    return true;
                }
            }
            target = null;
            return false;
        }

        private bool RaycastForVehicle(UnturnedPlayer source, out InteractableVehicle vehicle)
        {
            Ray ray = new Ray(source.Player.look.aim.position, source.Player.look.aim.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, 6f, RayMasks.VEHICLE))
            {
                if (hit.transform.TryGetComponent(out vehicle)) return true;
            }
            vehicle = null;
            return false;
        }

        private bool HasPermission(IRocketPlayer player, string permission)
        {
            return player != null && player.HasPermission(permission);
        }

        #endregion

        #region Other Plugin Logic
        public IEnumerator DropItemsCoroutine(UnturnedPlayer target)
        {
            if (target?.Player == null) yield break;
            CSteamID originalCaptorID = target.Player.animator.captorID;
            if (originalCaptorID == CSteamID.Nil) yield break;
            target.Player.animator.captorID = CSteamID.Nil;
            yield return new WaitForFixedUpdate();
            PlayerInventory inventory = target.Player.inventory;
            PlayerEquipment equipment = target.Player.equipment;
            for (byte slot = 0; slot <= 1; slot++)
            {
                ItemJar equippedItem = inventory.getItem(slot, 0);
                if (equippedItem != null && equippedItem.item != null)
                {
                    ItemManager.dropItem(equippedItem.item, target.Position, true, true, true);
                    inventory.removeItem(slot, 0);
                }
            }
            equipment.dequip();
            List<byte> pagesToClear = new List<byte> { 2, 3, 4, 5, 6 };
            foreach (byte page in pagesToClear)
            {
                int count = inventory.getItemCount(page);
                for (int i = count - 1; i >= 0; i--)
                {
                    var jar = inventory.getItem(page, (byte)i);
                    if (jar?.item != null)
                    {
                        ItemManager.dropItem(jar.item, target.Position, true, true, true);
                        inventory.removeItem(page, (byte)i);
                    }
                }
            }
        }

        private void OnDamageRequested(ref DamagePlayerParameters parameters, ref bool shouldAllow)
        {
            UnturnedPlayer attacker = UnturnedPlayer.FromCSteamID(parameters.killer);
            if (attacker == null)
            {
                return;
            }

            UnturnedPlayer victim = UnturnedPlayer.FromPlayer(parameters.player);
            if (victim == null || attacker == victim)
            {
                return;
            }

            if (attacker.Player.equipment.asset?.id == Configuration.Instance.TaserId && HasPermission(attacker, "UPI.Taser"))
            {
                shouldAllow = false;
                Messaging.Say(victim, Translate("taser_victim"), Color.white);
                Messaging.Say(attacker, Translate("taser_attacker", victim.CharacterName), Color.white);
                StartCoroutine(TasePlayerCoroutine(victim));
            }
        }

        private IEnumerator TasePlayerCoroutine(UnturnedPlayer victim)
        {
            if (victim == null || victim.Player == null) yield break;
            victim.Player.movement.sendPluginSpeedMultiplier(0);
            victim.Player.movement.sendPluginJumpMultiplier(0);
            float taserEndTime = Time.time + Configuration.Instance.TaserTime;
            while (Time.time < taserEndTime)
            {
                if (victim == null || victim.Player == null || victim.Player.life.isDead) yield break;
                victim.Player.stance.stance = EPlayerStance.PRONE;
                victim.Player.equipment.dequip();
                victim.Player.animator.sendGesture(EPlayerGesture.SURRENDER_START, true);
                yield return null;
            }
            if (victim?.Player != null)
            {
                victim.Player.movement.sendPluginSpeedMultiplier(1);
                victim.Player.movement.sendPluginJumpMultiplier(1);
                victim.Player.animator.sendGesture(EPlayerGesture.NONE, true);
            }
        }

        #endregion
    }
}
