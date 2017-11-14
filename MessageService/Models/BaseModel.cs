using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace MessageService.Models
{
    public abstract class RootModel
    {
        [Key]
        [StringLength(36)]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public String Id { get; set; }
    }

    public abstract class BaseModel : RootModel
    {
        [Required]
        public DateTime CreatedTime { get; set; }
        [Required]
        [StringLength(36)]
        public String CreatedUserId { get; set; }

        public BaseModel()
        {
            CreatedTime = DateTime.UtcNow;
            CreatedUserId = Constants.SYSTEM_USER_ID;
        }
    }
}
