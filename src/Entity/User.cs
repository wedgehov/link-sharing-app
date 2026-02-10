using System.ComponentModel.DataAnnotations.Schema;

namespace Entity;

[Table("users")]
public class User
{
    [Column("id")]
    public int Id { get; set; }

    [Column("email")]
    public string Email { get; set; } = string.Empty;

    [Column("password_hash")]
    public string PasswordHash { get; set; } = string.Empty;

    public UserProfile? UserProfile { get; set; }
}
