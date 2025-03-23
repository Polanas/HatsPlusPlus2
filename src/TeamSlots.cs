using System;
using System.Collections.Generic;
using System.Linq;

namespace HatsPlusPlus; 

public record struct TeamId(uint Id);
public record struct TeamGeneration(uint Value);
public record struct TeamHandle(TeamGeneration Gen, TeamId TeamId);
public record struct TeamRecord(TeamHandle Handle);

public class TeamSlots {
    Option<TeamRecord>[] Slots;
    Queue<TeamHandle> recycledHandles;
    uint length;

    public static TeamSlots New() {
        return new TeamSlots {
            Slots = new Option<TeamRecord>[1000],
            recycledHandles = new Queue<TeamHandle>(),
        };
    }

    TeamHandle NewTeamHandle() {
        if (recycledHandles.Count > 0) {
            var recycledHandle = recycledHandles.Dequeue();
            recycledHandle.Gen = new TeamGeneration(recycledHandle.Gen.Value + 1);
            return recycledHandle;
        }

        this.length += 1;
        return new TeamHandle(new TeamGeneration(0), new TeamId(this.length - 1));
    }

    public Option<TeamHandle> AddTeam() {
        if (length >= Slots.Length) {
            return None;
        }

        var newHanlde = NewTeamHandle();
        Slots[newHanlde.TeamId.Id] = Some(new TeamRecord(newHanlde));
        return newHanlde;
    }

    public void RemoveTeam(TeamHandle handle) {
        if (!IsHandleValid(handle)) {
            return;
        }

        recycledHandles.Enqueue(handle);
        Slots[handle.TeamId.Id] = None;
    }

    public bool IsHandleValid(TeamHandle handle) {
        return Slots[handle.TeamId.Id].Map((record) => record.Handle == handle).IfNone(false);
    }

    public Option<int> GetSlotId(TeamHandle handle) {
        if (!IsHandleValid(handle)) {
            return None;
        }

        return (int)handle.TeamId.Id;
    }
}
