namespace AdventureLandSharp.Core.Util;

// A node has only one requirement - it must implement the tick function which returns a status (success, running, or fail).
public interface INode {
    Task<Status> Tick();
}

public enum Status {
    Success,
    Running,
    Fail
}

//
// Actions have no children. They should "do something". Think of them like a leaf in a tree.
// Users are expected to implement the majority of their logic by implementing custom actions which inherit from Node.
//

// Always returns success.
public sealed class Success : INode {
    public Task<Status> Tick() => Task.FromResult(Status.Success);
}

// Always returns failure.
public sealed class Fail : INode {
    public Task<Status> Tick() => Task.FromResult(Status.Fail);
}

// Executes the function and returns the result.
// This is a convenient way to implement an action without creating a custom action node.
public sealed class Do(Func<Task<Status>> fn) : INode {
    public Do(Func<Task> fn) : this(async () => {
        await fn();
        return Status.Success;
    }) { }

    public Task<Status> Tick() => fn();
}

//
// Decorators have one child and may mutate the output of that child (or, indeed, omit updating the child as appropriate).
//

// Evaluates a predicate and (conditionally) executes their child.
// * If the predicate returns false, the condition returns fail.
// * If the predicate returns true, the condition returns the child's status.
public sealed class If(Func<bool> pred, Func<Task<Status>> fn) : INode {
    public If(Func<bool> pred, INode child) : this(pred, child.Tick) { }

    public If(Func<bool> pred, Func<Task> fn) : this(pred, async () => {
        await fn();
        return Status.Success;
    }) { }

    public async Task<Status> Tick() => pred() ? await fn() : Status.Fail;
}

// Evaluates a predicate and (conditionally) executes their child, otherwise executes their other child.
// * If the predicate returns false, the condition returns the other child's status.
// * If the predicate returns true, the condition returns the child's status.
public sealed class ElseIf(Func<bool> pred, Func<Task<Status>> fn, Func<Task<Status>> otherFn) : INode {
    public ElseIf(Func<bool> pred, INode child, INode otherChild) : this(pred, child.Tick, otherChild.Tick) { }

    public ElseIf(Func<bool> pred, Func<Task> fn, Func<Task> otherFn) : this(pred, async () => {
        await fn();
        return Status.Success;
    }, async () => {
        await otherFn();
        return Status.Success;
    }) { }

    public async Task<Status> Tick() => pred() ? await fn() : await otherFn();
}

// Inverters execute their child and then invert their result.
// * If the child returns success, the inverter returns fail.
// * If the child returns running, the inverter returns running.
// * If the child returns fail, the inverter returns success.
public sealed class Inverter(Func<Task<Status>> fn) : INode {
    public Inverter(INode child) : this(child.Tick) { }

    public async Task<Status> Tick() {
        Status ret = await fn();

        return ret switch {
            Status.Success => Status.Fail,
            Status.Fail => Status.Success,
            _ => Status.Running
        };
    }
}

// AlwaysSucceeeds executes their child and returns success.
public sealed class AlwaysSucceeds(Func<Task<Status>> fn) : INode {
    public AlwaysSucceeds(INode child) : this(child.Tick) { }

    public async Task<Status> Tick() {
        await fn();
        return Status.Success;
    }
}

// AlwaysFails executes their child and returns success.
public sealed class AlwaysFails(Func<Task<Status>> fn) : INode {
    public AlwaysFails(INode child) : this(child.Tick) { }

    public async Task<Status> Tick() {
        await fn();
        return Status.Fail;
    }
}

//
// Composites may have multiple children. They are a holder of nodes which determine in which order their children are visited and processed.
//

// Selectors tick their children from first to last.
// * If a child returns success or running, the selector returns the same.
// * If a child returns fail, the selector moves onto the next child.
// * If all children return fail, the selector returns fail.
public sealed class Selector(params Func<Task<Status>>[] fns) : INode {
    public Selector(params INode[] children) : this([..children.Select<INode, Func<Task<Status>>>(x => x.Tick)]) { }

    public async Task<Status> Tick() {
        foreach (Func<Task<Status>> fn in fns) {
            Status ret = await fn();

            if (ret == Status.Fail) continue;

            return ret;
        }

        return Status.Fail;
    }
}

// Sequences tick their children from first to last.
// * If a child returns success, the sequence moves onto the next child.
// * If a child returns running or fail, the sequence returns the same.
public sealed class Sequence(params Func<Task<Status>>[] fns) : INode {
    public Sequence(params INode[] children) : this([..children.Select<INode, Func<Task<Status>>>(x => x.Tick)]) { }

    public async Task<Status> Tick() {
        foreach (Func<Task<Status>> fn in fns) {
            Status ret = await fn();

            if (ret == Status.Success) continue;

            return ret;
        }

        return Status.Success;
    }
}

// FixedSequence tick their children from first to last.
// * They ignore their children's returns.
// * They always return success.
public sealed class FixedSequence(params Func<Task<Status>>[] fns) : INode {
    public FixedSequence(params INode[] children) : this([..children.Select<INode, Func<Task<Status>>>(x => x.Tick)]) { }

    public async Task<Status> Tick() {
        foreach (Func<Task<Status>> fn in fns) {
            await fn();
        }

        return Status.Success;
    }
}
