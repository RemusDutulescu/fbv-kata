namespace FunBooksAndVideos.Domain.Tests;

public sealed class ItemLineTests
{
    [Fact]
    public void Books_are_physical_and_videos_are_not()
    {
        Assert.True(new BookLine("The Girl on the train").IsPhysical);
        Assert.False(new VideoLine("Comprehensive First Aid Training").IsPhysical);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void A_product_needs_a_title(string title) =>
        Assert.Throws<DomainException>(() => new BookLine(title));

    [Fact]
    public void A_product_title_has_a_maximum_length() =>
        Assert.Throws<DomainException>(() => new VideoLine(new string('x', ProductLine.MaxTitleLength + 1)));

    [Theory]
    [InlineData(MembershipType.None)]
    [InlineData((MembershipType)4)]
    public void A_membership_line_needs_a_real_membership(MembershipType membership) =>
        Assert.Throws<DomainException>(() => new MembershipLine(membership));

    [Fact]
    public void Lines_compare_by_kind_and_value()
    {
        Assert.Equal(new BookLine("Dune"), new BookLine("Dune"));
        Assert.NotEqual<ItemLine>(new BookLine("Dune"), new VideoLine("Dune"));
    }
}
