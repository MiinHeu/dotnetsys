using SQLite;

namespace VinhKhanh.App.Data;

[Table("cached_tours")]
public sealed class CachedTourEntity
{
	[PrimaryKey] public int Id { get; set; }
	public string PayloadJson { get; set; } = "";
	public long UpdatedUtcTicks { get; set; }
}
