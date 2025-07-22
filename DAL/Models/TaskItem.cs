using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using DAL.Enums;
using TaskStatus = DAL.Enums.TaskStatus;


namespace DAL.Models;

public class TaskItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;
    
    [MaxLength(1000)]
    public string Description { get; set; } = string.Empty;
    
    public TaskStatus Status { get; set; } = TaskStatus.Todo;
    public TaskPriority Priority { get; set; } = TaskPriority.Medium;
    public DateTime? DueDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    [MaxLength(100)]
    public string? Category { get; set; }
    
    public string TagsJson { get; set; } = "[]";
    
    // Foreign keys
    public Guid AssignedToId { get; set; }
    public Guid CreatedById { get; set; }
    
    // Navigation properties
    [ForeignKey("AssignedToId")]
    public User AssignedTo { get; set; } = null!;
    
    [ForeignKey("CreatedById")]
    public User CreatedBy { get; set; } = null!;
    
    // Not mapped property for tags
    [NotMapped]
    public string[] Tags
    {
        get => string.IsNullOrEmpty(TagsJson) ? Array.Empty<string>() : 
            JsonSerializer.Deserialize<string[]>(TagsJson) ?? Array.Empty<string>();
        set => TagsJson = JsonSerializer.Serialize(value);
    }
}