using Robust.Client.Graphics;

namespace Content.Client.Stylesheets;

/// <summary>
/// Hands out a separate shader instance for each draw within a frame, reusing the same instances on later frames.
/// </summary>
/// <remarks>
/// Clyde reads shader parameters when it flushes a batch rather than when the draw is queued, so a style box that is
/// drawn several times in one frame needs a distinct instance per draw. Creating a new instance per draw works, but
/// allocates a finalizable object and a parameter dictionary every time, which shows up as GC hitches.
/// </remarks>
public sealed class FrameShaderInstancePool<TParams> where TParams : struct, IEquatable<TParams>
{
    private readonly ShaderPrototype _prototype;
    private readonly List<(ShaderInstance Shader, TParams Params)> _slots = new();
    private uint _frame;
    private int _next;

    public FrameShaderInstancePool(ShaderPrototype prototype)
    {
        _prototype = prototype;
    }

    /// <summary>
    /// Gets the instance for the next draw in <paramref name="frame"/>.
    /// </summary>
    /// <returns>True if the instance's parameters need to be set, false if it already holds <paramref name="parameters"/>.</returns>
    public bool Next(uint frame, in TParams parameters, out ShaderInstance shader)
    {
        if (frame != _frame)
        {
            _frame = frame;
            _next = 0;
        }

        if (_next == _slots.Count)
        {
            shader = _prototype.InstanceUnique();
            _slots.Add((shader, parameters));
            _next++;
            return true;
        }

        var slot = _slots[_next];
        shader = slot.Shader;
        var changed = !slot.Params.Equals(parameters);
        if (changed)
            _slots[_next] = (shader, parameters);

        _next++;
        return changed;
    }
}
