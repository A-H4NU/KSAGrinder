using CommunityToolkit.Diagnostics;

namespace dsgen;

public sealed class MultipleTaskHandler
{
    private Action? _initializeAction = null;
    private Action? _cleanUpAction = null;

    private List<ActionWrapper> _tasks = new();

    public int TaskCount => _tasks.Count;

    public MultipleTaskHandler() { }

    /// <summary>
    /// Set the initialization action.
    /// It is called at the beginning, before any actions are invoked.
    /// </summary>
    public MultipleTaskHandler SetInitializeAction(Action initializeAction)
    {
        _initializeAction = initializeAction;
        return this;
    }

    /// <summary>
    /// Set the cleanup action.
    /// It is called at the final step---when all actions are completed, for instance.
    /// The action is always invoked at the end even if an exception is thrown.
    /// </summary>
    public MultipleTaskHandler SetCleanUpAction(Action cleanUpAction)
    {
        _cleanUpAction = cleanUpAction;
        return this;
    }

    /// <summary>
    /// Add an action to be invoked by either <see cref="InvokeActions"/>
    /// or <see cref="InvokeActionsAsync(CancellationToken)"/>.
    /// </summary>
    public MultipleTaskHandler AddAction(Action action)
    {
        _tasks.Add(new(action));
        return this;
    }

    /// <summary>
    /// Add an indexed action to be invoked by either <see cref="InvokeActions"/>
    /// or <see cref="InvokeActionsAsync(CancellationToken)"/>.
    /// </summary>
    /// <param name="indexedAction">
    /// The integer parameter represents the index of the action.
    /// <c>i</c>-th (zero-based) action will receive <c>i</c> as a parameter.
    /// </param>
    public MultipleTaskHandler AddIndexedAction(Action<int> indexedAction)
    {
        _tasks.Add(new(indexedAction));
        return this;
    }

    /// <summary>
    /// Add actions to be invoked by either <see cref="InvokeActions"/>
    /// or <see cref="InvokeActionsAsync(CancellationToken)"/>.
    /// </summary>
    /// <param name="actions">The list of <see cref="Action"/> to invoke.</param>
    public MultipleTaskHandler AddActions(IEnumerable<Action> actions)
    {
        _tasks.AddRange(actions.Select(Wrap));
        return this;
    }

    /// <summary>
    /// Add an action to be invoked by either <see cref="InvokeActions"/>
    /// or <see cref="InvokeActionsAsync(CancellationToken)"/> multiple times.
    /// </summary>
    /// <param name="count">
    /// The number of times to register the action.
    /// </param>
    public MultipleTaskHandler AddActions(Action action, int count)
    {
        Guard.IsGreaterThanOrEqualTo(count, 0);
        ActionWrapper wrapper = new(action);
        for (int i = 0; i < count; i++)
            _tasks.Add(wrapper);
        return this;
    }

    /// <summary>
    /// Add indexed actions to be invoked by either <see cref="InvokeActions"/>
    /// or <see cref="InvokeActionsAsync(CancellationToken)"/>.
    /// </summary>
    /// <param name="indexedActions">
    /// The list of <see cref="Action{int}"/>s.
    /// The integer parameter represents the index of the action.
    /// <c>i</c>-th (zero-based) action will receive <c>i</c> as a parameter.
    /// </param>
    public MultipleTaskHandler AddIndexedActions(IEnumerable<Action<int>> indexedActions)
    {
        _tasks.AddRange(indexedActions.Select(Wrap));
        return this;
    }

    /// <summary>
    /// Add an indexed action to be invoked by either <see cref="InvokeActions"/>
    /// or <see cref="InvokeActionsAsync(CancellationToken)"/> multiple times.
    /// </summary>
    /// <param name="indexedAction">
    /// The integer parameter represents the index of the action.
    /// <c>i</c>-th (zero-based) action will receive <c>i</c> as a parameter.
    /// </param>
    /// <param name="count">
    /// The number of times to register the action.
    /// </param>
    public MultipleTaskHandler AddIndexedActions(Action<int> indexedAction, int count)
    {
        Guard.IsGreaterThanOrEqualTo(count, 0);
        ActionWrapper wrapper = new(indexedAction);
        for (int i = 0; i < count; i++)
            _tasks.Add(wrapper);
        return this;
    }

    /// <summary>
    /// Invoke the registered actions one by one synchronously.
    /// </summary>
    public void InvokeActions()
    {
        try
        {
            _initializeAction?.Invoke();
            for (int i = 0; i < _tasks.Count; i++)
            {
                _tasks[i].Invoke(i);
            }
        }
        finally
        {
            _cleanUpAction?.Invoke();
        }
    }

    /// <summary>
    /// Invoke the registered actions asynchronously.
    /// </summary>
    public async Task InvokeActionsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (cancellationToken.IsCancellationRequested)
                return;
            _initializeAction?.Invoke();
            if (cancellationToken.IsCancellationRequested)
                return;
            Task?[] awaitings = new Task?[_tasks.Count];
            for (int i = 0; !cancellationToken.IsCancellationRequested && i < _tasks.Count; i++)
            {
                awaitings[i] = _tasks[i].InvokeAsync(i, cancellationToken);
            }
            await Task.WhenAll(awaitings.TakeWhile(t => t is not null).ToArray()!);

            if (cancellationToken.IsCancellationRequested)
                return;
        }
        finally
        {
            _cleanUpAction?.Invoke();
        }
    }

    private static ActionWrapper Wrap(Action task)
    {
        return new(task);
    }

    private static ActionWrapper Wrap(Action<int> task)
    {
        return new(task);
    }

    private sealed class ActionWrapper
    {
        private readonly Action? _nonIndexedTask;
        private readonly Action<int>? _indexedTask;

        public ActionWrapper(Action nonIndexedTask)
        {
            _nonIndexedTask = nonIndexedTask;
            _indexedTask = null;
        }

        public ActionWrapper(Action<int> indexedTask)
        {
            _nonIndexedTask = null;
            _indexedTask = indexedTask;
        }

        public void Invoke(int index)
        {
            if (_nonIndexedTask is not null)
            {
                _nonIndexedTask();
            }
            else
            {
                _indexedTask!(index);
            }
        }

        public async Task InvokeAsync(int index, CancellationToken cancellationToken = default)
        {
            await Task.Run(() => Invoke(index), cancellationToken);
        }
    }
}
