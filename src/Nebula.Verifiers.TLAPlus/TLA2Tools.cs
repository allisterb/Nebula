namespace Nebula.Verifiers.TLAPlus;

using tla2sany.drivers;

public class TLA2Tools: Runtime
{
    public static void SANY(params string[] args) => tla2sany.drivers.SANY.SANYmain(args);
}
