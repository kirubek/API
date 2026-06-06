using System.Reflection;
using BaseOps.API.Controllers;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BaseOps.SecurityTests;

public sealed class DehangaringAuthorizationTests
{
    [Fact]
    public void Dehangaring_controller_requires_authenticated_users()
    {
        typeof(DehangaringController)
            .GetCustomAttributes<AuthorizeAttribute>(inherit: true)
            .Should()
            .ContainSingle();
    }

    [Fact]
    public void Dehangaring_list_uses_typed_non_compatibility_route()
    {
        var routes = typeof(DehangaringController)
            .GetMethod(nameof(DehangaringController.List))!
            .GetCustomAttributes<HttpGetAttribute>(inherit: true)
            .Select(attribute => attribute.Template)
            .ToArray();

        routes.Should().ContainSingle("api/dehangaring-reports/list");
        routes.Should().NotContain(route => route != null && route.Contains("compatibility", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Dehangaring_detail_uses_typed_non_compatibility_route()
    {
        var routes = typeof(DehangaringController)
            .GetMethod(nameof(DehangaringController.GetById))!
            .GetCustomAttributes<HttpGetAttribute>(inherit: true)
            .Select(attribute => attribute.Template)
            .ToArray();

        routes.Should().ContainSingle("api/dehangaring-reports/{id:guid}");
        routes.Should().NotContain(route => route != null && route.Contains("compatibility", StringComparison.OrdinalIgnoreCase));
    }
}
