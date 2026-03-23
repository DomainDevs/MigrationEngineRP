/* 
=================================================================
-- MIGRACION SISTRAN
--Autor: Sistran
--Fecha Creación:       20260301
--Fecha Modificación:   20260303
--Funcionalidad:        Crear objeto [dbo].[Polizas]
=================================================================
*/
IF OBJECT_ID('[dbo].[Polizas]') IS NOT NULL
BEGIN
    DROP TABLE [dbo].[Polizas]
    IF OBJECT_ID('[dbo].[Polizas]') IS NOT NULL
        PRINT '<<< FAILED DROPPING TABLE [dbo].[Polizas] >>>'
    ELSE
        PRINT '<<< DROPPED TABLE [dbo].[Polizas] >>>'
END
GO
CREATE TABLE [dbo].[Polizas]
(
    [Id_pv] INT IDENTITY(1,1) NOT NULL,
    [NumeroPoliza] NVARCHAR(100) NOT NULL,
    [FechaEmision] DATETIME NOT NULL,
    [FechaInicio] DATETIME NOT NULL,
    [FechaFin] DATETIME NOT NULL,
    [AseguradoId] INT NOT NULL,
    [SumaAsegurada] DECIMAL(18,2) NULL,
    [PrimaTotal] DECIMAL(18,2) NULL,
    [Estado] NVARCHAR(100) NULL,
	[InfoAdicional] [nvarchar](200) NULL,
	[Observaciones] [nvarchar](500) NULL,
    CONSTRAINT [PK_Polizas] PRIMARY KEY ([Id_pv])
)
GO

IF EXISTS (
    SELECT 1
    FROM sysobjects
    WHERE id = OBJECT_ID('dbo.Polizas')
    AND type = 'U'
)
BEGIN
    PRINT '<<CREATE TABLE dbo.Polizas SUCCESSFUL!! >>'
END
GO