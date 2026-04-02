using SQLite;

namespace VinhKhanh.App.Data;

[Table("cached_pois")]
public sealed class CachedPoiEntity
{
	[PrimaryKey] public int Id { get; set; }
	public string PayloadJson { get; set; } = "";
	public long UpdatedUtcTicks { get; set; }
}
