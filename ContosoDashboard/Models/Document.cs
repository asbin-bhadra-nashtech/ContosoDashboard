using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ContosoDashboard.Services;

namespace ContosoDashboard.Models;

public class Document
{
    [Key]
    public int DocumentId { get; set; }

    [Required, MaxLength(255)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; set; }

    [Required, MaxLength(100)]
    public string Category { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Tags { get; set; }

    [Required, MaxLength(255)]
    public string OriginalFileName { get; set; } = string.Empty;

    [Required, MaxLength(500)]
    public string FilePath { get; set; } = string.Empty;

    [Required, MaxLength(255)]
    public string FileType { get; set; } = string.Empty;

    [Range(1, DocumentValidation.MaxFileSizeBytes)]
    public long FileSize { get; set; }

    public DateTime UploadedDate { get; set; } = DateTime.UtcNow;

    [Required]
    public int UploadedByUserId { get; set; }

    public int? ProjectId { get; set; }
    public int? TaskId { get; set; }

    [ForeignKey(nameof(UploadedByUserId))]
    public virtual User UploadedByUser { get; set; } = null!;
    public virtual Project? Project { get; set; }
    public virtual TaskItem? Task { get; set; }
    public virtual ICollection<DocumentShare> Shares { get; set; } = new List<DocumentShare>();
    public virtual ICollection<DocumentActivity> Activities { get; set; } = new List<DocumentActivity>();
}

public class DocumentShare
{
    [Key]
    public int DocumentShareId { get; set; }
    public int? DocumentId { get; set; }
    public int? SharedWithUserId { get; set; }
    [MaxLength(100)]
    public string? SharedWithDepartment { get; set; }
    [Required]
    public int SharedByUserId { get; set; }
    public DateTime SharedDate { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;

    public virtual Document? Document { get; set; }
    public virtual User? SharedWithUser { get; set; }
    public virtual User SharedByUser { get; set; } = null!;
}

public class DocumentActivity
{
    [Key]
    public int DocumentActivityId { get; set; }
    public int? DocumentId { get; set; }
    [Required]
    public int UserId { get; set; }
    [Required, MaxLength(40)]
    public string Action { get; set; } = string.Empty;
    public DateTime OccurredDate { get; set; } = DateTime.UtcNow;
    [MaxLength(1000)]
    public string? Details { get; set; }

    public virtual Document? Document { get; set; }
    public virtual User User { get; set; } = null!;
}
