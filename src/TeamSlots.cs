using MoonSharp.Interpreter;
using MoonSharp.Interpreter.Interop;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HatsPlusPlus; 

[MoonSharpUserData]
internal record struct TeamId {
    [MoonSharpVisible(true)]
    public uint value;

    public static TeamId New(uint value) {
        return new TeamId {
            value = value
        };
    }
}

[MoonSharpUserData]
internal record struct TeamGen {
    [MoonSharpVisible(true)]
    public uint value;

    public static TeamGen New(uint value) {
        return new TeamGen {
            value = value
        };
    }
}

[MoonSharpUserData]
internal record struct TeamHandle {
    [MoonSharpVisible(true)]
    public TeamGen gen;
    [MoonSharpVisible(true)]
    public TeamId id;

    internal static TeamHandle New(TeamGen gen, TeamId id) {
        return new TeamHandle {
            gen = gen,
            id = id,
        };
    }
}
//internal record struct TeamHandle(TeamGeneration Gen, TeamId TeamId);
internal record struct TeamRecord(TeamHandle Handle);

internal class TeamSlots {
    Option<TeamRecord>[] Slots;
    Stack<TeamHandle> recycledHandles;
    internal uint Length { get; private set; }

    internal static TeamSlots New() {
        return new TeamSlots {
            Slots = new Option<TeamRecord>[1000],
            recycledHandles = new Stack<TeamHandle>(),
        };
    }

    internal void Clear() {
        recycledHandles.Clear();
        Length = 0;
    }

    TeamHandle NewTeamHandle() {
        if (recycledHandles.Count > 0) {
            var recycledHandle = recycledHandles.Pop();
            recycledHandle.gen = TeamGen.New(recycledHandle.gen.value + 1);
            return recycledHandle;
        }

        Length += 1;
        return TeamHandle.New(TeamGen.New(0), TeamId.New(Length - 1));
    }

    internal Option<TeamHandle> AddTeam() {
        if (Length >= Slots.Length) {
            return None;
        }

        var newHanlde = NewTeamHandle();
        Slots[newHanlde.id.value] = Some(new TeamRecord(newHanlde));
        return newHanlde;
    }

    internal void RemoveTeam(TeamHandle handle) {
        if (!IsHandleValid(handle)) {
            return;
        }

        recycledHandles.Push(handle);
        Slots[handle.id.value] = None;
    }

    internal bool IsHandleValid(TeamHandle handle) {
        return Slots[handle.id.value].Map((record) => record.Handle == handle).IfNone(false);
    }

    internal Option<int> GetSlotId(TeamHandle handle) {
        if (!IsHandleValid(handle)) {
            return None;
        }

        return (int)handle.id.value;
    }
}
