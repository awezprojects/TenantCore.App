using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;
using TenantCore.Infrastructure.Services;

namespace TenantCore.Infrastructure.Tests.Services;

public class CurrentUserContextTests
{
    [Fact]
    public void UserId_AuthenticatedUserWithNameIdClaim_ReturnsParsedGuid()
    {
        var userId = Guid.NewGuid();
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim("nameid", userId.ToString())], authenticationType: "TestAuth"));

        var httpContext = new Mock<HttpContext>();
        httpContext.SetupGet(h => h.User).Returns(principal);

        var accessor = new Mock<IHttpContextAccessor>();
        accessor.SetupGet(a => a.HttpContext).Returns(httpContext.Object);

        var context = new CurrentUserContext(accessor.Object);

        context.UserId.Should().Be(userId);
    }

    [Fact]
    public void UserId_NoHttpContext_ReturnsNull()
    {
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.SetupGet(a => a.HttpContext).Returns((HttpContext?)null);

        var context = new CurrentUserContext(accessor.Object);

        context.UserId.Should().BeNull();
    }

    [Fact]
    public void UserId_UnauthenticatedUserWithNoClaims_ReturnsNull()
    {
        var httpContext = new Mock<HttpContext>();
        httpContext.SetupGet(h => h.User).Returns(new ClaimsPrincipal(new ClaimsIdentity()));

        var accessor = new Mock<IHttpContextAccessor>();
        accessor.SetupGet(a => a.HttpContext).Returns(httpContext.Object);

        var context = new CurrentUserContext(accessor.Object);

        context.UserId.Should().BeNull();
    }
}
