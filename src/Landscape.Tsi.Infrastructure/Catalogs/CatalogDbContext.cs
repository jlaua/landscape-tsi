using Landscape.Tsi.Domain.Catalogs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Linq.Expressions;

namespace Landscape.Tsi.Infrastructure.Catalogs;

public sealed class CatalogDbContext(DbContextOptions<CatalogDbContext> options) : DbContext(options)
{
    public DbSet<TmDominio> Domains => Set<TmDominio>();
    public DbSet<TBuildingBlock> BuildingBlocks => Set<TBuildingBlock>();
    public DbSet<TCapacidadSeguridad> Capabilities => Set<TCapacidadSeguridad>();
    public DbSet<TMEstadoCapacidad> CapabilityStates => Set<TMEstadoCapacidad>();
    public DbSet<TFuncionalidad> Functionalities => Set<TFuncionalidad>();
    public DbSet<TMEstadoFuncionalidad> FunctionalityStates => Set<TMEstadoFuncionalidad>();
    public DbSet<TEstadoFaseAdopcion> AdoptionPhases => Set<TEstadoFaseAdopcion>();
    public DbSet<TTecnologiaTSI> Technologies => Set<TTecnologiaTSI>();
    public DbSet<TMFamilia> Families => Set<TMFamilia>();
    public DbSet<TCasosDeUso> UseCases => Set<TCasosDeUso>();
    public DbSet<TEmpresaSubsidiaria> Companies => Set<TEmpresaSubsidiaria>();
    public DbSet<TCiso> Cisos => Set<TCiso>();
    public DbSet<TMPosturaRoadmap> RoadmapPostures => Set<TMPosturaRoadmap>();
    public DbSet<TMEstadoAdopcionTSI> TechnologyAdoptionStates => Set<TMEstadoAdopcionTSI>();
    public DbSet<TModalidadLaboral> WorkModes => Set<TModalidadLaboral>();
    public DbSet<TTipoOperacion> OperationTypes => Set<TTipoOperacion>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        Map<TmDominio>(b, "TMDominio", "iddominio", (e, p) => { p(e, x => x.Dominio, "dominio"); p(e, x => x.DescripcionDominio, "descripcionDominio"); p(e, x => x.Referencias, "referencias"); p(e, x => x.HomologacionDimensionSegunCiber, "homologacionDimensionSegunCiber"); p(e, x => x.HomologacionDimensionSegunLineamiento, "homologacionDimensionSegunLineamiento"); p(e, x => x.SubDominioCvt, "subDominioCVT"); p(e, x => x.Ejemplos, "Ejemplos"); });
        Map<TBuildingBlock>(b, "TBuildingBlock", "idBuildingBlock", (e, p) => { p(e, x => x.IdDominio, "idDominio"); p(e, x => x.Nombre, "nombreBuildingBlock"); p(e, x => x.Definicion, "definicionBuildingBlock"); p(e, x => x.IdFase, "idEstadoFaseDeAdopcionBuildingBlock"); p(e, x => x.Ruta, "rutaDelEntregable"); p(e, x => x.Pilar, "PilarZT"); });
        Map<TCapacidadSeguridad>(b, "TCapacidadDeSeguridad", "idCapacidad", (e, p) => { p(e, x => x.IdBuildingBlock, "idBuildingBlock"); p(e, x => x.Nombre, "nombreCapacidad"); p(e, x => x.Descripcion, "descripcionCapacidad"); p(e, x => x.IdEstado, "idEstadoCapacidad"); });
        Map<TMEstadoCapacidad>(b, "TMEstadoCapacidad", "idEstadoCapacidad", (e, p) => { p(e, x => x.Nombre, "nombreEstadoCapacidad"); p(e, x => x.Descripcion, "descripcionEstadoCapacidad"); });
        Map<TFuncionalidad>(b, "TFuncionalidad", "idFuncionalidad", (e, p) => { p(e, x => x.IdCapacidad, "idCapacidad"); p(e, x => x.Nombre, "nombreFuncionalidad"); p(e, x => x.Descripcion, "descripcionFuncionalidad"); p(e, x => x.IdEstado, "idEstadoCoberturaFuncionalidad"); });
        Map<TMEstadoFuncionalidad>(b, "TMEstadoFuncionalidad", "idEstadoCoberturaFuncionalidad", (e, p) => { p(e, x => x.Nombre, "nombreEstadoFuncionalidad"); p(e, x => x.Descripcion, "descripcionEstadoFuncionalidad"); });
        Map<TEstadoFaseAdopcion>(b, "TEstadoFaseAdopcion", "idEstadoFaseAdopcion", (e, p) => { p(e, x => x.Nombre, "nombreFaseAdopcion"); p(e, x => x.Descripcion, "descripcionFaseAdopcion"); });
        Map<TTecnologiaTSI>(b, "TTecnologiaTSI", "idTecnologiaTSI", (e, p) => { p(e, x => x.NombreCorporativo, "nombreTecnologiaAlternativa1-Corporativo"); p(e, x => x.NombreLocal, "nombreTecnologiaAlternativa2-Local"); p(e, x => x.IdFamilia, "idFamilia"); p(e, x => x.Grupo, "grupoQpertenece"); p(e, x => x.IdEstadoAdopcion, "idEstadoAdopcionTSI"); p(e, x => x.IdPostura, "idPosturaResumenRoadmap"); p(e, x => x.FechaEvaluacion, "fechaCompromisoEvaluacionCorporativo", "datetime2(3)"); p(e, x => x.FechaFinContrato, "fechaFinDeContratoMasCercanaCorporativo", "datetime2(3)"); p(e, x => x.FechaAdjudicacion, "fechaDeAdjudicacionCorporativo", "datetime2(3)"); p(e, x => x.Licenciamiento, "modeloEsquemaLicenciamientoSubscripcion"); p(e, x => x.Entorno, "entornoImplementacion"); p(e, x => x.Referencia, "linkReferencia-Fuente"); p(e, x => x.Responsable, "responsableTecnologiaTSI"); p(e, x => x.UnidadResponsable, "unidadResponsableTecnologiaTSI"); p(e, x => x.CategoriaAsIs, "categoriaAS-IS"); p(e, x => x.Fuente, "flagFuente"); });
        Map<TMFamilia>(b, "TMFamilia", "idFamilia", (e, p) => { p(e, x => x.Nombre, "nombreFamilia"); p(e, x => x.Descripcion, "descripcionFamilia"); });
        Map<TCasosDeUso>(b, "TCasosDeUso", "idCasosDeUso", (e, p) => { p(e, x => x.IdTecnologia, "idTecnologiaTSI"); p(e, x => x.Nombre, "casoDeUso"); p(e, x => x.Descripcion, "descripcionCasoDeUso"); });
        Map<TEmpresaSubsidiaria>(b, "TEmpresaSubsidiaria", "idEmpresaSubsidiaria", (e, p) => { p(e, x => x.Nombre, "nombreEmpresa"); p(e, x => x.Alias2, "alias2"); p(e, x => x.Agrupador, "alias3-agrupador"); p(e, x => x.Pais, "Pais"); p(e, x => x.Ciudad, "ciudad"); p(e, x => x.Rubro, "Rubro"); p(e, x => x.ContactoCiso, "contactoCiso"); });
        Map<TCiso>(b, "TCISO", "idCiso", (e, p) => { p(e, x => x.IdEmpresa, "idEmpresaSubsidiaria"); p(e, x => x.Nombre, "nombreCISO"); p(e, x => x.Email, "email"); p(e, x => x.Telefono, "telefono"); p(e, x => x.Otro, "otro"); p(e, x => x.LineaDeNegocio, "LineaDeNegocio"); p(e, x => x.Representante, "Representante"); });
        Map<TMPosturaRoadmap>(b, "TMPosturaRoadmap", "idPosturaResumenRoadmap", (e, p) => { p(e, x => x.Nombre, "nombrePosturaRoadmap"); p(e, x => x.Descripcion, "descripcionPosturaRoadmap"); });
        Map<TMEstadoAdopcionTSI>(b, "TMEstadoAdopcionTSI", "idEstadoAdopcionTSI", (e, p) => { p(e, x => x.Nombre, "nombreEstadoAdopcionTSI"); p(e, x => x.Descripcion, "descripcionEstadoAdopcionTSI"); });
        Map<TModalidadLaboral>(b, "TModalidadLaboral", "idModalidadLaboral", (e, p) => { p(e, x => x.Nombre, "TipoModalidadLaboral"); p(e, x => x.Descripcion, "descripcion"); });
        Map<TTipoOperacion>(b, "TTipoOperacion", "idTipoModeloOperacion", (e, p) => { p(e, x => x.Nombre, "TipoModeloDeOperacion"); p(e, x => x.Descripcion, "Descripcion"); });

        b.Entity<TBuildingBlock>().HasOne<TmDominio>().WithMany().HasForeignKey(x => x.IdDominio).OnDelete(DeleteBehavior.NoAction);
        b.Entity<TBuildingBlock>().HasOne<TEstadoFaseAdopcion>().WithMany().HasForeignKey(x => x.IdFase).OnDelete(DeleteBehavior.NoAction);
        b.Entity<TCapacidadSeguridad>().HasOne<TBuildingBlock>().WithMany().HasForeignKey(x => x.IdBuildingBlock).OnDelete(DeleteBehavior.NoAction);
        b.Entity<TCapacidadSeguridad>().HasOne<TMEstadoCapacidad>().WithMany().HasForeignKey(x => x.IdEstado).OnDelete(DeleteBehavior.NoAction);
        b.Entity<TFuncionalidad>().HasOne<TCapacidadSeguridad>().WithMany().HasForeignKey(x => x.IdCapacidad).OnDelete(DeleteBehavior.NoAction);
        b.Entity<TFuncionalidad>().HasOne<TMEstadoFuncionalidad>().WithMany().HasForeignKey(x => x.IdEstado).OnDelete(DeleteBehavior.NoAction);
        b.Entity<TTecnologiaTSI>().HasOne<TMFamilia>().WithMany().HasForeignKey(x => x.IdFamilia).OnDelete(DeleteBehavior.NoAction);
        b.Entity<TTecnologiaTSI>().HasOne<TMEstadoAdopcionTSI>().WithMany().HasForeignKey(x => x.IdEstadoAdopcion).OnDelete(DeleteBehavior.NoAction);
        b.Entity<TTecnologiaTSI>().HasOne<TMPosturaRoadmap>().WithMany().HasForeignKey(x => x.IdPostura).OnDelete(DeleteBehavior.NoAction);
        b.Entity<TCasosDeUso>().HasOne<TTecnologiaTSI>().WithMany().HasForeignKey(x => x.IdTecnologia).OnDelete(DeleteBehavior.NoAction);
        b.Entity<TCiso>().HasOne<TEmpresaSubsidiaria>().WithMany().HasForeignKey(x => x.IdEmpresa).OnDelete(DeleteBehavior.NoAction);
    }

    private delegate void ColumnMap<TEntity>(EntityTypeBuilder<TEntity> entity, Expression<Func<TEntity, object?>> property, string name, string? type = null) where TEntity : class;

    private static void Map<TEntity>(ModelBuilder b, string table, string key, Action<EntityTypeBuilder<TEntity>, ColumnMap<TEntity>> columns) where TEntity : MasterCatalogEntity
    {
        var e = b.Entity<TEntity>(); e.ToTable(table, "dbo", t => t.ExcludeFromMigrations()); e.HasKey(x => x.Id); e.Property(x => x.Id).HasColumnName(key).ValueGeneratedOnAdd();
        columns(e, static (entity, property, name, type) => { var p = entity.Property(property).HasColumnName(name); if (type is not null) p.HasColumnType(type); else if (property.Body is MemberExpression) p.HasColumnType("nvarchar(max)"); });
    }
}
