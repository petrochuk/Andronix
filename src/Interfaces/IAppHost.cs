namespace Andronix.Interfaces;

public interface IAppHost
{
    IntPtr GetMainWindowHandle();

    string Title { get; }
}
