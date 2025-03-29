using MoonSharp.Interpreter;
using MoonSharp.Interpreter.Interop;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HatsPlusPlus; 

[MoonSharpUserData]
public record struct TeamId {
    [MoonSharpVisible(true)]
    public uint value;

    public static TeamId New(uint value) {
        return new TeamId {
            value = value
        };
    }
}

[MoonSharpUserData]
public record struct TeamGen {
    [MoonSharpVisible(true)]
    public uint value;

    public static TeamGen New(uint value) {
        return new TeamGen {
            value = value
        };
    }
}

[MoonSharpUserData]
public record struct TeamHandle {
    [MoonSharpVisible(true)]
    public TeamGen gen;
    [MoonSharpVisible(true)]
    public TeamId id;

    public static TeamHandle New(TeamGen gen, TeamId id) {
        return new TeamHandle {
            gen = gen,
            id = id,
        };
    }
}
//public record struct TeamHandle(TeamGeneration Gen, TeamId TeamId);
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
            recycledHandle.gen = TeamGen.New(recycledHandle.gen.value + 1);
            return recycledHandle;
        }

        length += 1;
        return TeamHandle.New(TeamGen.New(0), TeamId.New(length - 1));
    }

    public Option<TeamHandle> AddTeam() {
        if (length >= Slots.Length) {
            return None;
        }

        var newHanlde = NewTeamHandle();
        Slots[newHanlde.id.value] = Some(new TeamRecord(newHanlde));
        return newHanlde;
    }

    public void RemoveTeam(TeamHandle handle) {
        if (!IsHandleValid(handle)) {
            return;
        }

        recycledHandles.Enqueue(handle);
        Slots[handle.id.value] = None;
    }

    public bool IsHandleValid(TeamHandle handle) {
        return Slots[handle.id.value].Map((record) => record.Handle == handle).IfNone(false);
    }

    public Option<int> GetSlotId(TeamHandle handle) {
        if (!IsHandleValid(handle)) {
            return None;
        }

        return (int)handle.id.value;
    }
}
