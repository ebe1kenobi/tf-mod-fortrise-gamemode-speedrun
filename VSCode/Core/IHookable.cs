using FortRise;

namespace TFModFortRiseSpeedRun;

public interface IHookable
{
    abstract static void Load(IHarmony harmony);
}

public interface IRegisterable
{
    abstract static void Register(IModContent content, IModRegistry registry);
}
