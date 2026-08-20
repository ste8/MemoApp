using MemoApp.Core.MajorSystem.Models;
using MemoApp.Core.MajorSystem.Services;

namespace MemoApp.Core.Tests.MajorSystem;

/// <summary>
/// AssociationDefaults.Resolve answers "which association represents this token?": the one
/// explicitly marked default, otherwise the first available.
/// </summary>
public class AssociationDefaultsTests
{
    private static TokenAssociation Association(string word, bool isDefault = false)
        => new() { Token = "07", Word = word, IsDefault = isDefault };

    [Fact]
    public void ResolveDefault_WithAnExplicitDefault_ReturnsIt()
    {
        var associations = new[] { Association("sack"), Association("sock", isDefault: true), Association("sick") };

        var resolved = ResolveDefault(associations);

        Assert.Equal("sock", resolved?.Word);
    }

    [Fact]
    public void ResolveDefault_WithNoExplicitDefault_ReturnsTheFirst()
    {
        var associations = new[] { Association("sack"), Association("sock") };

        var resolved = ResolveDefault(associations);

        Assert.Equal("sack", resolved?.Word);
    }

    [Fact]
    public void ResolveDefault_WithSeveralDefaults_ReturnsTheFirstOfThem()
    {
        var associations = new[] { Association("sack"), Association("sock", isDefault: true), Association("sick", isDefault: true) };

        Assert.Equal("sock", ResolveDefault(associations)?.Word);
    }

    [Fact]
    public void ResolveDefault_WithNoAssociations_ReturnsNull()
    {
        Assert.Null(ResolveDefault(Array.Empty<TokenAssociation>()));
    }

    [Fact]
    public void ResolveDefault_WithNull_ReturnsNull()
    {
        Assert.Null(ResolveDefault(null!));
    }

    private static TokenAssociation? ResolveDefault(IReadOnlyList<TokenAssociation> associations)
        => AssociationDefaults.Resolve(associations);
}
