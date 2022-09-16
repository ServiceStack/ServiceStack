namespace ServiceStack.Blazor.Components.Tailwind;

/// <summary>
/// To capture Tailwind's animation rules, e.g:
/// {
///     entering: { cls:'ease-out duration-300', from:'opacity-0',    to:'opacity-100' },
///      leaving: { cls:'ease-in duration-200',  from:'opacity-100',  to: 'opacity-0'  }
/// }
/// {
///    entering: { cls:'ease-out duration-300', from:'opacity-0 translate-y-4 sm:translate-y-0 sm:scale-95', to:'opacity-100 translate-y-0 sm:scale-100' }, 
///     leaving: { cls:'ease-in duration-200',  from:'opacity-100 translate-y-0 sm:scale-100',               to:'opacity-0 translate-y-4 sm:translate-y-0 sm:scale-95' }
/// }
/// </summary>
public class DataTransition
{
    public DataTransitionEvent Entering { get; }
    public DataTransitionEvent Leaving { get; }
    public bool EnteringState { get; set; }
    public string Class { get; set; }
    public string DisplayClass => EnteringState ? "block" : "hidden";
    public string OpacityClass => EnteringState ? "opacity-100" : "opacity-0";

    bool transitioning = false;
    static void log(string? message = null) => BlazorUtils.Log(message);

    public int DelayMs { get; set; } = 60;

    public async Task TransitionAsync(bool show, Action? onChange)
    {
        if (!CanAcquireLock())
            return;

        log("TransitionAsync: " + show);

        for (var step = 0; step < TotalSteps; step++)
        {
            TransitionStep(show, onChange, step);
            log($"Step {step}. ({show}): {Class}");
            onChange?.Invoke();
            await Task.Delay(DelayMs);
        }

        log();
        ReleaseLock();
    }

    bool CanAcquireLock()
    {
        if (transitioning) return false; // ignore multiple calls during transition
        return transitioning = true;
    }
    void ReleaseLock() => transitioning = false;


    const int TotalSteps = 2;
    void TransitionStep(bool show, Action? onChange, int step)
    {
        //var prev = show
        //    ? Leaving
        //    : Entering;
        var next = show
            ? Entering
            : Leaving;

        switch (step)
        {
            case 0:
                Class = CssUtils.ClassNames(next.Class, next.From);
                break;
            case 1:
                Class = CssUtils.ClassNames(next.Class, next.To);
                break;
        }
    }

    static void Each(DataTransition[] transitions, Action<DataTransition> fn)
    {
        foreach (var transition in transitions)
            fn(transition);
    }


    public static async Task TransitionAllAsync(bool show, Action? onChange, params DataTransition[] transitions)
    {
        if (transitions.Length == 0)
            return;

        if (!transitions.All(x => x.EnteringState != show))
            return;
        if (!transitions.All(x => x.CanAcquireLock()))
            return;

        log("TransitionAllAsync: " + show);

        if (show)
            Each(transitions, x => x.Show(show));
        else
            Array.Reverse(transitions);

        for (var step = 0; step < TotalSteps; step++)
        {
            foreach (var transition in transitions)
            {
                transition.TransitionStep(show, onChange, step);
                log($"Step {step}. ({show}): {transition.Class}");
            }
            onChange?.Invoke();
            var delayMs = transitions.Select(x => x.DelayMs).OrderByDescending(x => x).First();
            await Task.Delay(delayMs);
        }

        if (!show)
            Each(transitions, x => x.Show(show));

        log();
        Each(transitions, x => x.ReleaseLock());
    }

    public DataTransition(DataTransitionEvent entering, DataTransitionEvent leaving, bool visible = false)
    {
        Entering = entering;
        Leaving = leaving;
        Show(visible);
    }


    public void Show(bool visible)
    {
        EnteringState = visible;
    }
}

public class DataTransitionEvent
{
    public string Class { get; }
    public string From { get; }
    public string To { get; }
    public DataTransitionEvent(string @class, string from, string to)
    {
        Class = @class;
        From = from;
        To = to;
    }
}