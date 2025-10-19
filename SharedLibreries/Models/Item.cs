using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SharedLibreries.Models
{
    public class Item
    {
        public Guid Id { get; set; }
        
        [Required]
        public Guid UserId { get; set; }
        
        [Required]
        [MaxLength(500)]
        public string Title { get; set; } = string.Empty;
        
        [MaxLength(2000)]
        public string? Description { get; set; }
        
        public bool IsCompleted { get; set; } = false;
        
        public bool IsDeleted { get; set; } = false;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? DeletedAt { get; set; }
        
        // Navigation property
        [ForeignKey(nameof(UserId))]
        public virtual User User { get; set; } = null!;
    }
}
