using BaseOps.API.Controllers;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BaseOps.SecurityTests;

public sealed class CompatibilityReadScopingTests
{
    [Fact]
    public void Compatibility_controller_requires_authenticated_users()
    {
        typeof(OperationalCompatibilityController)
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .Should()
            .ContainSingle();
    }

    [Fact]
    public void Carry_over_read_route_attributes_exist()
    {
        var controller = typeof(OperationalCompatibilityController);
        var methods = controller.GetMethods();

        var hasCarryOverDashboard = methods.Any(m => 
            m.GetCustomAttributes(typeof(HttpGetAttribute), inherit: true)
                .OfType<HttpGetAttribute>()
                .Any(attr => attr.Template?.Contains("carryover/dashboard", StringComparison.OrdinalIgnoreCase) == true));

        var hasCarryOverAnalytics = methods.Any(m => 
            m.GetCustomAttributes(typeof(HttpGetAttribute), inherit: true)
                .OfType<HttpGetAttribute>()
                .Any(attr => attr.Template?.Contains("carryover/analytics", StringComparison.OrdinalIgnoreCase) == true));

        hasCarryOverDashboard.Should().BeTrue("carryover/dashboard route should exist");
        hasCarryOverAnalytics.Should().BeTrue("carryover/analytics route should exist");
    }

    [Fact]
    public void Aums_read_route_attributes_exist()
    {
        var controller = typeof(OperationalCompatibilityController);
        var methods = controller.GetMethods();

        var hasAumsDashboard = methods.Any(m => 
            m.GetCustomAttributes(typeof(HttpGetAttribute), inherit: true)
                .OfType<HttpGetAttribute>()
                .Any(attr => attr.Template?.Contains("aums/dashboard", StringComparison.OrdinalIgnoreCase) == true));

        var hasAumsProjects = methods.Any(m => 
            m.GetCustomAttributes(typeof(HttpGetAttribute), inherit: true)
                .OfType<HttpGetAttribute>()
                .Any(attr => attr.Template?.Contains("aums/projects", StringComparison.OrdinalIgnoreCase) == true));

        hasAumsDashboard.Should().BeTrue("aums/dashboard route should exist");
        hasAumsProjects.Should().BeTrue("aums/projects route should exist");
    }

    [Fact]
    public void Post_mortem_read_route_attributes_exist()
    {
        var controller = typeof(OperationalCompatibilityController);
        var methods = controller.GetMethods();

        var hasPostMortemDashboard = methods.Any(m => 
            m.GetCustomAttributes(typeof(HttpGetAttribute), inherit: true)
                .OfType<HttpGetAttribute>()
                .Any(attr => attr.Template?.Contains("post-mortem/dashboard", StringComparison.OrdinalIgnoreCase) == true));

        var hasPostMortemReports = methods.Any(m => 
            m.GetCustomAttributes(typeof(HttpGetAttribute), inherit: true)
                .OfType<HttpGetAttribute>()
                .Any(attr => attr.Template?.Contains("post-mortem/reports", StringComparison.OrdinalIgnoreCase) == true));

        hasPostMortemDashboard.Should().BeTrue("post-mortem/dashboard route should exist");
        hasPostMortemReports.Should().BeTrue("post-mortem/reports route should exist");
    }
}
