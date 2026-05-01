using Microsoft.AspNetCore.Http;
using Moq;
using NUnit.Framework;

namespace Gateway.Tests;

[TestFixture]
public class RequestLoggingMiddlewareTests
{
    private Mock<RequestDelegate> _nextMock;
    private RequestLoggingMiddleware _middleware;

    [SetUp]
    public void SetUp()
    {
        _nextMock = new Mock<RequestDelegate>();
        _middleware = new RequestLoggingMiddleware(_nextMock.Object);
    }

    [Test]
    public async Task Invoke_ShouldCallNextDelegate()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Method = "GET";
        context.Request.Path = "/test";
        context.Response.StatusCode = 200;

        // Act
        await _middleware.Invoke(context);

        // Assert
        _nextMock.Verify(next => next(context), Times.Once);
    }

    [Test]
    public async Task Invoke_ShouldHandleDifferentStatusCodes()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.Path = "/api/resource";
        context.Response.StatusCode = 404;

        // Act
        await _middleware.Invoke(context);

        // Assert
        _nextMock.Verify(next => next(context), Times.Once);
        Assert.That(context.Response.StatusCode, Is.EqualTo(404));
    }
}
