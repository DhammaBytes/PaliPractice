using SQLite;

namespace PaliPractice.Services.UserData.Entities;

/// <summary>
/// Key-value store for user settings.
/// </summary>
[Table("user_settings")]
public class UserSetting
{
    [PrimaryKey]
    [Column("key")]
    public string Key { get; set; } = string.Empty;

    [Column("value")]
    public string Value { get; set; } = string.Empty;

    [Column("updated_utc")]
    public DateTime UpdatedUtc { get; set; }
}
