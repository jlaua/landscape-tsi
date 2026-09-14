using System.Linq.Expressions;

using Landscape.Tsi.Domain.Adoption;
using Landscape.Tsi.Domain.Catalogs;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

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

    // Entidades de Adopción y Operación
    public DbSet<TProcesoAdopcionTSI> AdoptionProcesses => Set<TProcesoAdopcionTSI>();
    public DbSet<TProcesoAdopcionEmpresa> AdoptionProcessCompanies => Set<TProcesoAdopcionEmpresa>();
    public DbSet<TEstandarTecnologiaHistorico> StandardTechnologyHistories => Set<TEstandarTecnologiaHistorico>();
    public DbSet<TContratoTecnologia> TechnologyContracts => Set<TContratoTecnologia>();
    public DbSet<TTecnologiaTSIimplementadaSubsidiaria> ImplementedTechnologies => Set<TTecnologiaTSIimplementadaSubsidiaria>();
    public DbSet<TDriver> Drivers => Set<TDriver>();

    // Entidades de Servicios y Tarifarios
    public DbSet<TTipoServicio> ServiceTypes => Set<TTipoServicio>();
    public DbSet<TServicioTecnologia> TechnologyServices => Set<TServicioTecnologia>();
    public DbSet<TTarifarioProyectoHoras> ProjectRateCards => Set<TTarifarioProyectoHoras>();
    public DbSet<TActividadNivelSoporte> SupportLevelActivities => Set<TActividadNivelSoporte>();
    public DbSet<TTarifarioOperacion> OperationRateCards => Set<TTarifarioOperacion>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        Map<TmDominio>(b, "TMDominio", "iddominio", (e, p) => { p(e, x => x.Dominio, "dominio"); p(e, x => x.DescripcionDominio, "descripcionDominio"); p(e, x => x.Referencias, "referencias"); p(e, x => x.HomologacionDimensionSegunCiber, "homologacionDimensionSegunCiber"); p(e, x => x.HomologacionDimensionSegunLineamiento, "homologacionDimensionSegunLineamiento"); p(e, x => x.SubDominioCvt, "subDominioCVT"); p(e, x => x.Ejemplos, "Ejemplos"); });
        Map<TBuildingBlock>(b, "TBuildingBlock", "idBuildingBlock", (e, p) => { p(e, x => x.IdDominio, "idDominio"); p(e, x => x.Nombre, "nombreBuildingBlock"); p(e, x => x.Definicion, "definicionBuildingBlock"); p(e, x => x.IdFase, "idEstadoFaseDeAdopcionBuildingBlock"); p(e, x => x.Ruta, "rutaDelEntregable"); p(e, x => x.Pilar, "PilarZT"); p(e, x => x.IdFamilia, "idFamilia"); });
        Map<TCapacidadSeguridad>(b, "TCapacidadDeSeguridad", "idCapacidad", (e, p) => { p(e, x => x.IdBuildingBlock, "idBuildingBlock"); p(e, x => x.Nombre, "nombreCapacidad"); p(e, x => x.Descripcion, "descripcionCapacidad"); p(e, x => x.IdEstado, "idEstadoCapacidad"); });
        Map<TMEstadoCapacidad>(b, "TMEstadoCapacidad", "idEstadoCapacidad", (e, p) => { p(e, x => x.Nombre, "nombreEstadoCapacidad"); p(e, x => x.Descripcion, "descripcionEstadoCapacidad"); });
        Map<TFuncionalidad>(b, "TFuncionalidad", "idFuncionalidad", (e, p) => { p(e, x => x.IdCapacidad, "idCapacidad"); p(e, x => x.Nombre, "nombreFuncionalidad"); p(e, x => x.Descripcion, "descripcionFuncionalidad"); p(e, x => x.IdEstado, "idEstadoCoberturaFuncionalidad"); });
        Map<TMEstadoFuncionalidad>(b, "TMEstadoFuncionalidad", "idEstadoCoberturaFuncionalidad", (e, p) => { p(e, x => x.Nombre, "nombreEstadoFuncionalidad"); p(e, x => x.Descripcion, "descripcionEstadoFuncionalidad"); });
        Map<TEstadoFaseAdopcion>(b, "TEstadoFaseAdopcion", "idEstadoFaseAdopcion", (e, p) => { p(e, x => x.Nombre, "nombreFaseAdopcion"); p(e, x => x.Descripcion, "descripcionFaseAdopcion"); });
        Map<TTecnologiaTSI>(b, "TTecnologiaTSI", "idTecnologiaTSI", (e, p) => { p(e, x => x.NombreCorporativo, "nombreTecnologiaAlternativa1-Corporativo"); p(e, x => x.NombreLocal, "nombreTecnologiaAlternativa2-Local"); p(e, x => x.IdFamilia, "idFamilia"); p(e, x => x.Grupo, "grupoQpertenece"); p(e, x => x.IdEstadoAdopcion, "idEstadoAdopcionTSI"); p(e, x => x.IdPostura, "idPosturaResumenRoadmap"); p(e, x => x.FechaEvaluacion, "fechaCompromisoEvaluacionCorporativo", "datetime2(3)"); p(e, x => x.FechaFinContrato, "fechaFinDeContratoMasCercanaCorporativo", "datetime2(3)"); p(e, x => x.FechaAdjudicacion, "fechaDeAdjudicacionCorporativo", "datetime2(3)"); p(e, x => x.Licenciamiento, "modeloEsquemaLicenciamientoSubscripcion"); p(e, x => x.Entorno, "entornoImplementacion"); p(e, x => x.Referencia, "linkReferencia-Fuente"); p(e, x => x.Responsable, "responsableTecnologiaTSI"); p(e, x => x.UnidadResponsable, "unidadResponsableTecnologiaTSI"); p(e, x => x.CategoriaAsIs, "categoriaAS-IS"); p(e, x => x.Fuente, "flagFuente"); });
        Map<TMFamilia>(b, "TMFamilia", "idFamilia", (e, p) => { p(e, x => x.Nombre, "nombreFamilia"); p(e, x => x.Descripcion, "descripcionFamilia"); });
        Map<TCasosDeUso>(b, "TCasosDeUso", "idCasosDeUso", (e, p) => { p(e, x => x.IdTecnologia, "idTecnologiaTSI"); p(e, x => x.Nombre, "casoDeUso"); p(e, x => x.Descripcion, "descripcionCasoDeUso"); p(e, x => x.IdEstandarTecnologia, "idEstandarTecnologia"); });
        Map<TEmpresaSubsidiaria>(b, "TEmpresaSubsidiaria", "idEmpresaSubsidiaria", (e, p) => { p(e, x => x.Nombre, "nombreEmpresa"); p(e, x => x.Alias2, "alias2"); p(e, x => x.Agrupador, "alias3-agrupador"); p(e, x => x.Pais, "Pais"); p(e, x => x.Ciudad, "ciudad"); p(e, x => x.Rubro, "Rubro"); p(e, x => x.ContactoCiso, "contactoCiso"); });
        Map<TCiso>(b, "TCISO", "idCiso", (e, p) => { p(e, x => x.IdEmpresa, "idEmpresaSubsidiaria"); p(e, x => x.Nombre, "nombreCISO"); p(e, x => x.Email, "email"); p(e, x => x.Telefono, "telefono"); p(e, x => x.Otro, "otro"); p(e, x => x.LineaDeNegocio, "LineaDeNegocio"); p(e, x => x.Representante, "Representante"); });
        Map<TMPosturaRoadmap>(b, "TMPosturaRoadmap", "idPosturaResumenRoadmap", (e, p) => { p(e, x => x.Nombre, "nombrePosturaRoadmap"); p(e, x => x.Descripcion, "descripcionPosturaRoadmap"); });
        Map<TMEstadoAdopcionTSI>(b, "TMEstadoAdopcionTSI", "idEstadoAdopcionTSI", (e, p) => { p(e, x => x.Nombre, "nombreEstadoAdopcionTSI"); p(e, x => x.Descripcion, "descripcionEstadoAdopcionTSI"); });
        Map<TModalidadLaboral>(b, "TModalidadLaboral", "idModalidadLaboral", (e, p) => { p(e, x => x.Nombre, "TipoModalidadLaboral"); p(e, x => x.Descripcion, "descripcion"); });
        Map<TTipoOperacion>(b, "TTipoOperacion", "idTipoModeloOperacion", (e, p) => { p(e, x => x.Nombre, "TipoModeloDeOperacion"); p(e, x => x.Descripcion, "Descripcion"); });

        b.Entity<TBuildingBlock>().HasOne<TmDominio>().WithMany().HasForeignKey(x => x.IdDominio).OnDelete(DeleteBehavior.NoAction);
        b.Entity<TBuildingBlock>().HasOne<TEstadoFaseAdopcion>().WithMany().HasForeignKey(x => x.IdFase).OnDelete(DeleteBehavior.NoAction);
        b.Entity<TBuildingBlock>().HasOne<TMFamilia>().WithMany().HasForeignKey(x => x.IdFamilia).OnDelete(DeleteBehavior.NoAction);
        b.Entity<TCapacidadSeguridad>().HasOne<TBuildingBlock>().WithMany().HasForeignKey(x => x.IdBuildingBlock).OnDelete(DeleteBehavior.NoAction);
        b.Entity<TCapacidadSeguridad>().HasOne<TMEstadoCapacidad>().WithMany().HasForeignKey(x => x.IdEstado).OnDelete(DeleteBehavior.NoAction);
        b.Entity<TFuncionalidad>().HasOne<TCapacidadSeguridad>().WithMany().HasForeignKey(x => x.IdCapacidad).OnDelete(DeleteBehavior.NoAction);
        b.Entity<TFuncionalidad>().HasOne<TMEstadoFuncionalidad>().WithMany().HasForeignKey(x => x.IdEstado).OnDelete(DeleteBehavior.NoAction);
        b.Entity<TTecnologiaTSI>().HasOne<TMFamilia>().WithMany().HasForeignKey(x => x.IdFamilia).OnDelete(DeleteBehavior.NoAction);
        b.Entity<TTecnologiaTSI>().HasOne<TMEstadoAdopcionTSI>().WithMany().HasForeignKey(x => x.IdEstadoAdopcion).OnDelete(DeleteBehavior.NoAction);
        b.Entity<TTecnologiaTSI>().HasOne<TMPosturaRoadmap>().WithMany().HasForeignKey(x => x.IdPostura).OnDelete(DeleteBehavior.NoAction);
        b.Entity<TCasosDeUso>().HasOne<TTecnologiaTSI>().WithMany().HasForeignKey(x => x.IdTecnologia).OnDelete(DeleteBehavior.NoAction);
        b.Entity<TCiso>().HasOne<TEmpresaSubsidiaria>().WithMany().HasForeignKey(x => x.IdEmpresa).OnDelete(DeleteBehavior.NoAction);

        // Mapeo TProcesoAdopcionTSI
        b.Entity<TProcesoAdopcionTSI>(entity =>
        {
            entity.ToTable("TProcesoAdopcionTSI", "dbo", t => t.ExcludeFromMigrations());
            entity.HasKey(x => x.IdProcesoAdopcionTSI);
            entity.Property(x => x.IdProcesoAdopcionTSI).HasColumnName("idProcesoAdopcionTSI").ValueGeneratedOnAdd();
            entity.Property(x => x.CodigoProceso).HasColumnName("codigoProceso").HasMaxLength(50);
            entity.Property(x => x.NombreProceso).HasColumnName("nombreProceso").HasMaxLength(200);
            entity.Property(x => x.IdBuildingBlock).HasColumnName("idBuildingBlock");
            entity.Property(x => x.IdEstadoAdopcionTSI).HasColumnName("idEstadoAdopcionTSI");
            entity.Property(x => x.Objetivo).HasColumnName("objetivo").HasMaxLength(1000);
            entity.Property(x => x.Alcance).HasColumnName("alcance").HasMaxLength(1000);
            entity.Property(x => x.LiderCorporativoTSI).HasColumnName("liderCorporativoTSI").HasMaxLength(150);
            entity.Property(x => x.FechaInicio).HasColumnName("fechaInicio").HasColumnType("date");
            entity.Property(x => x.FechaEstimadaCierre).HasColumnName("fechaEstimadaCierre").HasColumnType("date");
            entity.Property(x => x.FechaCreacion).HasColumnName("fechaCreacion").HasColumnType("datetime2(0)");
            entity.Property(x => x.UsuarioCreacion).HasColumnName("usuarioCreacion").HasMaxLength(100);

            entity.HasOne<TBuildingBlock>().WithMany().HasForeignKey(x => x.IdBuildingBlock).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne<TMEstadoAdopcionTSI>().WithMany().HasForeignKey(x => x.IdEstadoAdopcionTSI).OnDelete(DeleteBehavior.NoAction);
        });

        // Mapeo TProcesoAdopcionEmpresa
        b.Entity<TProcesoAdopcionEmpresa>(entity =>
        {
            entity.ToTable("TProcesoAdopcionEmpresa", "dbo", t => t.ExcludeFromMigrations());
            entity.HasKey(x => x.IdProcesoAdopcionEmpresa);
            entity.Property(x => x.IdProcesoAdopcionEmpresa).HasColumnName("idProcesoAdopcionEmpresa").ValueGeneratedOnAdd();
            entity.Property(x => x.IdProcesoAdopcionTSI).HasColumnName("idProcesoAdopcionTSI");
            entity.Property(x => x.IdEmpresaSubsidiaria).HasColumnName("idEmpresaSubsidiaria");
            entity.Property(x => x.IdContactoEmpresaSubsidiaria).HasColumnName("idContactoEmpresaSubsidiaria");
            entity.Property(x => x.Aplica).HasColumnName("aplica");
            entity.Property(x => x.JustificacionNoAplica).HasColumnName("justificacionNoAplica").HasMaxLength(1000);
            entity.Property(x => x.FechaIncorporacion).HasColumnName("fechaIncorporacion").HasColumnType("date");
            entity.Property(x => x.FechaModificacion).HasColumnName("fechaModificacion").HasColumnType("datetime2(0)");
            entity.Property(x => x.UsuarioModificacion).HasColumnName("usuarioModificacion").HasMaxLength(100);

            entity.HasOne<TProcesoAdopcionTSI>().WithMany().HasForeignKey(x => x.IdProcesoAdopcionTSI).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne<TEmpresaSubsidiaria>().WithMany().HasForeignKey(x => x.IdEmpresaSubsidiaria).OnDelete(DeleteBehavior.NoAction);
        });

        // Mapeo TEstandarTecnologiaHistorico
        b.Entity<TEstandarTecnologiaHistorico>(entity =>
        {
            entity.ToTable("TEstandarTecnologiaHistorico", "dbo", t => t.ExcludeFromMigrations());
            entity.HasKey(x => x.IdEstandarTecnologia);
            entity.Property(x => x.IdEstandarTecnologia).HasColumnName("idEstandarTecnologia").ValueGeneratedOnAdd();
            entity.Property(x => x.IdBuildingBlock).HasColumnName("idBuildingBlock");
            entity.Property(x => x.IdTecnologiaTSI).HasColumnName("idTecnologiaTSI");
            entity.Property(x => x.IdProcesoAdopcionTSI).HasColumnName("idProcesoAdopcionTSI");
            entity.Property(x => x.RolEstandar).HasColumnName("rolEstandar").HasMaxLength(30);
            entity.Property(x => x.EstadoVigencia).HasColumnName("estadoVigencia").HasMaxLength(30);
            entity.Property(x => x.FechaInicioVigencia).HasColumnName("fechaInicioVigencia").HasColumnType("date");
            entity.Property(x => x.FechaFinVigencia).HasColumnName("fechaFinVigencia").HasColumnType("date");
            entity.Property(x => x.MotivoCambio).HasColumnName("motivoCambio").HasMaxLength(1000);
            entity.Property(x => x.SustentoArquitectura).HasColumnName("sustentoArquitectura");
            entity.Property(x => x.FechaRegistro).HasColumnName("fechaRegistro").HasColumnType("datetime2(0)");
            entity.Property(x => x.UsuarioRegistro).HasColumnName("usuarioRegistro").HasMaxLength(100);

            entity.HasOne<TBuildingBlock>().WithMany().HasForeignKey(x => x.IdBuildingBlock).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne<TTecnologiaTSI>().WithMany().HasForeignKey(x => x.IdTecnologiaTSI).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne<TProcesoAdopcionTSI>().WithMany().HasForeignKey(x => x.IdProcesoAdopcionTSI).OnDelete(DeleteBehavior.NoAction);
        });

        // Mapeo TContratoTecnologia
        b.Entity<TContratoTecnologia>(entity =>
        {
            entity.ToTable("TContratoTecnologia", "dbo", t => t.ExcludeFromMigrations());
            entity.HasKey(x => x.IdContratoTecnologia);
            entity.Property(x => x.IdContratoTecnologia).HasColumnName("idContratoTecnologia").ValueGeneratedOnAdd();
            entity.Property(x => x.IdTecnologiaTSIimplementadaSubsidiaria).HasColumnName("idTecnologiaTSIimplementadaSubsidiaria");
            entity.Property(x => x.NumeroContrato).HasColumnName("numeroContrato").HasMaxLength(100);
            entity.Property(x => x.EsAdenda).HasColumnName("esAdenda");
            entity.Property(x => x.EsPayg).HasColumnName("esPayg");
            entity.Property(x => x.IdContratoPadre).HasColumnName("idContratoPadre");
            entity.Property(x => x.FechaInicio).HasColumnName("fechaInicio").HasColumnType("date");
            entity.Property(x => x.FechaFin).HasColumnName("fechaFin").HasColumnType("date");
            entity.Property(x => x.FechaAdjudicacion).HasColumnName("fechaAdjudicacion").HasColumnType("date");
            entity.Property(x => x.RutaDocumentoContrato).HasColumnName("rutaDocumentoContrato").HasMaxLength(500);
            entity.Property(x => x.MontoContratado).HasColumnName("montoContratado").HasColumnType("decimal(18,2)");
            entity.Property(x => x.Moneda).HasColumnName("moneda").HasMaxLength(10);
            entity.Property(x => x.Observaciones).HasColumnName("observaciones");
            entity.Property(x => x.FechaRegistro).HasColumnName("fechaRegistro").HasColumnType("datetime2(0)");
            entity.Property(x => x.UsuarioRegistro).HasColumnName("usuarioRegistro").HasMaxLength(100);

            entity.HasOne<TTecnologiaTSIimplementadaSubsidiaria>().WithMany().HasForeignKey(x => x.IdTecnologiaTSIimplementadaSubsidiaria).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne<TContratoTecnologia>().WithMany().HasForeignKey(x => x.IdContratoPadre).OnDelete(DeleteBehavior.NoAction);
        });

        // Mapeo TTecnologiaTSIimplementadaSubsidiaria
        b.Entity<TTecnologiaTSIimplementadaSubsidiaria>(entity =>
        {
            entity.ToTable("TTecnologiaTSIimplementadaSubsidiaria", "dbo", t => t.ExcludeFromMigrations());
            entity.HasKey(x => x.IdTecnologiaTSIimplementadaSubsidiaria);
            entity.Property(x => x.IdTecnologiaTSIimplementadaSubsidiaria).HasColumnName("idTecnologiaTSIimplementadaSubsidiaria").ValueGeneratedOnAdd();
            entity.Property(x => x.IdEmpresaSubsidiaria).HasColumnName("idEmpresaSubsidiaria");
            entity.Property(x => x.IdTecnologiaTSI).HasColumnName("idTecnologiaTSI");
            entity.Property(x => x.IdBuildingBlock).HasColumnName("idBuildingBlock");
            entity.Property(x => x.IdProcesoAdopcionEmpresa).HasColumnName("idProcesoAdopcionEmpresa");
            entity.Property(x => x.EsTecnologiaPrimaria).HasColumnName("esTecnologiaPrimaria");
            entity.Property(x => x.EsInstanciaCorporativa).HasColumnName("esInstanciaCorporativa");
            entity.Property(x => x.VersionDesplegada).HasColumnName("versionDesplegada").HasMaxLength(50);

            entity.HasOne<TEmpresaSubsidiaria>().WithMany().HasForeignKey(x => x.IdEmpresaSubsidiaria).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne<TTecnologiaTSI>().WithMany().HasForeignKey(x => x.IdTecnologiaTSI).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne<TBuildingBlock>().WithMany().HasForeignKey(x => x.IdBuildingBlock).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne<TProcesoAdopcionEmpresa>().WithMany().HasForeignKey(x => x.IdProcesoAdopcionEmpresa).OnDelete(DeleteBehavior.NoAction);
        });

        // Mapeo TDriver
        b.Entity<TDriver>(entity =>
        {
            entity.ToTable("TDriver", "dbo", t => t.ExcludeFromMigrations());
            entity.HasKey(x => x.IdDriver);
            entity.Property(x => x.IdDriver).HasColumnName("idDriver").ValueGeneratedOnAdd();
            entity.Property(x => x.IdTecnologiaTSIimplementadaSubsidiaria).HasColumnName("idTecnologiaTSIimplementadaSubsidiaria");
            entity.Property(x => x.DescripcionDriver).HasColumnName("descripcionDriver");
            entity.Property(x => x.UnidadMedida).HasColumnName("unidadMedida").HasMaxLength(50);
            entity.Property(x => x.Cantidad).HasColumnName("cantidad").HasColumnType("decimal(18,2)");
            entity.Property(x => x.PrecioUnitario).HasColumnName("precioUnitario").HasColumnType("decimal(18,2)");
            entity.Property(x => x.Moneda).HasColumnName("moneda").HasMaxLength(10);

            entity.HasOne<TTecnologiaTSIimplementadaSubsidiaria>().WithMany().HasForeignKey(x => x.IdTecnologiaTSIimplementadaSubsidiaria).OnDelete(DeleteBehavior.NoAction);
        });

        // Mapeo TTipoServicio
        b.Entity<TTipoServicio>(entity =>
        {
            entity.ToTable("TTipoServicio", "dbo", t => t.ExcludeFromMigrations());
            entity.HasKey(x => x.IdTipoServicio);
            entity.Property(x => x.IdTipoServicio).HasColumnName("idTipoServicio").ValueGeneratedOnAdd();
            entity.Property(x => x.Codigo).HasColumnName("codigo").HasMaxLength(20);
            entity.Property(x => x.Nombre).HasColumnName("nombre").HasMaxLength(100);
            entity.Property(x => x.Descripcion).HasColumnName("descripcion").HasMaxLength(500);
            entity.Property(x => x.Orden).HasColumnName("orden");
            entity.Property(x => x.EsActivo).HasColumnName("esActivo");
        });

        // Mapeo TServicioTecnologia
        b.Entity<TServicioTecnologia>(entity =>
        {
            entity.ToTable("TServicioTecnologia", "dbo", t => t.ExcludeFromMigrations());
            entity.HasKey(x => x.IdServicio);
            entity.Property(x => x.IdServicio).HasColumnName("idServicio").ValueGeneratedOnAdd();
            entity.Property(x => x.CodigoServicio).HasColumnName("codigoServicio").HasMaxLength(50);
            entity.Property(x => x.NombreServicio).HasColumnName("nombreServicio").HasMaxLength(200);
            entity.Property(x => x.Descripcion).HasColumnName("descripcion").HasMaxLength(1000);
            entity.Property(x => x.IdTipoServicio).HasColumnName("idTipoServicio");
            entity.Property(x => x.IdTecnologiaTSI).HasColumnName("idTecnologiaTSI");
            entity.Property(x => x.IdTecnologiaTSIimplementadaSubsidiaria).HasColumnName("idTecnologiaTSIimplementadaSubsidiaria");
            entity.Property(x => x.IdEmpresaSubsidiaria).HasColumnName("idEmpresaSubsidiaria");
            entity.Property(x => x.IdProcesoAdopcionTSI).HasColumnName("idProcesoAdopcionTSI");
            entity.Property(x => x.IdVendor).HasColumnName("idVendor");
            entity.Property(x => x.NombreProveedorServicio).HasColumnName("nombreProveedorServicio").HasMaxLength(150);
            entity.Property(x => x.EstadoServicio).HasColumnName("estadoServicio").HasMaxLength(30);
            entity.Property(x => x.CostoTotalEstimado).HasColumnName("costoTotalEstimado").HasColumnType("decimal(18,2)");
            entity.Property(x => x.Moneda).HasColumnName("moneda").HasMaxLength(10);
            entity.Property(x => x.FechaCreacion).HasColumnName("fechaCreacion").HasColumnType("datetime2(0)");
            entity.Property(x => x.UsuarioCreacion).HasColumnName("usuarioCreacion").HasMaxLength(100);
            entity.Property(x => x.FechaModificacion).HasColumnName("fechaModificacion").HasColumnType("datetime2(0)");
            entity.Property(x => x.UsuarioModificacion).HasColumnName("usuarioModificacion").HasMaxLength(100);

            entity.HasOne<TTipoServicio>().WithMany().HasForeignKey(x => x.IdTipoServicio).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne<TTecnologiaTSI>().WithMany().HasForeignKey(x => x.IdTecnologiaTSI).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne<TTecnologiaTSIimplementadaSubsidiaria>().WithMany().HasForeignKey(x => x.IdTecnologiaTSIimplementadaSubsidiaria).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne<TEmpresaSubsidiaria>().WithMany().HasForeignKey(x => x.IdEmpresaSubsidiaria).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne<TProcesoAdopcionTSI>().WithMany().HasForeignKey(x => x.IdProcesoAdopcionTSI).OnDelete(DeleteBehavior.NoAction);
            entity.HasMany(x => x.TarifariosProyecto).WithOne().HasForeignKey(x => x.IdServicio).OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(x => x.TarifariosOperacion).WithOne().HasForeignKey(x => x.IdServicio).OnDelete(DeleteBehavior.Cascade);
        });

        // Mapeo TTarifarioProyectoHoras
        b.Entity<TTarifarioProyectoHoras>(entity =>
        {
            entity.ToTable("TTarifarioProyectoHoras", "dbo", t => t.ExcludeFromMigrations());
            entity.HasKey(x => x.IdTarifarioProyecto);
            entity.Property(x => x.IdTarifarioProyecto).HasColumnName("idTarifarioProyecto").ValueGeneratedOnAdd();
            entity.Property(x => x.IdServicio).HasColumnName("idServicio");
            entity.Property(x => x.Complejidad).HasColumnName("complejidad").HasMaxLength(50);
            entity.Property(x => x.RangoHorasDesde).HasColumnName("rangoHorasDesde");
            entity.Property(x => x.RangoHorasHasta).HasColumnName("rangoHorasHasta");
            entity.Property(x => x.TarifaHora).HasColumnName("tarifaHora").HasColumnType("decimal(18,2)");
            entity.Property(x => x.HorasEstimadas).HasColumnName("horasEstimadas").HasColumnType("decimal(10,2)");
            entity.Property(x => x.Subtotal).HasColumnName("subtotal").HasColumnType("decimal(18,2)");
            entity.Property(x => x.Moneda).HasColumnName("moneda").HasMaxLength(10);
            entity.Property(x => x.Observaciones).HasColumnName("observaciones").HasMaxLength(500);
        });

        // Mapeo TActividadNivelSoporte
        b.Entity<TActividadNivelSoporte>(entity =>
        {
            entity.ToTable("TActividadNivelSoporte", "dbo", t => t.ExcludeFromMigrations());
            entity.HasKey(x => x.IdActividadSoporte);
            entity.Property(x => x.IdActividadSoporte).HasColumnName("idActividadSoporte").ValueGeneratedOnAdd();
            entity.Property(x => x.NivelSoporte).HasColumnName("nivelSoporte").HasMaxLength(10);
            entity.Property(x => x.DescripcionActividad).HasColumnName("descripcionActividad").HasMaxLength(300);
            entity.Property(x => x.OrdenVisual).HasColumnName("ordenVisual");
            entity.Property(x => x.EsActivo).HasColumnName("esActivo");
        });

        // Mapeo TTarifarioOperacion
        b.Entity<TTarifarioOperacion>(entity =>
        {
            entity.ToTable("TTarifarioOperacion", "dbo", t => t.ExcludeFromMigrations());
            entity.HasKey(x => x.IdTarifarioOperacion);
            entity.Property(x => x.IdTarifarioOperacion).HasColumnName("idTarifarioOperacion").ValueGeneratedOnAdd();
            entity.Property(x => x.IdServicio).HasColumnName("idServicio");
            entity.Property(x => x.NivelSoporte).HasColumnName("nivelSoporte").HasMaxLength(10);
            entity.Property(x => x.Modalidad).HasColumnName("modalidad").HasMaxLength(50);
            entity.Property(x => x.DetalleModalidad).HasColumnName("detalleModalidad").HasMaxLength(250);
            entity.Property(x => x.HorasBaseMensual).HasColumnName("horasBaseMensual");
            entity.Property(x => x.Expertise).HasColumnName("expertise").HasMaxLength(20);
            entity.Property(x => x.Locacion).HasColumnName("locacion").HasMaxLength(20);
            entity.Property(x => x.TarifaHora).HasColumnName("tarifaHora").HasColumnType("decimal(18,2)");
            entity.Property(x => x.TarifaMensual).HasColumnName("tarifaMensual").HasColumnType("decimal(18,2)");
            entity.Property(x => x.CantidadMeses).HasColumnName("cantidadMeses");
            entity.Property(x => x.HorasEstimadas).HasColumnName("horasEstimadas").HasColumnType("decimal(10,2)");
            entity.Property(x => x.Subtotal).HasColumnName("subtotal").HasColumnType("decimal(18,2)");
            entity.Property(x => x.Moneda).HasColumnName("moneda").HasMaxLength(10);
        });
    }

    private delegate void ColumnMap<TEntity>(EntityTypeBuilder<TEntity> entity, Expression<Func<TEntity, object?>> property, string name, string? type = null) where TEntity : class;

    private static void Map<TEntity>(ModelBuilder b, string table, string key, Action<EntityTypeBuilder<TEntity>, ColumnMap<TEntity>> columns) where TEntity : MasterCatalogEntity
    {
        var e = b.Entity<TEntity>(); e.ToTable(table, "dbo", t => t.ExcludeFromMigrations()); e.HasKey(x => x.Id); e.Property(x => x.Id).HasColumnName(key).ValueGeneratedOnAdd();
        columns(e, static (entity, property, name, type) => { var p = entity.Property(property).HasColumnName(name); if (type is not null) p.HasColumnType(type); else if (property.Body is MemberExpression) p.HasColumnType("nvarchar(max)"); });
    }
}