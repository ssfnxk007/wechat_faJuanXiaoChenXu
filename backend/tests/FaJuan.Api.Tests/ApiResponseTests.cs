using FaJuan.Api.Contracts;

namespace FaJuan.Api.Tests;

public class ApiResponseTests
{
    [Fact]
    public void Ok_Should_Create_Success_Response()
    {
        var response = ApiResponse<string>.Ok("pong");

        Assert.True(response.Success);
        Assert.Equal(200, response.Code);
        Assert.Equal("pong", response.Data);
    }
}
