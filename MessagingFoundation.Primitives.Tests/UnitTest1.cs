namespace MessagingFoundation.Primitives.Tests
{
    public class UnitTest1
    {
        [Fact]
        public void Test1()
        {
            Uri host = new Uri("http://localhost:1000");

            UriTemplate t = new UriTemplate("http://+:1000");

            var result = t.Match(host, new Uri("/", UriKind.Relative));

            Assert.NotNull(result);
        }
    }
}