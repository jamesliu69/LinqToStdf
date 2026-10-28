namespace StdfReader.Core
{
    /// <summary>由 PIR/PRR 配對出的單一料件（part）；在收到對應 PRR 前 Status 為 Unknown。</summary>
    public sealed class PartEntry { public int Index { get; set; } public string PartId { get; set; } public byte? Head { get; set; } public byte? Site { get; set; } public string Status { get; set; } public long? TestTimeMilliseconds { get; set; } }
}
