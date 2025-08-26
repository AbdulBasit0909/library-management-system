using System.ComponentModel.DataAnnotations;

namespace LibraryManagement.API.Models
{
    public class UpdateRoleModel
    {
        [Required]
        public string NewRole { get; set; }
    }
}