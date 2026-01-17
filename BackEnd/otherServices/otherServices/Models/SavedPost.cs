using System;
using System.Collections.Generic;

namespace otherServices.Models;

public partial class SavedPost
{
    public long UserId { get; set; }

    public long PostId { get; set; }

    public DateTime SavedDate { get; set; } = DateTime.UtcNow;

    public virtual Post Post { get; set; }

    public virtual User User { get; set; }
}
