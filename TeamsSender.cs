using Coroutines;
using DuckGame;
using LanguageExt.UnsafeValueAccess;
using Microsoft.Xna.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace HatsPlusPlus;

public struct ProfileId {
    public string value;

    public static ProfileId New(string value) {
        return new ProfileId {
            value = value
        };
    }
}

public struct SenderTeamData {
    public TeamHandle handle;
    public Dictionary<ProfileId, bool> loadedForProfiles;

    public static SenderTeamData New(TeamHandle handle) {
        return new SenderTeamData {
            handle = handle,
            loadedForProfiles = []
        };
    }
}

enum ProfileChangeKind {
    Removed,
    Added
}

struct TeamNetMessage {
    public TeamHandle handle;
    public Option<Profile> receiver;

    public static TeamNetMessage New(TeamHandle handle, Option<Profile> receiver) {
        return new TeamNetMessage {
            handle = handle,
            receiver = receiver,
        };
    }
}

public enum SendingState {
    Sending,
    FinishedSending,
    NotSending,
}

public static class TeamsSender {
    static CoroutineRunner coroutines;
    static Dictionary<TeamHandle, SenderTeamData> loadedTeams = [];
    static List<Profile> lastActiveProfiles = [];
    static List<TeamNetMessage> sendQueue = [];
    static SendingState sendingState;

    static IEnumerator SendTeamsCoroutine() {
        while (true) {
            Option<TeamData> teamDataOption = None;
            Option<TeamNetMessage> messageOption = None;
            while (teamDataOption.IsNone) {
                if (sendQueue.Count == 0) {
                    if (sendingState == SendingState.Sending) {

                        sendingState = SendingState.FinishedSending;
                    }
                    break;
                }

                var nextMessage = sendQueue.RemoveAndGet(sendQueue.Count - 1);
                teamDataOption = TeamsStorage.GetTeamData(nextMessage.handle);
                messageOption = nextMessage;
            }

            if (teamDataOption.Value() is var teamData && teamDataOption.IsSome
                && messageOption.Value() is var message && messageOption.IsSome) {
                Send.Message(new NMSpecialHat(teamData.team, message.receiver.ValueOrUnsafe(null)));
            }
            yield return 0.05;
        }
    }

    public static void Init() {
        coroutines = new CoroutineRunner();
        coroutines.Run(SendTeamsCoroutine());
    }

    public static void AddTeam(TeamHandle handle) {
        loadedTeams.Add(handle, SenderTeamData.New(handle));
        if (Profiles.active.Count == 1 && Profiles.active[0] == DuckNetwork.localProfile) {
            return;
        }
        sendingState = SendingState.Sending;
        sendQueue.Add(TeamNetMessage.New(handle, None));
    }

    public static void RemoveTeam(TeamHandle handle) {
        loadedTeams.Remove(handle);
    }

    public static void Update(GameTime gameTime) {
        if (!DuckNetwork.active) {
            return;
        }
        
        if (sendingState == SendingState.FinishedSending) {
            sendingState = SendingState.NotSending;
        }

        var activeProfiles = Profiles.active;
        if (activeProfiles.Count != lastActiveProfiles.Count) {
            var (changeKind, profiles) = GetProfileChange(lastActiveProfiles, activeProfiles);
            foreach (var data in loadedTeams.Values) {
                foreach (var profile in profiles) {
                    if (profile == DuckNetwork.localProfile) {
                        continue;
                    }
                    switch (changeKind) {
                        case ProfileChangeKind.Added:
                            data.loadedForProfiles.Add(ProfileId.New(profile.id), false);
                            sendingState = SendingState.Sending;
                            sendQueue.Add(TeamNetMessage.New(data.handle, profile));
                            break;
                        case ProfileChangeKind.Removed:
                            data.loadedForProfiles.Remove(ProfileId.New(profile.id));
                            sendQueue.RemoveAll((message) =>
                                message.receiver
                                .Map((removed) => removed == profile).ValueOr(false));
                            break;
                    }
                }
            }
        }

        coroutines.Update((float)gameTime.ElapsedGameTime.TotalSeconds);
        //active returns new list every time
        lastActiveProfiles = [.. Profiles.active];
    }

    static (ProfileChangeKind, List<Profile>) GetProfileChange(List<Profile> oldProfiles, List<Profile> newProfiles) {
        if (newProfiles.Count < oldProfiles.Count) {
            var removedProfiles = oldProfiles.Except(newProfiles).ToList();
            return (ProfileChangeKind.Removed, removedProfiles);
        }

        var addedProfiles = newProfiles.Except(oldProfiles).ToList();
        return (ProfileChangeKind.Added, addedProfiles);
    }
}
