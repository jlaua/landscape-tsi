# DAG y orden de dependencias

El orden se construyó a partir de `sys.foreign_keys` y
`sys.foreign_key_columns` de RECONCILE/DEV. Las flechas indican
`hija -> padre`; para insertar datos se procesa de izquierda a derecha por
capas (padres antes que hijas).

## Capas funcionales para inserción

1. **Catálogos raíz:** `TMDominio`, `TMEstadoCapacidad`,
   `TMEstadoFuncionalidad`, `TEstadoFaseAdopcion`, `TMFamilia`,
   `TMEstadoAdopcionTSI`, `TMPosturaRoadmap`, `TModalidadLaboral`,
   `TTipoOperacion`, `TEmpresaSubsidiaria`, `TVendor`.
2. **Nodos dependientes:** `TBuildingBlock` (dominio, fase),
   `TTecnologiaTSI` (familia, adopción, postura),
   `TTecnologiaTSIimplementadaSubsidiaria` (tecnología, empresa).
3. **Dependencias de segundo nivel:** `TCapacidadDeSeguridad` (building
   block, estado), `TCasosDeUso` (tecnología), `TCISO` (empresa), contactos,
   `TDriver`, `TModeloDeOperacion`, `TRegulacionAplicable`.
4. **Dependencias finales:** `TFuncionalidad` (capacidad, estado) y la tabla
   puente `TBuildingBlockVsTTecnologiaTSI` (building block, tecnología).

No se detectaron ciclos en el DAG funcional. Las tablas IAM de DEV forman un
subgrafo separado y deben aplicarse después del esquema IAM generado por EF:
`IamPermiso`/`IamRol` -> `IamRolPermiso`; `IamUsuario`/`IamRol` ->
`IamUsuarioRol`; `IamUsuario` -> claims, tokens, login externo, organización,
acceso de emergencia y auditoría relacionada.

La tabla puente no tiene PK declarada ni unique constraint; su cardinalidad
real se valida por sus dos FKs y no debe alterarse durante la reconciliación.
