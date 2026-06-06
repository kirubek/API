using System.Text.Json;

namespace BaseOps.API.Services;

public interface IWorkflowValidator
{
    bool CanTransition(string module, string currentStatus, string newStatus, JsonElement? payload = null);
    string? GetValidationError(string module, string currentStatus, string newStatus);
    IEnumerable<string> GetValidTransitions(string module, string currentStatus);
}

public sealed class WorkflowValidator : IWorkflowValidator
{
    private static readonly Dictionary<string, Dictionary<string, string[]>> WorkflowDefinitions = new()
    {
        // Post-Mortem workflow
        ["post-mortem"] = new Dictionary<string, string[]>
        {
            ["Draft"] = new[] { "Submitted", "Cancelled" },
            ["Submitted"] = new[] { "Under Review", "Cancelled" },
            ["Under Review"] = new[] { "Approved", "Rejected", "Returned" },
            ["Rejected"] = new[] { "Draft", "Cancelled" },
            ["Returned"] = new[] { "Draft", "Submitted" },
            ["Approved"] = new[] { "Completed", "Cancelled" },
            ["Completed"] = Array.Empty<string>(),
            ["Cancelled"] = Array.Empty<string>()
        },
        ["postmortem"] = new Dictionary<string, string[]>
        {
            ["Draft"] = new[] { "Submitted", "Cancelled" },
            ["Submitted"] = new[] { "Under Review", "Cancelled" },
            ["Under Review"] = new[] { "Approved", "Rejected", "Returned" },
            ["Rejected"] = new[] { "Draft", "Cancelled" },
            ["Returned"] = new[] { "Draft", "Submitted" },
            ["Approved"] = new[] { "Completed", "Cancelled" },
            ["Completed"] = Array.Empty<string>(),
            ["Cancelled"] = Array.Empty<string>()
        },

        // ACE Activities workflow
        ["ace"] = new Dictionary<string, string[]>
        {
            ["Draft"] = new[] { "Submitted", "Cancelled" },
            ["Submitted"] = new[] { "Under Review", "Cancelled" },
            ["Under Review"] = new[] { "Approved", "Rejected", "Returned" },
            ["Rejected"] = new[] { "Draft", "Cancelled" },
            ["Returned"] = new[] { "Draft", "Submitted" },
            ["Approved"] = new[] { "Completed", "Cancelled" },
            ["Completed"] = Array.Empty<string>(),
            ["Cancelled"] = Array.Empty<string>()
        },
        ["ace-activities"] = new Dictionary<string, string[]>
        {
            ["Draft"] = new[] { "Submitted", "Cancelled" },
            ["Submitted"] = new[] { "Under Review", "Cancelled" },
            ["Under Review"] = new[] { "Approved", "Rejected", "Returned" },
            ["Rejected"] = new[] { "Draft", "Cancelled" },
            ["Returned"] = new[] { "Draft", "Submitted" },
            ["Approved"] = new[] { "Completed", "Cancelled" },
            ["Completed"] = Array.Empty<string>(),
            ["Cancelled"] = Array.Empty<string>()
        },

        // Leave Request workflow
        ["leave"] = new Dictionary<string, string[]>
        {
            ["Draft"] = new[] { "Submitted", "Cancelled" },
            ["Submitted"] = new[] { "Pending Approval", "Cancelled" },
            ["Pending Approval"] = new[] { "Approved", "Rejected", "Cancelled" },
            ["Rejected"] = new[] { "Draft", "Cancelled" },
            ["Approved"] = new[] { "In Progress", "Cancelled" },
            ["In Progress"] = new[] { "Completed", "Cancelled" },
            ["Completed"] = Array.Empty<string>(),
            ["Cancelled"] = Array.Empty<string>()
        },
        ["annualleave"] = new Dictionary<string, string[]>
        {
            ["Draft"] = new[] { "Submitted", "Cancelled" },
            ["Submitted"] = new[] { "Pending Approval", "Cancelled" },
            ["Pending Approval"] = new[] { "Approved", "Rejected", "Cancelled" },
            ["Rejected"] = new[] { "Draft", "Cancelled" },
            ["Approved"] = new[] { "In Progress", "Cancelled" },
            ["In Progress"] = new[] { "Completed", "Cancelled" },
            ["Completed"] = Array.Empty<string>(),
            ["Cancelled"] = Array.Empty<string>()
        },

        // Handover workflow - aligned with HandoverStatus enum (Draft=1, Pending=2, Accepted=3, Rejected=4)
        ["handover"] = new Dictionary<string, string[]>
        {
            ["Draft"] = new[] { "Pending" },
            ["Pending"] = new[] { "Accepted", "Rejected" },
            ["Accepted"] = Array.Empty<string>(),
            ["Rejected"] = new[] { "Draft" }
        },
        ["handover-logbook"] = new Dictionary<string, string[]>
        {
            ["Draft"] = new[] { "Pending" },
            ["Pending"] = new[] { "Accepted", "Rejected" },
            ["Accepted"] = Array.Empty<string>(),
            ["Rejected"] = new[] { "Draft" }
        },

        // Material Order workflow
        ["mo"] = new Dictionary<string, string[]>
        {
            ["Draft"] = new[] { "Submitted", "Cancelled" },
            ["Submitted"] = new[] { "Pending Approval", "Cancelled" },
            ["Pending Approval"] = new[] { "Approved", "Rejected", "Escalated", "Cancelled" },
            ["Rejected"] = new[] { "Draft", "Cancelled" },
            ["Escalated"] = new[] { "Approved", "Rejected", "Cancelled" },
            ["Approved"] = new[] { "In Progress", "Cancelled" },
            ["In Progress"] = new[] { "Completed", "Cancelled" },
            ["Completed"] = Array.Empty<string>(),
            ["Cancelled"] = Array.Empty<string>()
        },
        ["material-order"] = new Dictionary<string, string[]>
        {
            ["Draft"] = new[] { "Submitted", "Cancelled" },
            ["Submitted"] = new[] { "Pending Approval", "Cancelled" },
            ["Pending Approval"] = new[] { "Approved", "Rejected", "Escalated", "Cancelled" },
            ["Rejected"] = new[] { "Draft", "Cancelled" },
            ["Escalated"] = new[] { "Approved", "Rejected", "Cancelled" },
            ["Approved"] = new[] { "In Progress", "Cancelled" },
            ["In Progress"] = new[] { "Completed", "Cancelled" },
            ["Completed"] = Array.Empty<string>(),
            ["Cancelled"] = Array.Empty<string>()
        },

        // Carry Over workflow
        ["carryover"] = new Dictionary<string, string[]>
        {
            ["Draft"] = new[] { "Pending", "Cancelled" },
            ["Pending"] = new[] { "In Progress", "Cancelled" },
            ["In Progress"] = new[] { "Completed", "Cancelled" },
            ["Completed"] = Array.Empty<string>(),
            ["Cancelled"] = Array.Empty<string>()
        },
        ["carry-over"] = new Dictionary<string, string[]>
        {
            ["Draft"] = new[] { "Pending", "Cancelled" },
            ["Pending"] = new[] { "In Progress", "Cancelled" },
            ["In Progress"] = new[] { "Completed", "Cancelled" },
            ["Completed"] = Array.Empty<string>(),
            ["Cancelled"] = Array.Empty<string>()
        },

        // AUMS workflow
        ["aums"] = new Dictionary<string, string[]>
        {
            ["Draft"] = new[] { "In Progress", "Cancelled" },
            ["In Progress"] = new[] { "On Hold", "Completed", "Delayed", "Cancelled" },
            ["On Hold"] = new[] { "In Progress", "Cancelled" },
            ["Delayed"] = new[] { "In Progress", "Cancelled" },
            ["Completed"] = Array.Empty<string>(),
            ["Cancelled"] = Array.Empty<string>()
        },

        // SAFA workflow
        ["safa"] = new Dictionary<string, string[]>
        {
            ["Draft"] = new[] { "In Progress", "Cancelled" },
            ["In Progress"] = new[] { "Completed", "Cancelled" },
            ["Completed"] = Array.Empty<string>(),
            ["Cancelled"] = Array.Empty<string>()
        },

        // Default workflow for unknown modules
        ["default"] = new Dictionary<string, string[]>
        {
            ["Draft"] = new[] { "Submitted", "Cancelled" },
            ["Submitted"] = new[] { "Approved", "Rejected", "Cancelled" },
            ["Rejected"] = new[] { "Draft", "Cancelled" },
            ["Approved"] = new[] { "Completed", "Cancelled" },
            ["Completed"] = Array.Empty<string>(),
            ["Cancelled"] = Array.Empty<string>()
        }
    };

    public bool CanTransition(string module, string currentStatus, string newStatus, JsonElement? payload = null)
    {
        // Normalize module name
        var normalizedModule = NormalizeModuleName(module);
        
        // Get workflow definition
        if (!WorkflowDefinitions.TryGetValue(normalizedModule, out var workflow))
        {
            workflow = WorkflowDefinitions["default"];
        }

        // Normalize status names
        var normalizedCurrent = NormalizeStatus(currentStatus);
        var normalizedNew = NormalizeStatus(newStatus);

        // If current status not in workflow, allow transition (for new records)
        if (!workflow.ContainsKey(normalizedCurrent))
        {
            return true;
        }

        // Check if transition is valid
        var validTransitions = workflow[normalizedCurrent];
        return validTransitions.Contains(normalizedNew);
    }

    public string? GetValidationError(string module, string currentStatus, string newStatus)
    {
        var normalizedModule = NormalizeModuleName(module);
        var normalizedCurrent = NormalizeStatus(currentStatus);
        var normalizedNew = NormalizeStatus(newStatus);

        if (!WorkflowDefinitions.TryGetValue(normalizedModule, out var workflow))
        {
            workflow = WorkflowDefinitions["default"];
        }

        if (!workflow.ContainsKey(normalizedCurrent))
        {
            return null; // Unknown current status, allow transition
        }

        var validTransitions = workflow[normalizedCurrent];
        if (!validTransitions.Contains(normalizedNew))
        {
            return $"Invalid status transition from '{currentStatus}' to '{newStatus}' for module '{module}'. Valid transitions are: {string.Join(", ", validTransitions)}";
        }

        return null;
    }

    public IEnumerable<string> GetValidTransitions(string module, string currentStatus)
    {
        var normalizedModule = NormalizeModuleName(module);
        var normalizedCurrent = NormalizeStatus(currentStatus);

        if (!WorkflowDefinitions.TryGetValue(normalizedModule, out var workflow))
        {
            workflow = WorkflowDefinitions["default"];
        }

        if (!workflow.ContainsKey(normalizedCurrent))
        {
            return Array.Empty<string>();
        }

        return workflow[normalizedCurrent];
    }

    private static string NormalizeModuleName(string module)
    {
        return module.ToLower().Replace("-", "").Replace("_", "");
    }

    private static string NormalizeStatus(string status)
    {
        return status.ToLower().Replace("-", " ").Replace("_", " ");
    }
}
