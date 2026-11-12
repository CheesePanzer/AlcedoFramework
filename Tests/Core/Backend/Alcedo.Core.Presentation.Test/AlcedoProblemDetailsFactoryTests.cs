using Alcedo.Core.Abstractions;
using Microsoft.AspNetCore.Http;

namespace Alcedo.Core.Presentation.Test;

public class TestNotFoundException : AlcedoNotFoundException
{
    public TestNotFoundException(string message, string? friendly = null, string? clientAction = null)
        : base(message, friendly ?? message)
    {
        ClientAction = clientAction;
    }
}

public class TestUnauthorizedException : AlcedoUnauthorizedException
{
    public TestUnauthorizedException(string message) : base(message)
    {
    }
}

public class TestForbiddenException : AlcedoForbiddenException
{
    public TestForbiddenException(string message) : base(message)
    {
    }
}

public class TestBadRequestException : AlcedoBadRequestException
{
    public TestBadRequestException(string message) : base(message)
    {
    }
}

public class TestConflictException : AlcedoConflictException
{
    public TestConflictException(string message) : base(message)
    {
    }
}

public class TestTooManyRequestsException : AlcedoTooManyRequestsException
{
    public TestTooManyRequestsException(string message) : base(message)
    {
    }
}

public class TestInternalException : AlcedoInternalException
{
    public TestInternalException(string message, string friendly) : base(message, friendly)
    {
    }
}

public class AlcedoProblemDetailsFactoryTests
{
    private static DefaultHttpContext CreateContext(string path = "/test")
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Path = path;
        ctx.TraceIdentifier = "trace-123";
        return ctx;
    }

    [Fact]
    public void Create_NotFoundException_Returns404()
    {
        var ctx = CreateContext();
        var pd = AlcedoProblemDetailsFactory.Create(ctx, new TestNotFoundException("msg"));
        Assert.Equal(404, pd.Status);
        Assert.Equal("Resource Not Found", pd.Title);
    }

    [Fact]
    public void Create_UnauthorizedException_Returns401()
    {
        var ctx = CreateContext();
        var pd = AlcedoProblemDetailsFactory.Create(ctx, new TestUnauthorizedException("msg"));
        Assert.Equal(401, pd.Status);
        Assert.Equal("Unauthorized", pd.Title);
    }

    [Fact]
    public void Create_ForbiddenException_Returns403()
    {
        var ctx = CreateContext();
        var pd = AlcedoProblemDetailsFactory.Create(ctx, new TestForbiddenException("msg"));
        Assert.Equal(403, pd.Status);
        Assert.Equal("Forbidden", pd.Title);
    }

    [Fact]
    public void Create_BadRequestException_Returns400()
    {
        var ctx = CreateContext();
        var pd = AlcedoProblemDetailsFactory.Create(ctx, new TestBadRequestException("msg"));
        Assert.Equal(400, pd.Status);
        Assert.Equal("Bad Request", pd.Title);
    }

    [Fact]
    public void Create_ConflictException_Returns409()
    {
        var ctx = CreateContext();
        var pd = AlcedoProblemDetailsFactory.Create(ctx, new TestConflictException("msg"));
        Assert.Equal(409, pd.Status);
        Assert.Equal("Conflict", pd.Title);
    }

    [Fact]
    public void Create_TooManyRequestsException_Returns429()
    {
        var ctx = CreateContext();
        var pd = AlcedoProblemDetailsFactory.Create(ctx, new TestTooManyRequestsException("msg"));
        Assert.Equal(429, pd.Status);
        Assert.Equal("Too Many Requests", pd.Title);
    }

    [Fact]
    public void Create_GenericAlcedoException_Returns500BusinessError()
    {
        var ctx = CreateContext();
        var ex = new TestInternalException("internal msg", "friendly msg");

        var pd = AlcedoProblemDetailsFactory.Create(ctx, ex);

        Assert.Equal(500, pd.Status);
        Assert.Equal("Business Error", pd.Title);
    }

    [Fact]
    public void Create_UnknownException_Returns500InternalServerError()
    {
        var ctx = CreateContext();
        var ex = new InvalidOperationException("boom");

        var pd = AlcedoProblemDetailsFactory.Create(ctx, ex);

        Assert.Equal(500, pd.Status);
        Assert.Equal("Internal Server Error", pd.Title);
    }

    [Fact]
    public void Create_AlcedoException_UsesFriendlyMessageAsDetail()
    {
        var ctx = CreateContext();
        var ex = new TestInternalException("log msg", "user friendly");

        var pd = AlcedoProblemDetailsFactory.Create(ctx, ex);

        Assert.Equal("user friendly", pd.Detail);
    }

    [Fact]
    public void Create_NonAlcedoException_DetailIsUnhandledException()
    {
        var ctx = CreateContext();
        var ex = new InvalidOperationException("boom");

        var pd = AlcedoProblemDetailsFactory.Create(ctx, ex);

        Assert.Equal("Unhandled Exception", pd.Detail);
    }

    [Fact]
    public void Create_AlcedoException_IncludesInnerErrorCode()
    {
        var ctx = CreateContext();
        var ex = new TestNotFoundException("msg");

        var pd = AlcedoProblemDetailsFactory.Create(ctx, ex);

        Assert.True(pd.Extensions.ContainsKey("innerErrorCode"));
        Assert.Equal(ex.Code, pd.Extensions["innerErrorCode"]);
    }

    [Fact]
    public void Create_AlcedoException_WithClientAction_IncludesClientAction()
    {
        var ctx = CreateContext();
        var ex = new TestNotFoundException("msg", clientAction: "redirect:/login");

        var pd = AlcedoProblemDetailsFactory.Create(ctx, ex);

        Assert.True(pd.Extensions.ContainsKey("clientAction"));
        Assert.Equal("redirect:/login", pd.Extensions["clientAction"]);
    }

    [Fact]
    public void Create_AlcedoException_WithoutClientAction_DoesNotIncludeClientAction()
    {
        var ctx = CreateContext();
        var ex = new TestNotFoundException("msg");

        var pd = AlcedoProblemDetailsFactory.Create(ctx, ex);

        Assert.False(pd.Extensions.ContainsKey("clientAction"));
    }

    [Fact]
    public void Create_NonAlcedoException_DoesNotIncludeInnerErrorCode()
    {
        var ctx = CreateContext();
        var ex = new InvalidOperationException("boom");

        var pd = AlcedoProblemDetailsFactory.Create(ctx, ex);

        Assert.False(pd.Extensions.ContainsKey("innerErrorCode"));
    }

    [Fact]
    public void Create_UsesOriginalRequestPath_WhenProvided()
    {
        var ctx = CreateContext("/from-context");
        var ex = new TestBadRequestException("msg");

        var pd = AlcedoProblemDetailsFactory.Create(ctx, ex, "/original-path");

        Assert.Equal("/original-path", pd.Instance);
    }

    [Fact]
    public void Create_FallsBackToHttpContextPath_WhenOriginalNotProvided()
    {
        var ctx = CreateContext("/from-context");
        var ex = new TestBadRequestException("msg");

        var pd = AlcedoProblemDetailsFactory.Create(ctx, ex);

        Assert.Equal("/from-context", pd.Instance);
    }

    [Fact]
    public void Create_IncludesTraceIdentifier()
    {
        var ctx = CreateContext();
        var ex = new TestBadRequestException("msg");

        var pd = AlcedoProblemDetailsFactory.Create(ctx, ex);

        Assert.True(pd.Extensions.ContainsKey("traceId"));
        Assert.Equal("trace-123", pd.Extensions["traceId"]);
    }

    [Fact]
    public void Create_DBConcurrencyException_Returns409Conflict()
    {
        var ctx = CreateContext();
        var ex = new System.Data.DBConcurrencyException("conflict");

        var pd = AlcedoProblemDetailsFactory.Create(ctx, ex);

        Assert.Equal(409, pd.Status);
        Assert.Equal("Conflict", pd.Title);
    }
}