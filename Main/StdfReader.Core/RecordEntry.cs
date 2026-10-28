using Stdf;
namespace StdfReader.Core
{
    /// <summary>保留的原始 STDF 記錄及其檔案位移與顯示名稱；未知記錄以 "Unknown " + RecordType 命名。</summary>
    public sealed class RecordEntry { public StdfRecord Record { get; set; } public long Offset { get; set; } public string Name { get; set; } }
}
