namespace InertiaCoreTests;

public partial class Tests
{
    /// <summary>
    /// Tests if the generated HTML contains valid page data.
    /// </summary>
    [Test]
    public async Task TestHtml()
    {
        var html = await _factory.Html(new { Test = "Test" });

        Assert.That(html.ToString(),
            Is.EqualTo("<div id=\"app\" data-page=\"{&quot;test&quot;:&quot;Test&quot;}\"></div>"));
    }
}
