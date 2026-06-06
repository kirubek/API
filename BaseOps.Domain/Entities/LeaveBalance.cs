using BaseOps.Domain.Common;

namespace BaseOps.Domain.Entities;

public sealed class LeaveBalance : AuditableEntity
{
    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;
    
    public int Year { get; set; }
    
    public int TotalEntitled { get; set; } = 30; // Default 30 days per year
    
    public int Taken { get; set; } = 0;
    
    public int Pending { get; set; } = 0;
    
    public int Remaining => TotalEntitled - Taken - Pending;
    
    public int CarryOverFromPrevious { get; set; } = 0;
    
    public int CarryOverToNext { get; set; } = 0;
}
