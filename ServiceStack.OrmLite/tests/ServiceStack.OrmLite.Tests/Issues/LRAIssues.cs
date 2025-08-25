using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.DataAnnotations;
using ServiceStack.Model;
using ServiceStack.Text;

namespace ServiceStack.OrmLite.Tests.Issues;

[TestFixtureOrmLite]
public class AdhocJoinIssue(DialectContext context) : OrmLiteProvidersTestBase(context)
{
    [Test]
    public void Can_run_LRA_Query()
    {
        using var db = OpenDbConnection();
        CreateTables(db);

        var query = db.From<LRAAnalisi>()
            .Join<LRAAnalisi, LRAContenitore>((ana, cont) => ana.ContenitoreId == cont.Id)
            .Join<LRAContenitore, LRARichiesta>((cont, ric) => cont.RichiestaId == ric.Id)
            .Join<LRARichiesta, LRAPaziente>((ric, paz) => ric.PazienteId == paz.Id)
            .Select<LRAAnalisi, LRAContenitore, LRARichiesta, LRAPaziente>((ana, cont, ric, paz) =>
                new
                {
                    AnalisiId = ana.Id,
                    ContenitoreId = cont.Id,
                    RichiestaId = ric.Id,
                    DataAccettazioneRichiesta = ric.DataOraAccettazione,
                    DataCheckinContenitore = cont.DataOraPrimoCheckin,
                    DataEsecuzioneAnalisi = ana.DataOraEsecuzione,
                    DataPrelievoContenitore = cont.DataOraPrelievo,
                    DataValidazioneAnalisi = ana.DataOraValidazione,
                    sessoPaziente = paz.Sesso,
                    dataDiNascitaPaziente = paz.DataDiNascita,
                    etaPazienteRichiesta = ric.EtaPaziente,
                    dataOraAccettazioneRichiesta = ric.DataOraAccettazione,
                    unitaMisuraEtaPaziente = ric.UnitaDiMisuraEtaPaziente,
                    settimaneGravidanza = ric.SettimaneGravidanza,
                    repartoId = ric.RepartoId,
                    prioritaRichiesta = ric.PrioritaId,
                    laboratorioRichiedente = ric.LaboratorioRichiedenteId,
                    prioritaContenitore = cont.PrioritaId,
                    idLaboratorioEsecutoreCandidatoAnalisi = ana.LaboratorioEsecutoreCandidatoId
                });
    }

    [Test]
    public void Can_run_expression_using_captured_lambda_params()
    {
        using var db = OpenDbConnection();
        CreateTables(db);

        var idContenitore = 1;
                
        var q = db.From<LRAAnalisi>()
            .Where(ana => ana.ContenitoreId == idContenitore)
            .And(ana => !Sql.In(ana.Id, db.From<LRAContenitore>()
                    .Where(ris => ris.Id == ana.ContenitoreId)
                    .Select(ris => new { ris.Id })
                )
            )
            .Select(x => new { A = 1 });

        var sql = q.ToSelectStatement();
        sql.Print();
        Assert.That(sql, Is.Not.Null);
    }
        
    [Test]
    public void Does_select_aliases_on_custom_expressions()
    {
        var statements = new List<string>();
        OrmLiteConfig.BeforeExecFilter = cmd => statements.Add(cmd.GetDebugString());

        using var db = OpenDbConnection();
        var ADeviceId = 1;
                
        var q = db.From<LRDDevice>();
        q.LeftJoin<LRDDevice,LRDConnessione>((a,b)=>a.Id==b.DeviceId);
        q.LeftJoin<LRDDevice, LRAAlert>((a, b) => a.Id == b.DeviceID && b.Visto==(int)SiNo.No);
        q.Where<LRDDevice>(x => x.Id == ADeviceId);
        q.GroupBy<LRDDevice,LRDConnessione, LRAAlert>((dev,con, ale) => new {dev.Id, con.DeviceId, ale.DeviceID,ale.Tipo });
        q.Select<LRDDevice, LRDConnessione, LRAAlert>((dev, con, ale) => new //DeviceDashboardDTO
        {
            DeviceId = dev.Id,
            //Device = dev,
            StatoLinea = Sql.Min(con.StatoConnessione),
            StatoQC = (Sql.Count(ale.Tipo == (int)TipoAlert.AlertQC ? ale.Id : 0) > 0 ? (int)SiNo.Si : (int)SiNo.No),
            StatoWarning = (Sql.Count(ale.Tipo == (int)TipoAlert.WarningDevice ? ale.Id : 0) > 0 ? (int)SiNo.Si : (int)SiNo.No)
        });


        try
        {
            var results = db.Select<DeviceDashboardDTO>(q);
        }
        catch {} // no tables

        var lastStatement = statements.Last();
        lastStatement.Print();
                
        lastStatement = lastStatement.NormalizeSql();
        Assert.That(lastStatement, Does.Contain("as deviceid"));
        Assert.That(lastStatement, Does.Contain("as statolinea"));
        Assert.That(lastStatement, Does.Contain("as statoqc"));
        Assert.That(lastStatement, Does.Contain("as statowarning"));
    }

    [Test]
    public async Task Does_RowCount_LRARichiesta_Async()
    {
        using var db = OpenDbConnection();
        CreateTables(db);
                
        var q = db.From<LRARichiesta>()
            .Where(x => x.Id > 0);

        var result = await db.RowCountAsync(q);
    }

    [Test]
    public void Does_InsertIntoSelect_LRARichiesta()
    {
        using var db = OpenDbConnection();
        RecreateLRARichiesta(db);

        long numeroRichieste = db.Count<LRARichiesta>();

        var q = db.From<LRARichiesta>()
            .Select(ric => new //LRARisultato
            {
                AnalisiId = 1,
                Commento = ric.Commento,
                TipoValore = 1,
                Stato = 1,
                RisultatoId = 1,
                DataOraRicezione = DateTime.UtcNow,
                DataModifica = DateTime.UtcNow,
                VersioneRecord = 1,
                InviareALIS = 1,
                RisultatoPrincipale = 1,
                TipoInserimento = 1,
                Citrato = 1,
            });

        long result = db.InsertIntoSelect<LRARisultato>(q);

        Assert.That(result, Is.EqualTo(numeroRichieste));
    }

    private static void RecreateLRARichiesta(IDbConnection db)
    {
        db.DropTable<LRAAnalisi>();
        db.DropTable<LRAContenitore>();
        db.DropTable<LRARichiesta>();
        db.DropTable<LRARisultato>();
        db.DropTable<LRDLaboratorio>();
        db.DropTable<LRAPaziente>();
        db.DropTable<LRDReparto>();
        db.DropTable<LRDPriorita>();

        db.CreateTable<LRDLaboratorio>();
        db.CreateTable<LRAPaziente>();
        db.CreateTable<LRDReparto>();
        db.CreateTable<LRDPriorita>();
        db.CreateTable<LRARisultato>();
        db.CreateTable<LRARichiesta>();
        db.CreateTable<LRAContenitore>();
        db.CreateTable<LRAAnalisi>();
    }

    [Test]
    public async Task Does_InsertIntoSelect_LRARichiesta_Async()
    {
        using var db = OpenDbConnection();
        RecreateLRARichiesta(db);

        long numeroRichieste = await db.CountAsync<LRARichiesta>();

        var q = db.From<LRARichiesta>()
            .Select(ric => new //LRARisultato
            {
                AnalisiId = 1,
                Commento = ric.Commento,
                TipoValore = 1,
                Stato = 1,
                RisultatoId = 1,
                DataOraRicezione = DateTime.UtcNow,
                DataModifica = DateTime.UtcNow,
                VersioneRecord = 1,
                InviareALIS = 1,
                RisultatoPrincipale = 1,
                TipoInserimento = 1,
                Citrato = 1,
            });

        long result = await db.InsertIntoSelectAsync<LRARisultato>(q);

        Assert.That(result, Is.EqualTo(numeroRichieste));
    }
        
    private static void CreateTables(IDbConnection db)
    {
        DropTables(db);

        db.CreateTable<LRARisultato>();
        db.CreateTable<LRDLaboratorio>();
        db.CreateTable<LRDPriorita>();
        db.CreateTable<LRDReparto>();
        db.CreateTable<LRAPaziente>();
        db.CreateTable<LRARichiesta>();
        db.CreateTable<LRAContenitore>();
        db.CreateTable<LRAAnalisi>();
    }

    private static void DropTables(IDbConnection db)
    {
        db.DropTable<LRAAnalisi>();
        db.DropTable<LRAContenitore>();
        db.DropTable<LRARichiesta>();
        db.DropTable<LRAPaziente>();
        db.DropTable<LRDReparto>();
        db.DropTable<LRDPriorita>();
        db.DropTable<LRDLaboratorio>();
        db.DropTable<LRARisultato>();
    }

    private static void InitAliasTables(IDbConnection db)
    {
        db.DropTable<LRDProfiloAnalisi>();
        db.DropTable<LRDAnalisi>();
        db.DropTable<LRDContenitore>();
                
        db.CreateTable<LRDContenitore>();
        db.CreateTable<LRDAnalisi>();
        db.CreateTable<LRDProfiloAnalisi>();

        db.Insert(new LRDAnalisi
        {
            Codice = "TEST", 
            Descrizione = "DESCRIPTION",
            DataModifica = DateTime.UtcNow
        });

        db.Insert(new LRDProfiloAnalisi
        {
            AnalisiId = 1,
            DataModifica = DateTime.UtcNow,
            VersioneRecord = 1,
            ProfiloAnalisiId = null
        });
    }
        
    [Test]
    public void Table_Alias()
    {
        using var db = OpenDbConnection();
        InitAliasTables(db);
                
        var q = db.From<LRDAnalisi>(options => {
                options.SetTableAlias("dana");
                options.UseSelectPropertiesAsAliases = true;
            })
            .Join<LRDAnalisi, LRDContenitore>((dana, dcont) => dana.ContenitoreId == dcont.Id, db.TableAlias("c"))
            .Join<LRDAnalisi, LRDProfiloAnalisi>((dana, dprofana) => dana.Id == dprofana.AnalisiId, db.TableAlias("dprofana"))
            .Where<LRDProfiloAnalisi>(dprofana => Sql.TableAlias(dprofana.ProfiloAnalisiId, "dprofana") == null)
            .SelectDistinct<LRDAnalisi, LRDProfiloAnalisi, LRDContenitore>((dana, dprofana, dcont) =>
                new //ProfiloAnalisiDTO
                {
                    Id = Sql.TableAlias(dprofana.Id, "dprofana"),
                    AnalisiId = dana.Id,
                    Codice = dana.Codice,
                    Descrizione = dana.Descrizione,
                    ContenitoreId = dana.ContenitoreId,
                    ContenitoreCodice = Sql.TableAlias(dcont.Codice, "c"),
                    ContenitoreDescrizione = Sql.TableAlias(dcont.Descrizione, "c"),
                    VersioneRecord = Sql.TableAlias(dprofana.VersioneRecord, "dprofana")
                });
                
        var result = db.Select<ProfiloAnalisiDTO>(q);
    }
        
    [Test]
    public void Join_Alias()
    {
        using var db = OpenDbConnection();
        InitAliasTables(db);

        var q = db.From<LRDAnalisi>()
            .Join<LRDAnalisi, LRDContenitore>((dana, dcont) => dana.ContenitoreId == dcont.Id, db.JoinAlias("c"))
            .Join<LRDAnalisi, LRDProfiloAnalisi>((dana, dprofana) => dana.Id == dprofana.AnalisiId, db.JoinAlias("dprofana"))
            .Where<LRDProfiloAnalisi>(dprofana => Sql.JoinAlias(dprofana.ProfiloAnalisiId, "dprofana") == null)
            .SelectDistinct<LRDAnalisi, LRDProfiloAnalisi, LRDContenitore>((dana, dprofana, dcont) =>
                new //ProfiloAnalisiDTO
                {
                    Id = Sql.JoinAlias(dprofana.Id, "dprofana"),
                    AnalisiId = dana.Id,
                    Codice = dana.Codice,
                    Descrizione = dana.Descrizione,
                    ContenitoreId = Sql.JoinAlias(dcont.Id, "c"),
                    ContenitoreCodice = Sql.JoinAlias(dcont.Codice, "c"),
                    ContenitoreDescrizione = Sql.JoinAlias(dcont.Descrizione, "c"),
                    VersioneRecord = Sql.JoinAlias(dprofana.VersioneRecord, "dprofana")
                });
                
        var result = db.Select<ProfiloAnalisiDTO>(q);
    }        
}
    
public class ElementoProfiloAnalisiDTO
{
    public int Id { get; set; }

    public int ProfiloAnalisiId { get; set; }

    public int AnalisiId { get; set; }

    public string CodiceAnalisi { get; set; }

    public string DescrizioneAnalisi { get; set; }

    public int VersioneRecord { get; set; }
}
    
public class ProfiloAnalisiDTO
{
    public int Id { get; set; }

    public string Codice { get; set; }

    public string Descrizione { get; set; }

    public int ContenitoreId { get; set; }

    public string ContenitoreCodice { get; set; }

    public string ContenitoreDescrizione { get; set; }

    public int VersioneRecord { get; set; }

    public int AnalisiId { get; set; }

    public IEnumerable<ElementoProfiloAnalisiDTO> Elementi { get; set; }
}
    

public class DBObject : ICloneable
{
    [Alias("DATAMODIFICA")]
    public DateTime DataModifica { get; set; }

    [Default(1)]
    [Alias("VERSIONERECORD")]
    public int VersioneRecord { get; set; }

    public object Clone()
    {
        return MemberwiseClone();
    }
}

public class SessiPaziente
{
    public const int NonDichiarato = 0;
}

public class UnitaMisuraEtaPaziente
{
    public const int Anni = 0;
}
    
[Alias("LRAANALISI")]
public class LRAAnalisi : IHasId<int>
{
    [PrimaryKey]
    [AutoIncrement]        
    [Alias("IDAANALISI")]
    public int Id { get; set; }

    [Alias("ACONTENITOREID")]
    [References(typeof(LRAContenitore))]
    public int ContenitoreId { get; set; }

    [Reference]
    public LRAContenitore Contenitore { get; set; }

    [Alias("DATAORAVALIDAZIONE")]
    public DateTime? DataOraValidazione { get; set; }

    [Alias("DLABORATORIOCANDIDATOID")]
    [References(typeof(LRDLaboratorio))]
    public int? LaboratorioEsecutoreCandidatoId { get; set; }

    [Alias("DATAORAESECUZIONE")]
    public DateTime? DataOraEsecuzione { get; set; }
}
    
[Alias("LRACONTENITORI")]
public class LRAContenitore : DBObject, IHasId<int>
{
    [PrimaryKey]
    [AutoIncrement]
    [Alias("IDACONTENITORE")]
    public int Id { get; set; }

    [Alias("ARICHIESTAID")]
    [References(typeof(LRARichiesta))]
    public int RichiestaId { get; set; }
        
    [Alias("DPRIORITAID")]
    [References(typeof(LRDPriorita))]
    public int PrioritaId { get; set; }

    [Alias("DATAORAPRELIEVO")]
    public DateTime? DataOraPrelievo { get; set; }

    [Alias("DATAORAPRIMOCHECKIN")]
    public DateTime? DataOraPrimoCheckin { get; set; }
}

[Alias("LRARICHIESTE")]
public class LRARichiesta : DBObject, IHasId<int>
{
    [PrimaryKey]
    [AutoIncrement]
    [Alias("IDARICHIESTA")]                
    public int Id { get; set; }
        
    [Alias("APAZIENTEID")]
    [ForeignKey(typeof(LRAPaziente))]
    public int PazienteId { get; set; }

    [Index(Unique = false)]
    [Alias("DATAORAACCETTAZIONE")]                        
    public DateTime DataOraAccettazione { get; set; }

    [Alias("ETAPAZIENTE")]
    public int? EtaPaziente { get; set; }

    [Alias("UNITADIMISURAETAPAZIENTE")]
    [Default((int) UnitaMisuraEtaPaziente.Anni)]
    public int UnitaDiMisuraEtaPaziente { get; set; }

    [Alias("SETTIMANEGRAVIDANZA")]               
    public int? SettimaneGravidanza { get; set; }
        
    [Alias("DREPARTOID")]
    [References(typeof(LRDReparto))]
    public int? RepartoId { get; set; }
        
    [Required]
    [Alias("DPRIORITAID")]
    [References(typeof(LRDPriorita))]        
    public int PrioritaId { get; set; }

    [Required]
    [References(typeof(LRDLaboratorio))]
    [Alias("DLABORATORIORICHIEDENTEID")]        
    public int? LaboratorioRichiedenteId { get; set; }
        
    [Alias("COMMENTO")]
    [StringLength(StringLengthAttribute.MaxText)]
    public string Commento { get; set; }
}
    
[Alias("LRAPAZIENTI")]
public class LRAPaziente : DBObject, IHasId<int>
{
    [PrimaryKey]
    [AutoIncrement]
    [Alias("IDAPAZIENTE")]               
    public int Id { get; set; }
               
    [Alias("SESSO")]
    [Default((int) SessiPaziente.NonDichiarato)]
    public int Sesso { get; set; }

    [Alias("DATADINASCITA")]
    public DateTime? DataDiNascita { get; set; }
}

public class LRDReparto
{
    public int Id { get; set; }
}

public class LRDPriorita
{
    public int Id { get; set; }
}

public class LRDLaboratorio
{
    public int Id { get; set; }
}

public class LRDDevice
{
    public int Id { get; set; }
}

public class LRDConnessione
{
    public int Id { get; set; }

    public int DeviceId { get; set; }
        
    public int StatoConnessione { get; set; }
}

public class LRAAlert
{
    public int Id { get; set; }

    public int DeviceID { get; set; }
        
    public int Visto { get; set; }
        
    public int Tipo { get; set; }
}
    
public static class SiNo
{
    public static int No { get; set; }
        
    public static int Si { get; set; }
}
    
public static class TipoAlert
{
    public static int AlertQC { get; set; }
        
    public static int WarningDevice { get; set; }
}

public class DeviceDashboardDTO
{
    public int DeviceId { get; set; }
    public int StatoLinea { get; set; }
    public int StatoQC { get; set; }
    public int StatoWarning { get; set; }
}
    
[Alias("LRARISULTATI")]
[CompositeIndex("AANALISIID", "DRISULTATOID", "STATO", Unique = false, Name = "IDXLRARISULTATI")]
public class LRARisultato : DBObject, IHasId<int>
{
    [PrimaryKey]
    [AutoIncrement]
    [Alias("IDARISULTATO")]
    public int Id { get; set; }

    [Index]
    [Alias("AANALISIID")]
    public int AnalisiId { get; set; }

    [Required]
    [Alias("TIPOVALORE")]
    public int TipoValore { get; set; }

    [Alias("OPERATORERELAZIONALE")]
    public string OperatoreRelazionale { get; set; }

    [Alias("VALORENUMERICO")]
    public decimal? ValoreNumerico { get; set; }

    [Index]
    [Alias("DTESTOCODIFICATOID")]
    public int? TestoCodificatoId { get; set; }

    [Alias("TESTOLIBERO")]
    [StringLength(StringLengthAttribute.MaxText)]
    public string TestoLibero { get; set; }

    [Index]
    [Alias("DRISULTATOID")]
    public int RisultatoId { get; set; }

    [Required]
    [Alias("STATO")]
    public int Stato { get; set; }

    [Alias("INVIAREALIS")]
    public int InviareALIS { get; set; }

    [Alias("RISULTATOPRINCIPALE")]
    public int RisultatoPrincipale { get; set; }

    [Alias("TIPOINSERIMENTO")]
    public int TipoInserimento { get; set; }

    [Index]
    [Alias("DOPERATOREINSERIMENTOID")]
    public int? OperatoreInserimentoId { get; set; }

    [Index]
    [Alias("DDEVICEID")]
    public int? DeviceId { get; set; }

    [Index]
    [Alias("DLABORATORIOESECUTOREID")]
    public int? LaboratorioEsecutoreId { get; set; }

    [Alias("CITRATO")]
    public int Citrato { get; set; }

    [Alias("DATAORARIPETIZIONE")]
    public DateTime? DataOraRipetizione { get; set; }

    [Alias("DATAORAESECUZIONE")]
    public DateTime? DataOraEsecuzione { get; set; }

    [Alias("DATAORARICEZIONE")]
    public DateTime DataOraRicezione { get; set; }

    [Alias("IDENTIFICATIVODEVICE")]
    public string IdentificativoDevice { get; set; }

    [Alias("POSIZIONESUDEVICE")]
    public string PosizioneSuDevice { get; set; }

    [Alias("REAGENTE")]
    public string Reagente { get; set; }

    [Alias("LOTTOREAGENTE")]
    public string LottoReagente { get; set; }

    [Alias("DATASCADENZAREAGENTE")]
    public DateTime? DataScadenzaReagente { get; set; }

    [Alias("CURVADICALIBRAZIONE")]
    public string CurvaDiCalibrazione { get; set; }

    [Index]
    [Alias("DDILUIZIONEID")]
    public int? DiluizioneId { get; set; }

    [Index]
    [Alias("DRANGENORMALITAID")]
    public int? RangeNormalitaId { get; set; }

    [Index]
    [Alias("DRANGECONVALIDAID")]
    public int? RangeConvalidaId { get; set; }

    [Index]
    [Alias("DDELTACHECKSTORICOID")]
    public int? DeltaCheckStoricoId { get; set; }

    [Index]
    [Alias("DDELTACHECKROUTINEID")]
    public int? DeltaCheckRoutineId { get; set; }

    [Index]
    [Alias("DREGOLACONVALIDAID")]
    public int? RegolaConvalidaId { get; set; }

    [Alias("COMMENTO")]
    [StringLength(StringLengthAttribute.MaxText)]
    public string Commento { get; set; }

    [StringLength(250)]
    [Alias("RISULTATORAW")]
    public string RisultatoRaw { get; set; }

    [Index]
    [References(typeof(LRARisultato))]
    [Alias("ADELTARISULTATOSTORICODID")]
    public int? DeltaRisultatoStoricoId { get; set; }

    [Index]
    [References(typeof(LRARisultato))]
    [Alias("ADELTARISULTATOPRECEDENTEID")]
    public int? DeltaRisultatoPrecedenteId { get; set; }
}
    
[Alias("LRDPROFILOANALISI")]
[CompositeIndex("DPROFILOANALISIID", "DANALISIID", Unique = true, Name = "IDXPROFILO")]
public class LRDProfiloAnalisi : DBObject, IHasId<int>
{
    [Alias("IDDPROFILOANALISI")]
    [AutoIncrement]
    [PrimaryKey]
    public int Id { get; set; }

    [ApiMember(Description = "Analisi profilo a cui appartiene l'analisi")]
    [Alias("DPROFILOANALISIID")]
    [References(typeof(LRDProfiloAnalisi))]
    public int? ProfiloAnalisiId { get; set; } // dove NULL allora DANALISIID e' l'analisi profilo

    [ApiMember(Description = "Analisi dal dizionario")]
    [Alias("DANALISIID")]
    [References(typeof(LRDAnalisi))]
    public int AnalisiId { get; set; }
}
    
[Alias("LRDCONTENITORI")]
public class LRDContenitore : DBObject, IHasId<int>
{
    private const int CColore = 7; // lunghezza colore HTML es. #AABBCC
    private const int CPrefisso = 5;

    [Alias("IDDCONTENITORE")]
    [AutoIncrement]
    [PrimaryKey]
    public int Id { get; set; }

    [Alias("CODICE")]
    [Required]
    [Index(Unique = true)]
    public string Codice { get; set; }

    [Required]
    [Alias("DESCRIZIONE")]
    public string Descrizione { get; set; }

    [Alias("DESCRIZIONEESTESA")]
    public string DescrizioneEstesa { get; set; }

    [Alias("ORDINE")]
    [Required]
    public int Ordine { get; set; }

    [Required]
    [Alias("TIPOCONTENITORE")]
    public int TipoContenitore { get; set; }

    [Alias("COLORE")]
    [StringLength(CColore)]
    public string Colore { get; set; }

    [Alias("PREFISSO")]
    [StringLength(CPrefisso)]
    public string Prefisso { get; set; }

    [Alias("PROGRESSIVOBARCODEMIN")]
    [DecimalLength(30, 0)]
    public decimal ProgressivoBarcodeMin { get; set; }

    [Alias("PROGRESSIVOBARCODEMAX")]
    [DecimalLength(30, 0)]
    [Default(int.MaxValue)]
    public decimal ProgressivoBarcodeMax { get; set; }

    [Alias("DMATERIALEID")]
    public int? MaterialeId { get; set; }

    [Alias("DETICHETTAID")]
    public int? EtichettaId { get; set; }

    [Required]
    [Alias("EMATOLOGIA")]
    public int Ematologia { get; set; }

    [Required]
    [Alias("URINE")]
    public int Urine { get; set; }
}    

[Alias("LRDANALISI")]
public class LRDAnalisi : DBObject, IHasId<int>
{
    [Alias("IDDANALISI")]
    [AutoIncrement]
    [PrimaryKey]
    public int Id { get; set; }

    [Alias("CODICE")]
    [Required]
    [Index(Unique = true)]
    public string Codice { get; set; }

    [Alias("DESCRIZIONE")]
    [Required]
    public string Descrizione { get; set; }

    [Alias("DESCRIZIONEESTESA")]
    public string DescrizioneEstesa { get; set; }

    [Alias("CODICEREGIONALE")]
    public string CodiceRegionale { get; set; }

    [Alias("DCONTENITOREID")]
    public int ContenitoreId { get; set; }

    [Alias("ORDINE")]
    public int Ordine { get; set; }

    [Alias("DMETODOID")]
    public int? MetodoId { get; set; }

    [Alias("DPANNELLOANALISIID")]
    public int? PannelloAnalisiId { get; set; }

    [Alias("DCLASSEANALISIID")]
    public int? ClasseAnalisiId { get; set; }

    [Alias("QCREGISTRAZIONERISULTATI")]
    public int QCRegistrazioneRisultati { get; set; }

    [Alias("QCVERIFICA")]
    public int QCVerifica { get; set; }

    [Alias("QCOREINTERVALLOVERIFICA")]
    public int? QCOreIntervalloVerifica { get; set; }
}