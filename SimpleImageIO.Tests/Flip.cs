using Xunit;

namespace SimpleImageIO.Tests;

public class Flip {
    [Fact]
    public void FlipCreation_ShouldSucceed() {
        FlipBook.New.Add("test", new RgbImage(64, 64), FlipBook.DataType.RGB_HALF).ToString();
        string h = FlipBook.Header;
    }
}