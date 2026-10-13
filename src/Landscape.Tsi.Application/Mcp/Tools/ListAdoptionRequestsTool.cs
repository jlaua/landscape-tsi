using System.Text.Json;
using Landscape.Tsi.Application.Adoption;

namespace Landscape.Tsi.Application.Mcp.Tools;

public sealed class ListAdoptionRequestsTool : IMcpTool
{
    private readonly IAdoptionProcessService _adoptionService;

    public ListAdoptionRequestsTool(IAdoptionProcessService adoptionService)
    {
        _adoptionService = adoptionService;
    }

    public string Name => "list_adoption_requests";

    public string Description =>
        "Consulta en modo lectura las solicitudes y procesos de adopción tecnológica TSI en curso o finalizados, junto con su estado, building blocks asociados y subsidiarias involucradas.";

    public object InputSchema => new
    {
        type = "object",
        properties = new
        {
            processId = new
            {
                type = "integer",
                description = "ID del proceso específico a detallar. Si no se indica, devuelve el listado general de procesos."
            },
            statusFilter = new
            {
                type = "string",
                description = "Filtro opcional por texto del estado (ej: 'En Evaluacion', 'Estandarizado', 'Cerrado')."
            }
        }
    };

    public async Task<McpToolResult> ExecuteAsync(JsonElement arguments, McpContext context, CancellationToken cancellationToken = default)
    {
        if (arguments.TryGetProperty("processId", out var idProp) && idProp.TryGetInt32(out var processId) && processId > 0)
        {
            var detail = await _adoptionService.GetProcessDetailAsync(processId, cancellationToken);
            if (detail is null)
            {
                return McpToolResult.FromText($"No se encontró la solicitud o proceso de adopción con ID {processId}.", isError: true);
            }

            var cleanDetail = new
            {
                id = detail.Id,
                codigo = detail.Codigo,
                nombre = detail.Nombre,
                buildingBlock = detail.BuildingBlockNombre,
                estado = detail.EstadoAdopcionNombre,
                liderCorporativo = detail.LiderCorporativo,
                fechaInicio = detail.FechaInicio.ToString("yyyy-MM-dd"),
                fechaEstimadaCierre = detail.FechaEstimadaCierre?.ToString("yyyy-MM-dd"),
                tecnologiaEstandar = detail.EstandarPrincipalVigente?.TecnologiaNombre,
                empresasConvocadas = detail.EmpresasParticipantes.Select(e => new
                {
                    empresaId = e.EmpresaId,
                    empresa = e.EmpresaNombre,
                    aplica = e.Aplica,
                    justificacionNoAplica = e.JustificacionNoAplica
                }),
                tecnologiasAlternativas = detail.EstandaresAlternativos.Select(t => new
                {
                    tecnologiaId = t.TecnologiaId,
                    tecnologia = t.TecnologiaNombre,
                    rolEstandar = t.RolEstandar,
                    vigencia = t.EstadoVigencia
                })
            };

            return McpToolResult.FromJson(cleanDetail);
        }

        var processes = await _adoptionService.ListProcessesAsync(cancellationToken);
        var statusFilter = arguments.TryGetProperty("statusFilter", out var sf) ? sf.GetString() : null;

        if (!string.IsNullOrWhiteSpace(statusFilter))
        {
            processes = processes.Where(p =>
                p.EstadoAdopcionNombre.Contains(statusFilter, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        var listResult = processes.Select(p => new
        {
            id = p.Id,
            codigo = p.Codigo,
            nombre = p.Nombre,
            buildingBlock = p.BuildingBlockNombre,
            estado = p.EstadoAdopcionNombre,
            liderCorporativo = p.LiderCorporativo,
            empresasConvocadas = p.TotalEmpresasConvocadas,
            empresasImplementadas = p.TotalEmpresasImplementadas
        }).ToList();

        return McpToolResult.FromJson(new
        {
            total = listResult.Count,
            processes = listResult
        });
    }
}
