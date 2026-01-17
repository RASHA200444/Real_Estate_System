using System;
using System.Collections.Generic;

namespace otherServices.Models;

public partial class Comment
{
    public long CommentId { get; set; }

    public long PostId { get; set; }

    public long UserId { get; set; }

    public string Description { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual Post Post { get; set; } = null!;

    public virtual User User { get; set; } = null!;

}
