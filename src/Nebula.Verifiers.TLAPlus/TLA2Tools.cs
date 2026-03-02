using tla2sany.drivers;

namespace Nebula.Verifiers.TLAPlus;

public class TLA2Tools: Runtime
{
    public void SANYmain(params string[] args) => tla2sany.drivers.SANY.SANYmain(args);

}
