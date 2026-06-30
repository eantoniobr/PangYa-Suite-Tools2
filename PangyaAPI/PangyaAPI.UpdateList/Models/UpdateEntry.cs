namespace PangyaAPI.UpdateList.Models;

public class UpdateEntry
{
    // ── Campos persistidos no XML (<fileinfo .../>) ─────────────────────────
    public string fname { get; set; } = "";
    public string fdir { get; set; } = "";
    public long fsize { get; set; }
    public int fcrc { get; set; }
    public string fdate { get; set; } = "";
    public string ftime { get; set; } = "";
    public string pname { get; set; } = "";
    public int psize { get; set; }
    public string? FullPath { get; set; }
    public DateTime? DataModificacao { get; set; }
}
