using JobPortalApi.Services.Helpers;
using Xunit;

namespace JobPortalApi.Tests;

public class PrivateCvStorageTests
{
    [Fact]
    public void ValidatePdf_RejectsWrongExtensionAndMagicBytes()
    {
        Assert.Throws<ArgumentException>(() =>
            PrivateCvStorage.ValidatePdf("resume.doc", 5, "%PDF-"u8.ToArray()));
        Assert.Throws<ArgumentException>(() =>
            PrivateCvStorage.ValidatePdf("resume.pdf", 5, "not-p"u8.ToArray()));
    }

    [Fact]
    public void Resolve_RejectsPathTraversalAndAcceptsOpaquePdfKey()
    {
        Assert.NotNull(PrivateCvStorage.Resolve("11111111-1111-1111-1111-111111111111.pdf"));
        Assert.Null(PrivateCvStorage.Resolve("..\\secret.pdf"));
        Assert.Null(PrivateCvStorage.Resolve("resume.pdf"));
    }
}
