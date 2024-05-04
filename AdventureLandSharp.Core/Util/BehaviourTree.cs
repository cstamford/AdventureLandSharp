namespace AdventureLandSharp.Utility;

// A node has only one requirement - it must implement the tick function which returns a status (success, running, or fail).
public interface INode {
    Status Tick();
}

public enum Status {
    Success,
    Running,
    Fail,
}

//
// Actions have no children. They should "do something". Think of them like a leaf in a tree.
// Users are expected to implement the majority of their logic by implementing custom actions which inherit from Node.
//

// Always returns success.
public sealed class Success : INode {
    public Status Tick() => Status.Success;
}

// Always returns failure.
public sealed class Fail : INode {
    public Status Tick() => Status.Fail;
}

// Executes the function and returns the result.
// This is a convenient way to implement an action without creating a custom action node.
public sealed class Do(Func<Status> fn) : INode {
    public Do(Action fn) : this(() => { fn(); return Status.Success; }) { }
    public Status Tick() => fn();
}

//
// Decorators have one child and may mutate the output of that child (or, indeed, omit updating the child as appropriate).
//

// Evaluates a predicate and (conditionally) executes their child.
// * If the predicate returns false, the condition returns fail.
// * If the predicate returns true, the condition returns the child's status.
public sealed class If(Func<bool> pred, Func<Status> fn) : INode {
    public If(Func<bool> pred, INode child) : this(pred, child.Tick) { }
    public If(Func<bool> pred, Action fn) : this(pred, () => { fn(); return Status.Success; }) { }
    public Status Tick() => pred() ? fn() : Status.Fail;
}

// Inverters execute their child and then invert their result.
// * If the child returns success, the inverter returns fail.
// * If the child returns running, the inverter returns running.
// * If the child returns fail, the inverter returns success.
public sealed class Inverter(Func<Status> fn) : INode {
    public Inverter(INode child) : this(child.Tick) { }

    public Status Tick() {
        Status ret = fn();

        if (ret == Status.Success) {
            return Status.Fail;
        }

        if (ret == Status.Fail) {
            return Status.Success;
        }

        return Status.Running;
    }
}

// AlwaysSucceeeds executes their child and returns success.
public sealed class AlwaysSucceeds(Func<Status> fn) : INode {
    public AlwaysSucceeds(INode child) : this(child.Tick) { }

    public Status Tick() {
        fn();
        return Status.Success;
    }
}


// AlwaysFails executes their child and returns success.
public sealed class AlwaysFails(Func<Status> fn) : INode {
    public AlwaysFails(INode child) : this(child.Tick) { }
    public Status Tick() {
        fn();
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
public sealed class Selector(params Func<Status>[] fns) : INode {
    public Selector(params INode[] children) : this([..children.Select<INode, Func<Status>>(x => x.Tick)]) { }

    public Status Tick() {
        foreach (Func<Status> fn in fns) {
            Status ret = fn();

            if (ret == Status.Fail) {
                continue;
            }

            return ret;
        }

        return Status.Fail;
    }
}

// Sequences tick their children from first to last.
// * If a child returns success, the sequence moves onto the next child.
// * If a child returns running or fail, the sequence returns the same.
public sealed class Sequence(params Func<Status>[] fns) : INode {
    public Sequence(params INode[] children) : this([..children.Select<INode, Func<Status>>(x => x.Tick)]) { }

    public Status Tick() {
        foreach (Func<Status> fn in fns) {
            Status ret = fn();

            if (ret == Status.Success) {
                continue;
            }

            return ret;
        }

        return Status.Success;
    }
}

// FixedSequence tick their children from first to last.
// * They ignore their children's returns.
// * They always return success.
public sealed class FixedSequence(params Func<Status>[] fns) : INode {
    public FixedSequence(params INode[] children) : this([..children.Select<INode, Func<Status>>(x => x.Tick)]) { }

    public Status Tick() {
        foreach (Func<Status> fn in fns) {
            fn();
        }

        return Status.Success;
    }
}
