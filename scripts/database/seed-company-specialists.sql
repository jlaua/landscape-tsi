/* =========================================================================
   Script: seed-company-specialists.sql
   Propósito: Registrar y actualizar especialistas técnicos de empresas
              subsidiarias en [dbo].[TContactoEmpresaSubsidiaria],
              conforme a la asignación corporativa de empresas.
   Idempotencia: Emplea comprobaciones IF NOT EXISTS y actualiza registros
                 preexistentes sin duplicar.
   Seguridad: Solo ejecutable en bases de datos autorizadas bajo transacción.
   ========================================================================= */

IF DB_NAME() NOT IN (N'db-landscape-tsi-dev-v2', N'db-landscape-tsi')
    THROW 50001, 'Operación bloqueada: nombre de base de datos no autorizado.', 1;

SET XACT_ABORT ON;
BEGIN TRANSACTION;

-- 1. Actualizar registros preexistentes identificados en base de datos
-- Contacto 1002: Banco de Crédito de Miami (idEmpresaSubsidiaria = 3)
IF EXISTS (SELECT 1 FROM [dbo].[TContactoEmpresaSubsidiaria] WHERE [idContactoEmpresaSubsidiaria] = 1002)
BEGIN
    UPDATE [dbo].[TContactoEmpresaSubsidiaria]
    SET [nombreContactoEmpresaSubsidiaria] = N'Li Chuica',
        [email] = N'lchuica@bcpmiami.com',
        [rol] = ISNULL([rol], N'Especialista')
    WHERE [idContactoEmpresaSubsidiaria] = 1002;
    PRINT N'Contacto 1002 actualizado para Banco de Crédito de Miami.';
END;

-- Contacto 1003: Mi Banco Perú (idEmpresaSubsidiaria = 13)
IF EXISTS (SELECT 1 FROM [dbo].[TContactoEmpresaSubsidiaria] WHERE [idContactoEmpresaSubsidiaria] = 1003)
BEGIN
    UPDATE [dbo].[TContactoEmpresaSubsidiaria]
    SET [nombreContactoEmpresaSubsidiaria] = N'Angelo Avalos',
        [email] = N'angelo.avalos.pa@mibanco.com.pe',
        [rol] = ISNULL([rol], N'ingeniero waf')
    WHERE [idContactoEmpresaSubsidiaria] = 1003;
    PRINT N'Contacto 1003 actualizado para Mi Banco Perú.';
END;

-- 2. Registrar especialistas por empresa subsidiaria

-- =========================================================================
-- Empresa: Pacifico Prestaciones / Laboratorios (idEmpresaSubsidiaria = 23)
-- =========================================================================
IF NOT EXISTS (
    SELECT 1 FROM [dbo].[TContactoEmpresaSubsidiaria] 
    WHERE [idEmpresaSubsidiaria] = 23 AND [email] = N'StefanoArrietaE@pacifico.com.pe'
)
BEGIN
    INSERT INTO [dbo].[TContactoEmpresaSubsidiaria] ([idEmpresaSubsidiaria], [nombreContactoEmpresaSubsidiaria], [rol], [email])
    VALUES (23, N'Stefano Ivan Arrieta Escalaya', N'Especialista', N'StefanoArrietaE@pacifico.com.pe');
    PRINT N'Registrado Stefano Ivan Arrieta Escalaya en Pacifico Prestaciones / Laboratorios.';
END;

IF NOT EXISTS (
    SELECT 1 FROM [dbo].[TContactoEmpresaSubsidiaria] 
    WHERE [idEmpresaSubsidiaria] = 23 AND [email] = N'miguel.valeriano@aliada.com.pe'
)
BEGIN
    INSERT INTO [dbo].[TContactoEmpresaSubsidiaria] ([idEmpresaSubsidiaria], [nombreContactoEmpresaSubsidiaria], [rol], [email])
    VALUES (23, N'Miguel Valeriano', N'Especialista', N'miguel.valeriano@aliada.com.pe');
    PRINT N'Registrado Miguel Valeriano en Pacifico Prestaciones / Laboratorios.';
END;

IF NOT EXISTS (
    SELECT 1 FROM [dbo].[TContactoEmpresaSubsidiaria] 
    WHERE [idEmpresaSubsidiaria] = 23 AND [email] = N'abautista@clinicasanfelipe.com'
)
BEGIN
    INSERT INTO [dbo].[TContactoEmpresaSubsidiaria] ([idEmpresaSubsidiaria], [nombreContactoEmpresaSubsidiaria], [rol], [email])
    VALUES (23, N'Alexis Bautista', N'Especialista', N'abautista@clinicasanfelipe.com');
    PRINT N'Registrado Alexis Bautista en Pacifico Prestaciones / Laboratorios.';
END;

-- =========================================================================
-- Empresa: PACIFICO SALUD (idEmpresaSubsidiaria = 21)
-- =========================================================================
IF NOT EXISTS (
    SELECT 1 FROM [dbo].[TContactoEmpresaSubsidiaria] 
    WHERE [idEmpresaSubsidiaria] = 21 AND [email] = N'StefanoArrietaE@pacifico.com.pe'
)
BEGIN
    INSERT INTO [dbo].[TContactoEmpresaSubsidiaria] ([idEmpresaSubsidiaria], [nombreContactoEmpresaSubsidiaria], [rol], [email])
    VALUES (21, N'Stefano Ivan Arrieta Escalaya', N'Especialista', N'StefanoArrietaE@pacifico.com.pe');
    PRINT N'Registrado Stefano Ivan Arrieta Escalaya en PACIFICO SALUD.';
END;

IF NOT EXISTS (
    SELECT 1 FROM [dbo].[TContactoEmpresaSubsidiaria] 
    WHERE [idEmpresaSubsidiaria] = 21 AND [email] = N'WilianFabianE@pacifico.com.pe'
)
BEGIN
    INSERT INTO [dbo].[TContactoEmpresaSubsidiaria] ([idEmpresaSubsidiaria], [nombreContactoEmpresaSubsidiaria], [rol], [email])
    VALUES (21, N'Wilian Fabian', N'Especialista', N'WilianFabianE@pacifico.com.pe');
    PRINT N'Registrado Wilian Fabian en PACIFICO SALUD.';
END;

IF NOT EXISTS (
    SELECT 1 FROM [dbo].[TContactoEmpresaSubsidiaria] 
    WHERE [idEmpresaSubsidiaria] = 21 AND [email] = N'miguel.valeriano@aliada.com.pe'
)
BEGIN
    INSERT INTO [dbo].[TContactoEmpresaSubsidiaria] ([idEmpresaSubsidiaria], [nombreContactoEmpresaSubsidiaria], [rol], [email])
    VALUES (21, N'Miguel Valeriano', N'Especialista', N'miguel.valeriano@aliada.com.pe');
    PRINT N'Registrado Miguel Valeriano en PACIFICO SALUD.';
END;

-- =========================================================================
-- Empresa: iO (idEmpresaSubsidiaria = 16)
-- =========================================================================
IF NOT EXISTS (
    SELECT 1 FROM [dbo].[TContactoEmpresaSubsidiaria] 
    WHERE [idEmpresaSubsidiaria] = 16 AND [email] = N'fredymendez@bcp.com.pe'
)
BEGIN
    INSERT INTO [dbo].[TContactoEmpresaSubsidiaria] ([idEmpresaSubsidiaria], [nombreContactoEmpresaSubsidiaria], [rol], [email])
    VALUES (16, N'Fredy Gerardo Mendez Gonzales', N'Especialista', N'fredymendez@bcp.com.pe');
    PRINT N'Registrado Fredy Gerardo Mendez Gonzales en iO.';
END;

IF NOT EXISTS (
    SELECT 1 FROM [dbo].[TContactoEmpresaSubsidiaria] 
    WHERE [idEmpresaSubsidiaria] = 16 AND [email] = N'rodrigodiaz@bcp.com.pe'
)
BEGIN
    INSERT INTO [dbo].[TContactoEmpresaSubsidiaria] ([idEmpresaSubsidiaria], [nombreContactoEmpresaSubsidiaria], [rol], [email])
    VALUES (16, N'Rodrigo Diaz Romero', N'Especialista', N'rodrigodiaz@bcp.com.pe');
    PRINT N'Registrado Rodrigo Diaz Romero en iO.';
END;

-- =========================================================================
-- Empresa: Culqi (idEmpresaSubsidiaria = 11)
-- =========================================================================
IF NOT EXISTS (
    SELECT 1 FROM [dbo].[TContactoEmpresaSubsidiaria] 
    WHERE [idEmpresaSubsidiaria] = 11 AND [email] = N'fredymeres@bcp.com.pe'
)
BEGIN
    INSERT INTO [dbo].[TContactoEmpresaSubsidiaria] ([idEmpresaSubsidiaria], [nombreContactoEmpresaSubsidiaria], [rol], [email])
    VALUES (11, N'Freddy Javier Meres Vargas', N'Especialista', N'fredymeres@bcp.com.pe');
    PRINT N'Registrado Freddy Javier Meres Vargas en Culqi.';
END;

-- =========================================================================
-- Empresa: Monokera (idEmpresaSubsidiaria = 19)
-- =========================================================================
IF NOT EXISTS (
    SELECT 1 FROM [dbo].[TContactoEmpresaSubsidiaria] 
    WHERE [idEmpresaSubsidiaria] = 19 AND [email] = N'edison.mayo@monokera.com'
)
BEGIN
    INSERT INTO [dbo].[TContactoEmpresaSubsidiaria] ([idEmpresaSubsidiaria], [nombreContactoEmpresaSubsidiaria], [rol], [email])
    VALUES (19, N'Edison Mayo', N'Especialista', N'edison.mayo@monokera.com');
    PRINT N'Registrado Edison Mayo en Monokera.';
END;

-- =========================================================================
-- Empresa: Tyba (idEmpresaSubsidiaria = 20)
-- =========================================================================
IF NOT EXISTS (
    SELECT 1 FROM [dbo].[TContactoEmpresaSubsidiaria] 
    WHERE [idEmpresaSubsidiaria] = 20 AND [email] = N'victoriaperez@tyba.com.co'
)
BEGIN
    INSERT INTO [dbo].[TContactoEmpresaSubsidiaria] ([idEmpresaSubsidiaria], [nombreContactoEmpresaSubsidiaria], [rol], [email])
    VALUES (20, N'Victoria Alexandra Perez Cerveleon', N'Especialista', N'victoriaperez@tyba.com.co');
    PRINT N'Registrada Victoria Alexandra Perez Cerveleon en Tyba.';
END;

IF NOT EXISTS (
    SELECT 1 FROM [dbo].[TContactoEmpresaSubsidiaria] 
    WHERE [idEmpresaSubsidiaria] = 20 AND [email] = N'stiben@tyba.com.co'
)
BEGIN
    INSERT INTO [dbo].[TContactoEmpresaSubsidiaria] ([idEmpresaSubsidiaria], [nombreContactoEmpresaSubsidiaria], [rol], [email])
    VALUES (20, N'Stiben Pérez', N'Especialista', N'stiben@tyba.com.co');
    PRINT N'Registrado Stiben Pérez en Tyba.';
END;

-- =========================================================================
-- Empresa: Mi Banco Perú (idEmpresaSubsidiaria = 13)
-- =========================================================================
IF NOT EXISTS (
    SELECT 1 FROM [dbo].[TContactoEmpresaSubsidiaria] 
    WHERE [idEmpresaSubsidiaria] = 13 AND [email] = N'giancarlo.palacios.l@mibanco.com.pe'
)
BEGIN
    INSERT INTO [dbo].[TContactoEmpresaSubsidiaria] ([idEmpresaSubsidiaria], [nombreContactoEmpresaSubsidiaria], [rol], [email])
    VALUES (13, N'Giancarlo Andre Palacios Loayza', N'Especialista', N'giancarlo.palacios.l@mibanco.com.pe');
    PRINT N'Registrado Giancarlo Andre Palacios Loayza en Mi Banco Perú.';
END;

-- =========================================================================
-- Empresa: Tenpo (idEmpresaSubsidiaria = 18)
-- =========================================================================
IF NOT EXISTS (
    SELECT 1 FROM [dbo].[TContactoEmpresaSubsidiaria] 
    WHERE [idEmpresaSubsidiaria] = 18 AND [email] = N'francisco.carreno@tenpo.cl'
)
BEGIN
    INSERT INTO [dbo].[TContactoEmpresaSubsidiaria] ([idEmpresaSubsidiaria], [nombreContactoEmpresaSubsidiaria], [rol], [email])
    VALUES (18, N'Francisco Carreño', N'Especialista', N'francisco.carreno@tenpo.cl');
    PRINT N'Registrado Francisco Carreño en Tenpo.';
END;

IF NOT EXISTS (
    SELECT 1 FROM [dbo].[TContactoEmpresaSubsidiaria] 
    WHERE [idEmpresaSubsidiaria] = 18 AND [email] = N'art.gonzales@tenpo.cl'
)
BEGIN
    INSERT INTO [dbo].[TContactoEmpresaSubsidiaria] ([idEmpresaSubsidiaria], [nombreContactoEmpresaSubsidiaria], [rol], [email])
    VALUES (18, N'Art Gonzales', N'Especialista', N'art.gonzales@tenpo.cl');
    PRINT N'Registrado Art Gonzales en Tenpo.';
END;

-- =========================================================================
-- Empresa: CREDICORP CAPITAL (idEmpresaSubsidiaria = 25)
-- =========================================================================
IF NOT EXISTS (
    SELECT 1 FROM [dbo].[TContactoEmpresaSubsidiaria] 
    WHERE [idEmpresaSubsidiaria] = 25 AND [email] = N'sacosta@asbnet.com.pa'
)
BEGIN
    INSERT INTO [dbo].[TContactoEmpresaSubsidiaria] ([idEmpresaSubsidiaria], [nombreContactoEmpresaSubsidiaria], [rol], [email])
    VALUES (25, N'Sherly Acosta', N'Especialista', N'sacosta@asbnet.com.pa');
    PRINT N'Registrada Sherly Acosta en CREDICORP CAPITAL.';
END;

IF NOT EXISTS (
    SELECT 1 FROM [dbo].[TContactoEmpresaSubsidiaria] 
    WHERE [idEmpresaSubsidiaria] = 25 AND [email] = N'rbarcenas@asbnet.com.pa'
)
BEGIN
    INSERT INTO [dbo].[TContactoEmpresaSubsidiaria] ([idEmpresaSubsidiaria], [nombreContactoEmpresaSubsidiaria], [rol], [email])
    VALUES (25, N'Roberto Barcenas', N'Especialista', N'rbarcenas@asbnet.com.pa');
    PRINT N'Registrado Roberto Barcenas en CREDICORP CAPITAL.';
END;

IF NOT EXISTS (
    SELECT 1 FROM [dbo].[TContactoEmpresaSubsidiaria] 
    WHERE [idEmpresaSubsidiaria] = 25 AND [email] = N'vvargas@credicorpcapital.com'
)
BEGIN
    INSERT INTO [dbo].[TContactoEmpresaSubsidiaria] ([idEmpresaSubsidiaria], [nombreContactoEmpresaSubsidiaria], [rol], [especialistaEnQuePlataformas], [email], [notas])
    VALUES (25, N'Victor Hugo Vargas (frontdoor)', N'Especialista', N'frontdoor', N'vvargas@credicorpcapital.com', N'frontdoor');
    PRINT N'Registrado Victor Hugo Vargas en CREDICORP CAPITAL.';
END;

IF NOT EXISTS (
    SELECT 1 FROM [dbo].[TContactoEmpresaSubsidiaria] 
    WHERE [idEmpresaSubsidiaria] = 25 AND [email] = N'eespitia@credicorpcapital.com'
)
BEGIN
    INSERT INTO [dbo].[TContactoEmpresaSubsidiaria] ([idEmpresaSubsidiaria], [nombreContactoEmpresaSubsidiaria], [rol], [email])
    VALUES (25, N'Erwin Espitia', N'Especialista', N'eespitia@credicorpcapital.com');
    PRINT N'Registrado Erwin Espitia en CREDICORP CAPITAL.';
END;

IF NOT EXISTS (
    SELECT 1 FROM [dbo].[TContactoEmpresaSubsidiaria] 
    WHERE [idEmpresaSubsidiaria] = 25 AND [email] = N'guillermolluen@credicorpcapital.com'
)
BEGIN
    INSERT INTO [dbo].[TContactoEmpresaSubsidiaria] ([idEmpresaSubsidiaria], [nombreContactoEmpresaSubsidiaria], [rol], [especialistaEnQuePlataformas], [email], [notas])
    VALUES (25, N'Guillermo Steven Lluen (cloudflare)', N'Especialista', N'cloudflare', N'guillermolluen@credicorpcapital.com', N'cloudflare');
    PRINT N'Registrado Guillermo Steven Lluen en CREDICORP CAPITAL.';
END;

-- =========================================================================
-- Empresa: Pacifico Seguros y Pensiones (idEmpresaSubsidiaria = 9)
-- =========================================================================
IF NOT EXISTS (
    SELECT 1 FROM [dbo].[TContactoEmpresaSubsidiaria] 
    WHERE [idEmpresaSubsidiaria] = 9 AND [email] = N'luissoto@prima.com.pe'
)
BEGIN
    INSERT INTO [dbo].[TContactoEmpresaSubsidiaria] ([idEmpresaSubsidiaria], [nombreContactoEmpresaSubsidiaria], [rol], [email])
    VALUES (9, N'Felipe Soto', N'Especialista', N'luissoto@prima.com.pe');
    PRINT N'Registrado Felipe Soto en Pacifico Seguros y Pensiones.';
END;

IF NOT EXISTS (
    SELECT 1 FROM [dbo].[TContactoEmpresaSubsidiaria] 
    WHERE [idEmpresaSubsidiaria] = 9 AND [email] = N'miguelvillanuevaa@pacifico.com.pe'
)
BEGIN
    INSERT INTO [dbo].[TContactoEmpresaSubsidiaria] ([idEmpresaSubsidiaria], [nombreContactoEmpresaSubsidiaria], [rol], [email])
    VALUES (9, N'Miguel Villanueva', N'Especialista', N'miguelvillanuevaa@pacifico.com.pe');
    PRINT N'Registrado Miguel Villanueva en Pacifico Seguros y Pensiones.';
END;

-- =========================================================================
-- Empresa: Prima AFP (idEmpresaSubsidiaria = 10)
-- =========================================================================
IF NOT EXISTS (
    SELECT 1 FROM [dbo].[TContactoEmpresaSubsidiaria] 
    WHERE [idEmpresaSubsidiaria] = 10 AND [email] = N'luissoto@prima.com.pe'
)
BEGIN
    INSERT INTO [dbo].[TContactoEmpresaSubsidiaria] ([idEmpresaSubsidiaria], [nombreContactoEmpresaSubsidiaria], [rol], [email])
    VALUES (10, N'Felipe Soto', N'Especialista', N'luissoto@prima.com.pe');
    PRINT N'Registrado Felipe Soto en Prima AFP.';
END;

IF NOT EXISTS (
    SELECT 1 FROM [dbo].[TContactoEmpresaSubsidiaria] 
    WHERE [idEmpresaSubsidiaria] = 10 AND [email] = N'miguelvillanuevaa@pacifico.com.pe'
)
BEGIN
    INSERT INTO [dbo].[TContactoEmpresaSubsidiaria] ([idEmpresaSubsidiaria], [nombreContactoEmpresaSubsidiaria], [rol], [email])
    VALUES (10, N'Miguel Villanueva', N'Especialista', N'miguelvillanuevaa@pacifico.com.pe');
    PRINT N'Registrado Miguel Villanueva en Prima AFP.';
END;

-- =========================================================================
-- Empresa: Mi Banco Colombia (idEmpresaSubsidiaria = 12)
-- =========================================================================
IF NOT EXISTS (
    SELECT 1 FROM [dbo].[TContactoEmpresaSubsidiaria] 
    WHERE [idEmpresaSubsidiaria] = 12 AND [email] = N'angelo.avalos.pa@mibanco.com.pe'
)
BEGIN
    INSERT INTO [dbo].[TContactoEmpresaSubsidiaria] ([idEmpresaSubsidiaria], [nombreContactoEmpresaSubsidiaria], [rol], [email])
    VALUES (12, N'Angelo Avalos', N'Especialista', N'angelo.avalos.pa@mibanco.com.pe');
    PRINT N'Registrado Angelo Avalos en Mi Banco Colombia.';
END;

-- =========================================================================
-- Empresa: Yape Perú (idEmpresaSubsidiaria = 14)
-- =========================================================================
IF NOT EXISTS (
    SELECT 1 FROM [dbo].[TContactoEmpresaSubsidiaria] 
    WHERE [idEmpresaSubsidiaria] = 14 AND [email] = N'fredymendez@bcp.com.pe'
)
BEGIN
    INSERT INTO [dbo].[TContactoEmpresaSubsidiaria] ([idEmpresaSubsidiaria], [nombreContactoEmpresaSubsidiaria], [rol], [email])
    VALUES (14, N'Fredy Gerardo Mendez Gonzales', N'Especialista', N'fredymendez@bcp.com.pe');
    PRINT N'Registrado Fredy Gerardo Mendez Gonzales en Yape Perú.';
END;

-- =========================================================================
-- Empresa: Yape Bolivia (idEmpresaSubsidiaria = 15)
-- =========================================================================
IF NOT EXISTS (
    SELECT 1 FROM [dbo].[TContactoEmpresaSubsidiaria] 
    WHERE [idEmpresaSubsidiaria] = 15 AND [email] = N'cirollanol@bcp.com.bo'
)
BEGIN
    INSERT INTO [dbo].[TContactoEmpresaSubsidiaria] ([idEmpresaSubsidiaria], [nombreContactoEmpresaSubsidiaria], [rol], [email])
    VALUES (15, N'Cesar Rollano', N'Especialista', N'cirollanol@bcp.com.bo');
    PRINT N'Registrado Cesar Rollano en Yape Bolivia.';
END;

IF NOT EXISTS (
    SELECT 1 FROM [dbo].[TContactoEmpresaSubsidiaria] 
    WHERE [idEmpresaSubsidiaria] = 15 AND [email] = N'fredymendez@bcp.com.pe'
)
BEGIN
    INSERT INTO [dbo].[TContactoEmpresaSubsidiaria] ([idEmpresaSubsidiaria], [nombreContactoEmpresaSubsidiaria], [rol], [email])
    VALUES (15, N'Fredy Gerardo Mendez Gonzales', N'Especialista', N'fredymendez@bcp.com.pe');
    PRINT N'Registrado Fredy Gerardo Mendez Gonzales en Yape Bolivia.';
END;

IF NOT EXISTS (
    SELECT 1 FROM [dbo].[TContactoEmpresaSubsidiaria] 
    WHERE [idEmpresaSubsidiaria] = 15 AND [email] = N'rodrigodiaz@bcp.com.pe'
)
BEGIN
    INSERT INTO [dbo].[TContactoEmpresaSubsidiaria] ([idEmpresaSubsidiaria], [nombreContactoEmpresaSubsidiaria], [rol], [email])
    VALUES (15, N'Rodrigo Diaz Romero', N'Especialista', N'rodrigodiaz@bcp.com.pe');
    PRINT N'Registrado Rodrigo Diaz Romero en Yape Bolivia.';
END;

-- =========================================================================
-- Empresa: Banco de Crédito de Bolivia (idEmpresaSubsidiaria = 2)
-- =========================================================================
IF NOT EXISTS (
    SELECT 1 FROM [dbo].[TContactoEmpresaSubsidiaria] 
    WHERE [idEmpresaSubsidiaria] = 2 AND [email] = N'cirollanol@bcp.com.bo'
)
BEGIN
    INSERT INTO [dbo].[TContactoEmpresaSubsidiaria] ([idEmpresaSubsidiaria], [nombreContactoEmpresaSubsidiaria], [rol], [email])
    VALUES (2, N'Cesar Rollano', N'Especialista', N'cirollanol@bcp.com.bo');
    PRINT N'Registrado Cesar Rollano en Banco de Crédito de Bolivia.';
END;

COMMIT TRANSACTION;
PRINT N'Todos los especialistas de empresas fueron registrados y sincronizados exitosamente.';
