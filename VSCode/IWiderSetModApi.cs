using Microsoft.Xna.Framework;

namespace Teuria.WiderSet;

// Copie partielle de l'interface publiee par le mod WiderSet (Teuria.WiderSet).
// FortRise 5 : interop via GetApi<IWiderSetModApi> au lieu de MonoMod.ModInterop.
// On ne declare que ce dont on a besoin : la simple presence de l'API (non-null)
// signale que WiderSet est installe, ce qui remplace l'ancien EigthPlayerImport.
public partial interface IWiderSetModApi
{
    bool IsWide { get; set; }
}
