namespace PangyaAPI.UpdateList.Models;

/// <summary>
/// Define, em um único lugar, todos os atributos do elemento &lt;fileinfo&gt;
/// da updatelist: nome do atributo XML, como ler de um UpdateEntry (para
/// escrita) e como escrever de volta em um UpdateEntry (para leitura).
///
/// Antes, UpdateReader e UpdateWriter conheciam a lista de 8 campos cada um
/// de forma independente — esquecer de atualizar um dos dois ao adicionar um
/// campo novo gerava divergência silenciosa. Agora ambos iteram sobre esta
/// lista, então adicionar um campo aqui já propaga para leitura e escrita.
/// </summary>
public static class UpdateEntryFieldMap
{
    public static readonly IReadOnlyList<UpdateEntryField> Fields = new[]
    {
        new UpdateEntryField("fname", e => e.fname, (e, v) => e.fname = v),
        new UpdateEntryField("fdir",  e => e.fdir,  (e, v) => e.fdir  = v),
        new UpdateEntryField("fsize", e => e.fsize.ToString(), (e, v) => e.fsize = ParseLong(v)),
        new UpdateEntryField("fcrc",  e => e.fcrc.ToString(),  (e, v) => e.fcrc  = ParseInt(v)),
        new UpdateEntryField("fdate", e => e.fdate, (e, v) => e.fdate = v),
        new UpdateEntryField("ftime", e => e.ftime, (e, v) => e.ftime = v),
        new UpdateEntryField("pname", e => e.pname, (e, v) => e.pname = v),
        new UpdateEntryField("psize", e => e.psize.ToString(), (e, v) => e.psize = ParseInt(v)),
    };

    private static long ParseLong(string v) => long.TryParse(v, out var r) ? r : 0L;
    private static int ParseInt(string v) => int.TryParse(v, out var r) ? r : 0;
}

/// <summary>Definição de um único atributo do &lt;fileinfo&gt;: nome XML + getter/setter tipados.</summary>
public sealed class UpdateEntryField
{
    public string XmlAttributeName { get; }
    public Func<UpdateEntry, string> Get { get; }
    public Action<UpdateEntry, string> Set { get; }

    public UpdateEntryField(string xmlAttributeName, Func<UpdateEntry, string> get, Action<UpdateEntry, string> set)
    {
        XmlAttributeName = xmlAttributeName;
        Get = get;
        Set = set;
    }
}
