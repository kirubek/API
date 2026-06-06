using System.Reflection;
using BaseOps.API.Controllers;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;

namespace BaseOps.SecurityTests;

public sealed class UserManagementAuthorizationTests
{
    private const string ManagementRoles = "Manager,Director,SystemAdmin";

    public static TheoryData<string> RestrictedActions => new()
    {
        nameof(UsersController.Create),
        nameof(UsersController.GetAssignments),
        nameof(UsersController.CreateAssignment),
        nameof(UsersController.UpdateAssignment),
        nameof(UsersController.DeleteAssignment),
        nameof(UsersController.Update),
        nameof(UsersController.Deactivate),
        nameof(UsersController.Reactivate),
        nameof(UsersController.LockUnlock),
        nameof(UsersController.Delete)
    };

    [Theory]
    [MemberData(nameof(RestrictedActions))]
    public void User_management_mutations_require_management_roles(string actionName)
    {
        var methods = typeof(UsersController)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Where(method => method.Name == actionName)
            .ToArray();

        methods.Should().NotBeEmpty($"{actionName} should exist on UsersController");
        methods.Should().AllSatisfy(method =>
        {
            var authorize = method.GetCustomAttributes<AuthorizeAttribute>(inherit: true).SingleOrDefault();
            authorize.Should().NotBeNull($"{method.Name} must explicitly restrict roles");
            authorize!.Roles.Should().Be(ManagementRoles);
        });
    }
}
