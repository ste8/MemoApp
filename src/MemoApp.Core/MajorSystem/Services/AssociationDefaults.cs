using MemoApp.Core.MajorSystem.Models;

namespace MemoApp.Core.MajorSystem.Services;

/// <summary>
/// The rule for choosing which association represents a token.
///
/// A pure function, kept in one place because it was previously written out at each call
/// site and drifted. <see cref="IAssociationComposerService.ResolveDefault"/> exposes it to
/// callers that already depend on the composer.
/// </summary>
public static class AssociationDefaults
{
    /// <summary>
    /// The association explicitly marked default, otherwise the first available.
    /// Null only when there are no associations.
    /// </summary>
    public static TokenAssociation? Resolve(IReadOnlyList<TokenAssociation>? associations)
    {
        if (associations is null || associations.Count == 0)
        {
            return null;
        }

        return associations.FirstOrDefault(a => a.IsDefault) ?? associations[0];
    }
}
