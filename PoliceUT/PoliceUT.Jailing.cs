using Rocket.API;
using Rocket.Unturned;
using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Logger = Rocket.Core.Logging.Logger;
using Newtonsoft.Json;

namespace nexusUT
{
    public partial class PoliceUT
    {
        #region Jailing System
        public void JailPlayer(UnturnedPlayer officer, UnturnedPlayer target, JailCell chosenCell, int seconds, uint bailAmount, string reason)
        {
            if (JailedPlayers.ContainsKey(target.CSteamID))
            {
                if (officer != null) Messaging.Say(officer, Translate("jail_already_jailed", target.CharacterName), Color.red);
                return;
            }
            target.Teleport(chosenCell.Position, target.Rotation);

            DateTime releaseTime = DateTime.UtcNow.AddSeconds(seconds);
            Coroutine releaseCoroutine = StartCoroutine(ReleasePlayerCoroutine(target.CSteamID, seconds));
            JailedPlayerData data = new JailedPlayerData { ReleaseCoroutine = releaseCoroutine, AssignedCell = chosenCell, ReleaseUtcTime = releaseTime, BailAmount = bailAmount };
            JailedPlayers.Add(target.CSteamID, data);

            PersistentlyJailedPlayers.RemoveAll(p => p.PlayerId == target.CSteamID.m_SteamID);
            PersistentlyJailedPlayers.Add(new PersistentJailInfo
            {
                PlayerId = target.CSteamID.m_SteamID,
                CellName = chosenCell.Name,
                SecondsRemaining = seconds,
                BailAmount = bailAmount,
                Reason = reason
            });
            SavePersistentJails();

            Messaging.Say(target, Translate("jail_success_target", reason, seconds, bailAmount), Color.white);
            if (officer != null)
            {
                Messaging.Say(officer, Translate("jail_success_officer", target.CharacterName, seconds, reason), Color.white);
            }

            if (Configuration.Instance.EnableJailUI)
            {
                TimeSpan initialTime = TimeSpan.FromSeconds(seconds);
                EffectManager.sendUIEffect(Configuration.Instance.JailUI_ID, JAIL_UI_KEY, target.CSteamID, true);
                EffectManager.sendUIEffectText(JAIL_UI_KEY, target.CSteamID, true, "ReasonText", $"Reason | {reason}");
                EffectManager.sendUIEffectText(JAIL_UI_KEY, target.CSteamID, true, "TimeText", $"Time left | {FormatTime(initialTime)}");
                EffectManager.sendUIEffectText(JAIL_UI_KEY, target.CSteamID, true, "BailText", $"Bail cost | ${bailAmount}");
            }

            if (officer != null && Configuration.Instance.EnableJailWebhook && !string.IsNullOrEmpty(Configuration.Instance.JailWebhookUrl) && !Configuration.Instance.JailWebhookUrl.Contains("PASTE"))
            {
                DiscordHelper.SendJailLog(officer.CharacterName, target.CharacterName, target.CSteamID, seconds, bailAmount, reason, Configuration.Instance.JailWebhookUrl);
            }
        }

        public void UnjailPlayer(UnturnedPlayer admin, UnturnedPlayer target, bool wasBailed = false)
        {
            JailedPlayerData data = null;
            bool isOnline = JailedPlayers.TryGetValue(target.CSteamID, out data);

            if (!isOnline && PersistentlyJailedPlayers.All(p => p.PlayerId != target.CSteamID.m_SteamID))
            {
                if (admin != null) Messaging.Say(admin, Translate("unjail_not_jailed", target.CharacterName), Color.white);
                return;
            }

            if (isOnline && data != null)
            {
                StopCoroutine(data.ReleaseCoroutine);
            }
            JailedPlayers.Remove(target.CSteamID);

            PersistentlyJailedPlayers.RemoveAll(p => p.PlayerId == target.CSteamID.m_SteamID);
            SavePersistentJails();

            Vector3 releasePos = new Vector3(Configuration.Instance.JailReleaseX, Configuration.Instance.JailReleaseY, Configuration.Instance.JailReleaseZ);
            target.Teleport(releasePos, target.Rotation);

            if (Configuration.Instance.EnableJailUI)
            {
                EffectManager.askEffectClearByID(Configuration.Instance.JailUI_ID, target.CSteamID);
            }

            if (admin != null)
            {
                string targetMessage = wasBailed ? Translate("bail_success_target", admin.CharacterName) : Translate("unjail_success_target", admin.CharacterName);
                string adminMessage = wasBailed ? Translate("bail_success_officer", target.CharacterName) : Translate("unjail_success_officer", target.CharacterName);
                Messaging.Say(target, targetMessage, Color.white);
                Messaging.Say(admin, adminMessage, Color.white);
            }
        }

        public bool RemoveJail(string name)
        {
            var jailToRemove = Jails.FirstOrDefault(j => j.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (jailToRemove != null)
            {
                Jails.Remove(jailToRemove);
                SaveJails();
                return true;
            }
            return false;
        }

        private IEnumerator ReleasePlayerCoroutine(CSteamID targetId, int seconds)
        {
            yield return new WaitForSeconds(seconds);
            var target = UnturnedPlayer.FromCSteamID(targetId);
            if (target != null && (JailedPlayers.ContainsKey(targetId) || PersistentlyJailedPlayers.Any(p => p.PlayerId == targetId.m_SteamID)))
            {
                Messaging.Say(target, Translate("jail_release_notification"), Color.white);
                UnjailPlayer(null, target);
            }
        }

        private IEnumerator CheckJailRadiusCoroutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(1f);
                if (JailedPlayers.Count == 0) continue;

                foreach (var entry in JailedPlayers.ToList())
                {
                    UnturnedPlayer player = UnturnedPlayer.FromCSteamID(entry.Key);
                    if (player == null)
                    {
                        JailedPlayers.Remove(entry.Key);
                        continue;
                    };

                    JailedPlayerData data = entry.Value;

                    if (Configuration.Instance.EnforceJailRadius && data.AssignedCell.Radius > 0)
                    {
                        if (Vector3.Distance(player.Position, data.AssignedCell.Position) > data.AssignedCell.Radius)
                        {
                            player.Teleport(data.AssignedCell.Position, player.Rotation);
                            Messaging.Say(player, Translate("jail_escape_attempt"), Color.red);
                        }
                    }

                    if (Configuration.Instance.EnableJailUI)
                    {
                        TimeSpan timeLeft = data.ReleaseUtcTime - DateTime.UtcNow;
                        if (timeLeft.TotalSeconds < 0) timeLeft = TimeSpan.Zero;
                        EffectManager.sendUIEffectText(JAIL_UI_KEY, player.CSteamID, true, "TimeText", $"{FormatTime(timeLeft)}");
                    }
                }
            }
        }

        private string FormatTime(TimeSpan timeSpan)
        {
            if (timeSpan.TotalHours >= 1) return $"{timeSpan.Hours:D2}h:{timeSpan.Minutes:D2}m:{timeSpan.Seconds:D2}s";
            if (timeSpan.TotalMinutes >= 1) return $"{timeSpan.Minutes:D2}m:{timeSpan.Seconds:D2}s";
            return $"{timeSpan.Seconds}s";
        }

        public void AddJail(string name, Vector3 position, float radius) { Jails.Add(new JailCell { Name = name, X = position.x, Y = position.y, Z = position.z, Radius = radius }); SaveJails(); }
        private void LoadJails() { if (File.Exists(jailFilePath)) { string json = File.ReadAllText(jailFilePath); Jails = JsonConvert.DeserializeObject<List<JailCell>>(json) ?? new List<JailCell>(); } else { SaveJails(); } }
        private void SaveJails() { string json = JsonConvert.SerializeObject(Jails, Formatting.Indented); File.WriteAllText(jailFilePath, json); }
        #endregion

        #region Persistence Methods
        private void OnPlayerConnected(UnturnedPlayer player)
        {
            var jailInfo = PersistentlyJailedPlayers.FirstOrDefault(p => p.PlayerId == player.CSteamID.m_SteamID);
            if (jailInfo != null)
            {
                if (jailInfo.SecondsRemaining > 0)
                {
                    var cell = Jails.FirstOrDefault(c => c.Name.Equals(jailInfo.CellName, StringComparison.OrdinalIgnoreCase));
                    if (cell != null)
                    {
                        Logger.Log($"{player.CharacterName} connected while jailed. Restoring sentence for {jailInfo.SecondsRemaining:F0} seconds.");
                        JailPlayer(null, player, cell, (int)jailInfo.SecondsRemaining, jailInfo.BailAmount, jailInfo.Reason);
                    }
                    else
                    {
                        Logger.LogWarning($"{player.CharacterName} was jailed in cell '{jailInfo.CellName}', but it no longer exists. Releasing them.");
                        PersistentlyJailedPlayers.Remove(jailInfo);
                        SavePersistentJails();
                    }
                }
                else
                {
                    Logger.Log($"{player.CharacterName}'s jail sentence expired. Releasing them.");
                    PersistentlyJailedPlayers.Remove(jailInfo);
                    SavePersistentJails();
                }
            }
        }

        private void OnPlayerDisconnected(UnturnedPlayer player)
        {
            OnPlayerDisconnected_Drag(player);

            if (JailedPlayers.TryGetValue(player.CSteamID, out JailedPlayerData data))
            {
                double secondsLeft = (data.ReleaseUtcTime - DateTime.UtcNow).TotalSeconds;
                if (secondsLeft < 0) secondsLeft = 0;

                var persistentInfo = PersistentlyJailedPlayers.FirstOrDefault(p => p.PlayerId == player.CSteamID.m_SteamID);
                if (persistentInfo != null)
                {
                    persistentInfo.SecondsRemaining = secondsLeft;
                    SavePersistentJails();
                    Logger.Log($"{player.CharacterName} disconnected while jailed. Saved {secondsLeft:F0} seconds remaining.");
                }
                JailedPlayers.Remove(player.CSteamID);
            }
        }

        private void LoadPersistentJails()
        {
            if (File.Exists(persistentJailFilePath))
            {
                string json = File.ReadAllText(persistentJailFilePath);
                PersistentlyJailedPlayers = JsonConvert.DeserializeObject<List<PersistentJailInfo>>(json) ?? new List<PersistentJailInfo>();
            }
        }

        private void SavePersistentJails()
        {
            string json = JsonConvert.SerializeObject(PersistentlyJailedPlayers, Formatting.Indented);
            File.WriteAllText(persistentJailFilePath, json);
        }
        #endregion
    }
}
