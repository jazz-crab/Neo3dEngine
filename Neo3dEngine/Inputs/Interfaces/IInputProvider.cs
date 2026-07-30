namespace Neo3dEngine.Inputs;

internal interface IInputProvider : IDisposable
{
    void Update();
    bool IsGetKey(ConsoleKey key);
    bool IsGetKey(int virtualKey);
    bool IsShift { get; }
    bool IsCtrl  { get; }
    bool IsAlt { get; }
}